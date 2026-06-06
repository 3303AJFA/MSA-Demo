// WallDissolve.shader — URP 17 (Unity 6.4) unlit с экранной круглой дыркой по позиции героя.
// Текстовый HLSL/ShaderLab, НЕ Shader Graph.
//
// Контроллер (WallDissolveController) кормит per-renderer через MaterialPropertyBlock:
//   _Position (viewport 0..1) — центр дырки (положение героя на экране)
//   _Size (0..1)              — текущая интенсивность дырки (0 = стена целая, 1 = полная дырка)
//
// Остальные свойства настраиваются в материале (_BaseMap, _Radius, _NoiseScale, _NoiseStrength,
// _NoiseOctaves, _EdgeSoftness, _DripStrength, _DripScale, _RimColor, _RimWidth, _RimStrength,
// _ScatterStrength).

Shader "MSA/WallDissolve"
{
    Properties
    {
        [Header(Base)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Hole Position and Size)]
        _Position("Hole Position (viewport)", Vector) = (0.5, 0.5, 0, 0)
        _Size("Hole Size (0..1)", Range(0, 1)) = 0
        _Radius("Hole Base Radius (0..1)", Range(0, 1)) = 0.25

        [Header(Edge Shape)]
        _EdgeSoftness("Edge Softness (0=sharp, 1=painterly bleed)", Range(0, 1)) = 0.6
        _NoiseScale("Noise Scale (base frequency)", Float) = 30
        _NoiseStrength("Noise Strength (perturbation amplitude)", Range(0, 0.5)) = 0.45
        [IntRange] _NoiseOctaves("Noise Octaves (1=blob, 4=crumble)", Range(1, 4)) = 3
        _AlphaClipThreshold("Alpha Clip Threshold", Range(0, 0.5)) = 0.1

        [Header(Inner Rim Depth)]
        _RimColor("Rim Color (inner dark band)", Color) = (0.058, 0.078, 0.063, 1)
        _RimWidth("Rim Width (0..1)", Range(0, 1)) = 0.15
        _RimStrength("Rim Strength (0..1)", Range(0, 1)) = 0.6

        [Header(Scatter Crumble)]
        _ScatterStrength("Scatter Strength (0..1)", Range(0, 1)) = 0.3

        [Header(Drip Streaks)]
        _DripStrength("Drip Strength (0..1)", Range(0, 1)) = 0.35
        _DripScale("Drip Scale", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _Position;
                float  _Size;
                float  _Radius;
                float  _EdgeSoftness;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _NoiseOctaves;
                float  _AlphaClipThreshold;
                float4 _RimColor;
                float  _RimWidth;
                float  _RimStrength;
                float  _ScatterStrength;
                float  _DripStrength;
                float  _DripScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            // Хэш-функция и value-noise: базовая октава процедурного шума.
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian motion: суммирует 1-4 октавы value-noise с убывающей амплитудой
            // и растущей частотой. Низкие октавы дают форму дырки, высокие — крошку на кромке.
            // lacunarity = 2 (freq *= 2), gain = 0.5 (amp *= 0.5).
            float Fbm(float2 p, int octaves)
            {
                float total = 0.0;
                float amp = 1.0;
                float freq = 1.0;
                float maxAmp = 0.0;

                [unroll(4)]
                for (int oct = 0; oct < 4; oct++)
                {
                    float gate = step((float)oct, (float)(octaves - 1));
                    total += ValueNoise(p * freq) * amp * gate;
                    maxAmp += amp * gate;
                    amp *= 0.5;
                    freq *= 2.0;
                }

                return total / max(maxAmp, 0.0001);
            }

            // Только высшая используемая октава — для scatter крошки (точечные пятна).
            float HighOctaveNoise(float2 p, int octaves)
            {
                float freq = 1.0;
                [unroll(4)]
                for (int oct = 1; oct < 4; oct++)
                {
                    float gate = step((float)oct, (float)(octaves - 1));
                    freq = lerp(freq, freq * 2.0, gate);
                }
                return ValueNoise(p * freq);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Поверхность стены: текстура × tint. _BaseMap="white" по умолчанию, при пустой
                // текстуре отображается чистый _BaseColor. Растворение НЕ перекрашивает поверхность —
                // влияет только на alpha и на тёмный кант (rim).
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 toCenter = screenUV - _Position.xy;
                toCenter.x *= aspect;
                float dist = length(toCenter);

                int octaves = max(1, (int)round(_NoiseOctaves));

                // fbm-шум по экрану: фрактальная рассыпчатость кромки.
                float edgeFbm = Fbm(screenUV * _NoiseScale, octaves) - 0.5;
                float noiseOffset = edgeFbm * _NoiseStrength * _Size;

                // Drip: вертикальные потёки книзу (стекающая сырость / грязь).
                float2 dripUV = float2(screenUV.x * _DripScale, screenUV.y * _DripScale * 0.18);
                float dripNoise = ValueNoise(dripUV);
                float dripColumns = smoothstep(0.45, 1.0, dripNoise);
                float downwardMask = saturate((_Position.y - screenUV.y) * 2.0);
                float dripOffset = dripColumns * downwardMask * _Radius * 0.6 * _DripStrength * _Size;

                float radius = _Radius * _Size;
                float perturbedDist = dist - noiseOffset - dripOffset;

                // Расширенный потолок softness: lerp(0.001, 0.5) — на 1 даёт широкий акварельный bleed,
                // не «чуть мягкий clip» как раньше.
                float softWidth = lerp(0.001, 0.5, _EdgeSoftness);

                // outsideHole: 1 снаружи дырки (стена), 0 внутри (дырка).
                float outsideHole = smoothstep(radius - softWidth, radius + softWidth, perturbedDist);
                float wallAlpha = lerp(1.0, outsideHole, _Size);

                // === Scatter крошка ===
                // Высокочастотная маска выбивает мелкие пятна в полосе сразу ЗА основной кромкой —
                // край осыпается отлетающими частицами, не остаётся чистым блобом.
                float scatterBandCenter = radius + softWidth * 0.8;
                float scatterBandHalfWidth = softWidth * 1.5 + 0.02;
                float scatterBand = saturate(1.0 - abs(perturbedDist - scatterBandCenter) / scatterBandHalfWidth);

                float scatterNoise = HighOctaveNoise(screenUV * _NoiseScale, octaves);
                // smoothstep с высоким порогом → только верхние пики шума выбиваются как точки
                float scatterMask = smoothstep(0.72, 0.88, scatterNoise) * scatterBand * _ScatterStrength * _Size;
                wallAlpha *= (1.0 - scatterMask);

                // === Внутренний тёмный кант (фейк-глубина) ===
                // Узкая полоса на стороне СТЕНЫ сразу за кромкой дырки — затемняется _RimColor.
                // Создаёт ощущение толщины пробитой стены без перехода в Lit.
                // Tent-функция: пик у самой кромки, fade вглубь стены.
                float rimBandWidth = max(_RimWidth * (softWidth + 0.08), 0.001);
                float distPastEdge = perturbedDist - radius;
                // peak около половины rimBandWidth, гаснет на 0 (внутри дырки) и rimBandWidth (вглубь стены)
                float rimEnvelope = smoothstep(0.0, rimBandWidth * 0.5, distPastEdge)
                                  * (1.0 - smoothstep(rimBandWidth * 0.5, rimBandWidth * 1.5, distPastEdge));
                float rimFactor = rimEnvelope * wallAlpha * _RimStrength * _Size;
                color.rgb = lerp(color.rgb, _RimColor.rgb, rimFactor);

                color.a *= wallAlpha;
                clip(color.a - _AlphaClipThreshold);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
