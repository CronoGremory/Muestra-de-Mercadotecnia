using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicController : ControllerBase
    {
        private readonly OracleConnection _connection;

        public PublicController(OracleConnection connection)
        {
            _connection = connection;
        }

        [HttpGet("gallery")]
        public async Task<IActionResult> GetGallery()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var items = new List<object>();
            var cmd = new OracleCommand("SELECT RutaArchivo, Descripcion, Tipo FROM Galeria ORDER BY IdGaleria DESC", _connection);
            await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) items.Add(new { Ruta = r.GetString(0), Descripcion = r.GetString(1), Tipo = r.GetString(2) });
            return Ok(items);
        }
    }
}