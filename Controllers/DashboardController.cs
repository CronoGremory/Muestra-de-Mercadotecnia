using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Muestra.Models;
using System.Data;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly OracleConnection _connection;

        public DashboardController(OracleConnection connection)
        {
            _connection = connection;
        }

        [HttpGet("evaluador")]
        public async Task<IActionResult> GetDashboardEvaluador([FromQuery] int evaluadorId)
        {
            if (evaluadorId == 0) return BadRequest(new { message = "ID inválido." });
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            var asignaciones = new List<AsignacionViewModel>();
            var cmd = new OracleCommand(
                @"SELECT a.IdAsignacion, p.NombreProyecto, c.NombreCategoria, e.Nombre, a.Estado
                  FROM Asignaciones a
                  JOIN Proyectos p ON a.IdProyecto = p.IdProyecto
                  JOIN Categorias c ON p.IdCategoria = c.IdCategoria
                  JOIN Equipos e ON p.IdEquipo = e.IdEquipo
                  WHERE a.IdUsuario_Evaluador = :EvaluadorId
                  ORDER BY a.Estado, p.NombreProyecto", _connection);
            cmd.Parameters.Add(new OracleParameter("EvaluadorId", evaluadorId));

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    asignaciones.Add(new AsignacionViewModel
                    {
                        IdAsignacion = reader.GetInt32(0),
                        NombreProyecto = reader.GetString(1),
                        NombreCategoria = reader.GetString(2),
                        NombreEquipo = reader.GetString(3),
                        Estado = reader.GetString(4)
                    });
                }
            }
            return Ok(asignaciones);
        }
    }
}