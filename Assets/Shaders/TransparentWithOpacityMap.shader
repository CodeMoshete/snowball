Shader "UI/Unlit/TransparentWithOpacityMap"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _OpacityMap ("Opacity Map", 2D) = "white" {}
        _ThresholdBase ("Threshold Base", Range(0,1)) = 0.5
        _ThresholdOpacity ("Threshold Opacity", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha // Standard Alpha Blending
        ZWrite Off  // Disable depth writing for proper transparency sorting
        Cull Off  // Render from both sides
        Lighting Off  // Unlit shader

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // Works on mobile and desktop

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            sampler2D _OpacityMap;
            float _ThresholdBase;
            float _ThresholdOpacity;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_BaseMap, i.uv);
                float opacity = tex2D(_OpacityMap, i.uv).r; // Grayscale from red channel
                opacity = lerp(opacity, 1, _ThresholdOpacity); // Apply threshold
                // opacity = max(opacity, _Threshold); // Apply threshold

                baseColor.a *= opacity * _ThresholdBase; // Adjust alpha based on opacity map

                return baseColor;
            }
            ENDHLSL
        }
    }
}