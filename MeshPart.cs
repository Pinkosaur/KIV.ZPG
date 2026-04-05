namespace ZPG
{
    public class MeshPart
    {
        public Material material { get; set; } = new Material();
        public Triangle[] Triangles = Array.Empty<Triangle>();
        public Texture2D? Texture { get; set; }
    }
}