using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace ZPG
{
    internal class Model : SceneObject, IDisposable
    {
        private VertexNormal[] vertices;
        public MeshPart[] meshParts;

        private int[] indices;              // flattened index buffer
        private int[] partIndexOffsets;     // first index for each part
        private int[] partIndexCounts;      // index count for each part

        public Texture2D? Texture { get; set; }

        private Vector3 min;
        private Vector3 max;
        private Vector3 center;
        private int width;
        private int height;

        private int VBO;
        private int IBO;
        private int VAO;

        private bool disposed = false;

        public Model(VertexNormal[] data, MeshPart[] parts)
        {
            data.CopyTo(vertices = new VertexNormal[data.Length], 0);
            parts.CopyTo(meshParts = new MeshPart[parts.Length], 0);

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

            // Flatten all triangle indices into one index buffer,
            // while remembering each part's offset/count.
            var indexList = new List<int>();
            partIndexOffsets = new int[meshParts.Length];
            partIndexCounts = new int[meshParts.Length];

            for (int p = 0; p < meshParts.Length; p++)
            {
                partIndexOffsets[p] = indexList.Count;

                foreach (var tri in meshParts[p].Triangles)
                {
                    indexList.Add(tri.i0);
                    indexList.Add(tri.i1);
                    indexList.Add(tri.i2);
                }

                partIndexCounts[p] = indexList.Count - partIndexOffsets[p];
            }

            indices = indexList.ToArray();

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Length * VertexNormal.SizeInBytes,
                vertices,
                BufferUsageHint.DynamicDraw);

            IBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                indices.Length * sizeof(int),
                indices,
                BufferUsageHint.StaticDraw);

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

        public void Draw(Shader shader)
        {
            GL.BindVertexArray(VAO);

            for (int p = 0; p < meshParts.Length; p++)
            {
                var part = meshParts[p];

                part.Texture?.Bind(TextureUnit.Texture0);
                part.Material.SetUniforms(shader);

                GL.DrawElements(
                    PrimitiveType.Triangles,
                    partIndexCounts[p],
                    DrawElementsType.UnsignedInt,
                    (IntPtr)(partIndexOffsets[p] * sizeof(int)));
            }

            GL.BindVertexArray(0);
        }

        public override void Dispose()
        {
            if (disposed) return;

            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(IBO);
            GL.DeleteVertexArray(VAO);
            Texture?.Dispose();

            disposed = true;
        }
    }
}