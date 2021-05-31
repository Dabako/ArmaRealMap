﻿using System;
using ArmaRealMap.Geometries;

namespace ArmaRealMap.Libraries
{
    public class SingleObjetInfos : ObjetInfosBase
    {
        public string Name { get; set; }

        public float? PlacementProbability { get; set; }

        public float? PlacementRadius { get; set; }

        public float GetPlacementRadius()
        {
            return PlacementRadius ?? Math.Max(Width, Depth);
        }
    }
}