﻿using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client
{

    public class CubeMeshUtil
    {
        /// <summary>
        /// Top, Front/Left, Back/Right, Bottom
        /// </summary>
        public static float[] CloudSideShadings = new float[] {
            1f,
            0.6f,
            0.6f,
            0.45f
        };

        /// <summary>
        /// Top, Front/Left, Back/Right, Bottom
        /// </summary>
        public static float[] DefaultBlockSideShadings = new float[] {
            1f,
            0.75f,
            0.6f,
            0.45f
        };

        /// <summary>
        /// Shadings by Blockfacing index
        /// </summary>
        public static float[] DefaultBlockSideShadingsByFacing = new float[] {
            DefaultBlockSideShadings[2],
            DefaultBlockSideShadings[1],
            DefaultBlockSideShadings[2],
            DefaultBlockSideShadings[1],
            DefaultBlockSideShadings[0],
            DefaultBlockSideShadings[3],
        };


        /// <summary>
        /// XYZ Vertex postions for every vertex in a cube. Origin is the cube middle point.
        /// </summary>
        public static int[] CubeVertices = {
            // North face
            -1, -1, -1,
            -1,  1, -1,
            1,  1, -1,
            1, -1, -1,

            // East face
            1, -1, -1,     // bot left
            1,  1, -1,     // top left
            1,  1,  1,     // top right
            1, -1,  1,     // bot right

            // South face
            -1, -1,  1,
            1, -1,  1,
            1,  1,  1,
            -1,  1,  1,

            // West face
            -1, -1, -1,
            -1, -1,  1,
            -1,  1,  1,
            -1,  1, -1,
            
            // Top face
            -1,  1, -1,
            -1,  1,  1,
            1,  1,  1,
            1,  1, -1,
                          
            // Bottom face
            -1, -1, -1,
            1, -1, -1,
            1, -1,  1,
            -1, -1,  1
        };

        /// <summary>
        /// Cube face indices, in order: North, East, South, West, Up, Down.
        /// </summary>
        public static byte[] CubeFaceIndices =
        {
            BlockFacing.NORTH.MeshDataIndex,
            BlockFacing.EAST.MeshDataIndex,
            BlockFacing.SOUTH.MeshDataIndex,
            BlockFacing.WEST.MeshDataIndex,
            BlockFacing.UP.MeshDataIndex,
            BlockFacing.DOWN.MeshDataIndex,
        };

        /// <summary>
        /// Cube UV Rotation.
        /// </summary>
        public static int[] CubeUvRotation =
        {
            0,
            1,
            2,
            3
        };


        /// <summary>
        /// UV Coords for every Vertex in a cube
        /// </summary>
        public static int[] CubeUvCoords = {
            // North
            1, 0,
            1, 1,
            0, 1,
            0, 0,

            // East 
            1, 0,
            1, 1,
            0, 1,
            0, 0,

            // South
            0, 0,
            1, 0,
            1, 1,
            0, 1,
            
            // West
            0, 0,
            1, 0,
            1, 1,
            0, 1,

            // Top face
            0, 1,
            0, 0,
            1, 0,
            1, 1,

            // Bottom face
            1, 1,
            0, 1,
            0, 0,
            1, 0,
        };

        /// <summary>
        /// Indices for every triangle in a cube
        /// </summary>
        public static int[] CubeVertexIndices = {
            0, 1, 2,      0, 2, 3,    // North face
            4, 5, 6,      4, 6, 7,    // East face
            8, 9, 10,     8, 10, 11,  // South face
            12, 13, 14,   12, 14, 15, // West face
            16, 17, 18,   16, 18, 19, // Top face
            20, 21, 22,   20, 22, 23  // Bottom face
        };


        /// <summary>
        /// Can be used for any face if offseted correctly
        /// </summary>
        public static int[] BaseCubeVertexIndices =
        {
            0, 1, 2,      0, 2, 3
        };


        /// <summary>
        /// Returns a default 2x2x2 cube with xyz,uv,rgba and indices set - ready for upload to the graphics card
        /// </summary>
        /// <returns></returns>
        public static MeshData GetCube()
        {
            MeshData m = new MeshData();
            float[] xyz = new float[3 * 4 * 6];
            for (int i = 0; i < 3 * 4 * 6; i++)
            {
                xyz[i] = CubeVertices[i];
            }
            m.SetXyz(xyz);
            float[] uv = new float[2 * 4 * 6];
            for (int i = 0; i < 2 * 4 * 6; i++)
            {
                uv[i] = CubeUvCoords[i];
            }

            byte[] rgba = new byte[4 * 4 * 6];
            m.SetRgba(rgba);

            m.SetUv(uv);
            m.SetVerticesCount(4 * 6);
            m.SetIndices(CubeVertexIndices);
            m.SetIndicesCount(3 * 2 * 6);
            m.Flags = new int[4 * 6];

            for (int i = 0; i < 24; i+=4)
            {
                BlockFacing face = BlockFacing.ALLFACES[i/6];

                m.Flags[i] = face.NormalPackedFlags;
                m.Flags[i + 1] = m.Flags[i];
                m.Flags[i + 2] = m.Flags[i];
                m.Flags[i + 3] = m.Flags[i];
            }

            m.VerticesMax = m.VerticesCount;


            return m;
        }


        /// <summary>
        /// Returns a rgba byte array to be used for default shading on a standard cube, can supply the shading levels
        /// </summary>
        /// <param name="baseColor"></param>
        /// <param name="blockSideShadings"></param>
        /// <param name="smoothShadedSides"></param>
        /// <returns></returns>
        public static byte[] GetShadedCubeRGBA(int baseColor, float[] blockSideShadings, bool smoothShadedSides)
        {
            int topSideColor = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[0]);
            int frontSideColor = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[1]);
            int backSideColor = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[2]);
            int bottomColor = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[3]);

            return GetShadedCubeRGBA(new int[] {
                frontSideColor,
                backSideColor,
                backSideColor,
                frontSideColor,
                topSideColor,
                bottomColor
            }, smoothShadedSides);
        }

        /// <summary>
        /// Returns a rgba byte array to be used for default shading on a standard cube
        /// </summary>
        /// <param name="colorSides"></param>
        /// <param name="smoothShadedSides"></param>
        /// <returns></returns>
        public static byte[] GetShadedCubeRGBA(int[] colorSides, bool smoothShadedSides)
        {
            byte[] result = new byte[6 * 4 * 4];

            // This loop makes a direct write of ints into a byte array (= more efficient)

            unsafe
            {
                fixed (byte* rgbaByte = result)
                {
                    int* rgbaInt = (int*)rgbaByte;

                    for (int facing = 0; facing < 6; facing++)
                    {
                        for (int vertex = 0; vertex < 4; vertex++)
                        {
                            rgbaInt[((facing * 4 + vertex) * 4 + 0) / 4] = colorSides[facing];
                        }
                    }

                    if (smoothShadedSides)
                    {
                        rgbaInt[0] = colorSides[3];
                        rgbaInt[1] = colorSides[3];
                        rgbaInt[4] = colorSides[3];

                        rgbaInt[7] = colorSides[3];
                        rgbaInt[16] = colorSides[3];
                        rgbaInt[19] = colorSides[3];
                        rgbaInt[20] = colorSides[3];
                        rgbaInt[21] = colorSides[3];
                    }

                }
            }


            return result;
        }


        /// <summary>
        /// Same as GetCubeModelData but can define scale and translation. Scale is applied first.
        /// </summary>
        /// <param name="scaleH"></param>
        /// <param name="scaleV"></param>
        /// <param name="translate"></param>
        /// <returns></returns>
        public static MeshData GetCubeOnlyScaleXyz(float scaleH, float scaleV, Vec3f translate)
        {
            MeshData modelData = GetCube();

            for (int i = 0; i < modelData.GetVerticesCount(); i++)
            {
                modelData.xyz[3 * i + 0] *= scaleH;
                modelData.xyz[3 * i + 1] *= scaleV;
                modelData.xyz[3 * i + 2] *= scaleH;

                modelData.xyz[3 * i + 0] += translate.X;
                modelData.xyz[3 * i + 1] += translate.Y;
                modelData.xyz[3 * i + 2] += translate.Z;
            }

            return modelData;
        }


        /// <summary>
        /// Same as GetCubeModelData but can define scale and translation. Scale is applied first.
        /// </summary>
        /// <param name="scaleH"></param>
        /// <param name="scaleV"></param>
        /// <param name="translate"></param>
        /// <returns></returns>
        public static MeshData GetCube(float scaleH, float scaleV, Vec3f translate)
        {
            MeshData modelData = GetCube();

            for (int i = 0; i < modelData.GetVerticesCount(); i++)
            {
                modelData.xyz[3 * i + 0] *= scaleH;
                modelData.xyz[3 * i + 1] *= scaleV;
                modelData.xyz[3 * i + 2] *= scaleH;

                modelData.xyz[3 * i + 0] += translate.X;
                modelData.xyz[3 * i + 1] += translate.Y;
                modelData.xyz[3 * i + 2] += translate.Z;

                // Cube UV order: N-E-S-W-U-D
                // => N-E-S-W: x coord = scaleH, y coord = scaleV
                modelData.Uv[2 * i + 0] *= 2*scaleH;
                modelData.Uv[2 * i + 1] *= i >= 16 ? 2 * scaleH : 2 * scaleV;
            }

            modelData.Rgba.Fill((byte)255);

            return modelData;
        }


        

        /// <summary>
        /// Same as GetCubeModelData but can define scale and translation. Scale is applied first.
        /// </summary>
        /// <param name="scaleH"></param>
        /// <param name="scaleV"></param>
        /// <param name="translate"></param>
        /// <returns></returns>
        public static MeshData GetCube(float scaleX, float scaleY, float scaleZ, Vec3f translate)
        {
            MeshData modelData = GetCube();
            
            float[] uScaleByAxis = new float[] { scaleZ, scaleX, scaleX };
            float[] vScaleByAxis = new float[] { scaleY, scaleZ, scaleY };

            float[] uOffsetByAxis = new float[] { translate.Z, translate.X, translate.X };
            float[] vOffsetByAxis = new float[] { translate.Y, translate.Z, translate.Y };

            for (int i = 0; i < modelData.GetVerticesCount(); i++)
            {
                modelData.xyz[3 * i + 0] *= scaleX;
                modelData.xyz[3 * i + 1] *= scaleY;
                modelData.xyz[3 * i + 2] *= scaleZ;

                modelData.xyz[3 * i + 0] += scaleX + translate.X;
                modelData.xyz[3 * i + 1] += scaleY + translate.Y;
                modelData.xyz[3 * i + 2] += scaleZ + translate.Z;

                // Cube UV order: N-E-S-W-U-D
                // => N-E-S-W: x coord = scaleH, y coord = scaleV
                BlockFacing facing = BlockFacing.ALLFACES[i / 4];
                int axis = (int)facing.Axis;
                
                switch (facing.Index)
                {
                    case 0: // N
                        modelData.Uv[2 * i + 0] = modelData.Uv[2 * i + 0] * 2 * uScaleByAxis[axis] + (1 - 2 * uScaleByAxis[axis]) - uOffsetByAxis[axis];
                        modelData.Uv[2 * i + 1] = (1 - modelData.Uv[2 * i + 1]) * 2 * vScaleByAxis[axis] + (1 - 2 * vScaleByAxis[axis]) - vOffsetByAxis[axis];
                        break;
                    case 1: // E
                        modelData.Uv[2 * i + 0] = modelData.Uv[2 * i + 0] * 2 * uScaleByAxis[axis] + (1 - 2 * uScaleByAxis[axis]) - uOffsetByAxis[axis];
                        modelData.Uv[2 * i + 1] = (1 - modelData.Uv[2 * i + 1]) * 2 * vScaleByAxis[axis] + (1 - 2 * vScaleByAxis[axis]) - vOffsetByAxis[axis];
                        break;
                    case 2: // S
                        modelData.Uv[2 * i + 0] = modelData.Uv[2 * i + 0] * 2 * uScaleByAxis[axis] + uOffsetByAxis[axis];
                        modelData.Uv[2 * i + 1] = (1-modelData.Uv[2 * i + 1]) * 2 * vScaleByAxis[axis] + (1 - 2 * vScaleByAxis[axis]) - vOffsetByAxis[axis];
                        break;
                    case 3: // W
                        modelData.Uv[2 * i + 0] = modelData.Uv[2 * i + 0] * 2 * uScaleByAxis[axis] + uOffsetByAxis[axis];
                        modelData.Uv[2 * i + 1] = (1 - modelData.Uv[2 * i + 1]) * 2 * vScaleByAxis[axis] + (1 - 2 * vScaleByAxis[axis]) - vOffsetByAxis[axis];
                        break;
                    case 4: // U
                        modelData.Uv[2 * i + 0] = (1 - modelData.Uv[2 * i + 0]) * 2 * uScaleByAxis[axis] + (1 - 2 * uScaleByAxis[axis]) - uOffsetByAxis[axis];
                        modelData.Uv[2 * i + 1] = modelData.Uv[2 * i + 1] * 2 * vScaleByAxis[axis] + (1 - 2 * vScaleByAxis[axis]) - vOffsetByAxis[axis];
                        break;
                    case 5: // D
                        modelData.Uv[2 * i + 0] = modelData.Uv[2 * i + 0] * 2 * uScaleByAxis[axis] + (1 - 2 * uScaleByAxis[axis]) - uOffsetByAxis[axis];
                        modelData.Uv[2 * i + 1] = (1 - modelData.Uv[2 * i + 1]) * 2 * vScaleByAxis[axis] + vOffsetByAxis[axis];
                        break;
                }
            }

            modelData.Rgba.Fill((byte)255);

            return modelData;
        }


        /// <summary>
        /// Gets the face of a given cube.
        /// </summary>
        /// <param name="face">The face you want to fetch in cardinal directions.</param>
        /// <returns>The mesh data for the cube's given face.</returns>
        public static MeshData GetCubeFace(BlockFacing face)
        {
            int offset = face.Index;

            MeshData m = new MeshData();
            float[] xyz = new float[3 * 4 * 1];
            for (int i = 0; i < xyz.Length; i++)
            {
                xyz[i] = CubeVertices[i + 3*4*offset];
            }
            m.SetXyz(xyz);
            float[] uv = new float[2 * 4 * 1];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = CubeUvCoords[i + 2*4*offset];
            }
            m.SetUv(uv);

            byte[] rgba = new byte[4 * 4 * 1];
            for (int i = 0; i < rgba.Length; i++)
            {
                rgba[i] = (byte)255;
            }
            m.SetRgba(rgba);
            
            m.SetVerticesCount(4 * 1);
            int[] indices = new int[3 * 2 * 1];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = CubeVertexIndices[i];

            }
            m.SetIndices(indices);
            m.SetIndicesCount(3 * 2 * 1);

            return m;
        }

        /// <summary>
        /// Gets the face of a given cube.
        /// </summary>
        /// <param name="face">The face you want to fetch in cardinal directions.</param>
        /// <param name="scaleH">The horizontal scale.</param>
        /// <param name="scaleV">The vertical scale.</param>
        /// <param name="translate">The translation desired.</param>
        /// <returns>The mesh data for the given parameters.</returns>
        public static MeshData GetCubeFace(BlockFacing face, float scaleH, float scaleV, Vec3f translate)
        {
            MeshData modelData = GetCubeFace(face);

            for (int i = 0; i < modelData.GetVerticesCount(); i++)
            {
                modelData.xyz[3 * i + 0] *= scaleH;
                modelData.xyz[3 * i + 1] *= scaleV;
                modelData.xyz[3 * i + 2] *= scaleH;

                modelData.xyz[3 * i + 0] += translate.X;
                modelData.xyz[3 * i + 1] += translate.Y;
                modelData.xyz[3 * i + 2] += translate.Z;

                // Cube UV order: N-E-S-W-U-D
                // => N-E-S-W: x coord = scaleH, y coord = scaleV
                modelData.Uv[2 * i + 0] *= 2 * scaleH;
                modelData.Uv[2 * i + 1] *= i >= 16 ? 2 * scaleH : 2 * scaleV;
            }

            modelData.Rgba.Fill((byte)255);

            return modelData;
        }

        public static void SetXyzFacesAndPacketNormals(MeshData mesh)
        {
            mesh.AddXyzFace(BlockFacing.NORTH.MeshDataIndex);
            mesh.AddXyzFace(BlockFacing.EAST.MeshDataIndex);
            mesh.AddXyzFace(BlockFacing.SOUTH.MeshDataIndex);
            mesh.AddXyzFace(BlockFacing.WEST.MeshDataIndex);
            mesh.AddXyzFace(BlockFacing.UP.MeshDataIndex);
            mesh.AddXyzFace(BlockFacing.DOWN.MeshDataIndex);

            for (int i = 0; i < 6; i++)
            {
                BlockFacing facing = BlockFacing.ALLFACES[i];
                
                mesh.XyzFaces[i] = facing.MeshDataIndex;

                int normal = BlockFacing.AllVertexFlagsNormals[facing.Index];
                mesh.Flags[i * 4 + 0] |= normal;
                mesh.Flags[i * 4 + 1] |= normal;
                mesh.Flags[i * 4 + 2] |= normal;
                mesh.Flags[i * 4 + 3] |= normal;

            }
        }
    }


}
