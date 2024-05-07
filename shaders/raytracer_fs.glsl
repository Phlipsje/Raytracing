#version 330
in vec4 color;
out vec4 outputColor;
uniform float test;
void main()
{
	outputColor = vec4(1.0f, test, color.x, 1.0f);
}