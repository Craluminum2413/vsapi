﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common
{
    public interface IMountableSupplier
    {
        bool IsMountedBy(Entity entity);

        Vec3f GetMountOffset(Entity entity);
    }

    public interface IMountable
    {
        /// <summary>
        /// Return null if you don't have a mountable supplier implementation
        /// </summary>
        IMountableSupplier MountSupplier { get; }

        Vec3d MountPosition { get; }

        float? MountYaw { get; }

        string SuggestedAnimation { get; }

        /// <summary>
        /// Return non-null controls if the player can control the mountable
        /// </summary>
        EntityControls Controls { get; }


        /// <summary>
        /// When the entity unloads you should write whatever you need in here to reconstruct the IMountable after it's loaded again
        /// Reconstruct it by registering a mountable instancer through api.RegisterMountable(string className, GetMountableDelegate mountableInstancer)
        /// You must also set a string with key className, that is the same string that you used for RegisterMountable()
        /// </summary>
        /// <param name="tree"></param>
        void MountableToTreeAttributes(TreeAttribute tree);

        /// <summary>
        /// Called when the entity unmounted himself
        /// </summary>
        /// <param name="entityAgent"></param>
        void DidUnmount(EntityAgent entityAgent);

        /// <summary>
        /// Called when the entity mounted himself
        /// </summary>
        /// <param name="entityAgent"></param>
        void DidMount(EntityAgent entityAgent);
    }
}
