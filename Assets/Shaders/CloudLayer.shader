Shader "Solar/CloudLayer"
{
    Properties
    {
        _CloudColor ("Cloud Color",  Color)        = (0.94, 0.96, 1.0, 1.0)
        _Coverage   ("Coverage",     Range(0,1))   = 0.35
        _Opacity    ("Opacity",      Range(0,0.7)) = 0.40
        _ScrollX    ("Scroll X / s", Float)        = 0.006
        _ScrollY    ("Scroll Y / s", Float)        = 0.002
        _Scale      ("Noise Scale",  Float)        = 4.0
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        ZWrite Off
        Cull   Back
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "CloudLayer"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CloudColor;
                half  _Coverage;
                half  _Opacity;
                half  _ScrollX;
                half  _ScrollY;
                half  _Scale;
            CBUFFER_END

            float3 _H3(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7,  74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float _GN3(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a  = dot(_H3(i),                 f);
                float b  = dot(_H3(i + float3(1,0,0)), f - float3(1,0,0));
                float c  = dot(_H3(i + float3(0,1,0)), f - float3(0,1,0));
                float d  = dot(_H3(i + float3(1,1,0)), f - float3(1,1,0));
                float e  = dot(_H3(i + float3(0,0,1)), f - float3(0,0,1));
                float ff = dot(_H3(i + float3(1,0,1)), f - float3(1,0,1));
                float g  = dot(_H3(i + float3(0,1,1)), f - float3(0,1,1));
                float h  = dot(_H3(i + float3(1,1,1)), f - float3(1,1,1));
                return lerp(lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y),
                            lerp(lerp(e, ff,u.x), lerp(g, h, u.x), u.y), u.z);
            }

            float _FBM3(float3 p)
            {
                float v = 0.0, amp = 0.5;
                float3 s = float3(1.7, 9.2, 5.4);
                v += amp * _GN3(p); p = p * 2.07 + s; amp *= 0.5;
                v += amp * _GN3(p); p = p * 2.11 + s; amp *= 0.5;
                v += amp * _GN3(p); p = p * 2.03 + s; amp *= 0.5;
                v += amp * _GN3(p);
                return v;
            }

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
                float3 positionWS  : TEXCOORD2;
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
                OUT.positionWS  = posWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 nN = normalize(IN.normalWS);

                float3 cloudCoord = (float3)nN * _Scale
                                  + float3(_ScrollX, _ScrollY, _ScrollX * 0.6) * _Time.y;

                float noise    = _FBM3(cloudCoord);
                float remapped = noise * 0.5 + 0.5;
                float thr      = 1.0 - _Coverage;
                float mask     = smoothstep(thr - 0.15, thr + 0.15, remapped);

                half NdotV = saturate(dot(nN, normalize(IN.viewDirWS)));
                half limb  = NdotV * NdotV;

                float3 sunDir  = normalize(-IN.positionWS);
                half   NdotL   = dot(nN, (half3)sunDir);
                half litFactor = smoothstep(-0.08, 0.22, NdotL);
                half bright    = lerp(0.80h, 1.00h, saturate(NdotL));

                half3 color = _CloudColor.rgb * bright;
                half  alpha = (half)mask * _Opacity * limb * litFactor;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
