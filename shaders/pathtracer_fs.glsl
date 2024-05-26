#version 430
//override default fragment shader precision
precision highp float;
precision highp int;

//settings
//shadow acne prevention margin
const float epsilon = 0.001f;
const vec3 ambiantLight = vec3(0.0f, 0.0f, 0.0f);
const vec3 skyColor = vec3(0.5f, 0.6f, 1.0f);
const int maxBounces = 10;
const float pi = 3.1415927;

//this will be squared, so setting this value to 3 will result in 9 samples. This value will make the ray tracer n^2 times slower btw
const int antiAliasingSamplesRoot = 3;
const int pathTracingBounces = 2;

//Value increments so you get a different random number each time you pull one within the same frame
int randomsUsed = 0;

out vec4 outputColor;
//max 50 lights
uniform float[300] lights;
//max 50 triangleLights;
uniform int[50] triangleLightPointers;
//max 50 sphereLights;
uniform int[50] sphereLightPointers;
//max 50 planeLights
uniform int[50] planeLightPointers;
//the lengths of each of the primitive arrays, in same order as they are declared
uniform int[4] lengths;
//Position: first three floats xyz. BottomleftPlane: 4th to 6th float. BottomRightPlane: 7th to 9th float. TopLeftPlane: 10th to 12th float. ScreenSize: last two floats
uniform float[14] camera;
//The time since the program started
uniform float time;
//The amount of iterations were spent without moving
uniform int iterations;  

struct Sphere {
	vec3 center;
	float radius;
	vec3 diffuseColor;
	bool isPureSpecular;
	vec3 specularColor;
	float specularity;
	vec3 emissionColor;
};
struct Plane {
	vec3 position;
	vec3 normal;
	vec3 diffuseColor;
	bool isPureSpecular;
	vec3 specularColor;
	float specularity;
	vec3 emissionColor;
};
struct Triangle {
	vec3 pointA;
	vec3 pointB;
	vec3 pointC;
	vec3 normal;
	vec3 diffuseColor;
	bool isPureSpecular;
	vec3 specularColor;
	float specularity;
	vec3 emissionColor;
};
//SSBO for sphere primitives
layout(binding = 0, std430) readonly buffer ssbo0
{
	Sphere spheres[];
};
//SSBO for plane primitives
layout(binding = 1, std430) readonly buffer ssbo1
{
	Plane planes[];
};
//SSBO for triangle primitives
layout(binding = 2, std430) readonly buffer ssbo2
{
	Triangle triangles[];
};
layout(binding = 3, std430) restrict buffer ssbo3
{
	vec4 lastScreen[];
};
//alternative functions for generating pseudo random numbers, might be good enough, needs testing with pathfinding implementation. 
//These function are less random and seem to form quite a lot of patterns after some time but they are computationally way cheaper.
vec2 hash22(vec2 p)
{
	vec3 p3 = fract(vec3(p.xyx) * vec3(.1031, .1030, .0973));
	p3 += dot(p3, p3.yzx+33.33);
	return fract((p3.xx+p3.yz)*p3.zy);

}
vec3 hash32(vec2 p)
{
	vec3 p3 = fract(vec3(p.xyx) * vec3(.1031, .1030, .0973));
	p3 += dot(p3, p3.yxz+33.33);
	return fract((p3.xxy+p3.yzz)*p3.zyx);
}
//random number generator(actually encryption algorithm found)
//after testing seems to be fairly random, with some small patterns appearing after some time, but nothing major
//setup
vec2 RandomNumber(float offset)
{
	uvec2 v = uvec2(gl_FragCoord.xy * (100.0f + 0.1f * time + randomsUsed + offset));
	uint sum = 0; uint k[4] = { 3355524772, 2738958700, 2911926141, 2123724318 }; uint delta = 2654435769;
	//32 rounds gave good results
	for (int i = 0; i < 128; i++)
	{
		sum += delta;
		v.x += ((v.y << 4) + k[0]) & (v.y + sum) & ((v.y >> 5) + k[1]);
		v.y += ((v.x << 4) + k[2]) & (v.x + sum) & ((v.x >> 5) + k[3]);
	}
	//divide by max range of uint to arrive at fractional number between 0 and 1
	float x = v.x / 4294967295.0f;
	float y = v.y / 4294967295.0f;
	return vec2(x, y);
	randomsUsed++;
}
//not finished, don't know how to complete it yet
vec3 RandomDirection(in vec3 normal, in float maxAngle)
{
	vec2 randoms = RandomNumber(0.0f);
	float theta = randoms.x * pi - 0.5f * pi;
	float phi = randoms.y * 2 * pi;
	float sinOfTheta = sin(theta);
	vec3 randomVecInHemisphere = vec3(sinOfTheta * cos(phi), cos(theta), sinOfTheta * sin(phi));
	//do stuff
	return randomVecInHemisphere;
}
//need to find a way to quickly generate 3 random numbers without wasting two computations
vec3 RandomDirection(in vec3 normal)
{
	vec3 d = vec3(RandomNumber(0.0f).xy, RandomNumber(10.0f).x);
	d *= 2.0f; d -= vec3(1, 1, 1);
	d = normalize(d);
	if(dot(d, normal) < 0)
		d = -d;
	return d;
}
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
	if (t2 < 0)
	{
		vec3 normal = normalize((rayOrigin + t1 * rayDirection) - center);
		return vec4(normal, t1);
	}
	vec3 normal = normalize((rayOrigin + t2 * rayDirection) - center);
	return vec4(normal, t2);
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
bool ObjectInWayOfLight(in float distanceToLightSquared, in vec3 shadowRayOrigin, in vec3 shadowRayDirection)
{
	//Check all spheres in the scene for an intersection
	for (int i = 0; i < spheres.length(); i++)
	{
		Sphere sphere = spheres[i];
		//we only care about the w component (the distance) of the result.
		float result = IntersectSphere(shadowRayOrigin, shadowRayDirection, sphere.center, sphere.radius).w;
		if (result > epsilon && result * result < distanceToLightSquared)
		{
			return true;
		}
	}
	//Check all planes in the scene for an intersection
	for (int i = 0; i < planes.length(); i++)
	{
		Plane plane = planes[i];
		float result = IntersectPlane(shadowRayOrigin, shadowRayDirection, plane.position, plane.normal);
		if (result > epsilon && result * result < distanceToLightSquared)
		{
			return true;
		}
	}
	//Check all triangles in the scene for an intersection
	for (int i = 0; i < triangles.length(); i++)
	{
		Triangle triangle = triangles[i];
		float result = IntersectTriangle(shadowRayOrigin, shadowRayDirection, triangle.pointA, triangle.pointB, triangle.pointC, triangle.normal);
		if (result > epsilon && result * result < distanceToLightSquared)
		{
			return true;
		}
	}	
	return false;
}
void FindClosestIntersection(in vec3 rayOrigin, in vec3 rayDirection, in float minDistance, inout float t, inout vec3 hitColor, inout bool hitPureSpecular, inout vec3 hitSpecularColor, inout float hitSpecularity, inout vec3 hitNormal, inout vec3 hitEmissionColor)
{
	//intersect with all spheres:
	for (int i = 0; i < spheres.length(); i++)
	{
		//sphere is the current sphere being looked at
		Sphere sphere = spheres[i];
		vec4 result = IntersectSphere(rayOrigin, rayDirection, sphere.center, sphere.radius);
		if (result.w > minDistance && result.w < t)
		{
			//result.w is the distance the IntersectSphere call returned
			t = result.w;
			hitColor = sphere.diffuseColor;
			hitPureSpecular = sphere.isPureSpecular;
			hitSpecularColor = sphere.specularColor;
			hitSpecularity = sphere.specularity;
			hitEmissionColor = sphere.emissionColor;
			//result.xyz is the normal the IntersectSphere call returned
			hitNormal = result.xyz;
		}
	}
	//intersect with all planes:
	for (int i = 0; i < planes.length(); i++)
	{
		Plane plane = planes[i];
		float result = IntersectPlane(rayOrigin, rayDirection, plane.position, plane.normal);
		if (result > minDistance && result < t)
		{
			t = result;
			hitColor = plane.diffuseColor;
			hitPureSpecular = plane.isPureSpecular;
			hitSpecularColor = plane.specularColor;
			hitSpecularity = plane.specularity;
			hitEmissionColor = plane.emissionColor;
			hitNormal = plane.normal;
		}
	}
	//intersect with all triangles
	for (int i = 0; i < triangles.length(); i++)
	{
		Triangle triangle = triangles[i];
		float result = IntersectTriangle(rayOrigin, rayDirection, triangle.pointA, triangle.pointB, triangle.pointC, triangle.normal);
		if (result > minDistance && result < t)
		{
			t = result;
			hitColor = triangle.diffuseColor;
			hitPureSpecular = triangle.isPureSpecular;
			hitSpecularColor = triangle.specularColor;
			hitSpecularity = triangle.specularity;
			hitEmissionColor = triangle.emissionColor;
			hitNormal = triangle.normal;
		}
	}
}
vec3 DetermineColorOfRay(in float minDistance, in vec3 rayOrigin, in vec3 rayDirection, out vec3 intersectionNormal, out vec3 intersectionPos, out vec3 intersectionDiffuse, out vec3 intersectionSpecular, out float intersectionSpecularity, out bool hitSomething)
{
	//t is the distance to the found intersections, it starts of as float.maxValue, because no intersection will ever return it. If t is this value, no intersections were found.
	float t = 3.402823466e+38;
	//we want to save the material values of the primitive which we intersect with.
	vec3 hitColor = vec3(0, 0, 0);
	bool hitPureSpecular = false;
	vec3 hitSpecularColor = vec3(0, 0, 0);
	float hitSpecularity = 1.0f;
	vec3 hitEmissionColor = vec3(0, 0, 0);
	//we also need to save the normal for the shading calculations later.
	vec3 hitNormal = vec3(0, 0, 0);

	//Find the closest intersection with all the objects in the scene
	FindClosestIntersection(rayOrigin, rayDirection, minDistance, t, hitColor, hitPureSpecular, hitSpecularColor, hitSpecularity, hitNormal, hitEmissionColor);

	//FOLLOWING SECTION: Pure specular implementation, NOTE THAT RECURSION IS NOT POSSIBLE IN GLSL
	//value that the final color will be multiplied with, instantiated to be just 1, 1, 1 so it has no effect if no mirrors are used.
	vec3 mirrorColorMultiplier = vec3(1, 1, 1);
	vec3 finalColor = vec3(0, 0, 0);
	int bounces = 0;
	//Go into "recursion" to calculate mirror reflection
	while (hitPureSpecular && bounces < maxBounces)
	{
		//determine location/position of the intersection
		vec3 hitPos = rayOrigin + t * rayDirection;

		//calculate diffuse component of lighting on previous found intersection, check for black diffuse first as this calculation is expensive and a lot of mirrors have black diffuse
		if (hitColor != vec3(0, 0, 0))
		{
			vec3 combinedColor = vec3(hitColor * ambiantLight);
			for (int l = 0; l < lengths[3]; l += 6)
			{
				vec3 lightPos = vec3(lights[l], lights[l + 1], lights[l + 2]);
				vec3 vectorToLight = lightPos - hitPos;
				float distanceToLightSquared = vectorToLight.x * vectorToLight.x + vectorToLight.y * vectorToLight.y + vectorToLight.z * vectorToLight.z;
				vec3 shadowRayOrigin = hitPos;
				vec3 shadowRayDirection = normalize(lightPos - hitPos);
				//if no objects were in the way of the light, add the appropriate lighting to it.
				if (!ObjectInWayOfLight(distanceToLightSquared, shadowRayOrigin, shadowRayDirection))
				{
					vec3 lightColor = vec3(lights[l + 3], lights[l + 4], lights[l + 5]);
					//formula for diffuse component, the specular component will be the pure specular component, which is calculated in a different way, as we are sure the current object is pureSpecular
					combinedColor += (lightColor * hitColor * max(0, dot(hitNormal, shadowRayDirection))) / distanceToLightSquared;
				}
			}
			finalColor += combinedColor * mirrorColorMultiplier;
		}

		//setup for next ray 
		mirrorColorMultiplier *= hitSpecularColor;
		//make sure normal is right way around
		if (dot(hitNormal, rayDirection) > 0.0f)
			hitNormal = -hitNormal;
		//calculate direction of new ray
		vec3 reflectionDirection = normalize(rayDirection - 2 * dot(rayDirection, hitNormal) * hitNormal);

		//create the new ray and reset ray variables
		rayOrigin = hitPos;
		rayDirection = reflectionDirection;
		t = 3.402823466e+38;
		hitColor = vec3(0, 0, 0);
		hitPureSpecular = false;
		hitSpecularColor = vec3(0, 0, 0);
		hitSpecularity = 1.0f;
		hitNormal = vec3(0, 0, 0);

		//calculate new intersection
		FindClosestIntersection(rayOrigin, rayDirection, epsilon, t, hitColor, hitPureSpecular, hitSpecularColor, hitSpecularity, hitNormal, hitEmissionColor);
		bounces++;
	}
	//if nothing got hit, return the color of the sky, t only changes if any of the primitives got hit by the viewray and t was instantiated to be float.maxValue
	if (t == 3.402823466e+38)
	{
		hitSomething = false;
		return finalColor + skyColor * mirrorColorMultiplier;
	}
	//if max mounces reached let the final added component of the color be the color black
	if (hitPureSpecular)
	{
		//Peter said just return black but if the mirrors also had a diffuse component i think we should still output that, if no mirrors had a diffuse component finalColor will be black.
		hitSomething = false; //set this to false so we dont keep bouncing when going into pathtracing
		return finalColor;
	}
	//FOLLOWING SECTION: calculate lighting of point on closest object
	//determine location/position of the intersection
	vec3 hitPos = rayOrigin + t * rayDirection;
	//combinedColor is the color the pixel will eventually be, each component can be added seperately and the ambient lighting will always be applied so it can happen now.
	vec3 combinedColor = vec3(hitColor * ambiantLight + hitEmissionColor);
	//Calculate the lighting on the found intersection for each light in the scene:
	for (int l = 0; l < lengths[3]; l += 6)
	{
		vec3 lightPos = vec3(lights[l], lights[l + 1], lights[l + 2]);
		vec3 vectorToLight = lightPos - hitPos;
		float distanceToLightSquared = vectorToLight.x * vectorToLight.x + vectorToLight.y * vectorToLight.y + vectorToLight.z * vectorToLight.z;
		vec3 shadowRayOrigin = hitPos;
		vec3 shadowRayDirection = normalize(lightPos - hitPos);
		//make sure normal is right way around
		if (dot(hitNormal, rayDirection) > 0.0f)
			hitNormal = -hitNormal;
		//skip light if on wrong side of object
		if(dot(hitNormal, shadowRayDirection) < 0.0f)
			continue;
		//if no objects were in the way of the light, add the appropriate lighting to it.
		if (!ObjectInWayOfLight(distanceToLightSquared, shadowRayOrigin, shadowRayDirection))
		{
			vec3 lightColor = vec3(lights[l + 3], lights[l + 4], lights[l + 5]);
			vec3 vectorR = normalize(shadowRayDirection - 2 * dot(shadowRayDirection, hitNormal) * hitNormal);
			//formula for diffuse and specular components, add it to the combined color as this happens for each light and we want the sum of the effects
			combinedColor += (lightColor * (hitColor * max(0, dot(hitNormal, shadowRayDirection)) + hitSpecularColor * pow(max(0, dot(rayDirection, vectorR)), hitSpecularity))) / distanceToLightSquared;
		}
	}
	//adjust for mirrors
	finalColor += combinedColor * mirrorColorMultiplier;
	intersectionNormal = hitNormal;
	intersectionPos = hitPos;
	intersectionDiffuse = hitColor;
	intersectionSpecular = hitSpecularColor;
	intersectionSpecularity = hitSpecularity;
	hitSomething = true;
	return finalColor;
}
vec3 CalculateColorOfPointOnCameraPlane(in float x, in float y)
{
	float width = camera[12];
	float height = camera[13];
	vec3 bottomLeft = vec3(camera[3], camera[4], camera[5]);
	vec3 bottomRight = vec3(camera[6], camera[7], camera[8]);
	vec3 topLeft = vec3(camera[9], camera[10], camera[11]);

	//FOLLOWING SECTION: First viewray to determine closest object
	//determine viewray from cameraData
	vec3 rayOrigin = vec3(camera[0], camera[1], camera[2]);
	vec3 rayDirection = normalize((bottomLeft + (x / width) * (bottomRight - bottomLeft) + (y / height) * (topLeft - bottomLeft)) - rayOrigin);
	vec3 hitNormal; vec3 hitPos; vec3 surfaceDiffuse; vec3 surfaceSpecular; float surfaceSpecularity; bool hitSomething;
	vec3 combinedColor = DetermineColorOfRay(0, rayOrigin, rayDirection, hitNormal, hitPos, surfaceDiffuse, surfaceSpecular, surfaceSpecularity, hitSomething);
	
	//Calculate path tracing ray, doesn't work for more than 2 bounces yet
	if(hitSomething)
	{
		vec3 shadowRayOrigin = hitPos;
		vec3 shadowRayDirection = RandomDirection(hitNormal);
		vec3 dummy; //we don't need to know the surface data of the found position as we regard it as a pointlight
		vec3 lightPos;
		vec3 lightColor = DetermineColorOfRay(epsilon, shadowRayOrigin, shadowRayDirection, dummy, lightPos, dummy, dummy, dummy.x, hitSomething);
		vec3 vectorToLight = lightPos - shadowRayOrigin;
		float distanceToLightSquared = vectorToLight.x * vectorToLight.x + vectorToLight.y * vectorToLight.y + vectorToLight.z * vectorToLight.z;
		vec3 vectorR = normalize(shadowRayDirection - 2 * dot(shadowRayDirection, hitNormal) * hitNormal);
		//formula for diffuse and specular components
		combinedColor += 5.0f * (lightColor * (surfaceDiffuse * max(0, dot(hitNormal, shadowRayDirection)) + surfaceSpecular * pow(max(0, dot(rayDirection, vectorR)), surfaceSpecularity))) / max(1.3f, distanceToLightSquared);
	}
	return combinedColor;
}

//for testing random generators
void OutputNoise()
{
	outputColor = vec4(RandomNumber(0.0f).xxx, 1.0f);
}

//This code will be run for each pixel on the screen
void main()
{
	vec3 sumOfColors = vec3(0.0f, 0.0f, 0.0f);
	float fraction = 1.0f / antiAliasingSamplesRoot;
	float halfFraction = fraction / 2.0f;
	for (int x = 0; x < antiAliasingSamplesRoot; x++)
	{
		for (int y = 0; y < antiAliasingSamplesRoot; y++)
		{
			float planeX = gl_FragCoord.x - 0.5f + halfFraction + x * fraction;
			float planeY = gl_FragCoord.y - 0.5f + halfFraction + y * fraction;
			sumOfColors += CalculateColorOfPointOnCameraPlane(planeX, planeY);
		}
	}
	vec3 averageColor = sumOfColors / (antiAliasingSamplesRoot * antiAliasingSamplesRoot);
	/**outputColor = vec4(averageColor, 1);
	return;*/
	
	//take last screen into account
	vec3 oldAverage = lastScreen[int(gl_FragCoord.x) + int(gl_FragCoord.y) * int(camera[12])].xyz;
	//averageColor = oldAverage * ((iterations - 1) / float(iterations)) + averageColor * (1.0f / iterations);
	averageColor = ((iterations - 1.0f) / iterations) * oldAverage + (1.0f / iterations) * averageColor;
	//output result of calculations to the screen and update the lastScreen
	lastScreen[int(gl_FragCoord.x) + int(gl_FragCoord.y) * int(camera[12])] = vec4(averageColor, 1.0f);
	outputColor = vec4(averageColor, 1.0f);
}