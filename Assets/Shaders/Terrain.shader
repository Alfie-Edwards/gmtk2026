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
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry" 
        }
        LOD 100

        // --- FORWARD PASS (Your custom triplanar look) ---
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Tiling;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord  : TEXCOORD2;
                #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = normalize(normalInput.normalWS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.worldNormal);
                float3 blend = abs(normalWS);
                blend /= (blend.x + blend.y + blend.z);

                float2 uvX = input.worldPos.zy * _Tiling;
                float2 uvY = input.worldPos.xz * _Tiling;
                float2 uvZ = input.worldPos.xy * _Tiling;

                float4 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                float4 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                float4 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                float4 finalColor = colX * blend.x + colY * blend.y + colZ * blend.z;
                finalColor *= _Color;

                Light mainLight;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                mainLight = GetMainLight(input.shadowCoord);
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                float4 shadowCoord = TransformWorldToShadowCoord(input.worldPos);
                mainLight = GetMainLight(shadowCoord);
                #else
                mainLight = GetMainLight();
                #endif

                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * (NdotL * mainLight.shadowAttenuation);
                float3 ambient = float3(0.2f, 0.2f, 0.2f);

                finalColor.rgb *= (lighting + ambient);

                return finalColor;
            }
            ENDHLSL
        }

        // --- INHERITED URP PASSES (Guarantees exact native depth and shadow behavior) ---
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}