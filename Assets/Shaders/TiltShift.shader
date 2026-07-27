Shader "Custom/DepthTiltShift"
{
    Properties
    {
        _FocusDistance ("Focus Distance", Range(0.1, 50)) = 5.0
        _FocusRange ("Focal Range Width", Range(0.01, 10)) = 1.0
        _BlurRadius ("Blur Radius", Int) = 8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "DepthTiltShiftPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _FocusDistance;
            float _FocusRange;
            int _BlurRadius;

            struct PixelInfo
            {
                float depth;
                float blur;
                half4 color;
            };

            float GetDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float GetBlur(float depth)
            {
                float depthDifference = abs(depth - _FocusDistance);
                float blurFactor = saturate((depthDifference - _FocusRange) / max(_FocusRange, 0.0001));
                return max(blurFactor * (0.5 + _BlurRadius), 0.0001);
            }

            float3 GetColour(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            float F(float z, float blur)
            {
                float u = clamp(z / blur, -1, 1);
                return 0.5 + u - 0.5 * u * abs(u);
            }

            float GetWeight(int x, int y, float blur)
            {
                float wx = F(x + 0.5, blur) - F(x - 0.5, blur);
                float wy = F(y + 0.5, blur) - F(y - 0.5, blur);
                return wx * wy;
            }

            float2 GetUV(float2 centerUV, int x, int y)
            {
                float2 texelSize = 1.0 / _ScreenParams.xy;
                return centerUV + float2(x, y) * texelSize;
            }
            
            bool IsInBounds(float2 uv)
            {
                return all(uv >= 0.0 && uv <= 1.0);
            }

            float3 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float maxBlur = 0.5 + _BlurRadius;
                float centerDepth = GetDepth(input.texcoord);
                float centerBlur = GetBlur(centerDepth);
                float edgeDist = abs(2 * (input.texcoord.y - 0.5));
                centerBlur = max(centerBlur, maxBlur * edgeDist * edgeDist);
                float blurWeightFactor = 1 / GetWeight(0, 0, maxBlur);

                float3 colourSum = float3(0, 0, 0);
                float weightSum = 0;
                float3 colourOverlaySum = float3(0, 0, 0);
                float weightOverlaySum = 0;
                int diameter = clamp(_BlurRadius, 2, 16);

                for (int y = -_BlurRadius; y <= _BlurRadius; y++)
                {
                    for (int x = -_BlurRadius; x <= _BlurRadius; x++)
                    {
                        float2 neighbor = GetUV(input.texcoord, x, y);
                        if (IsInBounds(neighbor))
                        {
                            float depth = GetDepth(neighbor);
                            if (depth < centerDepth)
                            {
                                float blur = GetBlur(depth);
                                float weight = GetWeight(x, y, blur);
                                colourOverlaySum += GetColour(neighbor) * weight;
                                weightOverlaySum += weight;
                                centerBlur = max(centerBlur, blur * GetWeight(x, y, maxBlur) * blurWeightFactor);
                            }
                        }
                    }
                }
                for (int y = -_BlurRadius; y <= _BlurRadius; y++)
                {
                    for (int x = -_BlurRadius; x <= _BlurRadius; x++)
                    {
                        float2 neighbor = GetUV(input.texcoord, x, y);
                        if (IsInBounds(neighbor))
                        {
                            float depth = GetDepth(neighbor);
                            if (depth >= centerDepth)
                            {
                                float weight = GetWeight(x, y, centerBlur);
                                colourSum += GetColour(neighbor) * weight;
                                weightSum += weight;
                            }
                        }
                    }
                }
                // Uncomment to see blur levels.
                // return float3(centerBlur / maxBlur,centerBlur / maxBlur,centerBlur / maxBlur);
                return lerp(colourSum / max(0.0001, weightSum), colourOverlaySum / max(0.0001, weightOverlaySum), weightOverlaySum);
            }
            ENDHLSL
        }
    }
}