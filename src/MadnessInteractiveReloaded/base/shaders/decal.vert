#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texcoord;
layout(location = 2) in vec3 in_normal;
layout(location = 3) in vec4 color;

layout(location = 4) in mat4 decalModel;
layout(location = 8) in vec4 decalColor;
layout(location = 9) in float decalIndex;

out vec2 uv;
out vec4 vertexColor;
out float frame;
out vec2 maskPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
   uv = texcoord;
   vertexColor = decalColor;
   frame = decalIndex;
   vec4 wp = (model * decalModel) * (vec4(position, 1.0));
   gl_Position = projection * view * wp;
   maskPos = (gl_Position.xy / gl_Position.w + 1.0) / 2.0;
}