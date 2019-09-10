using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Display;

namespace CavernVoxel
{
    class StructuralSpan
    {
        public List<StructuralBay> structuralBays = new List<StructuralBay>();
        public List<Line> linkElements = new List<Line>();
        public Mesh slice = new Mesh();
        public List<Line> xGrid = new List<Line>();
        public List<Line> yGrid = new List<Line>();
        public List<Line> baseGrid = new List<Line>();
        public List<Text3d> txt = new List<Text3d>();
        VoxelParameters parameters;
        public Plane minPlane;
        public Plane maxPlane;
        Plane referencePlane;
        double fillerMinimum = 400;
        int bayNum;
        public StructuralSpan(VoxelParameters vParams,Mesh m,Plane plane,int firstBay)
        {
            parameters = vParams;
            slice = m;
            referencePlane = plane;
            bayNum = firstBay;
            setBaseGrid();
            findVerticalFit();
            findHorizFit();
            structuralBays.Add(new StructuralBay(slice, minPlane, maxPlane, parameters, true,firstBay));
            structuralBays.Add(new StructuralBay(structuralBays[0]));
            setLinkElements();
            setGrid();
        }
        private void setBaseGrid()
        {
            for (int y = 0; y < 2; y++)
            {
                Vector3d shiftY = referencePlane.YAxis * y * parameters.yCell;
                //add the base grid line
                Line bGrid = new Line(referencePlane.Origin + shiftY, referencePlane.XAxis, 60000);
                baseGrid.Add(bGrid);
                Plane txtPn = new Plane(bGrid.From, referencePlane.XAxis, referencePlane.YAxis);
                string baynum = (bayNum + y).ToString();
                if (bayNum < 10) baynum = "0" + baynum;
                Text3d text3D = new Text3d("bay_" + baynum, txtPn, 500);
                txt.Add(text3D);
            }
        }
        private void setGrid()
        {
            for(int y = 0; y < 3; y++)
            {
                Vector3d shiftY = minPlane.YAxis * y * parameters.yCell;
                Line grid1 = new Line(minPlane.Origin+shiftY, maxPlane.Origin + shiftY);
                
                xGrid.Add(grid1);
                if (y < 2)
                {
                    //sideA
                    for (int x = 0; x < parameters.unitsXa; x++)
                    {
                        if (x == parameters.unitsXa - 1) yGridLines(x, y, minPlane, true);
                        else yGridLines(x, y, minPlane, false);
                    }
                    //sideB
                    for (int x = 0; x < parameters.unitsXb; x++)
                    {
                        yGridLines(x, y, maxPlane, false);
                    }
                    
                }
            }
        }
        
        private void yGridLines(double x,double y, Plane pln, bool filler)
        {
            Vector3d shiftX = new Vector3d();
            //includes a half cell shif in x direction
            if (filler)
            {
                Vector3d shiftPrev = pln.XAxis * (x - 1) * parameters.xCell + pln.XAxis * parameters.xCell / 2;
                Vector3d shiftFiller = pln.XAxis * parameters.fillerCellX;
                shiftX = shiftPrev + shiftFiller;
            }
            else shiftX = pln.XAxis * x * parameters.xCell + pln.XAxis * parameters.xCell / 2;
            Vector3d shiftY = pln.YAxis * y * parameters.yCell;
            Point3d start = pln.Origin + shiftX + shiftY;
            yGrid.Add(new Line(start, referencePlane.YAxis * parameters.yCell));
        }
        private void setLinkElements()
        {
            //first the z and x links
            foreach(StructuralBay sb in structuralBays)
            {
                foreach(List<StructuralCell> cells in sb.voxels)
                {
                    foreach(StructuralCell c in cells)
                    {
                        if(c.cellType != StructuralCell.CellType.Undefined)
                        {
                            var others = sb.voxels.SelectMany(sc => sc).ToList();
                            if (hasCellToSide(c, others)) makeLinks("x", c);
                            if (hasCellAbove(c, others)) makeLinks("z", c);
                        }
                    }
                }
            }
            //y links just need to check first bay against second bay
            foreach (List<StructuralCell> cells in structuralBays[0].voxels)
            {
                var others = structuralBays[1].voxels.SelectMany(sc => sc).ToList();
                foreach (StructuralCell c in cells)
                {
                    if (c.cellType != StructuralCell.CellType.Undefined)
                    {
                        if (hasCellInFront(c, others)) makeLinks("y", c);
                        
                    }
                }
            }
        }
        private bool hasCellInFront(StructuralCell cell, List<StructuralCell> cells)
        {
            foreach (StructuralCell othercell in cells)
            {
                if (othercell.cellType != StructuralCell.CellType.Undefined)
                {
                    Vector3d v = othercell.centroid - cell.centroid;
                    //small z and length close to yCell
                    if (Math.Abs(v.Z) < 10 && Math.Abs(v.Length - parameters.yCell) < 10)
                    {
                        //angle to xdirection should be 0
                        if (Vector3d.VectorAngle(v, minPlane.YAxis) < Math.PI / 2)
                        {
                            return true;
                        }
                    }

                }
            }
            return false;
        }
        private bool hasCellToSide(StructuralCell cell, List<StructuralCell> cells)
        {
            double testLength = parameters.xCell;
            
            foreach (StructuralCell othercell in cells)
            {
                if (othercell.fillerCell|| cell.fillerCell) testLength = parameters.xCell / 2 + parameters.fillerCellX / 2;
                else testLength = parameters.xCell;
                if (othercell.cellType != StructuralCell.CellType.Undefined)
                {
                    Vector3d v = othercell.centroid - cell.centroid;
                    //small z and length close to xCell
                    if (Math.Abs(v.Z) < 10 && Math.Abs(v.Length - testLength) < 10)
                    {
                        //angle to xdirection should be 0
                        if (Vector3d.VectorAngle(v, minPlane.XAxis) < Math.PI / 2)
                        {
                            return true;
                        }
                    }
                    
                }
            }
            return false;
        }
        private bool hasCellAbove(StructuralCell cell,List<StructuralCell> cells)
        {
            //check for neighbours above 
            foreach (StructuralCell othercell in cells)
            {
                if(othercell.cellType != StructuralCell.CellType.Undefined)
                {
                    Vector3d v = othercell.centroid - cell.centroid;
                    //small x and y positive z and length close to zCell
                    if (Math.Abs(v.X) < 10 && Math.Abs(v.Y) < 10 && v.Z>0 && Math.Abs(v.Length - parameters.zCell) < 10)
                    {
                        return true;
                    }
                }
                
            }
            return false;
        }
        private void makeLinks(string direction, StructuralCell c)
        {

            List<Vector3d> vectors = makeLinkVectors(direction,c.fillerCell);
            Vector3d linkDirection = new Vector3d();
            switch (direction)
            {
                case "x":
                    linkDirection = referencePlane.XAxis;
                    break;
                case "y":
                    linkDirection = referencePlane.YAxis;
                    break;
                case "z":
                    linkDirection = referencePlane.ZAxis;
                    break;
            }
            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, referencePlane);
            foreach (Vector3d v in vectors)
            {
                v.Transform(xform);
                Line link = new Line(c.centroid + v, linkDirection, parameters.memberSize);
                if (!StructuralCell.curveIsInsideMesh(link.ToNurbsCurve(), slice)) linkElements.Add(link);
            }
        }
        private List<Vector3d> makeLinkVectors(string direction,bool filler)
        {
            List<Vector3d> vectors = new List<Vector3d>();
            double x = 0;
            if(filler) x = parameters.fillerCellX/ 2 - parameters.memberSize / 2;
            else x = parameters.xCell / 2 - parameters.memberSize / 2;
            double y = parameters.yCell / 2 - parameters.memberSize / 2;
            double z = parameters.zCell / 2 - parameters.memberSize / 2;
            Vector3d v1 = new Vector3d();
            Vector3d v2 = new Vector3d();
            Vector3d v3 = new Vector3d();
            Vector3d v4 = new Vector3d();
            switch (direction)
            {
                case "x":
                    v1 = new Vector3d(x, y, z);
                    v2 = new Vector3d(x, -y, z);
                    v3 = new Vector3d(x, -y, -z);
                    v4 = new Vector3d(x, y, -z);
                    break;
                case "y":
                    v1 = new Vector3d(x, y, z);
                    v2 = new Vector3d(-x, y, z);
                    v3 = new Vector3d(-x, y, -z);
                    v4 = new Vector3d(x, y, -z);
                    break;
                case "z":
                    v1 = new Vector3d(x, y, z);
                    v2 = new Vector3d(-x, y, z);
                    v3 = new Vector3d(-x, -y, z);
                    v4 = new Vector3d(x, -y, z);
                    break;
            }
            vectors.Add(v1);
            vectors.Add(v2);
            vectors.Add(v3);
            vectors.Add(v4);
            return vectors;
        }
        private void findVerticalFit()
        {
            double min = Double.MaxValue;
            double max = Double.MinValue;
            minMaxFromPlane(slice, referencePlane, ref min, ref max);

            Vector3d shift = referencePlane.ZAxis;

            referencePlane.Origin = referencePlane.Origin + shift * min;
            parameters.unitsZ = Convert.ToInt32(Math.Ceiling((max - min) / parameters.zCell))+1;
        }
        private void minMaxFromPlane(Mesh mesh, Plane refPln, ref double min, ref double max)
        {
            min = Double.MaxValue;
            max = Double.MinValue;
            foreach (Point3d p in mesh.Vertices)
            {
                Point3d test = refPln.ClosestPoint(p);
                if (test.DistanceTo(p) < min)
                {
                    min = test.DistanceTo(p);
                }
                if (test.DistanceTo(p) > max)
                {
                    max = test.DistanceTo(p);
                }
            }
        }
        private void findHorizFit()
        {
            Plane refPlane = new Plane(referencePlane.Origin, referencePlane.XAxis);
            double min = Double.MaxValue;
            double max = Double.MinValue;
            minMaxFromPlane(slice, refPlane, ref min, ref max);
            //allow for support modules modules are set out from centre point
            min = min - (parameters.xCell * 0.9);
            max = max + (parameters.xCell * 0.9);
            Point3d minOrigin = referencePlane.Origin + referencePlane.XAxis * min;
            minPlane = new Plane(minOrigin, referencePlane.XAxis, referencePlane.YAxis);
            Point3d maxOrigin = referencePlane.Origin + referencePlane.XAxis * max;
            Vector3d maxXDir = referencePlane.XAxis;
            maxXDir.Reverse();
            maxPlane = new Plane(maxOrigin, maxXDir, referencePlane.YAxis);

            parameters.unitsX = Convert.ToInt32(Math.Floor((max - min) / parameters.xCell));
            parameters.fillerCellX = (max - min) - (parameters.unitsX * parameters.xCell);
            if (parameters.fillerCellX < fillerMinimum)
            {
                //make a larger than xCell filler
                parameters.unitsX = parameters.unitsX - 1;
                parameters.fillerCellX = parameters.fillerCellX + parameters.xCell;
            }
            if (parameters.unitsX % 2 == 0)
            {
                parameters.unitsXa = parameters.unitsXb = Convert.ToInt32(parameters.unitsX / 2) + 1;
            }
            else
            {
                parameters.unitsXa = Convert.ToInt32(Math.Floor(parameters.unitsX / 2.0)) + 2;
                parameters.unitsXb = Convert.ToInt32(Math.Ceiling(parameters.unitsX / 2.0));
            }

        }
    }
    

}
