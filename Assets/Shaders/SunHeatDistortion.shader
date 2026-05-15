Shader "Solar/SunHeatDistortion"
{
    Properties
    {
        _DistortStrength ("Distortion Strength", Range(0, 0.02)) = 0.007
        _DistortFreq     ("Wave Frequency",      Float)          = 5.0
        _DistortSpeed    ("Wave Speed",          Float)          = 0.8
        _FalloffPow      ("Center Falloff",      Range(1, 8))    = 3.5
        _HeatTint        ("Heat Tint",           Color)          = (1.0, 0.78, 0.40, 0.0)
        _HeatTintStr     ("Tint Strength",       Range(0, 0.15)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent+200"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        ZWrite Off
        ZTest  Less
        Cull   Front
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SunHeatDistortion"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                half  _DistortStrength;
                half  _DistortFreq;
                half  _DistortSpeed;
                half  _FalloffPow;
                half4 _HeatTint;
                half  _HeatTintStr;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(posWS);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                half3 N     = normalize(IN.normalWS);
                half3 V     = normalize(IN.viewDirWS);
                half  NdotV = abs(dot(N, V));
                half  falloff = pow(saturate(NdotV), _FalloffPow);

                float t = _Time.y * _DistortSpeed;
                float2 uv = screenUV * _DistortFreq;

                float2 distort;
                distort.x = sin(uv.y * 2.31 + t * 1.07) * cos(uv.x * 1.74 + t * 0.83);
                distort.y = cos(uv.x * 2.17 + t * 1.29) * sin(uv.y * 1.53 + t * 0.97);
                distort.x += sin(uv.y * 5.63 + t * 2.41) * 0.22;
                distort.y += cos(uv.x * 4.87 + t * 2.79) * 0.22;

                float2 sampleUV = screenUV + distort * (float)_DistortStrength * (float)falloff;

                half3 background = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture,
                                                       sampler_CameraOpaqueTexture,
                                                       sampleUV).rgb;

                background += _HeatTint.rgb * _HeatTintStr * falloff;

                half alpha = falloff * 0.45;

                return half4(background, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
