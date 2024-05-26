#version 430

out vec4 outputColor;
//max 50 lights
uniform float[300] lights;
//the lengths of each of the primitive arrays, in same order as they are declared
uniform int[4] lengths;
//Position: first three floats xyz. BottomleftPlane: 4th to 6th float. BottomRightPlane: 7th to 9th float. TopLeftPlane: 10th to 12th float. ScreenSize: last two floats
uniform float[14] camera;
//Time
uniform float timer;
//SSBO for primitive data
layout(binding = 0, std430) readonly buffer ssbo1
{
	float primitives[];
};

//shadow acne prevention margin
const float epsilon = 0.001f;
const vec3 ambiantLight = vec3(0.1f, 0.1f, 0.1f);
const vec3 skyColor = ambiantLight;
const float PI = 3.1415926;
const bool useTexture = true;


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
mat2 rotate(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	return mat2(c, -s, s, c);
}

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



//------------------------------INTERSECTION------------------------------------
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


//------------------------------INTERSECTION------------------------------------



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
	vec3 rayDirection = normalize( (bottomLeft + (x/width) * (bottomRight - bottomLeft) + (y/height) * (topLeft - bottomLeft)) - rayOrigin );
	float t = 3.402823466e+38;
	vec3 hitColor = vec3(0, 0, 0);
	vec3 hitSpecularColor = vec3(0, 0, 0);
	float hitSpecularity = 1.0f;
	vec3 hitNormal = vec3(0, 0, 0);
	float shapeType = 0;
	float textureIndex = 0;
	int shapeIndex = -1;

	for (int i = 0; i < lengths[0]; i += 12)
	{
		vec4 result = IntersectSphere(rayOrigin, rayDirection, vec3(primitives[i], primitives[i+1], primitives[i+2]), primitives[i + 3]);
		if (result.w > 0 && result.w < t)
		{
			t = result.w;
			hitColor = vec3( primitives[i+4], primitives[i+5], primitives[i+6]);
			hitSpecularColor = vec3(primitives[i + 7], primitives[i + 8], primitives[i + 9]);
			hitSpecularity = primitives[i + 10];
			hitNormal = result.xyz;
			shapeType = 1;
			shapeIndex = i;
			textureIndex = primitives[i + 11];
		}
	}
	int offset = lengths[0];
	int end = offset + lengths[1];
	for (int i = offset; i < end; i += 20)
	{
		vec3 planeNormal = vec3(primitives[i + 3], primitives[i + 4], primitives[i + 5]);
		float result = IntersectPlane(rayOrigin, rayDirection, vec3(primitives[i], primitives[i + 1], primitives[i + 2]), planeNormal);
		if (result > 0 && result < t)
		{
			t = result;
			hitColor = vec3(primitives[i + 6], primitives[i + 7], primitives[i + 8]);
			hitSpecularColor = vec3(primitives[i + 9], primitives[i + 10], primitives[i + 11]);
			hitSpecularity = primitives[i + 12];
			hitNormal = planeNormal;
			shapeType = 2;
			shapeIndex = i;
			textureIndex = primitives[i + 13];
		}
	}
	offset += lengths[1];
	end = offset + lengths[2];
	for (int i = offset; i < end; i += 26)
	{
		vec3 triangleNormal = vec3(primitives[i + 9], primitives[i + 10], primitives[i + 11]);
		float result = IntersectTriangle(rayOrigin, rayDirection, 
			vec3(primitives[i], primitives[i + 1], primitives[i + 2]),
			vec3(primitives[i + 3], primitives[i + 4], primitives[i + 5]), 
			vec3(primitives[i + 6], primitives[i + 7], primitives[i + 8]), 
			triangleNormal);
		if (result > 0 && result < t)
		{
			t = result;
			hitColor = vec3(primitives[i + 12], primitives[i + 13], primitives[i + 14]);
			hitSpecularColor = vec3(primitives[i + 15], primitives[i + 16], primitives[i + 17]);
			hitSpecularity = primitives[i + 18];
			hitNormal = triangleNormal;
			shapeType = 3;
			shapeIndex = i;
			textureIndex = primitives[shapeIndex + 19];
		}
	}
	//if nothing got hit, return the color of the sky
	if (t == 3.402823466e+38)
	{
		outputColor = vec4(skyColor, 1.0f);
		return;
	}
	
	vec3 hitPos = rayOrigin + t * rayDirection;
	
	if (textureIndex != 0)
	{
		vec2 uv = vec2(0,0);
		if (shapeType == 1)
		{uv = TextureMappingSphere(hitPos, vec3(primitives[shapeIndex], primitives[shapeIndex+1], primitives[shapeIndex+2]), primitives[shapeIndex+3]);}

		else if (shapeType == 2)
		{uv = TextureMappingPlane(hitPos, 
		vec3(primitives[shapeIndex], primitives[shapeIndex + 1], primitives[shapeIndex + 2]), 
		vec3(primitives[shapeIndex + 3], primitives[shapeIndex + 4], primitives[shapeIndex + 5]),
		vec3(primitives[shapeIndex + 14], primitives[shapeIndex + 15], primitives[shapeIndex + 16]),
		vec3(primitives[shapeIndex + 17], primitives[shapeIndex + 18], primitives[shapeIndex + 19]));}

		else if (shapeType == 3)
		{uv = TextureMappingTriangle(hitPos, 
			vec3(primitives[shapeIndex], primitives[shapeIndex + 1], primitives[shapeIndex + 2]),
			vec3(primitives[shapeIndex + 3], primitives[shapeIndex + 4], primitives[shapeIndex + 5]), 
			vec3(primitives[shapeIndex + 6], primitives[shapeIndex + 7], primitives[shapeIndex + 8]),
			vec2(primitives[shapeIndex + 20], primitives[shapeIndex + 21]),
			vec2(primitives[shapeIndex + 22], primitives[shapeIndex + 23]),
			vec2(primitives[shapeIndex + 24], primitives[shapeIndex + 25]));}
		

		hitColor = AplieTexture(uv, textureIndex);
	}

	//calculate lighting of point on closest objec
	
	vec3 combinedColor = vec3(hitColor * ambiantLight);
	for (int l = 0; l < lengths[3]; l += 6)
	{
		vec3 lightPos = vec3(lights[l], lights[l + 1], lights[l + 2]);
		float distanceToLight = length(lightPos - hitPos);
		vec3 shadowRayOrigin = hitPos;
		vec3 shadowRayDirection = normalize(lightPos - hitPos);
		float shadowRayT = -1;
		for (int i = 0; i < lengths[0]; i += 12)
		{
			float result = IntersectSphere(shadowRayOrigin, shadowRayDirection, vec3(primitives[i], primitives[i + 1], primitives[i + 2]), primitives[i + 3]).w;
			if (result > epsilon && result < distanceToLight)
			{
				shadowRayT = result;
				break;
			}
		}
		if (shadowRayT < 0)
		{
			int offset = lengths[0];
			int end = offset + lengths[1];
			for (int i = offset; i < end; i += 20)
			{
				float result = IntersectPlane(shadowRayOrigin, shadowRayDirection, vec3(primitives[i], primitives[i + 1], primitives[i + 2]), vec3(primitives[i + 3], primitives[i + 4], primitives[i + 5]));
				if (result > epsilon && result < distanceToLight)
				{
					shadowRayT = result;
					break;
				}
			}
			if (shadowRayT < 0)
			{
				offset += lengths[1];
				end = offset + lengths[2];
				for (int i = offset; i < end; i += 26)
				{
					float result = IntersectTriangle(shadowRayOrigin, shadowRayDirection, vec3(primitives[i], primitives[i + 1], primitives[i + 2]),
						vec3(primitives[i + 3], primitives[i + 4], primitives[i + 5]),
						vec3(primitives[i + 6], primitives[i + 7], primitives[i + 8]),
						vec3(primitives[i + 9], primitives[i + 10], primitives[i + 11]));
					if (result > epsilon && result < distanceToLight)
					{
						shadowRayT = result;
						break;
					}
				}
			}
		}
		if (shadowRayT < 0)
		{
			vec3 lightColor = vec3(lights[l + 3], lights[l + 4], lights[l + 5]);
			//make sure normal is right way around
			if (dot(hitNormal, rayDirection) > 0.0f)
				hitNormal = -hitNormal;
			vec3 vectorR = normalize(shadowRayDirection - 2 * dot(shadowRayDirection, hitNormal) * hitNormal);
			combinedColor += 1.0f / (distanceToLight * distanceToLight) * lightColor * (hitColor * max(0, dot(hitNormal, shadowRayDirection)) + hitSpecularColor * pow(max(0, dot(rayDirection, vectorR)), hitSpecularity));
		}
	}

	//output result of calculations
	outputColor = vec4(combinedColor, 1.0f);
}



