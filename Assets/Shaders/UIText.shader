Shader "Custom/URP UI Text" {

Properties {
    _FaceTex            ("Face Texture", 2D) = "white" {}
    _FaceUVSpeedX       ("Face UV Speed X", Range(-5, 5)) = 0.0
    _FaceUVSpeedY       ("Face UV Speed Y", Range(-5, 5)) = 0.0
    _FaceColor          ("Face Color", Color) = (1,1,1,1)
    _FaceDilate         ("Face Dilate", Range(-1,1)) = 0

    _OutlineColor       ("Outline Color", Color) = (0,0,0,1)
    _OutlineTex         ("Outline Texture", 2D) = "white" {}
    _OutlineUVSpeedX    ("Outline UV Speed X", Range(-5, 5)) = 0.0
    _OutlineUVSpeedY    ("Outline UV Speed Y", Range(-5, 5)) = 0.0
    _OutlineWidth       ("Outline Thickness", Range(0, 1)) = 0
    _OutlineSoftness    ("Outline Softness", Range(0,1)) = 0

    _WeightNormal       ("Weight Normal", float) = 0
    _WeightBold         ("Weight Bold", float) = 0.5

    _ScaleRatioA        ("Scale RatioA", float) = 1
    _ScaleRatioB        ("Scale RatioB", float) = 1
    _ScaleRatioC        ("Scale RatioC", float) = 1

    _MainTex            ("Font Atlas", 2D) = "white" {}
    _TextureWidth       ("Texture Width", float) = 512
    _TextureHeight      ("Texture Height", float) = 512
    _GradientScale      ("Gradient Scale", float) = 5.0
    _ScaleX             ("Scale X", float) = 1.0
    _ScaleY             ("Scale Y", float) = 1.0
    _Sharpness          ("Sharpness", Range(-1,1)) = 0
    _PerspectiveFilter  ("Perspective Filter", Range(0, 1)) = 0.875

    _VertexOffsetX      ("Vertex OffsetX", float) = 0
    _VertexOffsetY      ("Vertex OffsetY", float) = 0

    _StencilComp        ("Stencil Comparison", Float) = 8
    _Stencil            ("Stencil ID", Float) = 0
    _StencilOp          ("Stencil Operation", Float) = 0
    _StencilWriteMask   ("Stencil Write Mask", Float) = 255
    _StencilReadMask    ("Stencil Read Mask", Float) = 255

    _CullMode           ("Cull Mode", Float) = 0
    _ColorMask          ("Color Mask", Float) = 15
}

SubShader {
    Tags {
        "Queue"="Transparent"
        "IgnoreProjector"="True"
        "RenderType"="Transparent"
        "PreviewType"="Plane"
        "RenderPipeline"="UniversalPipeline"
    }

    Stencil {
        Ref [_Stencil]
        Comp [_StencilComp]
        Pass [_StencilOp]
        ReadMask [_StencilReadMask]
        WriteMask [_StencilWriteMask]
    }

    Cull [_CullMode]
    ZWrite Off
    ZTest LEqual
    Blend SrcAlpha OneMinusSrcAlpha
    ColorMask [_ColorMask]

    Pass {
        Name "UniversalForward"
        Tags { "LightMode" = "UniversalForward" }

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct appdata_t {
            float4 vertex : POSITION;
            float4 color : COLOR0;
            float4 texcoord : TEXCOORD0; 
            float2 texcoord1 : TEXCOORD1;
            float3 normal : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            half4 color : COLOR0;
            float2 atlas : TEXCOORD0;
            float2 faceUV : TEXCOORD1;
            float2 outlineUV : TEXCOORD2;
            float4 param : TEXCOORD3; 
            float3 worldNormal : TEXCOORD4;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _TextureWidth;
            float _TextureHeight;
            float _GradientScale;
            float _ScaleX;
            float _ScaleY;
            float _Sharpness;
            float _VertexOffsetX;
            float _VertexOffsetY;

            sampler2D _FaceTex;
            float4 _FaceColor;
            float _FaceDilate;
            float _FaceUVSpeedX;
            float _FaceUVSpeedY;
            float4 _FaceTex_ST;

            sampler2D _OutlineTex;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _OutlineUVSpeedX;
            float _OutlineUVSpeedY;
            float4 _OutlineTex_ST;

            float _WeightNormal;
            float _WeightBold;
            float _ScaleRatioA;
            float _UIVertexColorAlwaysGammaSpace;
            float4 _ClipRect;
        CBUFFER_END

        uniform sampler2D _MainTex;

        v2f vert (appdata_t v) {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            float bold = step(v.texcoord.w, 0);
            float4 vert = v.vertex;
            vert.x += _VertexOffsetX;
            vert.y += _VertexOffsetY;

            o.vertex = TransformObjectToHClip(vert.xyz);

            float2 pixelSize = o.vertex.w;
            pixelSize /= float2(_ScaleX, _ScaleY) * _ScreenParams.xy;
            float scale = rsqrt(dot(pixelSize, pixelSize));
            scale *= abs(v.texcoord.w) * _GradientScale * (_Sharpness + 1);

            float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
            weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

            float bias = (.5 - weight) + (.5 / scale);
            float alphaClip = (1.0 - _OutlineWidth * _ScaleRatioA - _OutlineSoftness * _ScaleRatioA);
            alphaClip = alphaClip / 2.0 - (.5 / scale) - weight;

            #if !defined(SHADER_API_GLES3) && defined(UNITY_COLORSPACE_GAMMA)
                bool isGamma = true;
            #else
                bool isGamma = false;
            #endif

            // Fixed: Use URP's native SRGBToLinear function instead of legacy GammaToLinearSpace
            if (_UIVertexColorAlwaysGammaSpace && !isGamma) {
                v.color.rgb = SRGBToLinear(v.color.rgb);
            }

            o.color = v.color;
            o.atlas = v.texcoord.xy;
            o.faceUV = TRANSFORM_TEX(v.texcoord1, _FaceTex);
            o.outlineUV = TRANSFORM_TEX(v.texcoord1, _OutlineTex);
            o.param = float4(alphaClip, scale, bias, 0);
            o.worldNormal = TransformObjectToWorldNormal(v.normal);

            return o;
        }

        half4 frag (v2f i) : SV_Target {
            float c = tex2D(_MainTex, i.atlas).a;
            clip(c - i.param.x);

            float scale = i.param.y;
            float bias = i.param.z;
            float sd = (bias - c) * scale;

            float outline = (_OutlineWidth * _ScaleRatioA) * scale;
            float softness = max((_OutlineSoftness * _ScaleRatioA) * scale, 0.0001);

            half4 faceColor = _FaceColor * i.color;
            faceColor *= tex2D(_FaceTex, i.faceUV + float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.y);

            half4 outlineColor = _OutlineColor;
            outlineColor *= tex2D(_OutlineTex, i.outlineUV + float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.y);

            float faceAlpha = 1.0 - saturate((sd - outline * 0.5 + softness * 0.5) / softness);
            float outlineAlpha = 1.0 - saturate((sd + outline * 0.5 + softness * 0.5) / softness);

            half4 finalColor = lerp(outlineColor, faceColor, faceAlpha);
            finalColor.a *= outlineAlpha;

            // URP Lighting Integration
            half3 ambientLighting = SampleSH(i.worldNormal);
            Light mainLight = GetMainLight();
            half3 mainLightColor = mainLight.color * mainLight.direction.y;
            
            finalColor.rgb *= (ambientLighting + mainLightColor);

            return finalColor;
        }
        ENDHLSL
    }
}

Fallback "TextMeshPro/Mobile/Distance Field"
CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}