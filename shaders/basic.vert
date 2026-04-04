#version 330 core                      // verze, muze byt vyssi

layout(location = 0) in vec3 position; // pozice bude na lokaci 0
layout(location = 1) in vec3 normal;    // normal na lokaci 1

out vec3 normalWorld;
out vec3 fragmentWorld;

uniform mat4 model;                    // konstanty predane z programu 
uniform mat4 view;
uniform mat4 projection;
uniform float time;

void main() {
    vec3 pos = position;
    gl_Position = projection * view * model * vec4(pos, 1.0);
    fragmentWorld = vec3(model * vec4(pos, 1.0));
    normalWorld = mat3(transpose(inverse(model))) * normal;
}
