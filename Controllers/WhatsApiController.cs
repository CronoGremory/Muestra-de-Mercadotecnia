using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI; // Necesario para WebDriverWait
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration; // Necesario para leer appsettings

namespace Muestra.Controllers
{
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsApiController : ControllerBase
    {
        // Inyectamos la configuración para leer la conexión correcta (Docker o Local)
        private readonly IConfiguration _configuration;

        public WhatsApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Variables Estáticas (Viven en la memoria del servidor)
        private static DateTime fechaEntrega = new DateTime(2025, 12, 10);
        private static IWebDriver? _driver;
        
        // 🛡️ Variable Anti-Spam: Recuerda cuándo fue la última vez que mandamos mensajes
        private static int ultimoAvisoEnviado = -999; 
        
        // Semáforo para controlar la concurrencia (1 mensaje a la vez)
        private static readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);

        // ============================================================
        // 1. INICIAR EL BOT (ABRIR CHROME)
        // ============================================================
        [HttpGet("iniciar")]
        public IActionResult IniciarBot()
        {
            if (_driver != null) return Ok("El sistema ya está corriendo.");
            try
            {
                var options = new ChromeOptions();
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "WhatsAppBot_Sesion_FINAL_V3");
                
                // Crear directorio si no existe para evitar errores
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                options.AddArgument($"user-data-dir={path}");
                options.AddArgument("--no-sandbox"); // Vital para Docker
                options.AddArgument("--disable-dev-shm-usage"); // Vital para Docker

                _driver = new ChromeDriver(options);
                _driver.Manage().Window.Maximize();
                _driver.Navigate().GoToUrl("https://web.whatsapp.com");

                return Ok("Sistema Iniciado. Escanea el QR en la ventana del servidor.");
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
                GuardarEnOracle(telefono);
                
                // Respuesta visual bonita (HTML)
                string html = @"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><link rel='stylesheet' href='/Estilos/styleflujos.css'></head><body><div class='wrapper'><div class='contenido-card' style='background:#fff;padding:40px;border-radius:15px;text-align:center;'><h1 style='color:green;font-size:4em;margin:0;'>✅</h1><h2>¡Guardado!</h2><p>Tu número ha sido registrado para recibir alertas.</p><a href='/Modelos/Numero.html'><button class='animated-button'>Regresar</button></a></div></div></body></html>";
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                // Muestra el error real en pantalla para depuración
                string htmlError = $@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><link rel='stylesheet' href='/Estilos/styleflujos.css'></head><body><div class='wrapper'><div class='contenido-card' style='border:2px solid red;background:#fff;padding:20px;'><h1 style='color:red;font-size:4em;margin:0;'>❌</h1><h2>Error al Guardar</h2><p><b>Detalle:</b> {ex.Message}</p><p>Verifica que la base de datos esté conectada.</p><a href='/Modelos/Numero.html'><button class='animated-button'>Intentar de Nuevo</button></a></div></div></body></html>";
                return Content(htmlError, "text/html");
            }
        }

        // ============================================================
        // 3. SET FECHA (CONFIGURACIÓN)
        // ============================================================
        [HttpGet("set-fecha")]
        public IActionResult SetFecha(DateTime nuevaFecha)
        {
            fechaEntrega = nuevaFecha;
            // Si cambiamos la fecha, reseteamos la memoria anti-spam para permitir envíos nuevos
            ultimoAvisoEnviado = -999; 
            return Ok(new { mensaje = $"Fecha actualizada a: {fechaEntrega:dd/MM/yyyy}" });
        }

        // ============================================================
        // 4. VERIFICAR (ENVÍO MASIVO INTELIGENTE)
        // ============================================================
        [HttpGet("verificar-fechas")]
        public IActionResult VerificarFechas()
        {
            // 1. Validar que el bot esté prendido
            if (_driver == null) return BadRequest(new { estado = "El bot no ha sido iniciado (Chrome cerrado)." });

            // 2. Calcular días
            DateTime hoy = DateTime.Today;
            int diasRestantes = (int)(fechaEntrega - hoy).TotalDays;

            // 🛡️ 3. LÓGICA ANTI-SPAM
            // Si ya enviamos recordatorios hoy para estos días faltantes, NO enviamos otra vez.
            if (diasRestantes == ultimoAvisoEnviado)
            {
                return Ok(new { 
                    estado = "⚠️ ALERTA DE SPAM DETENIDA", 
                    mensaje = $"Ya se enviaron los avisos de '{diasRestantes} días faltantes' hoy. No se duplicaron mensajes." 
                });
            }

            // 4. Obtener destinatarios
            List<string> numeros = ObtenerNumeros();
            if (numeros.Count == 0) return Ok(new { estado = "No hay números registrados en la BD." });

            int enviados = 0;
            int fallidos = 0;
            
            string mensaje = $"🔔 *Recordatorio Muestra Mercadológica*\n\nFaltan {diasRestantes} días para la entrega final ({fechaEntrega:dd/MM/yyyy}).\nPor favor revisa tus pendientes en la plataforma.";

            // 5. Enviar uno por uno
            foreach (var num in numeros)
            {
                if (EnviarMensaje(num, mensaje)) 
                    enviados++; 
                else 
                    fallidos++;
            }

            // ✅ 6. ACTUALIZAR MEMORIA
            // Si se envió al menos uno, guardamos registro para no repetir hoy.
            if (enviados > 0) ultimoAvisoEnviado = diasRestantes;

            return Ok(new { 
                total_procesados = numeros.Count, 
                enviados = enviados, 
                fallidos = fallidos,
                aviso_memoria = $"Se registró envío exitoso para el día {diasRestantes}"
            });
        }

        // ============================================================
        // 5. TEST ENVÍO (PRUEBA UNITARIA)
        // ============================================================
        [HttpGet("test-envio")]
        public IActionResult TestEnvio(string telefono)
        {
            if (_driver == null) return BadRequest("El bot está apagado.");
            
            bool result = EnviarMensaje(telefono, "🤖 Prueba de conexión del sistema Muestra Mercadológica.");
            
            return Ok(result ? "Enviado con éxito." : "Fallo al enviar (revisa el número o si el chat cargó).");
        }

        // ============================================================
        // 6. VER NÚMEROS (DEBUG)
        // ============================================================
        [HttpGet("ver-numeros")]
        public IActionResult VerNumeros()
        {
            try
            {
                var lista = ObtenerNumeros();
                return Ok(new { total = lista.Count, lista = lista });
            }
            catch (Exception ex) { return BadRequest("Error BD: " + ex.Message); }
        }

        // ============================================================
        // MÉTODOS PRIVADOS (AUXILIARES)
        // ============================================================

        private string GetConnectionString()
        {
            // Busca la conexión llamada "MyDbConnection" en appsettings.json
            // Docker reemplaza esto automáticamente con la variable de entorno
            return _configuration.GetConnectionString("MyDbConnection") 
                   ?? "User Id=MUESTRA_ADMIN;Password=Muestra.2025;Data Source=localhost:1521/XEPDB1;";
        }

        private void GuardarEnOracle(string telefono)
        {
            string tel = telefono.Replace(" ", "").Replace("-", "").Replace("+", "").Trim();
            
            using (OracleConnection con = new OracleConnection(GetConnectionString()))
            {
                con.Open();
                string query = "INSERT INTO NUMEROS (TELEFONO) VALUES (:t)";
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.Parameters.Add(new OracleParameter("t", tel));
                    cmd.ExecuteNonQuery();
                }
                // Commit explícito por seguridad
                using (OracleCommand c = new OracleCommand("COMMIT", con)) { c.ExecuteNonQuery(); }
            }
        }

        private List<string> ObtenerNumeros()
        {
            var lista = new List<string>();
            try
            {
                using (OracleConnection con = new OracleConnection(GetConnectionString()))
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
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Error leyendo números: " + ex.Message);
            }
            return lista;
        }

        private bool EnviarMensaje(string tel, string msj)
        {
            // Esperamos turno en el semáforo
            _browserLock.Wait(); 
            try
            {
                string url = $"https://web.whatsapp.com/send?phone={tel}&text={Uri.EscapeDataString(msj)}";
                _driver!.Navigate().GoToUrl(url);

                // Espera inteligente (hasta 20s)
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
                
                try 
                {
                    // Buscar botón de enviar (el selector data-icon='send' es robusto)
                    var btnEnviar = wait.Until(d => d.FindElement(By.CssSelector("span[data-icon='send']")));
                    
                    Thread.Sleep(800); // Pequeña pausa humana
                    btnEnviar.Click();
                    
                    Thread.Sleep(2000); // Esperar a que salga el mensaje (tic gris)
                    return true;
                }
                catch 
                {
                    // Plan B: Intentar con Enter si no se encuentra el botón
                    try {
                        _driver.SwitchTo().ActiveElement().SendKeys(Keys.Enter);
                        Thread.Sleep(1000);
                        return true;
                    } catch { return false; }
                }
            }
            catch { return false; }
            finally { _browserLock.Release(); } // Liberamos turno siempre
        }
    }
}