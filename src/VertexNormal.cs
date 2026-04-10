using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace ZPG
{
    /// <summary>
    /// Vertex layout containing position and normal vectors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexNormal
    {
        /// <summary>
        /// Vertex position in model space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Vertex normal in model space.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Initializes a vertex with position and normal data.
        /// </summary>
        /// <param name="position">Model-space position.</param>
        /// <param name="normal">Model-space normal.</param>
        public VertexNormal(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        /// <summary>
        /// Gets the struct size in bytes for GPU buffer uploads.
        /// </summary>
        public static int SizeInBytes => Marshal.SizeOf<VertexNormal>();
    }
}
