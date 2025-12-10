using Microsoft.Extensions.FileProviders;
using Muestra.Hubs; 
using Oracle.ManagedDataAccess.Client; 

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); 

// ⚠️ CONEXIÓN DIRECTA (HARDCODED) PARA EL LOGIN
// Si falla XEPDB1, cambiaremos esto a XE más abajo
string connectionString = "User Id=SYSTEM;Password=Muestra.2025;Data Source=localhost:1521/XE;";
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
// 2. CONFIGURACIÓN DEL PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Carpetas Estáticas
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Modelos")),
    RequestPath = "/Modelos"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Estilos")),
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

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<WhatsappHub>("/whatsappHub");

app.Run();