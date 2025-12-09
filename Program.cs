using Microsoft.Extensions.FileProviders;
using Oracle.ManagedDataAccess.Client;
using System.Data; // <-- Añade esto, lo necesitarás para los controladores

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // El paquete que instalaste

var connectionString = builder.Configuration.GetConnectionString("MyDbConnection");

builder.Services.AddTransient<OracleConnection>(_ => new OracleConnection(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    DefaultFileNames = new List<string> { "Modelos/index.html" }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath)
});

app.MapControllers();
app.Run();