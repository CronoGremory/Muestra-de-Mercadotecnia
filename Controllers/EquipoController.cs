using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Muestra.Models;
using System.Data;
using Oracle.ManagedDataAccess.Types;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipoController : ControllerBase
    {
        private readonly OracleConnection _connection;
        private readonly IWebHostEnvironment _environment;

        public EquipoController(OracleConnection connection, IWebHostEnvironment environment)
        {
            _connection = connection;
            _environment = environment;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int userId)
        {
            if (userId == 0) return BadRequest(new { message = "ID inválido." });
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            var dashboardData = new EquipoDashboardViewModel();
            using (var cmd = new OracleCommand(@"SELECT u.IdEquipo, e.Nombre FROM Usuarios u JOIN Equipos e ON u.IdEquipo = e.IdEquipo WHERE u.IdUsuario = :Id", _connection))
            {
                cmd.Parameters.Add(new OracleParameter("Id", userId));
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync()) return NotFound(new { message = "Equipo no encontrado." });
                    dashboardData.IdEquipo = reader.GetInt32(0);
                    dashboardData.NombreEquipo = reader.GetString(1);
                }
            }
            
            using (var cmd = new OracleCommand("SELECT COUNT(*) FROM Miembros WHERE IdEquipo = :Id", _connection))
            {
                cmd.Parameters.Add(new OracleParameter("Id", dashboardData.IdEquipo));
                dashboardData.CantidadMiembros = Convert.ToInt32(await cmd.ExecuteScalarAsync()) + 1;
            }

            using (var cmd = new OracleCommand(@"SELECT p.IdProyecto, c.NombreCategoria, p.Estado, p.RutaArchivoPDF, p.NombreProyecto FROM Proyectos p JOIN Categorias c ON p.IdCategoria = c.IdCategoria WHERE p.IdEquipo = :Id", _connection))
            {
                cmd.Parameters.Add(new OracleParameter("Id", dashboardData.IdEquipo));
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        dashboardData.ProyectosInscritos.Add(new ProyectoInscritoViewModel {
                            IdProyecto = reader.GetInt32(0),
                            NombreCategoria = reader.GetString(1),
                            Estado = reader.GetString(2),
                            RutaArchivoPDF = reader.IsDBNull(3) ? null : reader.GetString(3),
                            NombreProyecto = reader.GetString(4)
                        });
                    }
                }
            }
            return Ok(dashboardData);
        }

        [HttpGet("categorias-disponibles")]
        public async Task<IActionResult> GetCategoriasDisponibles([FromQuery] int userId)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            int idEquipo = 0;
            using (var cmd = new OracleCommand("SELECT IdEquipo FROM Usuarios WHERE IdUsuario = :Id", _connection)) {
                cmd.Parameters.Add(new OracleParameter("Id", userId));
                var res = await cmd.ExecuteScalarAsync();
                if (res == null) return BadRequest("No es equipo");
                idEquipo = Convert.ToInt32(res);
            }

            var todas = new List<CategoriaViewModel>();
            using (var cmd = new OracleCommand("SELECT IdCategoria, NombreCategoria FROM Categorias", _connection)) {
                using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) todas.Add(new CategoriaViewModel { IdCategoria = r.GetInt32(0), NombreCategoria = r.GetString(1) });
            }

            var inscritas = new List<int>();
            using (var cmd = new OracleCommand("SELECT IdCategoria FROM Proyectos WHERE IdEquipo = :Id", _connection)) {
                cmd.Parameters.Add(new OracleParameter("Id", idEquipo));
                using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) inscritas.Add(r.GetInt32(0));
            }

            return Ok(todas.Where(c => !inscritas.Contains(c.IdCategoria)).ToList());
        }

        [HttpPost("inscribir")]
        public async Task<IActionResult> InscribirCategoria([FromBody] InscripcionModel model)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            await using var trans = await _connection.BeginTransactionAsync();
            try {
                int idEquipo = 0;
                using (var cmd = new OracleCommand("SELECT IdEquipo FROM Usuarios WHERE IdUsuario = :Id", _connection)) {
                    cmd.Parameters.Add(new OracleParameter("Id", model.UserId));
                    idEquipo = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                
                var insert = new OracleCommand("INSERT INTO Proyectos (IdProyecto, IdEquipo, IdCategoria, NombreProyecto, Estado) VALUES (proyectos_seq.NEXTVAL, :Eq, :Cat, :Nom, 'Pendiente')", _connection);
                insert.Parameters.Add(new OracleParameter("Eq", idEquipo));
                insert.Parameters.Add(new OracleParameter("Cat", model.IdCategoria));
                insert.Parameters.Add(new OracleParameter("Nom", model.NombreProyecto));
                await insert.ExecuteNonQueryAsync();
                
                await trans.CommitAsync();
                return Ok(new { message = "Inscrito." });
            } catch (Exception ex) { await trans.RollbackAsync(); return StatusCode(500, ex.Message); }
        }

        [HttpPost("subir-archivo")]
        public async Task<IActionResult> SubirArchivo([FromForm] int idProyecto, [FromForm] IFormFile archivoPdf)
        {
            if (archivoPdf == null || archivoPdf.Length == 0) return BadRequest("Falta archivo");
            if (Path.GetExtension(archivoPdf.FileName).ToLower() != ".pdf") return BadRequest("Solo PDF");

            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var uploads = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                
                var name = $"{idProyecto}_{Guid.NewGuid()}.pdf";
                using (var stream = new FileStream(Path.Combine(uploads, name), FileMode.Create)) await archivoPdf.CopyToAsync(stream);
                
                var cmd = new OracleCommand("UPDATE Proyectos SET RutaArchivoPDF = :R, Estado = 'Subido' WHERE IdProyecto = :Id", _connection);
                cmd.Parameters.Add(new OracleParameter("R", $"/uploads/{name}"));
                cmd.Parameters.Add(new OracleParameter("Id", idProyecto));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Archivo subido." });
            } catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("resultados")]
        public async Task<IActionResult> GetResultados([FromQuery] int idProyecto)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var cmdInfo = new OracleCommand(@"SELECT p.NombreProyecto, c.NombreCategoria, e.Nombre FROM Proyectos p JOIN Categorias c ON p.IdCategoria = c.IdCategoria JOIN Equipos e ON p.IdEquipo = e.IdEquipo WHERE p.IdProyecto = :Id", _connection);
            cmdInfo.Parameters.Add(new OracleParameter("Id", idProyecto));
            string nombreProyecto = "", nombreCategoria = "", nombreEquipo = "";
            using (var r = await cmdInfo.ExecuteReaderAsync()) if (await r.ReadAsync()) { nombreProyecto = r.GetString(0); nombreCategoria = r.GetString(1); nombreEquipo = r.GetString(2); } else return NotFound(new { message = "No encontrado." });

            var cmdDetalle = new OracleCommand(@"SELECT cr.NombreCriterio, AVG(ev.PuntajeObtenido) FROM Evaluaciones ev JOIN Criterios cr ON ev.IdCriterio = cr.IdCriterio JOIN Asignaciones a ON ev.IdAsignacion = a.IdAsignacion WHERE a.IdProyecto = :Id GROUP BY cr.NombreCriterio", _connection);
            cmdDetalle.Parameters.Add(new OracleParameter("Id", idProyecto));
            var criterios = new List<object>();
            decimal suma = 0; int total = 0;
            using (var r = await cmdDetalle.ExecuteReaderAsync()) while (await r.ReadAsync()) { decimal p = r.GetDecimal(1); criterios.Add(new { Criterio = r.GetString(0), Puntaje = Math.Round(p, 1) }); suma += p; total++; }
            decimal final = total > 0 ? (suma / total) : 0;
            string rec = "Participación"; if (final >= 9) rec = "Mención Honorífica"; if (final >= 8 && final < 9) rec = "Destacado";

            return Ok(new { Proyecto = nombreProyecto, Categoria = nombreCategoria, Equipo = nombreEquipo, CalificacionFinal = Math.Round(final, 2), Reconocimiento = rec, Detalles = criterios });
        }
    }
}