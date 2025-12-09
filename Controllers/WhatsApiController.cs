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

        // Variables Estáticas
        private static DateTime fechaEntrega = new DateTime(2025, 12, 10);
        private static IWebDriver? _driver;
        private static int ultimoAvisoEnviado = -999;
        private static readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);

        // --- INICIAR BOT ---
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

        // --- ACTIVAR (GUARDAR NUMERO) ---
        [HttpGet("activar")]
        public IActionResult Activar([FromQuery] string telefono)
        {
            if (string.IsNullOrEmpty(telefono)) return BadRequest("Número vacío");

            try
            {
                // El ?? "" arregla la advertencia amarilla
                string connectionString = _configuration.GetConnectionString("MyDbConnection") ?? "";
                
                using (OracleConnection con = new OracleConnection(connectionString))
                {
                    con.Open();
                    string query = "INSERT INTO NUMEROS (TELEFONO) VALUES (:t)";
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("t", telefono));
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok("Guardado");
            }
            catch (Exception ex)
            {
                // Muestra el error real para saber qué pasa
                return BadRequest("Error Oracle: " + ex.Message);
            }
        }

        // --- VER NUMEROS ---
        [HttpGet("ver-numeros")]
        public IActionResult VerNumeros()
        {
            var lista = new List<string>();
            try
            {
                string connectionString = _configuration.GetConnectionString("MyDbConnection") ?? "";

                using (OracleConnection con = new OracleConnection(connectionString))
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

        // --- VERIFICAR FECHAS (ENVIO MASIVO) ---
        [HttpGet("verificar-fechas")]
        public async Task<IActionResult> VerificarFechas()
        {
            if (_driver == null) return BadRequest(new { estado = "Bot apagado." });

            DateTime hoy = DateTime.Today;
            int diasRestantes = (int)(fechaEntrega - hoy).TotalDays;

            // Anti-Spam (Esto arregla la advertencia de variable no usada)
            if (diasRestantes == ultimoAvisoEnviado)
            {
                await _hubContext.Clients.All.SendAsync("RecibirLog", "⚠️ Ya se enviaron los mensajes de hoy.");
                return Ok(new { estado = "SPAM DETECTADO" });
            }

            // Obtener números (Llamada interna segura)
            var actionResult = VerNumeros();
            if (actionResult is BadRequestObjectResult errorResult) 
            {
                return BadRequest(new { estado = "Error BD: " + errorResult.Value });
            }

            // Extracción segura de datos para evitar advertencia 'dynamic'
            var okResult = actionResult as OkObjectResult;
            dynamic data = okResult!.Value!; // El ! le dice a C# "confía en mí, no es nulo"
            List<string> numeros = data.lista;

            if (numeros.Count == 0) return Ok(new { estado = "Sin números registrados." });

            await _hubContext.Clients.All.SendAsync("RecibirLog", $"🚀 Iniciando envío a {numeros.Count} usuarios...");

            int enviados = 0;
            string mensaje = $"🔔 Recordatorio: Faltan {diasRestantes} días para la entrega.";

            foreach (var num in numeros)
            {
                bool exito = EnviarMensajeSelenium(num, mensaje);
                await _hubContext.Clients.All.SendAsync("RecibirProgreso", num, exito ? "Enviado ✅" : "Falló ❌");
                if (exito) enviados++;
            }

            if (enviados > 0) ultimoAvisoEnviado = diasRestantes;
            
            await _hubContext.Clients.All.SendAsync("RecibirLog", "🏁 Proceso finalizado.");
            return Ok(new { total = numeros.Count, enviados = enviados });
        }

        // --- TEST ENVIO ---
        [HttpGet("test-envio")]
        public IActionResult TestEnvio(string telefono)
        {
            if (_driver == null) return BadRequest("El bot está apagado.");
            bool result = EnviarMensajeSelenium(telefono, "🤖 Prueba de conexión.");
            return Ok(result ? "Enviado con éxito." : "Fallo al enviar.");
        }

        // --- HELPERS ---
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
                    var btnEnviar = wait.Until(d => d.FindElement(By.CssSelector("span[data-icon='send']")));
                    Thread.Sleep(500);
                    btnEnviar.Click();
                    Thread.Sleep(2000); 
                    return true;
                }
                catch 
                {
                    try {
                        _driver.SwitchTo().ActiveElement().SendKeys(Keys.Enter);
                        Thread.Sleep(1000);
                        return true;
                    } catch { return false; }
                }
            }
            catch { return false; }
            finally { _browserLock.Release(); }
        }
    }
}