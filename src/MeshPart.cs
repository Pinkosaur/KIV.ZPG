namespace ZPG
{
    /// <summary>
    /// A named sub-mesh with its own triangles and rendering resources.
    /// </summary>
    public class MeshPart
    {
        /// <summary>
        /// Human-readable mesh part name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Triangle index triplets belonging to this part.
        /// </summary>
        public Triangle[] Triangles = Array.Empty<Triangle>();

        /// <summary>
        /// Material used when drawing this part.
        /// </summary>
        public Material Material { get; set; } = new Material();

        /// <summary>
        /// Optional diffuse texture bound while rendering this part.
        /// </summary>
        public Texture2D? Texture { get; set; }
    }
}