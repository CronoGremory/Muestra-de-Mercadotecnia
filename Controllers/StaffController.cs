using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly OracleConnection _connection;

        public StaffController(OracleConnection connection)
        {
            _connection = connection;
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetAllProjects()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var projects = new List<object>();
            var cmd = new OracleCommand(@"SELECT p.IdProyecto, p.NombreProyecto, e.Nombre, c.NombreCategoria, p.Estado FROM Proyectos p JOIN Equipos e ON p.IdEquipo = e.IdEquipo JOIN Categorias c ON p.IdCategoria = c.IdCategoria ORDER BY p.Estado, p.NombreProyecto", _connection);
            await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) projects.Add(new { Id = r.GetInt32(0), Proyecto = r.GetString(1), Equipo = r.GetString(2), Categoria = r.GetString(3), Estado = r.GetString(4) });
            return Ok(projects);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var cmd = new OracleCommand("SELECT (SELECT COUNT(*) FROM Proyectos), (SELECT COUNT(*) FROM Proyectos WHERE Estado = 'Pendiente') FROM DUAL", _connection);
            await using (var r = await cmd.ExecuteReaderAsync()) if (await r.ReadAsync()) return Ok(new { total = r.GetInt32(0), pendientes = r.GetInt32(1) });
            return Ok(new { total = 0, pendientes = 0 });
        }
    }
}