#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#endif

#include "./Mad.fxh"

float4x4 View;
float4x4 Projection;
float4x4 ViewProj;
float3 SnapColor;
bool IsFullbright;
bool UseBaseColor;
float3 BaseColor;
float3 FogColor;
float FogDistance;
float FogDensity;
float2 EnvironmentLight;
float3 CameraPosition;
float Alpha;


// Damage
bool Expand;
float RandomFloat;
float Darken; // set below 1.0f to adjust brightness

// Charged line blink
float ChargedBlinkAmount;

float HalfThickness;
float2 Resolution;
float DistantOutlineDistanceFalloffWithCutoff;
float DistantOutlineClassicCutoff;
float DistantOutlineHideOutlines;
float DistantOutlineDistanceFalloff;
float OutlineClassicCutoffDistance;
float OutlineFalloffStartDistance;
float OutlineMinimumVisibleThickness;

struct VertexShaderInput
{
	float3 PositionA : POSITION0;
	float3 PositionB : POSITION1;
	float Side : TEXCOORD0; // -1 or 1
	float3 Normal : NORMAL0;
	float3 Color : COLOR0;
	float3 Centroid : POSITION2;
	float DecalOffset : TEXCOORD1;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
    float4 WorldPos : TEXCOORD2;
    float GetsShadowed : TEXCOORD3;
    float3 Normal : TEXCOORD4;
};

VertexShaderOutput MainVS(
    in VertexShaderInput input,
    // instance parameters
    in float4x4 world : TEXCOORD3,
    in float4 parameters : TEXCOORD7
)
{
    bool getsShadowed;
    float alphaOverride;
    bool isFullbright;
    bool glow;
    VS_UnpackParameters(parameters, getsShadowed, alphaOverride, isFullbright, glow);

    VertexShaderOutput output = (VertexShaderOutput)0;
    float thicknessScale = 1.0;
    float hideLine = 0.0;
    float3 worldCentroid = mul(float4(input.Centroid, 1), world).xyz;
    float viewDepth = abs(mul(float4(worldCentroid, 1), View).z);
    float classicCutoffEnabled = saturate(sign(OutlineClassicCutoffDistance));
    float falloffEnabled = saturate(sign(OutlineFalloffStartDistance));
    float falloffMode = saturate(DistantOutlineDistanceFalloff + DistantOutlineDistanceFalloffWithCutoff);
    float cutoffMode = DistantOutlineClassicCutoff * classicCutoffEnabled;
    float cullPastDistance = saturate(sign(viewDepth - OutlineClassicCutoffDistance));
    float minVisibleThickness = max(OutlineMinimumVisibleThickness, 0.0);
    float referenceDepth = max(OutlineFalloffStartDistance, 0.0001);
    float actualThickness = HalfThickness * min(1.0, referenceDepth / max(viewDepth, 0.0001));
    float safeHalfThickness = max(HalfThickness, 0.0001);

    thicknessScale = lerp(1.0, actualThickness / safeHalfThickness, falloffMode * falloffEnabled);

    float halfThicknessNotPositive = saturate(sign(0.0001 - HalfThickness));
    float halfThicknessBelowMin = saturate(sign(minVisibleThickness - HalfThickness));
    float actualThicknessBelowMin = saturate(sign(minVisibleThickness - actualThickness));
    float distanceCutoffHidden = DistantOutlineDistanceFalloffWithCutoff * max(halfThicknessNotPositive, max(halfThicknessBelowMin, actualThicknessBelowMin));
    float distanceHidden = DistantOutlineDistanceFalloff * halfThicknessNotPositive;
    float classicHidden = cutoffMode * cullPastDistance;
    hideLine = saturate(max(DistantOutlineHideOutlines, max(classicHidden, max(distanceCutoffHidden, distanceHidden))));

    // Decode Side: abs > 1.5 means endpoint B, sign gives offset direction
    float3 position = (abs(input.Side) > 1.5) ? input.PositionB : input.PositionA;
    float sideSign = sign(input.Side);

    VS_DecalOffset(position, input.Normal, input.DecalOffset);

    if (Expand == true)
    {
        VS_Expand(position, input.Centroid, RandomFloat);
    }

    // Save the vertices position in world space (for shadow mapping)
    output.WorldPos = mul(float4(position, 1), world);
    output.GetsShadowed = getsShadowed;

    float4 viewPos = mul(output.WorldPos, View);

    // Transform both endpoints to clip space for screen-space line direction
    float4 clipA = mul(mul(float4(input.PositionA, 1), world), ViewProj);
    float4 clipB = mul(mul(float4(input.PositionB, 1), world), ViewProj);

    float2 screenA = Resolution * clipA.xy / clipA.w;
    float2 screenB = Resolution * clipB.xy / clipB.w;

    // Guard against NaN from normalize((0,0)) when endpoints project to the
    // same screen pixel (near-degenerate lines). Fallback to horizontal.
    float2 delta = screenB - screenA;
    float deltaLenSq = dot(delta, delta);
    float2 dir = deltaLenSq < 0.0001 ? float2(1, 0) : normalize(delta);
    float2 normal = float2(-dir.y, dir.x);

    // Screen-space offset for line thickness
    float4 clipPos = mul(viewPos, Projection);
    float2 offset = normal * HalfThickness * thicknessScale * sideSign / Resolution * 2.0;

	float3 color = input.Color;

    // Apply base color
    if (UseBaseColor == true)
    {
        color = BaseColor;
    }

    output.Position = clipPos + float4(offset * clipPos.w, 0, 0);

    // Nudge outlines toward the camera so they render on top of the geometry they outline
    output.Position.z -= 0.1;
    // Collapse hidden line quads outside clip space without dynamic shader returns.
    output.Position = lerp(output.Position, float4(2.0, 2.0, 0.0, 1.0), hideLine);

    if (Darken < 1.0f)
    {
        VS_Darken(color, Darken);
    }

    if (glow == true)
    {
        color = color * 1.6;
        // clamp to 1.0
        color = min(color, float3(1.0, 1.0, 1.0));
    }

	// Apply diffuse lighting
	if (IsFullbright == false && isFullbright == false && glow == false)
    {
        VS_ApplyPolygonDiffuse(
            color,
            worldCentroid,
            normalize(mul(float4(input.Normal, 0), world).xyz),
            LightDirection,
            CameraPosition,
            EnvironmentLight
        );

        // Apply snap
        VS_Snap(color, SnapColor);
	}

    if (ChargedBlinkAmount > 0.0f)
    {
        color.r = (25.5 * ChargedBlinkAmount) / 255.0;
        color.g = (128.0 + 12.8 * ChargedBlinkAmount) / 255.0;
        color.b = 1.0;
    }

    VS_ApplyFog(color, viewPos.xyz, FogColor, FogDistance, FogDensity);

    VS_ColorCorrect(color);

    output.Color = float4(color, min(alphaOverride, Alpha));

    output.Normal = input.Normal;

	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
    float4 diffuse = input.Color;

    if (input.GetsShadowed > 0.0)
    {
        float3 diffuseRGB = diffuse.xyz;
        PS_ApplyShadowing(diffuseRGB, input.WorldPos, input.Normal);
        diffuse = float4(diffuseRGB, diffuse.w);
    }

	return diffuse;
}

technique Basic
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
