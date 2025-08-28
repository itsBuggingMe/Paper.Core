#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_5_0
    #define PS_SHADERMODEL ps_5_0
#endif

float4x4 Transform;

texture Atlas;

sampler Sampler = sampler_state
{
    Texture = <Atlas>;
};

struct VertexShaderInput
{
    float2 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinate : TEXCOORD0;
};


VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 normPos = float4(input.Position.x, input.Position.y, 0, 1);
    output.Position = mul(normPos, Transform);
    output.Color = input.Color;
    output.TextureCoordinate = input.TextureCoordinate;
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    return tex2D(Sampler, input.TextureCoordinate) * input.Color;
}

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};