namespace SistemaVisionTech.Common
{
    public class ImportacionLoteResultadoDto
    {
        public int TotalRecibidos { get; set; }
        public int Insertados { get; set; }
        public int Omitidos { get; set; }
        public List<string> Mensajes { get; set; } = new();
    }
}
