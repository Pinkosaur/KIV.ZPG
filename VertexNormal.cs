using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZPG
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexNormal
    {
        public Vector3 Position;
        public Vector3 Normal;

        public VertexNormal(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        public static int SizeInBytes => Marshal.SizeOf<VertexNormal>();
    }
}
