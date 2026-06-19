Shader "Game3C/Player Direction"
{
    Properties
    {
        _BodyColor ("Body Color", Color) = (0.42, 0.42, 0.42, 1)
        _FrontColor ("Front (+Z) Color", Color) = (0.05, 0.35, 1, 1)
        _BackColor ("Back (-Z) Color", Color) = (1, 0.08, 0.05, 1)
        _MarkerRadius ("Marker Radius", Range(0.05, 0.4)) = 0.23
        _MarkerHeight ("Marker Height", Range(-0.45, 0.45)) = 0.1
        _MarkerEmission ("Marker Emission", Range(0, 1)) = 0.35
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        fixed4 _BodyColor;
        fixed4 _FrontColor;
        fixed4 _BackColor;
        half _MarkerRadius;
        half _MarkerHeight;
        half _MarkerEmission;
        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float3 localPosition;
        };

        void vert(inout appdata_full vertexData, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.localPosition = vertexData.vertex.xyz;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float2 markerOffset = float2(
                input.localPosition.x,
                input.localPosition.y - _MarkerHeight
            );
            float markerDistance = length(markerOffset);
            float edgeSoftness = max(fwidth(markerDistance), 0.002);
            float circleMask = 1.0 - smoothstep(
                _MarkerRadius - edgeSoftness,
                _MarkerRadius + edgeSoftness,
                markerDistance
            );

            float frontMask = circleMask * step(0.0, input.localPosition.z);
            float backMask = circleMask * step(input.localPosition.z, 0.0);

            fixed3 color = _BodyColor.rgb;
            color = lerp(color, _FrontColor.rgb, frontMask);
            color = lerp(color, _BackColor.rgb, backMask);

            output.Albedo = color;
            output.Emission =
                (_FrontColor.rgb * frontMask + _BackColor.rgb * backMask)
                * _MarkerEmission;
            output.Metallic = _Metallic;
            output.Smoothness = _Smoothness;
            output.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Standard"
}
