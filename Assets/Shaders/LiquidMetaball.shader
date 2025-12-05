Shader "Matter/LiquidMetaball"
{
    Properties
    {
        _MainTex ("Metaball Texture", 2D) = "white" {}
        _Threshold ("Blob Threshold", Range(0.1, 0.9)) = 0.5
        _Softness ("Edge Softness", Range(0.01, 0.3)) = 0.1
        _InnerColor ("Inner Color", Color) = (0.2, 0.5, 1, 1)
        _OuterColor ("Outer Color", Color) = (0.5, 0.8, 1, 0.8)
        _EdgeColor ("Edge Color", Color) = (0.8, 0.95, 1, 1)
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
            Name "LiquidMetaball"
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Threshold;
                float _Softness;
                float4 _InnerColor;
                float4 _OuterColor;
                float4 _EdgeColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the metaball texture (accumulated particle alphas)
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half density = tex.a;
                
                // Apply threshold with smoothstep for soft edges
                half blob = smoothstep(_Threshold - _Softness, _Threshold + _Softness, density);
                
                // If below threshold, discard (transparent)
                if (blob < 0.01)
                    discard;
                
                // Create gradient based on density
                // Higher density = inner color, lower density = outer color
                half gradientFactor = smoothstep(_Threshold, 1.0, density);
                
                // Mix between outer and inner color based on density
                half4 baseColor = lerp(_OuterColor, _InnerColor, gradientFactor);
                
                // Add edge highlight
                half edgeFactor = 1.0 - smoothstep(_Threshold, _Threshold + _Softness * 2, density);
                half4 finalColor = lerp(baseColor, _EdgeColor, edgeFactor * 0.5);
                
                // Apply blob alpha
                finalColor.a *= blob;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    // Fallback for non-URP (Built-in RP)
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;
            float _Softness;
            float4 _InnerColor;
            float4 _OuterColor;
            float4 _EdgeColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float density = tex.a;
                
                float blob = smoothstep(_Threshold - _Softness, _Threshold + _Softness, density);
                
                if (blob < 0.01)
                    discard;
                
                float gradientFactor = smoothstep(_Threshold, 1.0, density);
                float4 baseColor = lerp(_OuterColor, _InnerColor, gradientFactor);
                
                float edgeFactor = 1.0 - smoothstep(_Threshold, _Threshold + _Softness * 2, density);
                float4 finalColor = lerp(baseColor, _EdgeColor, edgeFactor * 0.5);
                
                finalColor.a *= blob;
                
                return finalColor;
            }
            ENDCG
        }
    }
}
