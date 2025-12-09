using Microsoft.Extensions.FileProviders;
using Muestra.Hubs; // Importante para el Socket

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Servicio de Sockets

// Configuración de Sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

// A) Habilitar carpeta wwwroot (por defecto)
app.UseStaticFiles();

// B) HABILITAR TUS CARPETAS PERSONALIZADAS (Modelos, Estilos, Recursos)
// Esto soluciona el ERROR 404

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Modelos")),
    RequestPath = "/Modelos"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Estilos")),
    RequestPath = "/Estilos"
});

// Verifica si existe la carpeta Recursos antes de agregarla para evitar errores
string rutaRecursos = Path.Combine(builder.Environment.ContentRootPath, "Recursos");
if (Directory.Exists(rutaRecursos))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(rutaRecursos),
        RequestPath = "/Recursos"
    });
}

// C) Resto de la configuración
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<WhatsappHub>("/whatsappHub");

app.Run();