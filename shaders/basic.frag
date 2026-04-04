#version 330 core

in vec3 normalWorld;
in vec3 fragmentWorld;
out vec4 outColor;      // vystup musi byt vec4

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

void main() {
    //vec3 debugNormal = vNormal * 0.5 + 0.5;
    //outColor = vec4(debugNormal, 1.0);

    vec3 ambient = material.diffuse * lightColor * 0.08;

    vec3 norm = normalize(normalWorld);
    vec3 lightDir = lightPosWorld.w == 0.0
        ? normalize(-lightPosWorld.xyz)
        : normalize(lightPosWorld.xyz - fragmentWorld);


    float NdotL = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = NdotL * lightColor * lightIntensity * material.diffuse;

    vec3 specular = vec3(0.0);
    if (NdotL > 0.0) {
        vec3 reflectDir = reflect(-lightDir.xyz, norm);
        vec3 viewDir = normalize(cameraPosWorld - fragmentWorld);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        specular = spec * lightColor * lightIntensity * material.specular;
    }

    vec3 final = ambient + diffuse + specular;
    outColor = vec4(final, 1.0);
}
