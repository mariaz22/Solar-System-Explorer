Shader "Custom/FresnelAtmosphere"
{
    Properties
    {
        _AtmoColor  ("Atmosphere Color", Color) = (0.3, 0.6, 1.0, 1.0)
        _FresnelPow ("Fresnel Power",  Range(1.0, 8.0)) = 4.0
        _Intensity  ("Rim Intensity",  Range(0.1, 3.0)) = 1.2
        _InnerAlpha ("Inner Alpha",    Range(0.0, 0.3)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+1"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FresnelAtmosphere"
            Tags { "LightMode" = "UniversalForward" }

            // Additive blend — bright rim glows on top of everything
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _AtmoColor;
                half  _FresnelPow;
                half  _Intensity;
                half  _InnerAlpha;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS   = GetWorldSpaceViewDir(posWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N = normalize(IN.normalWS);
                half3 V = normalize(IN.viewDirWS);

                // Fresnel: 0 at face-on, 1 at grazing — creates the atmospheric rim
                half NdotV  = saturate(dot(N, V));
                half fresnel = pow(1.0h - NdotV, _FresnelPow);

                // Blend a tiny inner glow with the rim
                half alpha = lerp(_InnerAlpha, _Intensity, fresnel) * _AtmoColor.a;
                return half4(_AtmoColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
