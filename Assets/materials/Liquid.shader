// Rocking fluid container shader from https://www.reddit.com/r/Unity3D/comments/8d5uf5/stylized_simple_liquid_shader_shader_code/
// Interior shader from https://forum.unity.com/threads/interior-mapping.424676/#post-2751518 (depth test code by me)


Shader "Unlit/SpecialFX/Liquid"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _FillAmount("Fill Amount", Range(-10,10)) = 0.0
        [HideInInspector] _WobbleX("WobbleX", Range(-1,1)) = 0.0
        [HideInInspector] _WobbleZ("WobbleZ", Range(-1,1)) = 0.0
        _TopColor("Top Color", Color) = (1,1,1,1)
        _FoamColor("Foam Line Color", Color) = (1,1,1,1)
        _Rim("Foam Line Width", Range(0,0.1)) = 0.0
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0,10)) = 0.0

        _RoomCube("Room Cube Map", Cube) = "white" {}    // interior
    }

        SubShader
        {
            Tags {"Queue" = "Geometry"  "DisableBatching" = "True" }

            Pass
            {
             //Zwrite On
            //ZTest Always 
            Cull Off // we want the front and back faces
             AlphaToMask On // transparency

             CGPROGRAM


             #pragma vertex vert
             #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
              float4 vertex : POSITION;
              float2 uv : TEXCOORD0;
              float3 normal : NORMAL;
            };

            struct v2f
            {
               float2 uv : TEXCOORD0;
               UNITY_FOG_COORDS(1)
               float4 vertex : SV_POSITION;
               float3 viewDir : COLOR;
               float3 normal : COLOR2;
               float fillEdge : TEXCOORD2;

               float3 objSpace : TEXCOORD3;  //uvw
               float3 camOffset : TEXCOORD4;  //viewDir
               //float3 depth : TEXCOORD5;
               float3 objSpaceActual : TEXCOORD5;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FillAmount, _WobbleX, _WobbleZ;
            float4 _TopColor, _RimColor, _FoamColor, _Tint;
            float _Rim, _RimPower;

            samplerCUBE _RoomCube;  // interior textures
            float4 _RoomCube_ST;    // texture tiling and offset

            float4 RotateAroundYInDegrees(float4 vertex, float degrees)
            {
               float alpha = degrees * UNITY_PI / 180;
               float sina, cosa;
               sincos(alpha, sina, cosa);
               float2x2 m = float2x2(cosa, sina, -sina, cosa);
               return float4(vertex.yz , mul(m, vertex.xz)).xzyw;
            }


            v2f vert(appdata v)
            {
               v2f o;

               o.vertex = UnityObjectToClipPos(v.vertex);
               o.uv = TRANSFORM_TEX(v.uv, _MainTex);
               UNITY_TRANSFER_FOG(o,o.vertex);
               // get world position of the vertex
               float3 worldPos = mul(unity_ObjectToWorld, v.vertex.xyz);
               // rotate it around XY
               float3 worldPosX = RotateAroundYInDegrees(float4(worldPos,0),360);
               // rotate around XZ
               float3 worldPosZ = float3 (worldPosX.y, worldPosX.z, worldPosX.x);
               // combine rotations with worldPos, based on sine wave from script
               float3 worldPosAdjusted = worldPos + (worldPosX * _WobbleX) + (worldPosZ * _WobbleZ);
               // how high up the liquid is
               o.fillEdge = worldPosAdjusted.y + _FillAmount;

               o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
               o.normal = v.normal;


               // Interior shader
                // slight scaling adjustment to work around "noisy wall" when frac() returns a 0 on surface
               o.objSpaceActual = v.vertex;
               o.objSpace = v.vertex * _RoomCube_ST.xyx * 0.999 + _RoomCube_ST.zwz;

                // get object space camera vector
               float4 objCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
               o.camOffset = v.vertex.xyz - objCam.xyz;
               // adjust for tiling
               o.camOffset *= _RoomCube_ST.xyx;

               //float2 depthTemp = float2(0, 0);;
               //UNITY_TRANSFER_DEPTH(depthTemp);
               //o.depth = float3(depthTemp, 0);

               return o;
            }

            float LinearToDepth(float linearDepth)
            {
                return (1.0 - _ZBufferParams.w * linearDepth) / (linearDepth * _ZBufferParams.z);
            }

            fixed4 frag(v2f i, fixed facing : VFACE, out float depth : SV_Depth) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Tint;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                // rim light
                float dotProduct = 1 - pow(dot(i.normal, i.viewDir), _RimPower);
                float4 RimResult = smoothstep(0.5, 1.0, dotProduct);
                RimResult *= _RimColor;

                // foam edge
                float4 foam = (step(i.fillEdge, 0.5) - step(i.fillEdge, (0.5 - _Rim)));
                float4 foamColored = foam * (_FoamColor * 0.9);
                // rest of the liquid
                float4 result = step(i.fillEdge, 0.5) - foam;
                float4 resultColored = result * col;
                // both together, with the texture
                float4 finalResult = resultColored + foamColored;
                finalResult.rgb += RimResult;

                // color of backfaces/ top
                float4 topColor = _TopColor * (foam + result);
                //VFACE returns positive for front facing, negative for backfacing
                

                // Interiors
                float3 roomUVW = frac(i.objSpace);
                //roomUVW = float3(roomUVW.x, 0, roomUVW.z);

                // raytrace box from object view dir
                float3 pos = roomUVW * 2.0 - 1.0;
                float3 posRec = pos;
                float3 id = 1.0 / i.camOffset;
                //id = float3(id.x, 0, id.z);
                float3 k = abs(id) - pos * id;
                float kMin = min(min(k.x, k.y), k.z);
                pos += kMin * i.camOffset;

                //return float4(pos, 1);

                // depth
                float3 flooredUV = floor(i.objSpace);
                //float3 realPos = (pos + (flooredUV * 2) + 1) / 4;// +float3(.5, .5, .5);
                float3 realPosInterior = (pos + ((flooredUV + .5 - _RoomCube_ST.zwz) * 2)) / (_RoomCube_ST.xyx * 2);// +float3(.5, .5, .5);
                //return float4(realPos, 1);
                float3 realPos = realPosInterior;
                if (facing > 0 && finalResult.w > 0) realPos = i.objSpaceActual;

                /// https://forum.unity.com/threads/help-with-view-space-normals.454248/  code to fix the normals being off from the '2D' projection
                float2 screenUV = float2(i.vertex.x / _ScreenParams.x, i.vertex.y / _ScreenParams.y);	// TODO: Can all this go in the vert shader?
                float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
                float3 screenDir = -normalize(float3((screenUV * 2 - 1) / p11_22, -1));

                float4 worldPos = mul(unity_ObjectToWorld, float4(realPos, 1.0));
                float3 cameraPos = (worldPos - _WorldSpaceCameraPos);
                float d3 = length(cameraPos) * screenDir.z;

                //return float4(i.depth*1000, 1);

                depth = LinearToDepth(d3);
                if (finalResult.w == 0) {
                    // sample room cube map
                    //return float4(realPos, 1);
                    if (realPosInterior.y > .000001) { 
                        //depth = 0;
                        return float4(0, 0, 0, 0); 
                    }
                    fixed4 room = texCUBE(_RoomCube, pos.xyz);
                    return fixed4(room.rgb, 1.0);
                }
                else { 
                    return finalResult; }

                //depth = 1;
                return facing > 0 ? finalResult : topColor;

              }
              ENDCG
             }

        }
}