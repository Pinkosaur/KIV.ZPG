using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.IO;

namespace ZPG
{
    /// <summary>
    /// OpenGL 2D texture wrapper with image loading and lifecycle management.
    /// </summary>
    public sealed class Texture2D : IDisposable
    {
        /// <summary>
        /// OpenGL texture object handle.
        /// </summary>
        public int Handle { get; }

        /// <summary>
        /// Loads an image from disk and uploads it as a 2D texture.
        /// </summary>
        /// <param name="filename">Path to the texture image file.</param>
        public Texture2D(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"Texture not found: {filename}");

            Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Handle);

            using var stream = File.OpenRead(filename);

            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                image.Width,
                image.Height,
                0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                image.Data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Binds this texture to the requested texture unit.
        /// </summary>
        /// <param name="unit">Target texture unit.</param>
        public void Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        /// <summary>
        /// Deletes the underlying OpenGL texture object.
        /// </summary>
        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }
    }
}