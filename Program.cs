using Microsoft.Extensions.FileProviders;
using Muestra.Hubs; 
using Oracle.ManagedDataAccess.Client; // <--- NECESARIO PARA QUE EL LOGIN FUNCIONE

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Servicio de Sockets

// 1.1 CONEXIÓN A BASE DE DATOS (Arreglo del Login y Advertencia NULL)
string connectionString = builder.Configuration.GetConnectionString("MyDbConnection") ?? "";
builder.Services.AddTransient<OracleConnection>(_ => new OracleConnection(connectionString));

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

app.UseStaticFiles();

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

string rutaRecursos = Path.Combine(builder.Environment.ContentRootPath, "Recursos");
if (Directory.Exists(rutaRecursos))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(rutaRecursos),
        RequestPath = "/Recursos"
    });
}

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear el Socket
app.MapHub<WhatsappHub>("/whatsappHub");

app.Run();