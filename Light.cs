using OpenTK.Mathematics;

namespace ZPG
{
    public class Light
    {
        public Vector4 Position;
        public Vector3 BaseColor;
        public Vector3 Color;
        public float Intensity;

        public static Light CreateDirectional(Vector3 direction, Vector3 color, float intensity)
        {
            return new Light() { Position = new Vector4(direction, 0), BaseColor = color, Color = color, Intensity = intensity };
        }

        public static Light CreatePoint(Vector3 direction, Vector3 color, float intensity)
        {
            return new Light() { Position = new Vector4(direction, 1), BaseColor = color, Color = color, Intensity = intensity };
        }

        
    }
}