Shader "Caravans/VisionFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.05, 0.04, 0.03, 0.84)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.22
        _Soft ("Soft", Float) = 0.035
        _Aspect ("Aspect", Float) = 1.777
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _Center;
            float _Radius;
            float _Soft;
            float _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 delta = i.uv - _Center.xy;
                delta.x *= _Aspect;
                float dist = length(delta);
                float edge = smoothstep(_Radius, _Radius + _Soft, dist);
                fixed4 color = _Color;
                color.a *= edge * i.color.a;
                return color;
            }
            ENDCG
        }
    }
}
