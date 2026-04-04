using OpenTK.Mathematics;

namespace ZPG
{
    public class Material
    {
        public Vector3 diffuse = new Vector3(0.5f, .5f, .5f);
        public Vector3 specular = new Vector3(1.2f);
        public float shininess = 32.0f;

        public void SetUniforms(Shader shader)
        {
            shader.SetUniform("material.diffuse", diffuse);
            shader.SetUniform("material.specular", specular);
            shader.SetUniform("material.shininess", shininess);
        }
    }
}