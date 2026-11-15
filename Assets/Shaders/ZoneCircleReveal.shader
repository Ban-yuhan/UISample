Shader "Custom/ZoneCircleReveal"
{
    Properties
    {
        _TopColor ("Top Grass Color", Color) = (0.6117, 0.8274, 0.2902, 1)
        _GridColor ("Top Grid Line Color", Color) = (0.5098, 0.7294, 0.1961, 0.45)
        _SideLeftColor ("Side Left Color", Color) = (0.4706, 0.7020, 0.1961, 1)
        _SideRightColor ("Side Right Color", Color) = (0.3882, 0.6000, 0.1412, 1)
        _Origin ("Reveal Origin (World XYZ)", Vector) = (-0.1, 0, 0, 0)
        _RevealRadius ("Reveal Radius", Float) = 0.0
        _EdgeWidth ("Edge Glow Width", Float) = 0.22
        _EdgeColor ("Edge Glow Color", Color) = (0.95, 1.0, 0.72, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _GridColor;
                half4 _SideLeftColor;
                half4 _SideRightColor;
                float4 _Origin;
                float _RevealRadius;
                float _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float dist = distance(input.positionWS.xz, _Origin.xz);
                clip(_RevealRadius - dist);

                float3 n = normalize(input.normalWS);
                half3 baseCol;

                if (n.y > 0.5)
                {
                    baseCol = _TopColor.rgb;
                    // 1x1 월드 유닛 간격 타일 눈금 (x: [1,6], z: [-2.5,2.5])
                    float gx = abs(frac(input.positionWS.x) - 0.5);
                    float gz = abs(frac(input.positionWS.z + 0.5) - 0.5);
                    float lineX = step(0.475, gx);
                    float lineZ = step(0.475, gz);
                    float gridMask = saturate(max(lineX, lineZ)) * _GridColor.a;
                    baseCol = lerp(baseCol, _GridColor.rgb, gridMask);
                }
                else if (abs(n.x) > abs(n.z))
                {
                    baseCol = (n.x < 0.0) ? _SideLeftColor.rgb : _SideRightColor.rgb;
                }
                else
                {
                    baseCol = (n.z < 0.0) ? _SideLeftColor.rgb : _SideRightColor.rgb;
                }

                float edgeStart = max(0.0, _RevealRadius - _EdgeWidth);
                float edgeFactor = smoothstep(edgeStart, _RevealRadius, dist);
                half3 finalCol = lerp(baseCol, _EdgeColor.rgb, edgeFactor * 0.85);

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
