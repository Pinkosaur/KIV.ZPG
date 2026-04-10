using OpenTK.Mathematics;

namespace ZPG
{
    /// <summary>
    /// Normalized viewport region converted to pixel coordinates.
    /// </summary>
    internal class Viewport
    {
        /// <summary>
        /// Lower-left normalized corner in range 0..1.
        /// </summary>
        public Vector2 BottomLeft { get; set; } = Vector2.Zero;

        /// <summary>
        /// Upper-right normalized corner in range 0..1.
        /// </summary>
        public Vector2 TopRight { get; set; } = Vector2.One;

        /// <summary>
        /// Backbuffer client size in pixels.
        /// </summary>
        public Vector2i ClientSize { get; set; }    

        /// <summary>
        /// Converts normalized viewport region to pixel position and size.
        /// </summary>
        /// <returns>Pixel-space viewport position and dimensions.</returns>
        public (Vector2i position, Vector2i size) GetPixelViewport()
        {
            var left = (int)(BottomLeft.X * ClientSize.X);
            var right = (int)(TopRight.X * ClientSize.X);
            var bottom = (int)(BottomLeft.Y * ClientSize.Y);
            var top = (int)(TopRight.Y * ClientSize.Y);
            var width = right - left;
            var height = top - bottom;
            return (new Vector2i(left,bottom), new Vector2i(width,height));
        }

        /// <summary>
        /// Computes aspect ratio from the current pixel viewport.
        /// </summary>
        /// <returns>Viewport width divided by height.</returns>
        public float GetAspectRatio()
        {
            var (_, size) = GetPixelViewport();
            return (float)size.X / size.Y;
        }
    }
}
