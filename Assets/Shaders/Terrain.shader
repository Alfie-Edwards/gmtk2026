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

        // --- FORWARD RENDERING PASS ---
        Pass
        {
            Name "TriplanarPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords for main light shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float4 shadowCoord  : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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

                // Calculate shadow coordinates for the main light
                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.worldNormal);

                // Triplanar blending weights
                float3 blend = abs(normalWS);
                blend /= (blend.x + blend.y + blend.z);

                // Triplanar projections
                float2 uvX = input.worldPos.zy * _Tiling;
                float2 uvY = input.worldPos.xz * _Tiling;
                float2 uvZ = input.worldPos.xy * _Tiling;

                float4 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                float4 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                float4 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                float4 finalColor = colX * blend.x + colY * blend.y + colZ * blend.z;
                finalColor *= _Color;

                // --- LIGHTING & SHADOWS ---
                Light mainLight = GetMainLight(input.shadowCoord);

                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * (NdotL * mainLight.shadowAttenuation);

                float3 ambient = float3(0.2f, 0.2f, 0.2f);

                finalColor.rgb *= (lighting + ambient);

                return finalColor;
            }
            ENDHLSL
        }

        // --- SHADOW CASTER PASS (Allows this material to cast shadows on other objects) ---
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                // Apply URP shadow bias to avoid shadow acne/peter-panning
                float3 positionWS = vertexInput.positionWS;
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                return output;
            }

            float4 fragShadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}