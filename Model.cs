using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPG
{
    internal class Model : SceneObject, IDisposable
    {
        VertexNormal[] vertices;
        Triangle[] triangles;
        Vector3 min;
        Vector3 max;
        Vector3 center;
        int width;
        int height;

        int VBO;
        int IBO;
        int VAO;
        float rotspeed = 0.5f;
        float alpha = 0;
        public Model(VertexNormal[] data, Triangle[] tridata) {
            data.CopyTo(vertices = new VertexNormal[data.Length], 0);
            tridata.CopyTo(triangles = new Triangle[tridata.Length], 0);

            min = new Vector3(float.MaxValue);
            max = new Vector3(float.MinValue);

            foreach (VertexNormal v in data)
            {
                if (v.Position.X < min.X) min.X = v.Position.X;
                if (v.Position.Y < min.Y) min.Y = v.Position.Y;
                if (v.Position.Z < min.Z) min.Z = v.Position.Z;
                if (v.Position.X > max.X) max.X = v.Position.X;
                if (v.Position.Y > max.Y) max.Y = v.Position.Y;
                if (v.Position.Z > max.Z) max.Z = v.Position.Z;
            }
            center = (min + max) / 2;

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * VertexNormal.SizeInBytes, vertices, BufferUsageHint.DynamicDraw);

            IBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * 3 * sizeof(int), triangles, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexNormal.SizeInBytes, IntPtr.Zero);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, VertexNormal.SizeInBytes, (IntPtr)Vector3.SizeInBytes);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public void OnResize(int width, int height)
        {
            this.width = width;
            this.height = height;

        }

        public void Update(double time)
        {
        }

        public override void Draw()
        {
            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, 3 * triangles.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
        }


        bool disposed = false;
        public override void Dispose()
        {
            if (disposed) return;

            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(IBO);
            GL.DeleteVertexArray(VAO);

            disposed = true;
        }    
    }
}
