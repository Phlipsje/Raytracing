#version 330
in vec3 vPosition;
in vec3 vColor;
out vec4 color;
void main()
{
	gl_Position = vec4( vPosition, 0.0f);
	color = vec4(vColor, 1.0f);
}