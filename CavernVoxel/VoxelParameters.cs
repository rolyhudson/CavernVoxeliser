﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CavernVoxel
{
    class VoxelParameters
    {
        public double xCell;
        public double yCell;
        public double zCell;
        public bool explore;
        public double memberSize;
        public int unitsZ;
        public int unitsX;
        public int unitsXa;
        public int unitsXb;
        public double fillerCellX;
        public VoxelParameters(double x, double y, double z, double memberDim,bool exploreMode)
        {
            xCell = x;
            yCell = y;
            zCell = z;
            explore = exploreMode;
            memberSize = memberDim;
        }
    }
}