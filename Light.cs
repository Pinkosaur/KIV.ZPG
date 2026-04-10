using OpenTK.Mathematics;

namespace ZPG
{
    /// <summary>
    /// Represents a directional or point light used by the scene shader.
    /// </summary>
    public class Light
    {
        /// <summary>
        /// Homogeneous light position. W = 0 for directional, W = 1 for point lights.
        /// </summary>
        public Vector4 Position;

        /// <summary>
        /// The original light color before day/night modulation.
        /// </summary>
        public Vector3 BaseColor;

        /// <summary>
        /// The effective light color currently used for rendering.
        /// </summary>
        public Vector3 Color;

        /// <summary>
        /// Scalar light intensity multiplier.
        /// </summary>
        public float Intensity;

        /// <summary>
        /// Creates a directional light.
        /// </summary>
        /// <param name="direction">Direction vector of incoming light.</param>
        /// <param name="color">Base light color.</param>
        /// <param name="intensity">Initial light intensity.</param>
        /// <returns>A configured directional light instance.</returns>
        public static Light CreateDirectional(Vector3 direction, Vector3 color, float intensity)
        {
            return new Light() { Position = new Vector4(direction, 0), BaseColor = color, Color = color, Intensity = intensity };
        }

        /// <summary>
        /// Creates a point light.
        /// </summary>
        /// <param name="direction">World position of the light source.</param>
        /// <param name="color">Base light color.</param>
        /// <param name="intensity">Initial light intensity.</param>
        /// <returns>A configured point light instance.</returns>
        public static Light CreatePoint(Vector3 direction, Vector3 color, float intensity)
        {
            return new Light() { Position = new Vector4(direction, 1), BaseColor = color, Color = color, Intensity = intensity };
        }
    }
}