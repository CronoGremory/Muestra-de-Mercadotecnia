using Microsoft.Extensions.FileProviders;
using Muestra.Hubs; // Importante para el Socket

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ==========================================

// Agregar servicios MVC (Vistas y Controladores)
builder.Services.AddControllersWithViews();

// Agregar servicio de Sockets (SignalR)
builder.Services.AddSignalR(); 

// Configuración de Sesión (Para el Login)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// (Opcional) Si usaras inyección de dependencias para Oracle, iría aquí.
// builder.Services.AddTransient<OracleConnection>(...);

var app = builder.Build();

// ==========================================
// 2. CONFIGURACIÓN DEL PIPELINE HTTP
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Habilitar archivos estáticos (CSS, JS, Imágenes)
app.UseStaticFiles();

// Habilitar enrutamiento
app.UseRouting();

// Habilitar sesión antes de autorización
app.UseSession();
app.UseAuthorization();

// Mapear rutas de controladores
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear la ruta del Socket (SignalR)
app.MapHub<WhatsappHub>("/whatsappHub");

app.Run();