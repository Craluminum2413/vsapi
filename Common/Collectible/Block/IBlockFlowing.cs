﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vintagestory.API.Common
{
    public interface IBlockFlowing
    {
        string Flow { get; set; }
        MathTools.Vec3i FlowNormali { get; set; }
        bool IsLava { get; }
    }
}
