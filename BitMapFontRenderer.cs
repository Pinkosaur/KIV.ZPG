using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace ZPG
{
    public sealed class BitmapFontRenderer : IDisposable
    {
        private Font _font;
        private int _textureHandle;
        private int _vao, _vbo, _ebo;
        private int _uiShader;
        private float _screenWidth;
        private float _screenHeight;
        private int _texWidth;
        private int _texHeight;
        private bool _disposed;

        public BitmapFontRenderer(string fontPath, int fontSize, float screenWidth, float screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;

            // Load TTF font
            using var stream = File.OpenRead(fontPath);
            var collection = new FontCollection();
            var family = collection.Add(stream);
            _font = family.CreateFont(fontSize, FontStyle.Regular);

            // Create minimal UI shader
            _uiShader = CreateUIShader();

            // Create OpenGL objects
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();
            _textureHandle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, _textureHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Configure VAO layout: Pos(2) + Color(3) + TexCoord(2) = 7 floats
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 7 * sizeof(float), 5 * sizeof(float));
        }

        public void UpdateScreenSize(float w, float h)
        {
            _screenWidth = w;
            _screenHeight = h;
        }

        public void DrawText(string text, float x, float y, Vector3 color)
        {
            if (string.IsNullOrEmpty(text)) return;

            // 1. Measure text dimensions
            var size = TextMeasurer.MeasureSize(text, new TextOptions(_font));
            int w = (int)Math.Ceiling(size.Width) + 1;
            int h = (int)Math.Ceiling(size.Height) + 1;
            if (w == 0 || h == 0) return;

            // 2. Render to in-memory bitmap
            using var img = new Image<Rgba32>(w, h);
            img.Mutate(ctx => ctx.DrawText(text, _font, Color.FromRgba((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)255), new PointF(0, 0)));

            // 3. Upload to GPU (allocate if size changed, otherwise update)
            GL.BindTexture(TextureTarget.Texture2D, _textureHandle);
            unsafe
            {
                if (img.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory))
                {
                    using var handle = pixelMemory.Pin();
                    var data = (IntPtr)handle.Pointer;

                    if (w != _texWidth || h != _texHeight)
                    {
                        _texWidth = w;
                        _texHeight = h;
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    }
                    else
                    {
                        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    }
                }
                else
                {
                    var pixels = new Rgba32[w * h];
                    img.CopyPixelDataTo(pixels);

                    fixed (Rgba32* pPixels = pixels)
                    {
                        var data = (IntPtr)pPixels;

                        if (w != _texWidth || h != _texHeight)
                        {
                            _texWidth = w;
                            _texHeight = h;
                            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        }
                        else
                        {
                            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        }
                    }
                }
            }

            // 4. Convert screen pixels to NDC (-1 to 1)
            float left = (x / _screenWidth) * 2f - 1f;
            float top = 1f - (y / _screenHeight) * 2f;
            float right = left + ((float)w / _screenWidth) * 2f;
            float bottom = top - ((float)h / _screenHeight) * 2f;

            // 5. Vertex data
            float[] vertices = {
                left, top, color.X, color.Y, color.Z, 0.0f, 0.0f,
                right, top, color.X, color.Y, color.Z, 1.0f, 0.0f,
                right, bottom, color.X, color.Y, color.Z, 1.0f, 1.0f,
                left, bottom, color.X, color.Y, color.Z, 0.0f, 1.0f
            };

            uint[] indices = { 0, 1, 2, 2, 3, 0 };

            // 6. Upload buffers & draw
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            unsafe { fixed (float* p = vertices) GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), (IntPtr)p, BufferUsageHint.StreamDraw); }
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            unsafe { fixed (uint* p = indices) GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), (IntPtr)p, BufferUsageHint.StreamDraw); }

            GL.UseProgram(_uiShader);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureHandle);
            GL.Uniform1(GL.GetUniformLocation(_uiShader, "uTexture"), 0);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.DepthTest);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            // Restore state
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private int CreateUIShader()
        {
            const string vs = @"#version 330 core
                layout(location = 0) in vec2 aPos;
                layout(location = 1) in vec3 aColor;
                layout(location = 2) in vec2 aTexCoord;
                out vec3 vColor; out vec2 vTexCoord;
                void main() { gl_Position = vec4(aPos, 0.0, 1.0); vColor = aColor; vTexCoord = aTexCoord; }";
            
            const string fs = @"#version 330 core
                in vec3 vColor; in vec2 vTexCoord; out vec4 FragColor;
                uniform sampler2D uTexture;
                void main() { FragColor = texture(uTexture, vTexCoord) * vec4(vColor, 1.0); }";

            int vsId = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vsId, vs); GL.CompileShader(vsId);
            int fsId = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fsId, fs); GL.CompileShader(fsId);

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vsId); GL.AttachShader(prog, fsId); GL.LinkProgram(prog);
            GL.DeleteShader(vsId); GL.DeleteShader(fsId);
            return prog;
        }

        public void Dispose()
        {
            if (_disposed) return;
            GL.DeleteProgram(_uiShader);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteTexture(_textureHandle);
            _disposed = true;
        }
    }
}