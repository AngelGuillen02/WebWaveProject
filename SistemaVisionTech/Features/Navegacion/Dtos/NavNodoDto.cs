namespace SistemaVisionTech.Features.Navegacion.Dtos
{
    public class NavNodoDto
    {
        public int NodoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? PadreId { get; set; }
        public int Orden { get; set; }
        public string? Icono { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public bool Visible { get; set; }
        public bool Activo { get; set; }
        public List<int> PerfilesIds { get; set; } = [];
    }

    public class NavNodoCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "NODO";
        public int? PadreId { get; set; }
        public int Orden { get; set; } = 0;
        public string? Icono { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public bool Visible { get; set; } = true;
        public bool Activo { get; set; } = true;
    }

    public class GuardarPerfilesNodoDto
    {
        public List<int> PerfilesIds { get; set; } = [];
    }

    public class MenuNodoDto
    {
        public int NodoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? PadreId { get; set; }
        public int Orden { get; set; }
        public string? Icono { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
    }

    public class NavNodoConAccesoDto
    {
        public int NodoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? PadreId { get; set; }
        public int Orden { get; set; }
        public string? Icono { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public bool TieneAcceso { get; set; }
    }

    public class SetRestriccionesRolDto
    {
        public List<int> NodoIds { get; set; } = [];
    }
}
