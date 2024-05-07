#version 330
out vec4 outputColor;
uniform float[] planes;
uniform float[] spheres;
uniform float[] triangles;
uniform float[3] lengths;

//Position: first three floats xyz. BottomleftPlane: 4th to 6th float. BottomRightPlane: 7th to 9th float. TopLeftPlane: 10th to 12th float. ScreenSize: last two floats
uniform float[14] camera;

void main()
{
	float x = gl_FragCoord.x;
	float y = gl_FragCoord.y;
	float width = camera[12];
	float height = camera[13];
	vec3 bottomLeft = vec3(camera[3], camera[4], camera[5]);
	vec3 bottomRight = vec3(camera[6], camera[7], camera[8]);
	vec3 topLeft = vec3(camera[9], camera[10], camera[11]);

	vec3 rayPos = vec3(camera[0], camera[1], camera[2]);
	vec3 rayDir = (bottomLeft + (x/width) * (bottomRight - bottomLeft) + (y/height) * (topLeft - bottomLeft)) - rayPos;
	for (int i = 0; i < lengths[2]; i += 11)
	{

	}

	outputColor = vec4(rayDir.x, rayDir.y, rayDir.z, 1.0f);
}
void IntersectSphere()
{

}