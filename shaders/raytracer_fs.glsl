#version 430
//settings
//shadow acne prevention margin
const float epsilon = 0.001f;
const vec3 ambiantLight = vec3(0.1f, 0.1f, 0.1f);
const vec3 skyColor = vec3(0.5f, 0.6f, 1.0f);
const int maxBounces = 10;
const float PI = 3.1415926;

out vec4 outputColor;
//max 50 lights
uniform float[300] lights;
//the lengths of each of the primitive arrays, in same order as they are declared
uniform int[4] lengths;
//Position: first three floats xyz. BottomleftPlane: 4th to 6th float. BottomRightPlane: 7th to 9th float. TopLeftPlane: 10th to 12th float. ScreenSize: last two floats
uniform float[14] camera;

struct Sphere {
	vec3 center;
	float radius;
	vec3 diffuseColor;
	bool isPureSpecular;
	vec3 specularColor;
	float specularity;
	float textureIndex;
};
struct Plane {
	vec3 position;
	vec3 normal;
	vec3 diffuseColor;
	bool isPureSpecular;
	vec3 specularColor;
	float specularity;
	float textureIndex;
	vec3 uVector;
	vec3 vVector;
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
	float textureIndex;
	vec2 uvPointA;
	vec2 uvPointB;
	vec2 uvPointC;
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
//SSBO for the acceleration structure (in this case an R-Tree)
layout(binding = 3, std430) readonly buffer ssbo3
{
	float accStruct[]; //Short for acceleration structure
};

//Used to implement recursion
//Note, is static size and doesn't check size, choosing a low value can cause the array to be exceeded and then you get memory leaks
const int stackSize = 100; //Used for stack
int counter = -1; //Used for stack
int[stackSize] stackPointers;

//Add to end of stack
void StackPush(int value)
{
	//First increments the counter, then adds the value
	stackPointers[++counter] = value;
}
//Remove from stack
int StackPop()
{
	//Returns the value, then decrements the value
	return stackPointers[counter--];
}
//Empty the stack
void StackClear()
{
	counter = -1;
}
int StackSize()
{
	return counter+1;
}

//------------------------------TEXTURING------------------------------------

float AreaTriangle(vec3 pointA, vec3 pointB, vec3 pointC) { return (length(cross(pointC - pointB, pointA - pointB)))/2;}

vec2 TextureMappingSphere(vec3 intersectionPoint, vec3 sphereCenter, float r)
{
	vec3 adjustedIntersection = intersectionPoint - sphereCenter;
	float theta = acos(adjustedIntersection.z/r);
	float phi = atan(adjustedIntersection.y, adjustedIntersection.x);
	return vec2((phi + PI)/(2*PI), theta/PI);
}

vec2 TextureMappingPlane (vec3 intersectionPoint, vec3 center, vec3 normal, vec3 uVEC, vec3 vVEC)
{
	vec3 ajdustedIntersect = intersectionPoint - center;
	return vec2((dot(ajdustedIntersect, uVEC))/length(uVEC), (dot(ajdustedIntersect, vVEC))/ length(vVEC));
}


vec2 TextureMappingTriangle (vec3 intersectionPoint, vec3 pointA, vec3 pointB, vec3 pointC, vec2 UVa, vec2 UVb, vec2 UVc)
{

//Ik heb de shading normal er uit gehouden want ik begrijp het punt nog niet.

	float abcArea = AreaTriangle(pointA, pointB, pointC);
	float alpha = AreaTriangle(intersectionPoint, pointB, pointC)/abcArea;
	float beta = AreaTriangle(intersectionPoint, pointC, pointA)/abcArea;
	float gamma = 1 - beta - alpha;

	float u = alpha*UVa.x + beta*UVb.x + gamma*UVc.x;
	float v = alpha*UVa.y + beta*UVb.y + gamma*UVc.y;

	return vec2(u,v);

}
//--------------------------------------TEXTURES-------------------------------------


vec3 Texturing1 (vec2 uv)
{
	int color;
	color = (int(uv.x) + int(uv.y)) & 1;
	return vec3(color,color,color);
}

//------------------------------------TEXTURES----------------------------------------------

vec3 AplieTexture (vec2 uv, float index)
{
	if (index == 1){ return Texturing1(uv); }
	return vec3(0,0,0);
}
//------------------------------TEXTURING------------------------------------


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
//This is a more compact form of the slab method mentioned in the slide, 
//original code can be found here: https://tavianator.com/2011/ray_box.html
bool IntersectBoundingBox(vec3 rayOrigin, vec3 rayDirection, vec3 minValuesBB, vec3 maxValuesBB)
{
	float txmin, txmax, tymin, tymax, tzmin, tzmax;
	vec3 rayInverse = 1 / rayDirection;
	vec3[] bounds = {minValuesBB, maxValuesBB};
	bool[] raySign = {rayInverse.x < 0, rayInverse.y < 0, rayInverse.z < 0};

	txmin = (bounds[int(raySign[0])].x - rayOrigin.x) * rayInverse.x;
	txmax = (bounds[1-int(raySign[0])].x - rayOrigin.x) * rayInverse.x;
	tymin = (bounds[int(raySign[1])].y - rayOrigin.y) * rayInverse.y;
	tymax = (bounds[1-int(raySign[1])].y - rayOrigin.y) * rayInverse.y;

	if ((txmin > tymax) || (tymin > txmax))
	return false;

	if (tymin > txmin)
	txmin = tymin;
	if (tymax < txmax)
	txmax = tymax;

	tzmin = (bounds[int(raySign[2])].z - rayOrigin.z) * rayInverse.z;
	tzmax = (bounds[1-int(raySign[2])].z - rayOrigin.z) * rayInverse.z;

	if ((txmin > tzmax) || (tzmin > txmax))
	return false;

	return true;
}}
//Gets the primitives that are useful for the calculation by means of an acceleration structure
void GetRelevantPrimitives(vec3 shadowRayOrigin, vec3 shadowRayDirection, out int sphereCount, out int[100] spherePointers, out int triangleCount, out int[500] trianglePointers)
{
	//Start using bounding box
	StackClear(); //Clear just to be certain, but shouldn't be necessary in theory
	StackPush(0); //Push the start of the acceleration structure to the stack
	int pos; //Position in the acceleration structure
	while(StackSize() > 0)
	{
		pos = StackPop();
		bool hit = IntersectBoundingBox(shadowRayOrigin, shadowRayDirection,
										vec3(accStruct[pos], accStruct[pos+1], accStruct[pos+2]),
										vec3(accStruct[pos+3], accStruct[pos+4], accStruct[pos+5]));
		if(!hit)
			continue;

		//Get the amount of values stored in the bounding box
		int count = int(accStruct[pos+7]);
		if(accStruct[pos+6] == 0) //If it is a branch
		{
			//Add all branches to check to stack
			for (int i = 0; i < count; i++) {
				StackPush(pos + int(accStruct[pos+8+i]));
			}
		}
		else //It is a leaf
		{
			for (int i = 0; i < count; i++) {
				//Add all primitives in it to the list to check
				int value = int(accStruct[pos+8+i]);
				if(value >= spheres.length()) //See if it is a sphere or triangle
				{trianglePointers[triangleCount] = value - spheres.length(); triangleCount++;}
				else
				{spherePointers[sphereCount] = value; sphereCount++;}
			}
		}
	}
}
bool ObjectInWayOfLight(in float distanceToLight, in vec3 shadowRayOrigin, in vec3 shadowRayDirection)
{
	int sphereCount;
	int[] spherePointers;
	int triangleCount;
	int[] trianglePointers;
	GetRelevantPrimitives(shadowRayOrigin, shadowRayDirection, sphereCount, spherePointers, triangleCount, trianglePointers);
		
	//Check all relevant spheres in the scene for an intersection
	for (int i = 0; i < sphereCount; i++)
	{
		Sphere sphere = spheres[spherePointers[i]];
		//we only care about the w component (the distance) of the result.
		float result = IntersectSphere(shadowRayOrigin, shadowRayDirection, sphere.center, sphere.radius).w;
		if (result > epsilon && result < distanceToLight)
		{
			return true;
		}
	}
	//Check all planes in the scene for an intersection
	for (int i = 0; i < planes.length(); i++)
	{
		Plane plane = planes[i];
		float result = IntersectPlane(shadowRayOrigin, shadowRayDirection, plane.position, plane.normal);
		if (result > epsilon && result < distanceToLight)
		{
			return true;
		}
	}
	//Check all relevant triangles in the scene for an intersection
	for (int i = 0; i < triangleCount; i++)
	{
		Triangle triangle = triangles[trianglePointers[i]];
		float result = IntersectTriangle(shadowRayOrigin, shadowRayDirection, triangle.pointA, triangle.pointB, triangle.pointC, triangle.normal);
		if (result > epsilon && result < distanceToLight)
		{
			return true;
		}
	}	
	return false;
}
void FindClosestIntersection(in vec3 rayOrigin, in vec3 rayDirection, in float minDistance, inout float t, inout vec3 hitColor, inout bool hitPureSpecular, inout vec3 hitSpecularColor, inout float hitSpecularity, inout vec3 hitNormal, inout float shapeType, inout float textureIndex, int shapeIndex)
{
	int sphereCount;
	int[] spherePointers;
	int triangleCount;
	int[] trianglePointers;
	GetRelevantPrimitives(rayOrigin, rayDirection, sphereCount, spherePointers, triangleCount, trianglePointers);
	
	//intersect with all relevant spheres:
	for (int i = 0; i < sphereCount; i++)
	{
		//sphere is the current sphere being looked at
		Sphere sphere = spheres[spherePointers[i]];
		vec4 result = IntersectSphere(rayOrigin, rayDirection, sphere.center, sphere.radius);
		if (result.w > minDistance && result.w < t)
		{
			//result.w is the distance the IntersectSphere call returned
			t = result.w;
			hitColor = sphere.diffuseColor;
			hitPureSpecular = sphere.isPureSpecular;
			hitSpecularColor = sphere.specularColor;
			hitSpecularity = sphere.specularity;
			//result.xyz is the normal the IntersectSphere call returned
			hitNormal = result.xyz;

			shapeType = 1;
			shapeIndex = i;
			textureIndex = sphere.textureIndex;
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
			hitNormal = plane.normal;

			shapeType = 2;
			shapeIndex = i;
			textureIndex = plane.textureIndex;
		}
	}
	//intersect with all relevant triangles
	for (int i = 0; i < triangleCount; i++)
	{
		Triangle triangle = triangles[trianglePointers[i]];
		float result = IntersectTriangle(rayOrigin, rayDirection, triangle.pointA, triangle.pointB, triangle.pointC, triangle.normal);
		if (result > minDistance && result < t)
		{
			t = result;
			hitColor = triangle.diffuseColor;
			hitPureSpecular = triangle.isPureSpecular;
			hitSpecularColor = triangle.specularColor;
			hitSpecularity = triangle.specularity;
			hitNormal = triangle.normal;

			shapeType = 3;
			shapeIndex = i;
			textureIndex = triangle.textureIndex;
		}
	}
}

//This code will be run for each pixel on the screen
void main()
{
	float x = gl_FragCoord.x;
	float y = gl_FragCoord.y;
	float width = camera[12];
	float height = camera[13];
	vec3 bottomLeft = vec3(camera[3], camera[4], camera[5]);
	vec3 bottomRight = vec3(camera[6], camera[7], camera[8]);
	vec3 topLeft = vec3(camera[9], camera[10], camera[11]);

	//FOLLOWING SECTION: First viewray to determine closest object
	//determine viewray from cameraData
	vec3 rayOrigin = vec3(camera[0], camera[1], camera[2]);
	vec3 rayDirection = normalize( (bottomLeft + (x/width) * (bottomRight - bottomLeft) + (y/height) * (topLeft - bottomLeft)) - rayOrigin );
	//t is the distance to the found intersections, it starts of as float.maxValue, because no intersection will ever return it. If t is this value, no intersections were found.
	float t = 3.402823466e+38;
	//we want to save the material values of the primitive which we intersect with.
	vec3 hitColor = vec3(0, 0, 0);
	bool hitPureSpecular = false;
	vec3 hitSpecularColor = vec3(0, 0, 0);
	//!!dont remember if this should be 1, probably should be 0, NEED TO TEST!!
	float hitSpecularity = 1f;
	//we also need to save the normal for the shading calculations later.
	vec3 hitNormal = vec3(0, 0, 0);

	float shapeType = 0;
	float textureIndex = 0;
	int shapeIndex = -1;

	//Find the closest intersection with all the objects in the scene
	FindClosestIntersection(rayOrigin, rayDirection, 0, t, hitColor, hitPureSpecular, hitSpecularColor, hitSpecularity, hitNormal, shapeType, textureIndex, shapeIndex);


	//FOLLOWING SECTION: Pure specular implementation, NOTE THAT RECURSION IS NOT POSSIBLE IN GLSL (so this is a workaround), this was done before we implemented the stack
	//value that the final color will be multiplied with, instantiated to be just 1, 1, 1 so it has no effect if no mirrors are used.
	vec3 mirrorColorMultiplier = vec3(1, 1, 1);
	vec3 finalColor = vec3(0, 0, 0);
	int bounces = 0;
	//Go into "recursion" to calculate mirror reflection
	while (hitPureSpecular && bounces < maxBounces)
	{
		//determine location/position of the intersection
		vec3 hitPos = rayOrigin + t * rayDirection;

		if (textureIndex != 0)
		{
			vec2 uv = vec2(0, 0);
			if (shapeType == 1)
			{uv = TextureMappingSphere(hitPos, spheres[shapeIndex].center, spheres[shapeIndex].radius);}
			else if (shapeType == 2)
			{uv = TextureMappingPlane(hitPos,
									  planes[shapeIndex].position,
									  planes[shapeIndex].normal,
									  planes[shapeIndex].uVector,
									  planes[shapeIndex].vVector);}
			else if (shapeType == 3)
			{uv = TextureMappingTriangle(hitPos,
										 triangles[shapeIndex].pointA,
										 triangles[shapeIndex].pointB,
										 triangles[shapeIndex].pointC,
										 triangles[shapeIndex].uvPointA,
										 triangles[shapeIndex].uvPointB,
										 triangles[shapeIndex].uvPointC);}


			hitColor = AplieTexture(uv, textureIndex);
		}

		//calculate diffuse component of lighting on previous found intersection, check for black diffuse first as this calculation is expensive and a lot of mirrors have black diffuse
		if (hitColor != vec3(0, 0, 0))
		{
			vec3 combinedColor = vec3(hitColor * ambiantLight);
			for (int l = 0; l < lengths[3]; l += 6)
			{
				vec3 lightPos = vec3(lights[l], lights[l + 1], lights[l + 2]);
				float distanceToLight = length(lightPos - hitPos);
				vec3 shadowRayOrigin = hitPos;
				vec3 shadowRayDirection = normalize(lightPos - hitPos);
				//if no objects were in the way of the light, add the appropriate lighting to it.
				if (!ObjectInWayOfLight(distanceToLight, shadowRayOrigin, shadowRayDirection))
				{
					vec3 lightColor = vec3(lights[l + 3], lights[l + 4], lights[l + 5]);
					//formula for diffuse component, the specular component will be the pure specular component, which is calculated in a different way, as we are sure the current object is pureSpecular
					combinedColor += 1.0f / (distanceToLight * distanceToLight) * lightColor * hitColor * max(0, dot(hitNormal, shadowRayDirection));
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
		hitSpecularity = 1f;
		hitNormal = vec3(0, 0, 0);

		//calculate new intersection
		FindClosestIntersection(rayOrigin, rayDirection, epsilon, t, hitColor, hitPureSpecular, hitSpecularColor, hitSpecularity, hitNormal);
		bounces++;
	}

	//if nothing got hit, return the color of the sky, t only changes if any of the primitives got hit by the viewray and t was instantiated to be float.maxValue
	if (t == 3.402823466e+38)
	{
		outputColor = vec4(finalColor + skyColor * mirrorColorMultiplier, 1.0f);
		return;
	}
	//if max mounces reached let the final added component of the color be the color black
	if (hitPureSpecular)
	{
		//Peter said just return black but if the mirrors also had a diffuse component i think we should still output that, if no mirrors had a diffuse component finalColor will be black.
		outputColor = vec4(finalColor, 1.0f);
	}

	//FOLLOWING SECTION: calculate lighting of point on closest object
	//determine location/position of the intersection
	vec3 hitPos = rayOrigin + t * rayDirection;
	//combinedColor is the color the pixel will eventually be, each component can be added seperately and the ambient lighting will always be applied so it can happen now.
	vec3 combinedColor = vec3(hitColor * ambiantLight);
	//Calculate the lighting on the found intersection for each light in the scene:
	for (int l = 0; l < lengths[3]; l += 6)
	{
		vec3 lightPos = vec3(lights[l], lights[l + 1], lights[l + 2]);
		float distanceToLight = length(lightPos - hitPos);
		vec3 shadowRayOrigin = hitPos;
		vec3 shadowRayDirection = normalize(lightPos - hitPos);
		
		//if no objects were in the way of the light, add the appropriate lighting to it.
		if (!ObjectInWayOfLight(distanceToLight, shadowRayOrigin, shadowRayDirection))
		{
			vec3 lightColor = vec3(lights[l + 3], lights[l + 4], lights[l + 5]);
			//make sure normal is right way around
			if (dot(hitNormal, rayDirection) > 0.0f)
				hitNormal = -hitNormal;
			vec3 vectorR = normalize(shadowRayDirection - 2 * dot(shadowRayDirection, hitNormal) * hitNormal);
			//formula for diffuse and specular components, add it to the combined color as this happens for each light and we want the sum of the effects
			combinedColor += 1.0f / (distanceToLight * distanceToLight) * lightColor * (hitColor * max(0, dot(hitNormal, shadowRayDirection)) + hitSpecularColor * pow(max(0, dot(rayDirection, vectorR)), hitSpecularity));
		}
	}
	//adjust for mirrors
	finalColor += combinedColor * mirrorColorMultiplier;
	//output result of calculations to the screen
	outputColor = vec4(finalColor, 1.0f);
}