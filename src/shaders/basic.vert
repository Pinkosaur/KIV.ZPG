#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

out vec3 normalWorld;
out vec3 fragmentWorld;
out vec2 texCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float time;

void main()
{
    vec4 worldPos = model * vec4(position, 1.0);
    gl_Position = projection * view * worldPos;

    fragmentWorld = worldPos.xyz;
    normalWorld = mat3(transpose(inverse(model))) * normal;

    // Terrain-friendly planar UVs
    texCoord = fragmentWorld.xz * 0.05;
}