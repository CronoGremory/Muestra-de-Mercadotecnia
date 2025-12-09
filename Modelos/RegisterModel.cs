using Microsoft.AspNetCore.Mvc;
namespace Muestra.Models {
    public class RegisterModel {
        [FromForm(Name = "team_name")] public string TeamName { get; set; } = string.Empty;
        [FromForm(Name = "representative_name")] public string RepresentativeName { get; set; } = string.Empty;
        [FromForm(Name = "representative_id")] public string RepresentativeId { get; set; } = string.Empty;
        [FromForm(Name = "representative_semester")] public int RepresentativeSemester { get; set; }
        [FromForm(Name = "representative_email")] public string RepresentativeEmail { get; set; } = string.Empty;
        [FromForm(Name = "password")] public string Password { get; set; } = string.Empty;
        [FromForm(Name = "confirm_password")] public string ConfirmPassword { get; set; } = string.Empty;
        [FromForm(Name = "member_name[]")] public List<string> MemberNames { get; set; } = new List<string>();
        [FromForm(Name = "member_id[]")] public List<string> MemberIds { get; set; } = new List<string>();
    }
}