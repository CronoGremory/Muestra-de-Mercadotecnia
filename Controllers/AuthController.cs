using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Muestra.Models;
using System.Data;
using BCrypt.Net;
using Oracle.ManagedDataAccess.Types;

namespace Muestra.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly OracleConnection _connection;

        public AuthController(OracleConnection connection)
        {
            _connection = connection;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginModel model)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            var cmd = new OracleCommand(
                @"SELECT u.IdUsuario, u.Nombre, u.Contraseña, r.NombreRol
                  FROM Usuarios u
                  JOIN Roles r ON u.IdRol = r.IdRol
                  WHERE u.Correo = :Correo", _connection);
            cmd.Parameters.Add(new OracleParameter("Correo", model.Email));

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return BadRequest(new { message = "Correo o contraseña inválidos." });

            var userId = reader.GetDecimal(0);
            var nombre = reader.GetString(1);
            var hashedPassword = reader.GetString(2);
            var role = reader.GetString(3);

            if (!BCrypt.Net.BCrypt.Verify(model.Password, hashedPassword))
            {
                return BadRequest(new { message = "Correo o contraseña inválidos." });
            }

            return Ok(new { message = "Login exitoso", role = role, name = nombre, userId = userId });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterModel model)
        {
            if (model.Password != model.ConfirmPassword) return BadRequest(new { message = "Las contraseñas no coinciden." });
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            await using var transaction = (OracleTransaction)await _connection.BeginTransactionAsync();
            decimal newUserId = 0;

            try
            {
                var checkEmailCmd = new OracleCommand("SELECT COUNT(*) FROM Usuarios WHERE Correo = :Correo", _connection);
                checkEmailCmd.Parameters.Add(new OracleParameter("Correo", model.RepresentativeEmail));
                if (Convert.ToDecimal(await checkEmailCmd.ExecuteScalarAsync()) > 0) return BadRequest(new { message = "El correo ya está registrado." });

                var teamCmd = new OracleCommand("INSERT INTO Equipos (IdEquipo, Nombre) VALUES (equipos_seq.NEXTVAL, :Nombre) RETURNING IdEquipo INTO :newTeamId", _connection);
                teamCmd.Parameters.Add(new OracleParameter("Nombre", model.TeamName));
                var newTeamIdParam = new OracleParameter("newTeamId", OracleDbType.Decimal, ParameterDirection.Output);
                teamCmd.Parameters.Add(newTeamIdParam);
                await teamCmd.ExecuteNonQueryAsync();
                decimal newTeamId = ((OracleDecimal)newTeamIdParam.Value).Value;

                var roleCmd = new OracleCommand("SELECT IdRol FROM Roles WHERE NombreRol = 'Equipo'", _connection);
                var roleId = Convert.ToDecimal(await roleCmd.ExecuteScalarAsync());

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var userCmd = new OracleCommand(
                    @"INSERT INTO Usuarios (IdUsuario, Correo, Contraseña, Nombre, Matricula, Semestre, IdEquipo, IdRol)
                      VALUES (usuarios_seq.NEXTVAL, :Correo, :Pass, :Nombre, :Matricula, :Semestre, :IdEquipo, :IdRol)
                      RETURNING IdUsuario INTO :newUserId", _connection);
                userCmd.Parameters.Add(new OracleParameter("Correo", model.RepresentativeEmail));
                userCmd.Parameters.Add(new OracleParameter("Pass", hashedPassword));
                userCmd.Parameters.Add(new OracleParameter("Nombre", model.RepresentativeName));
                userCmd.Parameters.Add(new OracleParameter("Matricula", model.RepresentativeId));
                userCmd.Parameters.Add(new OracleParameter("Semestre", model.RepresentativeSemester));
                userCmd.Parameters.Add(new OracleParameter("IdEquipo", newTeamId));
                userCmd.Parameters.Add(new OracleParameter("IdRol", roleId));
                
                var newUserIdParam = new OracleParameter("newUserId", OracleDbType.Decimal, ParameterDirection.Output);
                userCmd.Parameters.Add(newUserIdParam);
                await userCmd.ExecuteNonQueryAsync();
                newUserId = ((OracleDecimal)newUserIdParam.Value).Value;

                for (int i = 0; i < model.MemberNames.Count; i++)
                {
                    if (!string.IsNullOrEmpty(model.MemberNames[i]))
                    {
                        var memberCmd = new OracleCommand("INSERT INTO Miembros (IdMiembro, Nombre, Matricula, IdEquipo) VALUES (miembros_seq.NEXTVAL, :Nombre, :Matricula, :IdEquipo)", _connection);
                        memberCmd.Parameters.Add(new OracleParameter("Nombre", model.MemberNames[i]));
                        memberCmd.Parameters.Add(new OracleParameter("Matricula", model.MemberIds[i]));
                        memberCmd.Parameters.Add(new OracleParameter("IdEquipo", newTeamId));
                        await memberCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { message = "¡Equipo registrado exitosamente!", role = "Equipo", name = model.RepresentativeName, userId = newUserId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("fixhash")]
        public async Task<IActionResult> FixHash()
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            try
            {
                string correctHash = BCrypt.Net.BCrypt.HashPassword("juez123");
                var updateCmd = new OracleCommand("UPDATE Usuarios SET Contraseña = :Hash WHERE Correo = 'juez1@uaa.mx'", _connection);
                updateCmd.Parameters.Add(new OracleParameter("Hash", correctHash));
                await updateCmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Contraseña de juez reparada." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
        
        // --- MÉTODOS PARA RECUPERACIÓN DE CONTRASEÑA ---
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordModel model)
        {
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();
            var cmdCheck = new OracleCommand("SELECT COUNT(*) FROM Usuarios WHERE Correo = :Correo", _connection);
            cmdCheck.Parameters.Add(new OracleParameter("Correo", model.Email));
            if (Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) == 0) return Ok(new { message = "Si el correo existe, se ha enviado un enlace." });

            string token = Guid.NewGuid().ToString();
            DateTime expiry = DateTime.Now.AddHours(1);
            var cmdUpdate = new OracleCommand("UPDATE Usuarios SET RecoveryToken = :Token, RecoveryExpiry = :Expiry WHERE Correo = :Correo", _connection);
            cmdUpdate.Parameters.Add(new OracleParameter("Token", token));
            cmdUpdate.Parameters.Add(new OracleParameter("Expiry", expiry));
            cmdUpdate.Parameters.Add(new OracleParameter("Correo", model.Email));
            await cmdUpdate.ExecuteNonQueryAsync();

            string resetLink = $"http://localhost:7100/Modelos/restablecer.html?token={token}";
            return Ok(new { message = "Se ha generado el enlace.", debugLink = resetLink });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordModel model)
        {
            if (model.NewPassword != model.ConfirmPassword) return BadRequest(new { message = "Las contraseñas no coinciden." });
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            var cmdCheck = new OracleCommand("SELECT COUNT(*) FROM Usuarios WHERE RecoveryToken = :Token AND RecoveryExpiry > SYSDATE", _connection);
            cmdCheck.Parameters.Add(new OracleParameter("Token", model.Token));
            if (Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) == 0) return BadRequest(new { message = "Enlace inválido o expirado." });

            string newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            var cmdUpdate = new OracleCommand("UPDATE Usuarios SET Contraseña = :Pass, RecoveryToken = NULL, RecoveryExpiry = NULL WHERE RecoveryToken = :Token", _connection);
            cmdUpdate.Parameters.Add(new OracleParameter("Pass", newHash));
            cmdUpdate.Parameters.Add(new OracleParameter("Token", model.Token));
            await cmdUpdate.ExecuteNonQueryAsync();

            return Ok(new { message = "Contraseña actualizada." });
        }
    }
}