// Silhueta do heroi e dos inimigos atras de paredes, pilares e caixotes (rubrica 1: o jogador nunca perde o personagem).
// Usado pelos RenderObjects do renderer (camadas Hero e Enemy), depois dos opacos:
//   passada 0 (Mask): marca no stencil os pixels do personagem que estao a vista;
//   passada 1 (Fill): pinta o que ficou atras de alguma coisa (ZTest Greater), menos o que ja esta marcado,
//   entao o braco atras do proprio corpo nao vira silhueta e as pecas sobrepostas nao escurecem duas vezes.
// Cor chapada com borda mais clara (fresnel), sem brilho: segue a paleta fosca do KayKit.
Shader "Quintessence/Silhouette"
{
    Properties
    {
        _Fill ("Preenchimento", Color) = (0.93, 0.86, 0.70, 0.38)
        _Rim ("Borda", Color) = (1.0, 0.95, 0.82, 0.85)
        _RimPower ("Largura da borda", Range(0.5, 6)) = 2.2
        [IntRange] _Ref ("Stencil: bit deste grupo", Range(0, 255)) = 2
        [IntRange] _ReadMask ("Stencil: bits lidos", Range(0, 255)) = 2
        [IntRange] _WriteMask ("Stencil: bits escritos", Range(0, 255)) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _Comp ("Stencil: teste da silhueta", Float) = 6
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Fill;
            half4 _Rim;
            half _RimPower;
        CBUFFER_END

        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

        Varyings Vert(Attributes v)
        {
            Varyings o;
            float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
            o.positionCS = TransformWorldToHClip(positionWS);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.viewWS = GetWorldSpaceViewDir(positionWS);
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "Mask"
            ZWrite Off
            ZTest LEqual
            ColorMask 0
            Offset -1, -1
            Stencil
            {
                Ref [_Ref]
                WriteMask [_WriteMask]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Fill"
            ZWrite Off
            ZTest Greater
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref [_Ref]
                ReadMask [_ReadMask]
                WriteMask [_WriteMask]
                Comp [_Comp]
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                half facing = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                half rim = pow(1.0h - facing, _RimPower);
                return lerp(_Fill, _Rim, rim);
            }
            ENDHLSL
        }
    }
}
