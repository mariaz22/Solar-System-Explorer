Shader "Solar/PlanetSurface"
{
    Properties
    {
        [MainTexture] _BaseMap  ("Albedo Map",   2D) = "white" {}
        _BumpMap                ("Normal Map",   2D) = "bump"  {}
        _BumpScale              ("Normal Scale", Float)       = 1.0
        _Smoothness             ("Smoothness",   Range(0,1))  = 0.2
        _Metallic               ("Metallic",     Range(0,1))  = 0.0

        [Header(Cloud Layer)]
        _CloudColor     ("Cloud Color",    Color)        = (0.95, 0.96, 1.0, 1.0)
        _CloudCoverage  ("Coverage",       Range(0,1))   = 0.35
        _CloudOpacity   ("Opacity",        Range(0,0.9)) = 0.45
        _CloudScrollX   ("Scroll X / s",   Float)        = 0.006
        _CloudScrollY   ("Scroll Y / s",   Float)        = 0.002
        _CloudScale     ("Noise Scale",    Float)        = 4.0

        [Header(Emission)]
        _EmissionColor      ("Emission Color",   Color)       = (0,0,0,0)
        _EmissionPulseSpeed ("Pulse Speed",      Float)       = 1.5
        _EmissionPulseAmt   ("Pulse Amplitude",  Range(0,1))  = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                half4  _CloudColor;
                half   _CloudCoverage;
                half   _CloudOpacity;
                half   _CloudScrollX;
                half   _CloudScrollY;
                half   _CloudScale;
                half4  _EmissionColor;
                half   _EmissionPulseSpeed;
                half   _EmissionPulseAmt;
                half   _Smoothness;
                half   _Metallic;
                half   _BumpScale;
            CBUFFER_END

            float2 _Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float _GradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a = dot(_Hash2(i + float2(0,0)), f - float2(0,0));
                float b = dot(_Hash2(i + float2(1,0)), f - float2(1,0));
                float c = dot(_Hash2(i + float2(0,1)), f - float2(0,1));
                float d = dot(_Hash2(i + float2(1,1)), f - float2(1,1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float _CloudFBM(float2 p)
            {
                float v = 0.0, amp = 0.5;
                float2 shift = float2(1.7, 9.2);
                v += amp * _GradNoise(p); p = p * 2.07 + shift; amp *= 0.5;
                v += amp * _GradNoise(p); p = p * 2.11 + shift; amp *= 0.5;
                v += amp * _GradNoise(p); p = p * 2.03 + shift; amp *= 0.5;
                v += amp * _GradNoise(p);
                return v;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                half3  normalWS    : TEXCOORD2;
                half4  tangentWS   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posData = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   norData = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posData.positionCS;
                OUT.positionWS  = posData.positionWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS    = half3(norData.normalWS);
                OUT.tangentWS   = half4(norData.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 surfaceColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;

                UNITY_BRANCH
                if (_CloudOpacity > 0.001)
                {
                    float2 cloudUV = IN.uv * _CloudScale
                                   + float2(_CloudScrollX, _CloudScrollY) * _Time.y;
                    float noise     = _CloudFBM(cloudUV);
                    float remapped  = noise * 0.5 + 0.5;
                    float threshold = 1.0 - _CloudCoverage;
                    float cloudMask = smoothstep(threshold - 0.15, threshold + 0.15, remapped);
                    surfaceColor = lerp(surfaceColor,
                                        _CloudColor.rgb * surfaceColor * 1.55,
                                        cloudMask * _CloudOpacity);
                }

                half4 bumpSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv);
                half3 normalTS   = UnpackNormalScale(bumpSample, _BumpScale);
                half3 bitangent  = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                half3x3 TBN      = half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS);
                half3 normalWS   = normalize(TransformTangentToWorld(normalTS, TBN));

                float pulse    = 1.0 + sin(_Time.y * _EmissionPulseSpeed) * _EmissionPulseAmt;
                half3 emission = _EmissionColor.rgb * _EmissionColor.a * pulse;

                InputData inputData = (InputData)0;
                inputData.positionWS              = IN.positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord             = float4(0, 0, 0, 0);
                inputData.fogCoord                = 0;
                inputData.vertexLighting          = half3(0, 0, 0);
                inputData.bakedGI                 = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = float2(0, 0);
                inputData.shadowMask              = half4(1, 1, 1, 1);
                inputData.tangentToWorld          = TBN;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = surfaceColor;
                surfaceData.metallic    = _Metallic;
                surfaceData.smoothness  = _Smoothness;
                surfaceData.normalTS    = normalTS;
                surfaceData.emission    = emission;
                surfaceData.alpha       = 1.0;
                surfaceData.occlusion   = 1.0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half   _Cutoff;
                float4 _BumpMap_ST;
                half4  _CloudColor;
                half   _CloudCoverage, _CloudOpacity, _CloudScrollX, _CloudScrollY, _CloudScale;
                half4  _EmissionColor;
                half   _EmissionPulseSpeed, _EmissionPulseAmt;
                half   _Smoothness, _Metallic, _BumpScale;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
