using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;

namespace ZPG
{
    public static class ObjLoader
    {
        public static (VertexNormal[], Triangle[]) Load(string filename)
        {
            var lines = File.ReadAllLines(filename);
            List<VertexNormal> vertices = new List<VertexNormal>();
            List<Triangle> triangles = new List<Triangle>();
            bool hasNormals = false;
            int normalIndex = 0;
            foreach (var line in lines)
            {
                if (line.StartsWith("v "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    Vector3 position = Vector3.Zero;
                    Vector3 normal = Vector3.Zero;
                    if (parts.Length >= 4)
                    {
                        if (float.TryParse(parts[1], InvariantCulture, out float x) && float.TryParse(parts[2], InvariantCulture, out float y) && float.TryParse(parts[3], InvariantCulture, out float z))
                        {
                            position = new Vector3(x, y, z);
                        }
                        /*if (parts.Length == 7)
                            if (float.TryParse(parts[4], InvariantCulture, out float r) && float.TryParse(parts[5], InvariantCulture, out float g) && float.TryParse(parts[6], InvariantCulture, out float b))
                            {                             
                                color = new Vector3(r, g, b);
                            }*/
                        vertices.Add(new VertexNormal(position, normal));
                    }
                }/*
                else if (line.StartsWith("vn "))
                {
                    hasNormals = true;
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        if (float.TryParse(parts[1], InvariantCulture, out float x) && float.TryParse(parts[2], InvariantCulture, out float y) && float.TryParse(parts[3], InvariantCulture, out float z))
                        {
                            VertexNormal v = vertices[normalIndex];
                            v.Normal = new Vector3(x, y, z).Normalized();
                            vertices[normalIndex] = v;
                        }
                        normalIndex++;
                    }
                }*/
                else if (line.StartsWith("f "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // OBJ face format: f v1 v2 v3 ... vn
                    // Each token may be v, v/vt, v//vn, or v/vt/vn
                    if (parts.Length >= 4)
                    {
                        int[] indices = new int[parts.Length - 1];

                        for (int i = 1; i < parts.Length; i++)
                        {
                            string s = parts[i].Split('/')[0];
                            if (!int.TryParse(s, out indices[i - 1]))
                                throw new InvalidDataException($"Invalid face index in line: {line}");

                            indices[i - 1] -= 1; // OBJ indices are 1-based
                        }

                        // Triangle fan: (0,1,2), (0,2,3), (0,3,4), ...
                        for (int i = 1; i < indices.Length - 1; i++)
                        {
                            triangles.Add(new Triangle
                            {
                                i0 = indices[0],
                                i1 = indices[i],
                                i2 = indices[i + 1]
                            });
                        }
                    }
                }
            }
            var verts = vertices.ToArray();
            var tris = triangles.ToArray();

            if (!hasNormals) ComputeNormals(verts, tris);

            return (verts, tris);
        }

        public static void ComputeNormals(VertexNormal[] vertices, Triangle[] triangles)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                Triangle t = triangles[i];
                int v0 = t.i0;
                int v1 = t.i1;
                int v2 = t.i2;

                Vector3 a = vertices[v1].Position - vertices[v0].Position;
                Vector3 b = vertices[v2].Position - vertices[v0].Position;
                Vector3 normal = Vector3.Cross(a, b).Normalized();

                vertices[v0].Normal += normal;
                vertices[v1].Normal += normal;
                vertices[v2].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Normal.Normalize();
            }
        }
    }
}
