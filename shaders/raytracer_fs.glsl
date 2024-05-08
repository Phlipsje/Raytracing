#version 330
out vec4 outputColor;
//1: you have to specify how big the arrays are going to be
//2: the max space available for uniform variables is a mere 4096 components/16192 bytes. 
//So i chose the biggest possible values for now but...
//To meet the 5000 primitive criteria we will have to look into SSBO's or texture buffers. And we could probably compress some of the data. 
//Additional bullshit: my integrated gpu does allow 4096 components WHICH IS THE MINIMUM REQUIREMNT OF OPENGL. But my dedicated rtx 3050 gpu fails to compile when using more then 1024 components WTF
//max 5 planes, don't see why we need more
uniform float[65] planes;
//max 90 spheres, 990 floats. OR on dedicated graphics max 10 spheres, 110 floats
uniform float[110] spheres;
//max 150 triangles (barely a simple model) OR on dedicated graphics max 40 triangles, 760 floats
uniform float[760] triangles;
//max 10 lights
uniform float[60] lights;
//the lengths of each of the primitive arrays, in same order as they are declared
uniform float[4] lengths;
//shadow acne prevention margin
const float epsilon = 0.001f;
const vec3 ambiantLight = vec3(0.01f, 0.01f, 0.01f);
const vec3 skyColor = vec3(0.5f, 0.6f, 1.0f);

//Position: first three floats xyz. BottomleftPlane: 4th to 6th float. BottomRightPlane: 7th to 9th float. TopLeftPlane: 10th to 12th float. ScreenSize: last two floats
uniform float[14] camera;

//first three values of vec4 form the normal vector, last value is the t value
vec4 IntersectSphere(vec3 rayOrigin, vec3 rayDirection, vec3 center, float radius)
{
	vec3 v = rayOrigin - center;
	float b = 2 * (rayDirection.x * v.x + rayDirection.y * v.y + rayDirection.z * v.z);
	float c = v.x * v.x + v.y * v.y + v.z * v.z - radius * radius;
	float d = b * b - 4 * c;
	if(d < 0)
		return vec4(-1, -1, -1, -1);
	float rootd = sqrt(d);
	float t1 = (-b + rootd) / 2;
	float t2 = (-b - rootd) / 2;
	if (t1 < 0)
		return vec4(-1, -1, -1, -1);
	if (t2 <= epsilon)
	{
		vec3 normal = t1 * rayDirection - center;
		return vec4(normal, t1);
	}
	vec3 normal = t2 * rayDirection - center;
	return vec4(-1, -1, -1, t2);
}
float IntersectPlane(vec3 rayOrigin, vec3 rayDirection, vec3 center, vec3 normal)
{
	float denominator = dot(rayDirection, normal);
	if (denominator == 0)
		return -1;
	return dot(center - rayOrigin, normal) / denominator;
}
float IntersectTriangle(vec3 rayOrigin, vec3 rayDirection, vec3 pointA, vec3 pointB, vec3 pointC, vec3 normal)
{
	float t = IntersectPlane(rayOrigin, rayDirection, pointA, normal);
	if (t <= 0)
		return -1;
	vec3 intersection = rayOrigin + t * rayDirection;
	bool liesOutsideTriangle = dot(cross(pointB - pointA, intersection - pointA), normal) < 0 || dot(cross(pointC - pointB, intersection - pointB), normal) < 0 || dot(cross(pointA - pointC, intersection - pointC), normal) < 0;
	if(liesOutsideTriangle)
		return -1;
	return t;
}

void main()
{
	float x = gl_FragCoord.x;
	float y = gl_FragCoord.y;
	float width = camera[12];
	float height = camera[13];
	vec3 bottomLeft = vec3(camera[3], camera[4], camera[5]);
	vec3 bottomRight = vec3(camera[6], camera[7], camera[8]);
	vec3 topLeft = vec3(camera[9], camera[10], camera[11]);

	//calculate closest object
	vec3 rayOrigin = vec3(camera[0], camera[1], camera[2]);
	vec3 rayDirection = normalize((bottomLeft + (x/width) * (bottomRight - bottomLeft) + (y/height) * (topLeft - bottomLeft)) - rayOrigin);
	float t = 3.402823466e+38;
	vec3 hitColor = vec3(0, 0, 0);
	vec3 hitNormal = vec3(0, 0, 0);
	for (int i = 0; i < lengths[1]; i += 11)
	{
		vec4 result = IntersectSphere(rayOrigin, rayDirection, vec3(spheres[i], spheres[i+1], spheres[i+2]), spheres[i + 3]);
		if (result.w > 0 && result.w < t)
		{
			t = result.w;
			hitColor = vec3( spheres[i+4], spheres[i+5], spheres[i+6]);
			hitNormal = result.xyz;
		}
	}
	for (int i = 0; i < lengths[0]; i += 13)
	{
		vec3 planeNormal = vec3(planes[i + 3], planes[i + 4], planes[i + 5]);
		float result = IntersectPlane(rayOrigin, rayDirection, vec3(planes[i], planes[i + 1], planes[i + 2]), planeNormal);
		if (result > 0 && result < t)
		{
			t = result;
			hitColor = vec3(planes[i + 6], planes[i + 7], planes[i + 8]);
			hitNormal = planeNormal;
		}
	}
	for (int i = 0; i < lengths[2]; i += 19)
	{
		vec3 triangleNormal = vec3(triangles[i + 9], triangles[i + 10], triangles[i + 11]);
		float result = IntersectTriangle(rayOrigin, rayDirection, vec3(triangles[i], triangles[i + 1], triangles[i + 2]),
			vec3(triangles[i + 3], triangles[i + 4], triangles[i + 5]), 
			vec3(triangles[i + 6], triangles[i + 7], triangles[i + 8]), 
			triangleNormal);
		if (result > 0 && result < t)
		{
			t = result;
			hitColor = vec3(triangles[i + 12], triangles[i + 13], triangles[i + 14]);
			hitNormal = triangleNormal;
		}
	}
	//if nothing got hit, return the color of the sky
	if (t == 3.402823466e+38)
	{
		outputColor = vec4(skyColor, 1.0f);
		return;
	}

	//calculate illumination of point on closest object
	float illumination = 0.0f;
	float lightIntensity = 1.0f / (lengths[3] / 7.0f);
	vec3 hitPos = rayOrigin + t * rayDirection;
	for (int i = 0; i < lengths[3]; i += 6)
	{
		vec3 lightPos = vec3(lights[i], lights[i + 1], lights[i + 2]);
		float distanceToLight = length(lightPos - hitPos);
		vec3 shadowRayOrigin = hitPos;
		vec3 shadowRayDirection = normalize(lightPos - hitPos);
		float shadowRayT = -1;
		for (int i = 0; i < lengths[1]; i += 11)
		{
			float result = IntersectSphere(shadowRayOrigin, shadowRayDirection, vec3(spheres[i], spheres[i + 1], spheres[i + 2]), spheres[i + 3]).w;
			if (result > epsilon && result < distanceToLight)
			{
				shadowRayT = result;
				break;
			}
		}
		if (shadowRayT < 0)
		{
			for (int i = 0; i < lengths[0]; i += 13)
			{
				float result = IntersectPlane(shadowRayOrigin, shadowRayDirection, vec3(planes[i], planes[i + 1], planes[i + 2]), vec3(planes[i + 3], planes[i + 4], planes[i + 5]));
				if (result > epsilon && result < distanceToLight)
				{
					shadowRayT = result;
					break;
				}
			}
			if (shadowRayT < 0)
			{
				for (int i = 0; i < lengths[2]; i += 19)
				{
					float result = IntersectTriangle(shadowRayOrigin, shadowRayDirection, vec3(triangles[i], triangles[i + 1], triangles[i + 2]),
						vec3(triangles[i + 3], triangles[i + 4], triangles[i + 5]),
						vec3(triangles[i + 6], triangles[i + 7], triangles[i + 8]),
						vec3(triangles[i + 9], triangles[i + 10], triangles[i + 11]));
					if (result > epsilon && result < distanceToLight)
					{
						shadowRayT = result;
						break;
					}
				}
			}
		}
		if (shadowRayT < 0)
			illumination += lightIntensity;
	}
	hitColor = illumination * hitColor;

	//output result of calculations
	outputColor = vec4(hitColor, 1.0f);
}