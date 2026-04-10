using OpenTK.Mathematics;

namespace ZPG
{
    /// <summary>
    /// Material coefficients used by the lighting shader.
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Diffuse reflectance color.
        /// </summary>
        public Vector3 diffuse = new Vector3(0.5f, .5f, .5f);

        /// <summary>
        /// Specular reflectance color.
        /// </summary>
        public Vector3 specular = new Vector3(1.2f);

        /// <summary>
        /// Specular highlight exponent.
        /// </summary>
        public float shininess = 32.0f;

        /// <summary>
        /// Uploads this material values to the active shader uniforms.
        /// </summary>
        /// <param name="shader">Shader receiving the material uniforms.</param>
        public void SetUniforms(Shader shader)
        {
            shader.SetUniform("material.diffuse", diffuse);
            shader.SetUniform("material.specular", specular);
            shader.SetUniform("material.shininess", shininess);
        }
    }
}