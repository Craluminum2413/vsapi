﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Common
{
    public enum EnumShapeFormat
    {
        VintageStory,
        Obj,
        GltfEmbedded
    }

    public class CompositeShape
    {
        public AssetLocation Base;
        public EnumShapeFormat Format;
        /// <summary>
        /// Whether or not to insert baked in textures for mesh formats such as gltf into the texture atlas.
        /// </summary>
        public bool InsertBakedTextures;

        public float rotateX;
        public float rotateY;
        public float rotateZ;

        public float offsetX;
        public float offsetY;
        public float offsetZ;

        public float Scale = 1f;

        /// <summary>
        /// The block shape may consists of any amount of alternatives, one of which will be randomly chosen when the block is placed in the world.
        /// </summary>
        public CompositeShape[] Alternates = null;

        public CompositeShape[] BakedAlternates = null;

        public CompositeShape[] Overlays = null;

        /// <summary>
        /// If true, the shape is created from a voxelized version of the first defined texture
        /// </summary>
        public bool VoxelizeTexture = false;

        /// <summary>
        /// If non zero will only tesselate the first n elements of the shape
        /// </summary>
        public int? QuantityElements = null;

        /// <summary>
        /// If set will only tesselate elements with given name
        /// </summary>
        public string[] SelectiveElements;


        public override int GetHashCode()
        {
            int hashcode = Base.GetHashCode() + ("@" + rotateX + "/" + rotateY + "/" + rotateZ + "o" + offsetX + "/" + offsetY + "/" + offsetZ).GetHashCode();
            if (Overlays != null)
            {
                for (int i = 0; i < Overlays.Length; i++)
                {
                    hashcode ^= Overlays[i].GetHashCode();
                }
            }

            return hashcode;
        }

        /// <summary>
        /// Creates a deep copy of the texture
        /// </summary>
        /// <returns></returns>
        public CompositeShape Clone()
        {
            CompositeShape[] alternatesClone = null;

            if (Alternates != null)
            {
                alternatesClone = new CompositeShape[Alternates.Length];
                for (int i = 0; i < alternatesClone.Length; i++)
                {
                    alternatesClone[i] = Alternates[i].CloneWithoutAlternates();
                }
            }

            CompositeShape[] overlaysClone = null;

            if (this.Overlays != null)
            {
                overlaysClone = new CompositeShape[Overlays.Length];
                for (int i = 0; i < overlaysClone.Length; i++)
                {
                    overlaysClone[i] = Overlays[i].CloneWithoutAlternates();
                }
            }

            CompositeShape ct = new CompositeShape()
            {
                Base = Base?.Clone(),
                Alternates = alternatesClone,
                Overlays = overlaysClone,
                Format = Format,
                VoxelizeTexture = VoxelizeTexture,
                InsertBakedTextures = InsertBakedTextures,
                rotateX = rotateX,
                rotateY = rotateY,
                rotateZ = rotateZ,
                offsetX = offsetX,
                offsetY = offsetY,
                offsetZ = offsetZ,
                Scale = Scale,
                QuantityElements = QuantityElements,
                SelectiveElements = (string[])SelectiveElements?.Clone()
            };

            return ct;
        }

        internal CompositeShape CloneWithoutAlternates()
        {
            CompositeShape ct = new CompositeShape()
            {
                Base = Base?.Clone(),
                Format = Format,
                InsertBakedTextures = InsertBakedTextures,
                rotateX = rotateX,
                rotateY = rotateY,
                rotateZ = rotateZ,
                offsetX = offsetX,
                offsetY = offsetY,
                offsetZ = offsetZ,
                Scale = Scale,
                VoxelizeTexture = VoxelizeTexture,
                QuantityElements = QuantityElements,
                SelectiveElements = (string[])SelectiveElements?.Clone()
            };
            return ct;
        }


        /// <summary>
        /// Expands the Composite Texture to a texture atlas friendly version and populates the Baked field
        /// </summary>
        public void LoadAlternates(IAssetManager assetManager, ILogger logger)
        {
            if (Base.Path.EndsWith("*"))
            {
                List<IAsset> assets = assetManager.GetMany("shapes/" + Base.Path.Substring(0, Base.Path.Length - 1), Base.Domain);

                if (assets.Count == 0)  
                {
                    Base = new AssetLocation("block/basic/cube");
                    logger.Warning("Could not find any variants for shape {0}, will use standard cube shape.", Base.Path);
                }   

                if (assets.Count == 1)
                {
                    Base = assets[0].Location.CopyWithPath(assets[0].Location.Path.Substring("shapes/".Length));
                    Base.RemoveEnding();
                }

                if (assets.Count > 1)
                {
                    int origLength = (Alternates == null ? 0 : Alternates.Length);
                    CompositeShape[] alternates = new CompositeShape[origLength + assets.Count];
                    if (Alternates != null)
                    {
                        Array.Copy(Alternates, alternates, Alternates.Length);
                    }

                    int i = 0;
                    foreach (IAsset asset in assets)
                    {
                        AssetLocation newLocation = asset.Location.CopyWithPath(asset.Location.Path.Substring("shapes/".Length));
                        newLocation.RemoveEnding();

                        if (i == 0)
                        {
                            Base = newLocation.Clone();
                        }
                        
                        alternates[origLength + i] = new CompositeShape() { Base = newLocation, rotateX = rotateX, rotateY = rotateY, rotateZ=rotateZ };

                        i++;
                    }

                    Alternates = alternates;
                }
            }

            if (Alternates != null)
            {
                BakedAlternates = new CompositeShape[Alternates.Length + 1];
                BakedAlternates[0] = this.Clone();
                BakedAlternates[0].Alternates = null;

                for (int i = 0; i < Alternates.Length; i++)
                {
                    BakedAlternates[i + 1] = Alternates[i].Clone();

                    if (BakedAlternates[i + 1].Base == null)
                    {
                        BakedAlternates[i + 1].Base = Base.Clone();
                    }

                    if (BakedAlternates[i + 1].QuantityElements == null)
                    {
                        BakedAlternates[i + 1].QuantityElements = QuantityElements;
                    }

                    if (BakedAlternates[i + 1].SelectiveElements == null)
                    {
                        BakedAlternates[i + 1].SelectiveElements = SelectiveElements;
                    }
                }
            }
        }
    }
}
