using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Muestra.Hubs;
using Oracle.ManagedDataAccess.Client;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Muestra.Controllers
{
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<WhatsappHub> _hubContext;

        public WhatsApiController(IConfiguration configuration, IHubContext<WhatsappHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // --- VARIABLES GLOBALES ---
        private static DateTime fechaEntrega = new DateTime(2025, 12, 10);
        private static IWebDriver? _driver;
        private static int ultimoAvisoEnviado = -999;
        private static readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);

        // ⚠️ CONEXIÓN DIRECTA CORREGIDA A 'XE' (Para el Bot)
        private const string CADENA_CONEXION = "User Id=SYSTEM;Password=Muestra.2025;Data Source=localhost:1521/XE;";

        // ============================================================
        // 1. INICIAR BOT (ABRIR CHROME)
        // ============================================================
        [HttpGet("iniciar")]
        public IActionResult IniciarBot()
        {
            if (_driver != null) return Ok("El sistema ya está corriendo.");
            try
            {
                var options = new ChromeOptions();
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "WhatsAppBot_Sesion_FINAL");
                
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                
                options.AddArgument($"user-data-dir={path}");
                options.AddArgument("--no-sandbox");
                
                _driver = new ChromeDriver(options);
                _driver.Manage().Window.Maximize();
                _driver.Navigate().GoToUrl("https://web.whatsapp.com");

                return Ok("Sistema Iniciado. Escanea el QR.");
            }
            catch (Exception ex) { return BadRequest("Error al abrir Chrome: " + ex.Message); }
        }

        // ============================================================
        // 2. ACTIVAR (GUARDAR NÚMERO EN ORACLE)
        // ============================================================
        [HttpGet("activar")]
        public IActionResult Activar([FromQuery] string telefono)
        {
            if (string.IsNullOrEmpty(telefono)) return BadRequest("Número vacío");

            try
            {
                using (OracleConnection con = new OracleConnection(CADENA_CONEXION))
                {
                    con.Open();
                    string query = "INSERT INTO NUMEROS (TELEFONO) VALUES (:t)";
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("t", telefono));
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok("Guardado correctamente.");
            }
            catch (Exception ex) 
            {
                return BadRequest("Error Oracle: " + ex.Message); 
            }
        }

        // ============================================================
        // 3. VER NÚMEROS (CONSULTA ORACLE)
        // ============================================================
        [HttpGet("ver-numeros")]
        public IActionResult VerNumeros()
        {
            var lista = new List<string>();
            try
            {
                using (OracleConnection con = new OracleConnection(CADENA_CONEXION))
                {
                    con.Open();
                    string query = "SELECT TELEFONO FROM NUMEROS";
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    using (OracleDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var t = r["TELEFONO"]?.ToString();
                            if (!string.IsNullOrEmpty(t)) lista.Add(t);
                        }
                    }
                }
                return Ok(new { total = lista.Count, lista = lista });
            }
            catch (Exception ex) 
            { 
                return BadRequest("Error Oracle: " + ex.Message); 
            }
        }

        // ============================================================
        // 4. VERIFICAR FECHAS (ENVÍO MASIVO)
        // ============================================================
        [HttpGet("verificar-fechas")]
        public async Task<IActionResult> VerificarFechas()
        {
            if (_driver == null) return BadRequest(new { estado = "Bot apagado." });

            DateTime hoy = DateTime.Today;
            int diasRestantes = (int)(fechaEntrega - hoy).TotalDays;

            if (diasRestantes == ultimoAvisoEnviado)
            {
                await _hubContext.Clients.All.SendAsync("RecibirLog", "⚠️ SPAM DETECTADO: Ya enviaste mensajes hoy.");
                return Ok(new { estado = "SPAM DETECTADO" });
            }

            var actionResult = VerNumeros() as OkObjectResult;
            if (actionResult?.Value == null) return BadRequest(new { estado = "Error al leer BD (Nulo)" });

            dynamic data = actionResult.Value;
            List<string> numeros = data.lista;

            if (numeros.Count == 0) return Ok(new { estado = "Sin números registrados." });

            await _hubContext.Clients.All.SendAsync("RecibirLog", $"🚀 Iniciando envío a {numeros.Count} usuarios...");

            int enviados = 0;
            string mensaje = $"🔔 Recordatorio: Faltan {diasRestantes} días para la entrega.";

            foreach (var num in numeros)
            {
                bool exito = EnviarMensajeSelenium(num, mensaje);
                string estado = exito ? "Enviado ✅" : "Falló ❌ (Inválido o Error)";
                await _hubContext.Clients.All.SendAsync("RecibirProgreso", num, estado);
                if (exito) enviados++;
            }

            if (enviados > 0) ultimoAvisoEnviado = diasRestantes;
            
            await _hubContext.Clients.All.SendAsync("RecibirLog", "🏁 Proceso finalizado.");
            return Ok(new { total = numeros.Count, enviados = enviados });
        }

        // ============================================================
        // 5. TEST ENVIO UNITARIO
        // ============================================================
        [HttpGet("test-envio")]
        public IActionResult TestEnvio(string telefono)
        {
            if (_driver == null) return BadRequest("Bot apagado.");
            bool result = EnviarMensajeSelenium(telefono, "🤖 Prueba de conexión del sistema.");
            return Ok(result ? "Enviado." : "Falló.");
        }

        // ============================================================
        // 🛡️ LÓGICA DE ENVÍO BLINDADA (SIN ERRORES NO SUCH ELEMENT)
        // ============================================================
        private bool EnviarMensajeSelenium(string tel, string msj)
        {
            _browserLock.Wait(); 
            try
            {
                string url = $"https://web.whatsapp.com/send?phone={tel}&text={Uri.EscapeDataString(msj)}";
                _driver!.Navigate().GoToUrl(url);

                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
                try 
                {
                    var cajaTexto = wait.Until(d => d.FindElement(By.CssSelector("div[contenteditable='true']")));
                    Thread.Sleep(1000); 
                    cajaTexto.SendKeys(Keys.Enter);
                    Thread.Sleep(2000); 
                    return true;
                }
                catch (Exception)
                {
                    return false; 
                }
            }
            catch { return false; }
            finally { _browserLock.Release(); }
        }
    }
}