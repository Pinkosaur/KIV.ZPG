using OpenTK.Mathematics;
using static System.Globalization.CultureInfo;

namespace ZPG
{
    public static class ObjLoader
    {
        public static (VertexNormal[], MeshPart[]) Load(string filename)
        {
            var lines = File.ReadAllLines(filename);
            List<VertexNormal> vertices = new List<VertexNormal>();
            List<MeshPart> meshParts = new List<MeshPart>();

            MeshPart currentMeshPart = new MeshPart();
            List<Triangle> currentTriangles = new List<Triangle>();

            foreach (var line in lines)
            {
                if (line.StartsWith("f "))
                {
                    var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (lineParts.Length >= 4)
                    {
                        int[] indices = new int[lineParts.Length - 1];

                        for (int i = 1; i < lineParts.Length; i++)
                        {
                            string s = lineParts[i].Split('/')[0];
                            if (!int.TryParse(s, out indices[i - 1]))
                                throw new InvalidDataException($"Invalid face index in line: {line}");

                            indices[i - 1] -= 1;
                        }

                        for (int i = 1; i < indices.Length - 1; i++)
                        {
                            currentTriangles.Add(new Triangle
                            {
                                i0 = indices[0],
                                i1 = indices[i],
                                i2 = indices[i + 1]
                            });
                        }
                    }
                }
                else if (line.StartsWith("v "))
                {
                    var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (lineParts.Length >= 4 &&
                        float.TryParse(lineParts[1], InvariantCulture, out float x) &&
                        float.TryParse(lineParts[2], InvariantCulture, out float y) &&
                        float.TryParse(lineParts[3], InvariantCulture, out float z))
                    {
                        vertices.Add(new VertexNormal(new Vector3(x, y, z), Vector3.Zero));
                    }
                }
                else if (line.StartsWith("o "))
                {
                    if (currentTriangles.Count > 0)
                    {
                        currentMeshPart.Triangles = currentTriangles.ToArray();
                        meshParts.Add(currentMeshPart);
                        currentMeshPart = new MeshPart();
                    }
                }
            }

            if (currentTriangles.Count > 0)
            {
                currentMeshPart.Triangles = currentTriangles.ToArray();
                meshParts.Add(currentMeshPart);
            }

            var verts = vertices.ToArray();
            var parts = meshParts.ToArray();

            ComputeNormals(verts, parts);

            return (verts, parts);
        }

        public static void ComputeNormals(VertexNormal[] vertices, MeshPart[] parts)
        {
            foreach (var part in parts)
            {
                foreach (var t in part.Triangles)
                {
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
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = vertices[i].Normal.Normalized();
        }
    }
}