using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // Importante para el socket
using Muestra.Hubs; // Importante para conectar con tu Hub
using Oracle.ManagedDataAccess.Client;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks; // Necesario para async
using Microsoft.Extensions.Configuration;

namespace Muestra.Controllers
{
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<WhatsappHub> _hubContext; // <--- El Socket

        // Inyectamos el HubContext en el constructor
        public WhatsApiController(IConfiguration configuration, IHubContext<WhatsappHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // ... (Variables estáticas igual que antes) ...
        private static DateTime fechaEntrega = new DateTime(2025, 12, 10);
        private static IWebDriver? _driver;
        private static int ultimoAvisoEnviado = -999;
        private static readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);

        // ... (Métodos Iniciar, Activar, SetFecha, TestEnvio igual que antes) ...
        // (Por espacio, asumo que dejas esos métodos igual, solo cambiaremos VerificarFechas y EnviarMensaje)

        // 1. INICIAR (Igual que antes)
        [HttpGet("iniciar")]
        public IActionResult IniciarBot()
        {
            if (_driver != null) return Ok("El sistema ya está corriendo.");
            try
            {
                var options = new ChromeOptions();
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "WhatsAppBot_Sesion_SOCKETS"); // Cambié nombre carpeta por seguridad
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                
                options.AddArgument($"user-data-dir={path}");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");

                _driver = new ChromeDriver(options);
                _driver.Manage().Window.Maximize();
                _driver.Navigate().GoToUrl("https://web.whatsapp.com");

                return Ok("Sistema Iniciado. Escanea el QR.");
            }
            catch (Exception ex) { return BadRequest("Error: " + ex.Message); }
        }

        // 2. ACTIVAR (Igual que antes - Copia tu código de guardar en Oracle)
        [HttpGet("activar")]
        public IActionResult Activar([FromQuery] string telefono)
        {
           // ... (Usa tu código anterior de guardar en Oracle) ...
           // Solo por brevedad no lo repito todo, pero mantén tu lógica de INSERT
           return Ok("Guardado"); 
        }

        // 3. VERIFICAR FECHAS (MODIFICADO CON SOCKETS)
        [HttpGet("verificar-fechas")]
        public async Task<IActionResult> VerificarFechas() // Ahora es async
        {
            if (_driver == null) return BadRequest(new { estado = "Bot apagado." });

            DateTime hoy = DateTime.Today;
            int diasRestantes = (int)(fechaEntrega - hoy).TotalDays;

            if (diasRestantes == ultimoAvisoEnviado)
            {
                await _hubContext.Clients.All.SendAsync("RecibirLog", "⚠️ Alerta Spam: Ya se enviaron hoy.");
                return Ok(new { estado = "SPAM DETECTADO" });
            }

            List<string> numeros = ObtenerNumeros(); // Tu método privado
            if (numeros.Count == 0) return Ok(new { estado = "Sin números." });

            // Avisar al Frontend que empezamos
            await _hubContext.Clients.All.SendAsync("RecibirLog", $"🚀 Iniciando envío masivo a {numeros.Count} usuarios...");

            int enviados = 0;
            string mensaje = $"🔔 Recordatorio: Faltan {diasRestantes} días.";

            foreach (var num in numeros)
            {
                // Enviamos y notificamos por Socket en tiempo real
                bool exito = EnviarMensaje(num, mensaje);
                if (exito) 
                {
                    enviados++;
                    // ESTO ES EL SOCKET EN ACCIÓN:
                    await _hubContext.Clients.All.SendAsync("RecibirProgreso", num, "Enviado ✅");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("RecibirProgreso", num, "Falló ❌");
                }
            }

            if (enviados > 0) ultimoAvisoEnviado = diasRestantes;
            
            await _hubContext.Clients.All.SendAsync("RecibirLog", "🏁 Proceso finalizado.");
            return Ok(new { total = numeros.Count, enviados = enviados });
        }

        // ... (Métodos privados GetConnectionString y ObtenerNumeros igual que antes) ...

        // ... (Tu método EnviarMensaje igual que antes) ...
        
        // Agrego estos helpers rápidos por si borraste el resto:
        private string GetConnectionString() { return _configuration.GetConnectionString("MyDbConnection") ?? ""; }
        private List<string> ObtenerNumeros() 
        {
            // ... (Tu lógica de Oracle SELECT) ...
            return new List<string>(); // Dummy para que compile si copias directo, pero usa tu lógica real.
        }
        private bool EnviarMensaje(string tel, string msj)
        {
             // ... (Tu lógica de Selenium) ...
             return true; 
        }
    }
}