namespace SistemaVisionTech.Infrastructure.Entities
{
    public class NavRestriccion
    {
        public int Id { get; set; }
        public int NodoId { get; set; }
        public int PerfilId { get; set; }

        public NavNodo? Nodo { get; set; }
        public Perfiles? Perfil { get; set; }
    }
}
