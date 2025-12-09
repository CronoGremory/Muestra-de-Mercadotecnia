namespace Muestra.Models
{
    // ==========================================================
    // MODELOS PARA EL FLUJO DEL EVALUADOR
    // ==========================================================

    // Describe un proyecto en la tabla del dashboard del evaluador
    public class AsignacionViewModel { public int IdAsignacion { get; set; } public string NombreProyecto { get; set; } = string.Empty; public string NombreCategoria { get; set; } = string.Empty; public string NombreEquipo { get; set; } = string.Empty; public string Estado { get; set; } = string.Empty; }
    
    // Describe una pregunta/criterio en el formulario de evaluación
    public class CriterioViewModel { public int IdCriterio { get; set; } public string NombreCriterio { get; set; } = string.Empty; public string TipoPregunta { get; set; } = "SiNo"; }
    
    // Describe una respuesta individual
    public class EvaluacionModel { public int IdCriterio { get; set; } public decimal PuntajeObtenido { get; set; } public string Comentarios { get; set; } = string.Empty; }
    
    // Contenedor para enviar el formulario de evaluación completo
    public class EvaluacionSubmitModel { public int IdAsignacion { get; set; } public List<EvaluacionModel> Respuestas { get; set; } = new List<EvaluacionModel>(); }


    // ==========================================================
    // MODELOS PARA EL FLUJO DEL EQUIPO
    // ==========================================================

    // Describe un proyecto en la tarjeta del dashboard del equipo
    public class ProyectoInscritoViewModel { public int IdProyecto { get; set; } public string NombreCategoria { get; set; } = string.Empty; public string Estado { get; set; } = string.Empty; public string? RutaArchivoPDF { get; set; } public string NombreProyecto { get; set; } = string.Empty; }
    
    // Describe todo lo que el dashboard del equipo necesita
    public class EquipoDashboardViewModel { public int IdEquipo { get; set; } public string NombreEquipo { get; set; } = string.Empty; public int CantidadMiembros { get; set; } public List<ProyectoInscritoViewModel> ProyectosInscritos { get; set; } = new List<ProyectoInscritoViewModel>(); }
    
    // Describe una categoría disponible para que un equipo se inscriba
    public class CategoriaViewModel { public int IdCategoria { get; set; } public string NombreCategoria { get; set; } = string.Empty; }
    
    // Modelo para recibir datos al inscribir una nueva categoría
    public class InscripcionModel { public int UserId { get; set; } public int IdCategoria { get; set; } public string NombreProyecto { get; set; } = string.Empty; }


    // ==========================================================
    // MODELOS PARA RECUPERACIÓN DE CONTRASEÑA (¡LOS QUE FALTABAN!)
    // ==========================================================
    
    public class ForgotPasswordModel
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordModel
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}