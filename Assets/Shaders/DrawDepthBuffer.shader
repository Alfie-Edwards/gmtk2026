Shader "Custom/ShowDepthBuffer"
{
    Properties
    {
        _MaxDepthVis ("Max Depth Range", Float) = 20.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "ShowDepthPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _MaxDepthVis;

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample the full screen depth texture using blit coordinates
                float rawDepth = SampleSceneDepth(input.texcoord);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                
                float normalizedDepth = saturate(sceneDepth / _MaxDepthVis);
                return float4(normalizedDepth, normalizedDepth, normalizedDepth, 1.0);
            }
            ENDHLSL
        }
    }
}