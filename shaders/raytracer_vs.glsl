#version 330
in vec3 vPosition;
void main()
{
	gl_Position = vec4(vPosition.xyz, 1.0);
}