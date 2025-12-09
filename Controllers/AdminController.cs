using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Muestra.Models;
using System.Data;
using BCrypt.Net;
using System.Text;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly OracleConnection _connection;
        private readonly IWebHostEnvironment _environment;

        public AdminController(OracleConnection connection, IWebHostEnvironment environment)
        {
            _connection = connection;
            _environment = environment;
        }

        // USUARIOS
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var users = new List<object>();
            var cmd = new OracleCommand(@"SELECT u.IdUsuario, u.Nombre, u.Correo, r.NombreRol FROM Usuarios u JOIN Roles r ON u.IdRol = r.IdRol ORDER BY u.IdUsuario DESC", _connection);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) users.Add(new { IdUsuario = reader.GetInt32(0), Nombre = reader.GetString(1), Correo = reader.GetString(2), Rol = reader.GetString(3) });
            }
            return Ok(users);
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromForm] string nombre, [FromForm] string correo, [FromForm] string password, [FromForm] int idRol)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var checkCmd = new OracleCommand("SELECT COUNT(*) FROM Usuarios WHERE Correo = :Correo", _connection);
                checkCmd.Parameters.Add(new OracleParameter("Correo", correo));
                if (Convert.ToDecimal(await checkCmd.ExecuteScalarAsync()) > 0) return BadRequest(new { message = "El correo ya existe." });
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                var insertCmd = new OracleCommand("INSERT INTO Usuarios (IdUsuario, Nombre, Correo, Contraseña, IdRol) VALUES (usuarios_seq.NEXTVAL, :Nombre, :Correo, :Pass, :IdRol)", _connection);
                insertCmd.Parameters.Add(new OracleParameter("Nombre", nombre));
                insertCmd.Parameters.Add(new OracleParameter("Correo", correo));
                insertCmd.Parameters.Add(new OracleParameter("Pass", hashedPassword));
                insertCmd.Parameters.Add(new OracleParameter("IdRol", idRol));
                await insertCmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Usuario creado." });
            } catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var cmd = new OracleCommand("DELETE FROM Usuarios WHERE IdUsuario = :Id", _connection);
                cmd.Parameters.Add(new OracleParameter("Id", id));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Eliminado." });
            } catch { return StatusCode(500, new { message = "Error al eliminar." }); }
        }

        // PROYECTOS
        [HttpGet("projects")]
        public async Task<IActionResult> GetAllProjects()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var projects = new List<object>();
            var cmd = new OracleCommand(@"SELECT p.IdProyecto, p.NombreProyecto, e.Nombre, c.NombreCategoria, p.Estado, p.RutaArchivoPDF FROM Proyectos p JOIN Equipos e ON p.IdEquipo = e.IdEquipo JOIN Categorias c ON p.IdCategoria = c.IdCategoria ORDER BY p.IdProyecto DESC", _connection);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) projects.Add(new { IdProyecto = reader.GetInt32(0), NombreProyecto = reader.GetString(1), NombreEquipo = reader.GetString(2), NombreCategoria = reader.GetString(3), Estado = reader.GetString(4), RutaArchivo = reader.IsDBNull(5) ? null : reader.GetString(5) });
            }
            return Ok(projects);
        }

        [HttpPost("update-project-status")]
        public async Task<IActionResult> UpdateProjectStatus([FromForm] int idProyecto, [FromForm] string nuevoEstado)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var cmd = new OracleCommand("UPDATE Proyectos SET Estado = :Estado WHERE IdProyecto = :Id", _connection);
                cmd.Parameters.Add(new OracleParameter("Estado", nuevoEstado));
                cmd.Parameters.Add(new OracleParameter("Id", idProyecto));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Estado actualizado." });
            } catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ASIGNACIONES
        [HttpGet("evaluators-list")]
        public async Task<IActionResult> GetEvaluatorsList()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var evaluators = new List<object>();
            var cmd = new OracleCommand("SELECT IdUsuario, Nombre, r.NombreRol FROM Usuarios u JOIN Roles r ON u.IdRol = r.IdRol WHERE u.IdRol IN (3, 5) ORDER BY u.Nombre", _connection);
            await using (var reader = await cmd.ExecuteReaderAsync()) while (await reader.ReadAsync()) evaluators.Add(new { IdUsuario = reader.GetInt32(0), Nombre = reader.GetString(1), Rol = reader.GetString(2) });
            return Ok(evaluators);
        }

        [HttpGet("assignments-full")]
        public async Task<IActionResult> GetAssignmentsFull()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var cmd = new OracleCommand(@"SELECT p.IdProyecto, p.NombreProyecto, c.NombreCategoria, u.Nombre, a.IdAsignacion FROM Proyectos p JOIN Categorias c ON p.IdCategoria = c.IdCategoria LEFT JOIN Asignaciones a ON p.IdProyecto = a.IdProyecto LEFT JOIN Usuarios u ON a.IdUsuario_Evaluador = u.IdUsuario ORDER BY p.NombreProyecto", _connection);
            var lista = new List<dynamic>();
            await using (var reader = await cmd.ExecuteReaderAsync()) while (await reader.ReadAsync()) lista.Add(new { IdProyecto = reader.GetInt32(0), NombreProyecto = reader.GetString(1), Categoria = reader.GetString(2), Evaluador = reader.IsDBNull(3) ? null : reader.GetString(3), IdAsignacion = reader.IsDBNull(4) ? 0 : reader.GetInt32(4) });
            var agrupados = lista.GroupBy(x => x.IdProyecto).Select(g => new { IdProyecto = g.Key, NombreProyecto = g.First().NombreProyecto, Categoria = g.First().Categoria, Evaluadores = g.Where(x => x.Evaluador != null).Select(x => new { x.Evaluador, x.IdAsignacion }).ToList() });
            return Ok(agrupados);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignEvaluator([FromForm] int idProyecto, [FromForm] int idEvaluador)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var check = new OracleCommand("SELECT COUNT(*) FROM Asignaciones WHERE IdProyecto=:P AND IdUsuario_Evaluador=:U", _connection);
                check.Parameters.Add(new OracleParameter("P", idProyecto));
                check.Parameters.Add(new OracleParameter("U", idEvaluador));
                if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0) return BadRequest(new { message = "Ya asignado." });
                var cmd = new OracleCommand("INSERT INTO Asignaciones (IdAsignacion, IdProyecto, IdUsuario_Evaluador, Estado) VALUES (asignaciones_seq.NEXTVAL, :P, :U, 'Pendiente')", _connection);
                cmd.Parameters.Add(new OracleParameter("P", idProyecto));
                cmd.Parameters.Add(new OracleParameter("U", idEvaluador));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Asignado." });
            } catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("unassign/{id}")]
        public async Task<IActionResult> Unassign(int id)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var cmd = new OracleCommand("DELETE FROM Asignaciones WHERE IdAsignacion = :ID", _connection);
                cmd.Parameters.Add(new OracleParameter("ID", id));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Eliminado." });
            } catch { return StatusCode(500, new { message = "Error." }); }
        }

        // PROGRESO
        [HttpGet("progress")]
        public async Task<IActionResult> GetProgress()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var stats = new { Total = 0, Completadas = 0, Porcentaje = 0, Detalle = new List<object>() };
            var cmdStats = new OracleCommand("SELECT COUNT(*), SUM(CASE WHEN Estado = 'Completada' THEN 1 ELSE 0 END) FROM Asignaciones", _connection);
            await using (var reader = await cmdStats.ExecuteReaderAsync()) if (await reader.ReadAsync()) stats = new { Total = reader.IsDBNull(0) ? 0 : reader.GetInt32(0), Completadas = reader.IsDBNull(1) ? 0 : reader.GetInt32(1), Porcentaje = (reader.IsDBNull(0) || reader.GetInt32(0) == 0) ? 0 : (reader.GetInt32(1) * 100 / reader.GetInt32(0)), Detalle = new List<object>() };
            var cmdDet = new OracleCommand("SELECT p.NombreProyecto, u.Nombre, a.Estado FROM Asignaciones a JOIN Proyectos p ON a.IdProyecto = p.IdProyecto JOIN Usuarios u ON a.IdUsuario_Evaluador = u.IdUsuario ORDER BY a.Estado, p.NombreProyecto", _connection);
            var det = new List<object>();
            await using (var reader = await cmdDet.ExecuteReaderAsync()) while (await reader.ReadAsync()) det.Add(new { Proyecto = reader.GetString(0), Evaluador = reader.GetString(1), Estado = reader.GetString(2) });
            return Ok(new { Resumen = stats, Lista = det });
        }

        // OVERVIEW
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var cmd = new OracleCommand("SELECT (SELECT COUNT(*) FROM Equipos), (SELECT COUNT(*) FROM Proyectos), (SELECT COUNT(*) FROM Usuarios WHERE IdRol IN (3,5)), (SELECT COUNT(*) FROM Asignaciones WHERE Estado = 'Pendiente') FROM DUAL", _connection);
            await using (var reader = await cmd.ExecuteReaderAsync()) if (await reader.ReadAsync()) return Ok(new { Equipos = reader.GetInt32(0), Proyectos = reader.GetInt32(1), Evaluadores = reader.GetInt32(2), EvaluacionesPendientes = reader.GetInt32(3) });
            return Ok(new { });
        }

        // RESULTADOS
        [HttpGet("results")]
        public async Task<IActionResult> GetResults()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var resultados = new List<object>();
            var cmd = new OracleCommand(@"SELECT p.IdProyecto, p.NombreProyecto, e.Nombre, c.NombreCategoria, COALESCE(AVG(ev.PuntajeObtenido), 0) as Prom FROM Proyectos p JOIN Equipos e ON p.IdEquipo = e.IdEquipo JOIN Categorias c ON p.IdCategoria = c.IdCategoria LEFT JOIN Asignaciones a ON p.IdProyecto = a.IdProyecto LEFT JOIN Evaluaciones ev ON a.IdAsignacion = ev.IdAsignacion GROUP BY p.IdProyecto, p.NombreProyecto, e.Nombre, c.NombreCategoria HAVING AVG(ev.PuntajeObtenido) > 0 ORDER BY Prom DESC", _connection);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                int pos = 1;
                while (await reader.ReadAsync())
                {
                    decimal prom = reader.GetDecimal(4);
                    string rec = pos == 1 ? "1er Lugar" : (prom >= 9 ? "Mención Honorífica" : (prom >= 8 ? "Destacado" : "Participación"));
                    resultados.Add(new { Posicion = pos++, IdProyecto = reader.GetInt32(0), NombreProyecto = reader.GetString(1), Equipo = reader.GetString(2), Categoria = reader.GetString(3), Promedio = Math.Round(prom, 2), Reconocimiento = rec });
                }
            }
            return Ok(resultados);
        }

        // REPORTES
        [HttpGet("report-teams-csv")]
        public async Task<IActionResult> ExportTeamsCsv()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var sb = new StringBuilder(); sb.AppendLine("ID,Equipo,Representante,Correo");
            var cmd = new OracleCommand("SELECT e.IdEquipo, e.Nombre, u.Nombre, u.Correo FROM Equipos e JOIN Usuarios u ON e.IdEquipo = u.IdEquipo", _connection);
            await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) sb.AppendLine($"{r[0]},{r[1]},{r[2]},{r[3]}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "equipos.csv");
        }

        [HttpGet("report-results-csv")]
        public async Task<IActionResult> ExportResultsCsv()
        {
            // (Lógica simplificada reutilizando query de resultados)
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var sb = new StringBuilder(); sb.AppendLine("Pos,Proyecto,Equipo,Promedio");
            var cmd = new OracleCommand(@"SELECT p.NombreProyecto, e.Nombre, COALESCE(AVG(ev.PuntajeObtenido), 0) as Prom FROM Proyectos p JOIN Equipos e ON p.IdEquipo = e.IdEquipo LEFT JOIN Asignaciones a ON p.IdProyecto = a.IdProyecto LEFT JOIN Evaluaciones ev ON a.IdAsignacion = ev.IdAsignacion GROUP BY p.NombreProyecto, e.Nombre HAVING AVG(ev.PuntajeObtenido) > 0 ORDER BY Prom DESC", _connection);
            int pos = 1;
            await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) sb.AppendLine($"{pos++},{r[0]},{r[1]},{Math.Round(r.GetDecimal(2), 2)}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "resultados.csv");
        }

        // GALERÍA
        [HttpGet("gallery")]
        public async Task<IActionResult> GetGalleryItems()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var items = new List<object>();
            var cmd = new OracleCommand("SELECT IdGaleria, RutaArchivo, Descripcion, Tipo FROM Galeria ORDER BY IdGaleria DESC", _connection);
            await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) items.Add(new { Id = r.GetInt32(0), Ruta = r.GetString(1), Descripcion = r.GetString(2), Tipo = r.GetString(3) });
            return Ok(items);
        }

        [HttpPost("upload-gallery")]
        public async Task<IActionResult> UploadGalleryItem([FromForm] string descripcion, [FromForm] string tipo, [FromForm] IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0) return BadRequest(new { message = "Falta archivo." });
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var folder = Path.Combine(_environment.WebRootPath, "uploads", "gallery");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var name = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                using (var s = new FileStream(Path.Combine(folder, name), FileMode.Create)) await archivo.CopyToAsync(s);
                var cmd = new OracleCommand("INSERT INTO Galeria (IdGaleria, RutaArchivo, Descripcion, Tipo) VALUES (galeria_seq.NEXTVAL, :R, :D, :T)", _connection);
                cmd.Parameters.Add(new OracleParameter("R", $"/uploads/gallery/{name}"));
                cmd.Parameters.Add(new OracleParameter("D", descripcion));
                cmd.Parameters.Add(new OracleParameter("T", tipo));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Subido." });
            } catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-gallery/{id}")]
        public async Task<IActionResult> DeleteGalleryItem(int id)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var cmd = new OracleCommand("DELETE FROM Galeria WHERE IdGaleria = :ID", _connection);
                cmd.Parameters.Add(new OracleParameter("ID", id));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Eliminado." });
            } catch { return StatusCode(500, new { message = "Error." }); }
        }

        // CONFIGURACIÓN
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var cmd = new OracleCommand("SELECT * FROM Configuracion WHERE IdConfig = 1", _connection);
            await using (var r = await cmd.ExecuteReaderAsync()) if (await r.ReadAsync()) return Ok(new EventoSettingsModel { NombreEvento = r.IsDBNull(1) ? "" : r.GetString(1), Edicion = r.IsDBNull(2) ? 0 : r.GetInt32(2), Descripcion = r.IsDBNull(3) ? "" : r.GetString(3), FechaInicioRegistro = r.IsDBNull(4) ? null : r.GetDateTime(4), FechaFinRegistro = r.IsDBNull(5) ? null : r.GetDateTime(5), FechaLimiteArchivos = r.IsDBNull(6) ? null : r.GetDateTime(6), FechaLimiteEvaluacion = r.IsDBNull(7) ? null : r.GetDateTime(7), CategoriasTexto = r.IsDBNull(8) ? "" : r.GetString(8), VerResultados = r.GetInt32(9) == 1, DescargarConstancias = r.GetInt32(10) == 1 });
            return NotFound(new { message = "No config." });
        }

        [HttpPost("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] EventoSettingsModel model)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try {
                var cmd = new OracleCommand("UPDATE Configuracion SET NombreEvento=:N, Edicion=:E, Descripcion=:D, FechaInicioRegistro=:F1, FechaFinRegistro=:F2, FechaLimiteArchivos=:F3, FechaLimiteEvaluacion=:F4, CategoriasTexto=:C, VerResultados=:V1, DescargarConstancias=:V2 WHERE IdConfig=1", _connection);
                cmd.Parameters.Add(new OracleParameter("N", model.NombreEvento));
                cmd.Parameters.Add(new OracleParameter("E", model.Edicion));
                cmd.Parameters.Add(new OracleParameter("D", model.Descripcion));
                cmd.Parameters.Add(new OracleParameter("F1", model.FechaInicioRegistro ?? (object)DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("F2", model.FechaFinRegistro ?? (object)DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("F3", model.FechaLimiteArchivos ?? (object)DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("F4", model.FechaLimiteEvaluacion ?? (object)DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("C", model.CategoriasTexto));
                cmd.Parameters.Add(new OracleParameter("V1", model.VerResultados ? 1 : 0));
                cmd.Parameters.Add(new OracleParameter("V2", model.DescargarConstancias ? 1 : 0));
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Actualizado." });
            } catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}