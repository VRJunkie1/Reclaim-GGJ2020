// From https://forum.unity.com/threads/interior-mapping.424676/#post-2751518
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Emil InteriorMapping"
// Adapted to Unity from http://www.humus.name/index.php?page=3D&ID=80
//Shader "Custom/InteriorMapping - Cubemap"
{
    Properties
    {
        _RoomCube("Room Cube Map", Cube) = "white" {}
        [Toggle(_USEOBJECTSPACE)] _UseObjectSpace("Use Object Space", Float) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100
            //Cull Front

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #pragma shader_feature _USEOBJECTSPACE

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                //#ifdef _USEOBJECTSPACE
                    float3 uvw : TEXCOORD0;

                    float3 viewDir : TEXCOORD1;
                };

                samplerCUBE _RoomCube;
                float4 _RoomCube_ST;

                // psuedo random
                float3 rand3(float co) {
                    return frac(sin(co * float3(12.9898,78.233,43.2316)) * 43758.5453);
                }

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);

                    // slight scaling adjustment to work around "noisy wall" when frac() returns a 0 on surface
                    o.uvw = v.vertex * _RoomCube_ST.xyx * 0.999 + _RoomCube_ST.zwz;
                    //o.uvw = -v.vertex * _RoomCube_ST.xyx * 0.999 + _RoomCube_ST.zwz;

                    // get object space camera vector
                    float4 objCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                    o.viewDir = v.vertex.xyz - objCam.xyz;
                    //o.viewDir = -v.vertex.xyz - objCam.xyz;

                    // adjust for tiling
                    o.viewDir *= _RoomCube_ST.xyx;

                    return o;
                }

                float LinearToDepth(float linearDepth)
                {
                    return (1.0 - _ZBufferParams.w * linearDepth) / (linearDepth * _ZBufferParams.z);
                }

                //fixed4 frag(v2f i) : SV_Target
                fixed4 frag(v2f i, out float depth : SV_Depth) : SV_Target
                {
                    // TODO: To get interior rooms from the backface polys, I could generate and pass along the object data, inverted, to rebuild the front side..? ...No it would still be projected to the back side, and perspective warped -__-

                    // room uvws
                    float3 roomUVW = frac(i.uvw);
                    //roomUVW = float3(roomUVW.x, 0, roomUVW.z);

                    // raytrace box from object view dir
                    float3 pos = roomUVW * 2.0 - 1.0;
                    //return float4(pos, 1);
                    float3 id = 1.0 / i.viewDir;
                    //id = float3(id.x, 0, id.z);
                    //return float4(i.viewDir, 1);
                    float3 k = abs(id) - pos * id;
                    //return float4(k, 1);
                    //return float4(k.z/1, 0, 0, 1);
                    float kMin = min(min(k.x, k.y), k.z);
                    //kMin = k.y;   // only horizontal surface appears correctly, if this is set
                    //return float4(kMin, 0, 0, 1);
                    pos += kMin * i.viewDir;
                    //return float4(pos, 1);

                    // depth
                    float3 flooredUV = floor(i.uvw);
                    //float3 realPos = (pos + (flooredUV * 2) + 1) / 4;// +float3(.5, .5, .5);
                    float3 realPos = (pos  + ((flooredUV+.5 - _RoomCube_ST.zwz) * 2)) / (_RoomCube_ST.xyx* 2);// +float3(.5, .5, .5);
                    //return float4(realPos, 1);

                    /// https://forum.unity.com/threads/help-with-view-space-normals.454248/  code to fix the normals being off from the '2D' projection
                    float2 screenUV = float2(i.pos.x / _ScreenParams.x, i.pos.y / _ScreenParams.y);	// TODO: Can all this go in the vert shader?
                    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
                    float3 screenDir = -normalize(float3((screenUV * 2 - 1) / p11_22, -1));

                    float4 worldPos = mul(unity_ObjectToWorld, float4(realPos, 1.0));
                    float3 cameraPos = (worldPos - _WorldSpaceCameraPos);
                    float d3 = length(cameraPos) * screenDir.z;

                    depth = LinearToDepth(d3);

                    //return float4(pos, 1);


                    // sample room cube map
                    fixed4 room = texCUBE(_RoomCube, pos.xyz);
                    return fixed4(room.rgb, 1.0);
                }


                ENDCG
            }
        }
}