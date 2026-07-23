namespace SistemaVisionTech.Infrastructure.Entities
{
    public class NavNodo
    {
        public int NodoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "NODO"; // NODO | VISTA
        public int? PadreId { get; set; }
        public int Orden { get; set; } = 0;
        public string? Icono { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public bool Visible { get; set; } = true;
        public bool Activo { get; set; } = true;

        public NavNodo? Padre { get; set; }
        public ICollection<NavNodo> Hijos { get; set; } = [];
        public ICollection<NavRestriccion> Restricciones { get; set; } = [];
    }
}
