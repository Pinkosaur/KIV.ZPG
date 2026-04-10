using System.Runtime.InteropServices;

namespace ZPG
{
    /// <summary>
    /// Triangle index triplet into a vertex array.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        /// <summary>
        /// Index of the first vertex.
        /// </summary>
        public int i0;

        /// <summary>
        /// Index of the second vertex.
        /// </summary>
        public int i1;

        /// <summary>
        /// Index of the third vertex.
        /// </summary>
        public int i2;
    }
}
