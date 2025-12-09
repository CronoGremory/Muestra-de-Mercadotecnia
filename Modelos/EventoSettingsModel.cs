namespace Muestra.Models {
    public class EventoSettingsModel {
        public string NombreEvento { get; set; } = string.Empty;
        public int Edicion { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? FechaInicioRegistro { get; set; }
        public DateTime? FechaFinRegistro { get; set; }
        public DateTime? FechaLimiteArchivos { get; set; }
        public DateTime? FechaLimiteEvaluacion { get; set; }
        public string CategoriasTexto { get; set; } = string.Empty;
        public bool VerResultados { get; set; }
        public bool DescargarConstancias { get; set; }
    }
}