namespace ZPG
{
    public class MeshPart
    {
        public string Name { get; set; } = ""; //debug
        public Material Material { get; set; } = new Material();
        public Triangle[] Triangles = Array.Empty<Triangle>();
        public Texture2D? Texture { get; set; }
    }
}