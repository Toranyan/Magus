Shader "Custom/URP_CustomWithSilhouette"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

        _SilhouetteColor("Silhouette Color", Color) = (0,0.5,1,1)
        _OutlineThickness("Outline Thickness", Float) = 0.02
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

            Pass
            {
                Name "ForwardLit"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS   : NORMAL;
                    float2 uv         : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv          : TEXCOORD0;
                    float3 normalWS    : NORMAL;
                };

                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = IN.uv;
                    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                    // Simple lit shading using URP's Lambert
                    Light light = GetMainLight();
                    float3 normal = normalize(IN.normalWS);
                    float NdotL = saturate(dot(normal, light.direction));
                    float3 litColor = baseTex.rgb * light.color * NdotL;
                    return half4(litColor, baseTex.a);
                }
                ENDHLSL
            }

            Pass
            {
                Name "SilhouettePass"
                Tags { "LightMode" = "UniversalForward" }
                ZWrite Off
                ZTest Greater

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                };

                float4 _SilhouetteColor;
                float _OutlineThickness;

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    return _SilhouetteColor;
                }
                ENDHLSL
            }
        }
}
