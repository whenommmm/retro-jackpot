// Upscales the low-resolution frame with hard pixel edges and adds faint
// CRT scanlines and a soft vignette. Used by RetroScreenEffect.
Shader "Hidden/SlotMachine/RetroUpscale"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _VirtualHeight ("Virtual Height", Float) = 720
        _Scanlines ("Scanline Strength", Range(0, 0.5)) = 0.1
        _Vignette ("Vignette Strength", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _VirtualHeight;
            float _Scanlines;
            float _Vignette;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);

                // One darker line every two virtual pixel rows. A two-row period
                // stays even when the frame is scaled by a non-integer amount.
                float row = frac(i.uv.y * _VirtualHeight * 0.5);
                c.rgb *= 1.0 - _Scanlines * step(0.66, row);

                float2 d = i.uv - 0.5;
                c.rgb *= 1.0 - _Vignette * dot(d, d) * 1.6;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
