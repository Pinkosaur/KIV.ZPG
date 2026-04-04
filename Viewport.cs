using OpenTK.Mathematics;

namespace ZPG
{
    internal class Viewport
    {
        public Vector2 BottomLeft { get; set; } = Vector2.Zero;
        public Vector2 TopRight { get; set; } = Vector2.One;

        public Vector2i ClientSize { get; set; }    

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

        public float GetAspectRatio()
        {
            var (_, size) = GetPixelViewport();
            return (float)size.X / size.Y;
        }

    }
}
