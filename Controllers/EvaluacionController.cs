using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Muestra.Models;
using System.Data;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluacionController : ControllerBase
    {
        private readonly OracleConnection _connection;

        public EvaluacionController(OracleConnection connection)
        {
            _connection = connection;
        }

        [HttpGet("criterios")]
        public async Task<IActionResult> GetCriterios([FromQuery] int idAsignacion, [FromQuery] string rolEvaluador = "Juez")
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            var cmd = new OracleCommand(@"SELECT c.IdCategoria, c.NombreCategoria FROM Asignaciones a JOIN Proyectos p ON a.IdProyecto = p.IdProyecto JOIN Categorias c ON p.IdCategoria = c.IdCategoria WHERE a.IdAsignacion = :Id", _connection);
            cmd.Parameters.Add(new OracleParameter("Id", idAsignacion));
            int idCategoria = 0; string nombreCategoria = "";
            await using (var r = await cmd.ExecuteReaderAsync()) if (await r.ReadAsync()) { idCategoria = r.GetInt32(0); nombreCategoria = r.GetString(1); } else return NotFound("No existe.");

            var critCmd = new OracleCommand("SELECT IdCriterio, NombreCriterio FROM Criterios WHERE IdCategoria = :Id AND RolEvaluador = :Rol ORDER BY IdCriterio", _connection);
            critCmd.Parameters.Add(new OracleParameter("Id", idCategoria));
            critCmd.Parameters.Add(new OracleParameter("Rol", rolEvaluador));

            var list = new List<CriterioViewModel>();
            await using (var r = await critCmd.ExecuteReaderAsync()) while (await r.ReadAsync()) list.Add(new CriterioViewModel { IdCriterio = r.GetInt32(0), NombreCriterio = r.GetString(1), TipoPregunta = DeterminarTipo(r.GetString(1), nombreCategoria, rolEvaluador) });
            return Ok(list);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitEvaluacion([FromBody] EvaluacionSubmitModel model)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            await using var trans = await _connection.BeginTransactionAsync();
            try {
                var del = new OracleCommand("DELETE FROM Evaluaciones WHERE IdAsignacion = :Id", _connection); del.Parameters.Add(new OracleParameter("Id", model.IdAsignacion)); await del.ExecuteNonQueryAsync();
                foreach (var r in model.Respuestas) {
                    var ins = new OracleCommand("INSERT INTO Evaluaciones (IdEvaluacion, IdAsignacion, IdCriterio, PuntajeObtenido, Comentarios) VALUES (evaluaciones_seq.NEXTVAL, :Id, :Crit, :Punt, :Comm)", _connection);
                    ins.Parameters.Add(new OracleParameter("Id", model.IdAsignacion));
                    ins.Parameters.Add(new OracleParameter("Crit", r.IdCriterio));
                    ins.Parameters.Add(new OracleParameter("Punt", r.PuntajeObtenido));
                    ins.Parameters.Add(new OracleParameter("Comm", string.IsNullOrEmpty(r.Comentarios) ? DBNull.Value : r.Comentarios));
                    await ins.ExecuteNonQueryAsync();
                }
                var up = new OracleCommand("UPDATE Asignaciones SET Estado = 'Completada' WHERE IdAsignacion = :Id", _connection); up.Parameters.Add(new OracleParameter("Id", model.IdAsignacion)); await up.ExecuteNonQueryAsync();
                await trans.CommitAsync();
                return Ok(new { message = "Guardado." });
            } catch (Exception ex) { await trans.RollbackAsync(); return StatusCode(500, ex.Message); }
        }

        private string DeterminarTipo(string n, string c, string r)
        {
            if (r == "Docente") return "Escala10";
            if (c == "Retail Revolution" && (n.StartsWith("Estímulos") || n.StartsWith("Descripción"))) return "Escala";
            if (c == "Fresh Creations" && (n.StartsWith("Resumen") || n.StartsWith("Segmentación") || n.StartsWith("Descripción") || n.StartsWith("Estrategias"))) return "Escala";
            return "SiNo";
        }
    }
}