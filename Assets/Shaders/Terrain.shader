Shader "Custom/Terrain" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
        _GridTex ("Grid Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
        _BackgroundColor ("Background Color", Color) = (0,0,0)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.5

        #pragma multi_compile _ GRID_ON

        #pragma multi_compile _ HEX_MAP_EDIT_MODE

        #include "HexCellData.cginc"

        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input {
            float4 color : COLOR;
            float3 worldPos;
            float3 terrain;
            float4 visibility;
        };

        void vert (inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            float4 cell0 = GetCellData(v, 0);
            float4 cell1 = GetCellData(v, 1);
            float4 cell2 = GetCellData(v, 2);

            data.terrain.x = cell0.w;
            data.terrain.y = cell1.w;
            data.terrain.z = cell2.w;

            data.visibility.x = cell0.x;
            data.visibility.y = cell1.x;
            data.visibility.z = cell2.x;
            data.visibility.xyz = lerp(0.25, 1, data.visibility.xyz);
            data.visibility.w = 
                cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;
        }

        half _Glossiness;
        fixed3 _Specular;
        fixed4 _Color;
        half3 _BackgroundColor;

        sampler2D _GridTex;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float4 GetTerrainColor (Input IN, int index) {
            float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
            float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
            return c * (IN.color[index] * IN.visibility[index]);
        }

        void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
            // Albedo comes from a texture tinted by color
            fixed4 c = 
                GetTerrainColor(IN, 0) + 
                GetTerrainColor(IN, 1) + 
                GetTerrainColor(IN, 2);

            fixed4 grid = 1;
            #if defined(GRID_ON)
            float2 gridUV = IN.worldPos.xz;
                gridUV.x *= 1 / (4 * 8.66025404);
                gridUV.y *= 1 / (2 * 15.0);
                grid = tex2D(_GridTex, gridUV);
            #endif

            float explored = IN.visibility.w;
            o.Albedo = c.rgb * grid * _Color * explored;
            // Metallic and smoothness come from slider variables
            o.Specular = _Specular * explored;
            o.Smoothness = _Glossiness;
            o.Occlusion = explored;
            o.Emission = _BackgroundColor * (1 - explored);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
