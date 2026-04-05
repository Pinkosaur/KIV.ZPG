using System.IO.Compression;
using OpenTK.Mathematics;

namespace ZPG
{
    public static class TerrainLoader
    {
        public static (VertexNormal[], MeshPart[], Vector4[], int, int) Load(string filename)
        {
            using var fs = File.OpenRead(filename);
            using var br = new BinaryReader(fs);

            // PNG signature
            byte[] signature = br.ReadBytes(8);
            byte[] expectedSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (signature.Length != 8 || !signature.SequenceEqual(expectedSignature))
                throw new InvalidDataException("Not a valid PNG file.");

            int width = 0;
            int height = 0;
            byte colorType = 0;
            byte bitDepth = 0;

            using var idatBuffer = new MemoryStream();

            while (true)
            {
                uint length = ReadUInt32BigEndian(br);
                string chunkType = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4));
                byte[] chunkData = br.ReadBytes((int)length);
                ReadUInt32BigEndian(br); // CRC, ignored here

                switch (chunkType)
                {
                    case "IHDR":
                        width = (int)ReadUInt32BigEndian(chunkData, 0);
                        height = (int)ReadUInt32BigEndian(chunkData, 4);
                        bitDepth = chunkData[8];
                        colorType = chunkData[9];
                        break;

                    case "IDAT":
                        idatBuffer.Write(chunkData, 0, chunkData.Length);
                        break;

                    case "IEND":
                        goto chunksDone;
                }
            }

        chunksDone:
            if (bitDepth != 8)
                throw new NotSupportedException($"Only 8-bit PNGs are supported, found bit depth {bitDepth}.");

            if (colorType != 6)
                throw new NotSupportedException($"Only RGBA PNGs are supported, found color type {colorType}.");

            byte[] decompressed;
            idatBuffer.Position = 0;
            using (var zlib = new ZLibStream(idatBuffer, CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                zlib.CopyTo(outMs);
                decompressed = outMs.ToArray();
            }

            int bytesPerPixel = 4; // RGBA
            int stride = width * bytesPerPixel;
            int expectedSize = height * (1 + stride); // 1 filter byte per row

            if (decompressed.Length < expectedSize)
                throw new InvalidDataException("Decompressed PNG data is smaller than expected.");

            byte[] pixelData = new byte[height * stride];
            byte[] prevRow = new byte[stride];
            byte[] curRow = new byte[stride];

            int src = 0;
            for (int y = 0; y < height; y++)
            {
                byte filterType = decompressed[src++];
                Array.Copy(decompressed, src, curRow, 0, stride);
                src += stride;

                ApplyPngFilter(filterType, curRow, prevRow, bytesPerPixel);

                Buffer.BlockCopy(curRow, 0, pixelData, y * stride, stride);

                // swap buffers
                var temp = prevRow;
                prevRow = curRow;
                curRow = temp;
            }

            List<VertexNormal> verts = new List<VertexNormal>(width * height);
            List<Triangle> tris = new List<Triangle>();
            List<Vector4> objects = new List<Vector4>(); // coordinates + type

            // PNG rows are top-to-bottom; this keeps the world layout centered.
            for (int y = 0; y < height; y++)
            {
                float z = (height / 2f - 0.5f) - y;

                for (int x = 0; x < width; x++)
                {
                    float worldX = (width / 2f - 0.5f) - x;

                    int p = (y * width + x) * 4;
                    byte r = pixelData[p + 0];
                    byte g = pixelData[p + 1];
                    // byte b = pixelData[p + 2];
                    // byte a = pixelData[p + 3];

                    //Console.WriteLine($"Pixel ({x}, {y}): R={r} G={g}");

                    float heightY = r * .05f;

                    //Console.WriteLine($"Height: {heightY}");

                    verts.Add(new VertexNormal(new Vector3(worldX / 2f, heightY, z / 2f), Vector3.Zero));

                    if (g != 0)
                    {
                        // Console.WriteLine($"Object at ({x}, {y}): type={g}");
                        objects.Add(new Vector4(worldX / 2f, heightY, z / 2f, g));
                    }
                }
            }

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int idx = j * width + i;

                    if (i < width - 1 && j < height - 1)
                    {
                        tris.Add(new Triangle() { i0 = idx, i1 = idx + width, i2 = idx + width + 1 });
                        tris.Add(new Triangle() { i0 = idx, i1 = idx + width + 1, i2 = idx + 1 });
                    }
                }
            }

            VertexNormal[] vertsArr = verts.ToArray();
            Triangle[] trisArr = tris.ToArray();
            MeshPart[] meshParts = new MeshPart[] { new MeshPart() { Triangles = trisArr } };

            ObjLoader.ComputeNormals(vertsArr, meshParts);

            return (vertsArr, meshParts, objects.ToArray(), width, height);
        }

        private static void ApplyPngFilter(byte filterType, byte[] curRow, byte[] prevRow, int bytesPerPixel)
        {
            switch (filterType)
            {
                case 0: // None
                    return;

                case 1: // Sub
                    for (int i = bytesPerPixel; i < curRow.Length; i++)
                        curRow[i] = (byte)(curRow[i] + curRow[i - bytesPerPixel]);
                    return;

                case 2: // Up
                    for (int i = 0; i < curRow.Length; i++)
                        curRow[i] = (byte)(curRow[i] + prevRow[i]);
                    return;

                case 3: // Average
                    for (int i = 0; i < curRow.Length; i++)
                    {
                        int left = i >= bytesPerPixel ? curRow[i - bytesPerPixel] : 0;
                        int up = prevRow[i];
                        curRow[i] = (byte)(curRow[i] + ((left + up) >> 1));
                    }
                    return;

                case 4: // Paeth
                    for (int i = 0; i < curRow.Length; i++)
                    {
                        int a = i >= bytesPerPixel ? curRow[i - bytesPerPixel] : 0; // left
                        int b = prevRow[i];                                          // up
                        int c = i >= bytesPerPixel ? prevRow[i - bytesPerPixel] : 0; // up-left
                        curRow[i] = (byte)(curRow[i] + PaethPredictor(a, b, c));
                    }
                    return;

                default:
                    throw new InvalidDataException($"Unknown PNG filter type {filterType}.");
            }
        }

        private static int PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        private static uint ReadUInt32BigEndian(BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(4);
            if (bytes.Length != 4)
                throw new EndOfStreamException();
            return ReadUInt32BigEndian(bytes, 0);
        }

        private static uint ReadUInt32BigEndian(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset + 0] << 24) |
                   ((uint)buffer[offset + 1] << 16) |
                   ((uint)buffer[offset + 2] << 8) |
                   buffer[offset + 3];
        }
    }
}