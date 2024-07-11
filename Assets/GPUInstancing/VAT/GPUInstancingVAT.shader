Shader "Unlit/GPUInstancingVAT"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PosTex("position texture", 2D) = "black"{}
        _NmlTex("normal texture", 2D) = "white"{}
        _DT ("delta time", float) = 0
        _Length ("animation length", Float) = 1
        [Toggle(ANIM_LOOP)] _Loop("loop", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100 Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile ___ ANIM_LOOP

            #include "UnityCG.cginc"

            #define ts _PosTex_TexelSize

            struct appdata
            {
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            struct VATData {
                float3 position;
                float animationOffset;
            };

            sampler2D _MainTex, _PosTex, _NmlTex;
            float4 _PosTex_TexelSize;
            float _Length, _DT;
            
            StructuredBuffer<VATData> _VATObjectDataBuffer;

            v2f vert (appdata v, uint vid : SV_VertexID, uint instanceID : SV_INSTANCEID)
            {
                //float dt = _VATObjectDataBuffer[instanceID].animationOffset;
                float3 worldPos = _VATObjectDataBuffer[instanceID].position;
                // 時間 - オフセット
                float t = (_Time.y) % _Length;
#if ANIM_LOOP
                t = fmod(t, 1.0);
#else
                t = saturate(t);
#endif
                float x = (vid + 0.5) * ts.x;
                float y = t;
                float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
                float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0));

                v2f o;
                // アニメーションしている頂点の座標位置 + ワールド座標
                o.vertex = UnityObjectToClipPos(pos + worldPos);
                o.normal = UnityObjectToWorldNormal(normal);
                o.uv = v.uv;
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                half diff = dot(i.normal, float3(0,1,0))*0.5 + 0.5;
                half4 col = tex2D(_MainTex, i.uv);
                return diff * col;
            }
            ENDCG
        }
    }
}