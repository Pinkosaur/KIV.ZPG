#version 330 core

in vec3 normalWorld;
in vec3 fragmentWorld;
in vec2 texCoord;

out vec4 outColor;

struct Material {
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

uniform Material material;
uniform vec3 cameraPosWorld;

uniform vec4 lightPosWorld;
uniform vec3 lightColor;
uniform float lightIntensity;

uniform sampler2D diffuseTexture;
uniform int useTexture;

void main()
{
    vec3 baseColor = material.diffuse;

    if (useTexture == 1)
    {
        baseColor *= texture(diffuseTexture, texCoord).rgb;
    }

    vec3 ambient = baseColor * lightColor * 0.08;

    vec3 norm = normalize(normalWorld);
    vec3 lightDir = lightPosWorld.w == 0.0
        ? normalize(-lightPosWorld.xyz)
        : normalize(lightPosWorld.xyz - fragmentWorld);

    float NdotL = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = NdotL * lightColor * lightIntensity * baseColor;

    vec3 specular = vec3(0.0);
    if (NdotL > 0.0)
    {
        vec3 reflectDir = reflect(-lightDir, norm);
        vec3 viewDir = normalize(cameraPosWorld - fragmentWorld);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        specular = spec * lightColor * lightIntensity * material.specular;
    }

    outColor = vec4(ambient + diffuse + specular, 1.0);
}