Shader "Custom/TriplanarFloor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Tiling ("Tiling", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "TriplanarPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Declare the color property variable
            float4 _Color;
            float _Tiling;

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = normalInput.normalWS;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Calculate blend weights based on absolute world normal
                float3 blend = abs(input.worldNormal);
                blend /= (blend.x + blend.y + blend.z);

                // Project texture coordinates using world position scaled by tiling
                float2 uvX = input.worldPos.zy * _Tiling;
                float2 uvY = input.worldPos.xz * _Tiling;
                float2 uvZ = input.worldPos.xy * _Tiling;

                // Sample the texture from three directions
                float4 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                float4 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                float4 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                // Blend them together based on the surface orientation
                float4 finalColor = colX * blend.x + colY * blend.y + colZ * blend.z;

                // Multiply by the tint color
                finalColor *= _Color;

                return finalColor;
            }
            ENDHLSL
        }
    }
}