Shader "Matter/LiquidBlob"
{
    // Simple, reliable shader for the marching squares liquid blob mesh
    // Works with URP 2D
    
    Properties
    {
        _Color ("Main Color", Color) = (0.2, 0.5, 1, 1)
        _InnerColor ("Inner Color", Color) = (0.1, 0.3, 0.8, 1)
        _OuterColor ("Outer Color", Color) = (0.4, 0.7, 1, 0.9)
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
            Name "LiquidBlob"
            Tags { "LightMode" = "Universal2D" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _InnerColor;
                float4 _OuterColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Simple gradient based on UV
                float2 centeredUV = input.uv - 0.5;
                float dist = saturate(length(centeredUV) * 2.0);
                
                // Blend between inner and outer color
                half4 finalColor = lerp(_InnerColor, _OuterColor, dist);
                
                // Use vertex color alpha if provided
                finalColor.a *= input.color.a;
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Additional pass for SRP Batcher compatibility
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
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
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _Color;
            float4 _InnerColor;
            float4 _OuterColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                float dist = saturate(length(centeredUV) * 2.0);
                
                float4 finalColor = lerp(_InnerColor, _OuterColor, dist);
                finalColor.a *= i.color.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    // Ultimate fallback
    Fallback "Sprites/Default"
}
