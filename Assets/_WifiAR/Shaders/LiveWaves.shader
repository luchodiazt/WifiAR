// Full-screen UI overlay: rings rising from the bottom of the screen, driven only by the live
// signal. Needs no AR tracking, so there is always something to see.
Shader "WifiAR/LiveWaves"
{
    Properties
    {
        _Color ("Color", Color) = (0.19, 0.84, 0.78, 1)
        _Signal ("Signal", Range(0, 1)) = 0.5
        _Spacing ("Ring spacing (screen widths)", Float) = 0.12
        _Speed ("Rings per second", Float) = 0.6
        _Intensity ("Intensity", Range(0, 1)) = 1
        _Aspect ("Screen height / width", Float) = 2.16

        // Unused, but UI graphics expect the property to exist.
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Signal;
            float _Spacing;
            float _Speed;
            float _Intensity;
            float _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from the bottom centre of the screen, measured in screen widths so rings stay round.
                float2 p = float2(i.uv.x - 0.5, i.uv.y * _Aspect);
                float dist = length(p);

                float phase = dist / _Spacing - _Time.y * _Speed;
                float crest = 0.5 + 0.5 * cos(6.28318530718 * phase);
                float ring = pow(abs(crest), lerp(10.0, 2.5, _Signal));

                // Strong signal climbs the whole screen; weak signal dies out near the bottom.
                float reach = lerp(0.35, 1.9, _Signal);
                float fade = saturate(1.0 - dist / reach);

                // Capped well below opaque: the camera view has to stay readable behind the rings.
                float alpha = ring * fade * fade * lerp(0.3, 0.6, _Signal) * _Intensity;
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
