Shader "Matter/SoftCircle"
{
    // This shader renders a soft, radial gradient circle
    // Used for liquid particles before metaball threshold is applied
    // URP Compatible
    
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Softness ("Softness", Range(0.0, 1.0)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SoftCircle"
            Tags { "LightMode" = "Universal2D" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Center UV
                float2 center = input.uv - 0.5;
                
                // Distance from center (0 at center, 0.5 at edge)
                float dist = length(center) * 2.0;
                
                // Create soft falloff
                float alpha = 1.0 - smoothstep(1.0 - _Softness, 1.0, dist);
                
                // Apply color with calculated alpha
                half4 col = _Color;
                col.a *= alpha;
                
                return col;
            }
            ENDHLSL
        }
    }
    
    // Fallback for Built-in RP
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = i.uv - 0.5;
                float dist = length(center) * 2.0;
                float alpha = 1.0 - smoothstep(1.0 - _Softness, 1.0, dist);
                
                float4 col = _Color;
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
}
