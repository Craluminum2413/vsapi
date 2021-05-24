﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client
{
    public struct ColorMapData
    {
        // 8 bit season map index
        // 8 bits climate map index
        // 8 bits temperature
        // 8 bits rainfall
        public int Value;
        
        public byte SeasonMapIndex => (byte)Value;
        public byte ClimateMapIndex => (byte)((Value >> 8) & 0xf);
        public byte Temperature => (byte)(Value >> 16);
        public byte Rainfall => (byte)(Value >> 24);

        public byte FrostableBit => (byte)((Value >> 12) & 0x1);


        public ColorMapData(int value)
        {
            Value = value;
        }

        public ColorMapData(byte seasonMapIndex, byte climateMapIndex, byte temperature, byte rainFall, bool frostable)
        {
            Value = (seasonMapIndex | ((climateMapIndex & 0xf) << 8) | (temperature << 16) | (rainFall << 24)) | (frostable ? 1 << 12 : 0);
        }

        public ColorMapData(int seasonMapIndex, int climateMapIndex, int temperature, int rainFall, bool frostable)
        {
            Value = (seasonMapIndex | ((climateMapIndex & 0xf) << 8) | (temperature << 16) | (rainFall << 24)) | (frostable ? 1 << 12 : 0);
        }

        public static int FromValues(byte seasonMapIndex, byte climateMapIndex, byte temperature, byte rainFall, bool frostable)
        {
            return (int)(seasonMapIndex | ((climateMapIndex & 0xf) << 8) | (temperature << 16) | (rainFall << 24)) | (frostable ? 1 << 12 : 0);
        }
    }
}
