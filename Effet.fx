cbuffer globals
{
	matrix finalMatrix;
}

struct VS_IN
{
	float3 pos : POSITION;
	float2 uv : TEXCOORD0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

// Vertex Shader
PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = mul(float4(input.pos, 1), finalMatrix);
	output.uv = input.uv;
	
	return output;
}

Texture2D yodaTexture;
SamplerState currentSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

// Pixel Shader
float4 PS( PS_IN input ) : SV_Target
{
	return yodaTexture.Sample(currentSampler, input.uv);
}

// Technique
technique10 Render
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}