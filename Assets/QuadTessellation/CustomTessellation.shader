Shader "Custom/Tessellation"
{
    Properties
    {
        [IntRange] _TessellationOutside ("Tessellation Outside", Range(1, 64)) = 1
        [IntRange] _TessellationInside ("Tessellation Inside", Range(1, 64)) = 1

        _GradientMap("Gradient map", 2D) = "white" {}
    }

    CGINCLUDE

    #pragma multi_compile_fwdbase nolightmap

    #include "AutoLight.cginc"
    #include "UnityCG.cginc"
    #pragma target 4.6


    #pragma vertex PreGeometryVert
    #pragma hull hull
    #pragma domain domain
    #pragma geometry geom
    #pragma fragment frag

    sampler2D _GradientMap;

    float _TessellationOutside;
    float _TessellationInside;

    /////////////
    // Structs //
    /////////////

    struct appdata
    {
        float4 vertex : POSITION;
        float4 normal : NORMAL;
    };

    struct v2g
    {
        float4 vertex : POSITION;
        float4 normal : NORMAL;
    };

    struct g2f
    {
        float4 pos : SV_POSITION;
        float3 worldNormal : NORMAL;
        float4 col : COLOR;
        float3 worldPos : TEXCOORD0;
        float2 uv : TEXCOORD1;
        SHADOW_COORDS(2)
    };

    struct TessellationFactors
    {
        float edges[4] : SV_TessFactor;
        float inside[2] : SV_InsideTessFactor;
    };

    /////////////////////////
    // Auxiliary Functions //
    /////////////////////////

    TessellationFactors patchConstantFunction (InputPatch<v2g, 4> patch)
    {
        TessellationFactors f;
        f.edges[0] = _TessellationOutside;
        f.edges[1] = _TessellationOutside;
        f.edges[2] = _TessellationOutside;
        f.edges[3] = _TessellationOutside;
        f.inside[0] = _TessellationInside;
        f.inside[1] = _TessellationInside;
        return f;
    }

    //////////////////////////////////////////////////
    // vertex, hull, geometry and fragment programs //
    //////////////////////////////////////////////////

    v2g PreGeometryVert(appdata input)
    {
        v2g output;

        output.vertex = input.vertex;
        output.normal = input.normal;

        return output;
    }

    g2f PostTesselation(v2g input)
    {
        g2f output;

        output.pos = input.vertex;
        output.worldNormal = input.normal;

        // if there is no geometry shader, these two lines need to be uncommented!
        // output.pos = UnityObjectToClipPos(input.vertex);
        // output.worldPos = mul(unity_ObjectToWorld, output.pos);

        return output;
    }

    g2f PostGeometryVert(float4 pos, float2 uv, fixed4 col, float3 normal)
    {
        g2f output;

        UNITY_INITIALIZE_OUTPUT(g2f, output);

        output.worldPos = mul(unity_ObjectToWorld, pos);
        output.worldNormal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;
        output.pos = UnityObjectToClipPos(pos);
        output.uv = uv;
        output.col = col;

        TRANSFER_SHADOW(output);

        return output;
    }

    [UNITY_domain("quad")]
    [UNITY_outputcontrolpoints(4)]
    [UNITY_outputtopology("triangle_cw")]
    [UNITY_partitioning("integer")]
    [UNITY_patchconstantfunc("patchConstantFunction")]
    v2g hull (InputPatch<v2g, 4> patch, uint id : SV_OutputControlPointID)
    {
        return patch[id];
    }

    [UNITY_domain("quad")]
    g2f domain(TessellationFactors factors, OutputPatch<v2g, 4> patch, float2 UV : SV_DomainLocation)
    {
        v2g v;

        UNITY_INITIALIZE_OUTPUT(v2g, v);

        // with this version slashes will become backslashes and backslashes will become slashes!
        // #define DOMAIN_INTERPOLATE(fieldName) v.fieldName = lerp(lerp(patch[0].fieldName, patch[1].fieldName, UV.x), lerp(patch[3].fieldName, patch[2].fieldName, UV.x), UV.y);

        #define DOMAIN_INTERPOLATE(fieldName) v.fieldName = lerp(lerp(patch[3].fieldName, patch[2].fieldName, UV.y), lerp(patch[0].fieldName, patch[1].fieldName, UV.y), UV.x);

        DOMAIN_INTERPOLATE(vertex)
        DOMAIN_INTERPOLATE(normal)

        return PostTesselation(v);
    }

    [maxvertexcount(48)] // 16 * 3 vertices
    void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
    {
        // original vertices
        for (int i = 0; i < 3; ++i) triStream.Append(PostGeometryVert(input[i].vertex, input[i].vertex.xz, fixed4(0,0,0,1), input[i].normal));
        triStream.RestartStrip();
    }

    ENDCG

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode"       = "ForwardBase"
                "Queue"           = "AlphaTest"
                "IgnoreProjector" = "True"
                "RenderType"      = "Vegetation"
            }

            CGPROGRAM
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdbase nolightmap

            fixed4 frag (g2f i) : SV_Target
            {
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                fixed4 col = tex2D(_GradientMap, i.uv);

                clip(col.a - 0.5f);

                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);

                half NdotL = dot (i.worldNormal, lightDirection);

                return col * atten * NdotL;
            }
            ENDCG
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            CGPROGRAM
            #pragma multi_compile_shadowcaster

            fixed4 frag (g2f i) : SV_Target
            {
                fixed4 col = tex2D(_GradientMap, i.uv);

                clip(col.a - 0.5f);

                SHADOW_CASTER_FRAGMENT(i)
            }

            ENDCG
        }
    }
}
