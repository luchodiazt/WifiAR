Shader "WifiAR/WaveField"
{
    Properties
    {
        [NoScaleOffset] _DataTex ("Signal (R) Confidence (G) Band (B)", 2D) = "black" {}
        _ColorWeak ("Weak", Color) = (0.9, 0.28, 0.3, 1)
        _ColorMid ("Mid", Color) = (1, 0.77, 0.24, 1)
        _ColorStrong ("Strong", Color) = (0.19, 0.84, 0.78, 1)
        _Wavelength24 ("Ring spacing 2.4 GHz (m)", Float) = 1.0
        _Wavelength5 ("Ring spacing 5 GHz (m)", Float) = 0.5
        _Speed ("Rings per second", Float) = 0.6
        _BaseAlpha ("Fill between rings", Range(0, 1)) = 0.16
        _HintAlpha ("Unmeasured hint", Range(0, 1)) = 0.14
        _HintRadius ("Unmeasured hint radius (m)", Float) = 4

        // Set from WaveFieldRenderer. Declared so the values survive a shader reload.
        [HideInInspector] _GridCenter ("Grid center (world XZ)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RouterPos ("Router (world XZ)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _GridSpan ("Grid span (m)", Float) = 32
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "WaveField"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_DataTex);
            SAMPLER(sampler_DataTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorWeak;
                half4 _ColorMid;
                half4 _ColorStrong;
                float4 _GridCenter;
                float4 _RouterPos;
                float _GridSpan;
                float _Wavelength24;
                float _Wavelength5;
                float _Speed;
                half _BaseAlpha;
                half _HintAlpha;
                float _HintRadius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                return output;
            }

            half Crest(float distanceMeters, float wavelength)
            {
                // Minus time: crests travel away from the router.
                float phase = distanceMeters / wavelength - _Time.y * _Speed;
                return 0.5 + 0.5 * cos(6.28318530718 * phase);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                float2 uv = (p - _GridCenter.xy) / _GridSpan + 0.5;
                half4 data = SAMPLE_TEXTURE2D(_DataTex, sampler_DataTex, uv);
                half signal = data.r;
                half confidence = data.g;
                half band = data.b;

                float dist = distance(p, _RouterPos.xy);

                // 5 GHz waves are about half as long as 2.4 GHz ones, so its rings sit closer together.
                // Cross-fade the two patterns: blending the wavelength itself would bend the rings
                // wherever the band changes.
                half crest = lerp(Crest(dist, _Wavelength24), Crest(dist, _Wavelength5), band);

                // Strong signal: broad, bright crests. Weak signal: thin, faint ones.
                half ring = pow(abs(crest), lerp(10.0, 2.5, signal));

                half3 color = signal < 0.5
                    ? lerp(_ColorWeak.rgb, _ColorMid.rgb, signal * 2.0)
                    : lerp(_ColorMid.rgb, _ColorStrong.rgb, signal * 2.0 - 1.0);

                half measured = confidence * (_BaseAlpha + (1.0 - _BaseAlpha) * ring * lerp(0.4, 1.0, signal));

                // Nothing measured here yet: neutral rings near the router show where the waves start.
                half hint = (1.0 - confidence) * pow(abs(Crest(dist, _Wavelength5)), 12.0)
                    * _HintAlpha * saturate(1.0 - dist / _HintRadius);

                half core = smoothstep(0.3, 0.0, dist);

                half3 rgb = lerp(half3(1, 1, 1), color, confidence);
                rgb = lerp(rgb, half3(1, 1, 1), core);
                half alpha = saturate(measured + hint + core * 0.9);

                float2 edge = abs(uv - 0.5) * 2.0;
                alpha *= 1.0 - smoothstep(0.9, 1.0, max(edge.x, edge.y));

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
