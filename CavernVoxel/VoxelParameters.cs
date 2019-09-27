using Rhino.Geometry;
using System;
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
        public int sectionNum;
        public double width;
        public double height;
        public double fillerMinimum;
        public List<Surface> slabs = new List<Surface>();
        public List<Brep> wall;
        public double topCellH;
        public VoxelParameters(double x, double y, double z, double memberDim,bool exploreMode, int sectNum,List<Surface> slbs, List<Brep> wll)
        {
            xCell = x;
            yCell = y;
            zCell = z;
            explore = exploreMode;
            memberSize = memberDim;
            sectionNum = sectNum;
            slabs = slbs;
            wall = wll;
            fillerMinimum = 4*memberSize;
            topCellH = zCell / 2;
            width = 60000;
            height = 40000;
        }
    }
}
