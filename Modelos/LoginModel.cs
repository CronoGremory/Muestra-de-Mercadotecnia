using Microsoft.AspNetCore.Mvc;
namespace Muestra.Models {
    public class LoginModel {
        [FromForm(Name = "email")] public string Email { get; set; } = string.Empty;
        [FromForm(Name = "password")] public string Password { get; set; } = string.Empty;
    }
}