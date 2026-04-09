#version 330 core
#define MAX_LIGHTS 16

in vec3 normalWorld;
in vec3 fragmentWorld;
in vec2 texCoord;

out vec4 outColor;

struct Material
{
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

struct Light
{
    vec4 position;
    vec3 color;
    float intensity;
};

uniform Material material;
uniform vec3 cameraPosWorld;

uniform Light lights[MAX_LIGHTS];
uniform int lightCount;

uniform sampler2D diffuseTexture;
uniform int useTexture;

void main()
{
    vec3 baseColor = material.diffuse;

    if (useTexture == 1)
        baseColor *= texture(diffuseTexture, texCoord).rgb;

    vec3 norm = normalize(normalWorld);
    vec3 viewDir = normalize(cameraPosWorld - fragmentWorld);

    vec3 result = vec3(0.0);

    for (int i = 0; i < lightCount; i++)
    {
        Light light = lights[i];

        vec3 toLight = light.position.xyz - fragmentWorld;
        float dist = length(toLight);
        vec3 lightDir = normalize(toLight);

        float attenuation = 1.0;
        if (light.position.w != 0.0)
        {
            float range = 20.0; // effective range of the light
            float distOverRange = dist / range;
            attenuation = 1.0 / (1.0 + (distOverRange) * (distOverRange) * (distOverRange) * (distOverRange) * (distOverRange) * (distOverRange) * (distOverRange)); // simplified attenuation
        }
        else
        {
            lightDir = normalize(-light.position.xyz); // directional light
        }

        float NdotL = max(dot(norm, lightDir), 0.0);

        vec3 ambient = baseColor * light.color * 0.08 * attenuation;
        vec3 diffuse = NdotL * light.color * light.intensity * baseColor * attenuation;

        vec3 specular = vec3(0.0);
        if (NdotL > 0.0)
        {
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
            specular = spec * light.color * light.intensity * material.specular * attenuation;
        }

        result += ambient + diffuse + specular;
    }

    outColor = vec4(result, 1.0);
}