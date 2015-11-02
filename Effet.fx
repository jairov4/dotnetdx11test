cbuffer globals
{
	matrix finalMatrix;
	float4 color;
}

struct VS_IN
{
	float2 pos : POSITION;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
};

// Vertex Shader
PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	float4 r = mul(float4(input.pos, 1, 1), finalMatrix);
	output.pos = r;
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
	return color;
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