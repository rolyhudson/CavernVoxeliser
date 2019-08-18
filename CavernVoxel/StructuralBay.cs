using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Accord.MachineLearning;
using Accord.Collections;

namespace CavernVoxel
{
    class StructuralBay
    {
        public List<List<StructuralCell>> voxels = new List<List<StructuralCell>>();
        public Plane minPlane;
        public Plane maxPlane;
        public Mesh slice = new Mesh();
        public Mesh container = new Mesh();
        public List<Line> xGrid = new List<Line>();
        public List<Line> yGrid = new List<Line>();
        Plane referencePlane;
        double xCell;
        double yCell;
        double zCell;
        int unitsZ;
        int unitsX;
        int unitsXa;
        int unitsXb;
        double memberSize;
        double fillerCellX;
        StructuralBay previous;
        bool leader;
        KDTree<double> tree;
        bool exploreMode;
        
        public StructuralBay(StructuralBay prevBay, bool explore)
        {
            leader = false;
            voxels.Add(new List<StructuralCell>());
            voxels.Add(new List<StructuralCell>());
            previous = prevBay;
            setoutFromPrev();
            exploreMode = explore;
            setGrid();
            if (!explore)voxelise();
            
        }
        public StructuralBay(Mesh sliceToVoxelise, Mesh box, Plane refPlane, double x, double y, double z,double memberS, bool explore)
        {
            voxels.Add(new List<StructuralCell>());
            voxels.Add(new List<StructuralCell>());
            slice = sliceToVoxelise;
            container = box;
            referencePlane = refPlane;
            xCell = x;
            yCell = y;
            zCell = z;
            
            memberSize = memberS;
            exploreMode = explore;
            leader = true;
            findVerticalFit();
            findHorizFit();
            setGrid();
            if (!explore) voxelise();
            
        }
        private void setKDTree()
        {
            List<double[]> points = new List<double[]>();
            foreach (Point3d p in slice.Vertices)
            {
                points.Add(new double[] { p.X, p.Y, p.Z });

            }
            tree = KDTree.FromData<double>(points.ToArray());
        }
        private void setoutFromPrev()
        {
            slice = previous.slice;
            container = previous.container;
            referencePlane = previous.referencePlane;
            xCell = previous.xCell;
            yCell = previous.yCell;
            zCell = previous.zCell;

            memberSize = previous.memberSize;

            
            unitsZ = previous.unitsZ;
            minPlane = previous.minPlane;
            maxPlane = previous.maxPlane;
            //shift in the y direction
            minPlane.Origin = minPlane.Origin + (minPlane.YAxis * yCell);
            maxPlane.Origin = maxPlane.Origin + (maxPlane.YAxis * yCell);
            unitsX = previous.unitsX;
            fillerCellX = previous.fillerCellX;
            unitsXa = previous.unitsXa;
            unitsXb = previous.unitsXb;
        }
        private void findVerticalFit()
        {
            double min = Double.MaxValue;
            double max = Double.MinValue;
            minMaxFromPlane(slice, referencePlane, ref min, ref max);
            
            Vector3d shift = referencePlane.ZAxis;

            referencePlane.Origin = referencePlane.Origin+shift*min;
            unitsZ = Convert.ToInt32(Math.Ceiling((max-min) / zCell));
        }
        private void findHorizFit()
        {
            Plane refPlane = new Plane(referencePlane.Origin, referencePlane.XAxis);
            double min = Double.MaxValue;
            double max = Double.MinValue;
            minMaxFromPlane(slice, refPlane, ref min, ref max);
            //allow for support modules modules are set out from centre point
            min = min - (xCell*0.9);
            max = max + (xCell*0.9);
            Point3d minOrigin = referencePlane.Origin + referencePlane.XAxis * min;
            minPlane = new Plane(minOrigin, referencePlane.XAxis, referencePlane.YAxis);
            Point3d maxOrigin = referencePlane.Origin + referencePlane.XAxis * max;
            Vector3d maxXDir = referencePlane.XAxis;
            maxXDir.Reverse();
            maxPlane = new Plane(maxOrigin, maxXDir, referencePlane.YAxis);

            unitsX = Convert.ToInt32(Math.Floor((max-min)/ xCell));
            fillerCellX = (max - min) - (unitsX * xCell);
            if (unitsX % 2 == 0)
            {
                unitsXa = unitsXb = Convert.ToInt32(unitsX / 2)+1;
            }
            else
            {
                unitsXa = Convert.ToInt32(Math.Floor(unitsX / 2.0))+2;
                unitsXb = Convert.ToInt32(Math.Ceiling(unitsX / 2.0));
            }
            
        }
        private void setGrid()
        {
            Line grid1 = new Line(minPlane.Origin, maxPlane.Origin);
            Line grid2 = new Line(minPlane.Origin, maxPlane.Origin);
            grid2.Transform(Transform.Translation(referencePlane.YAxis * yCell));
            xGrid.Add(grid1);
            xGrid.Add(grid2);
            //sideA
            for (int x = 0; x < unitsXa; x++)
            {
                if (x == unitsXa - 1) yGridLines(x, minPlane, true);
                else yGridLines(x, minPlane, false);
            }
            //sideB
            for (int x = 0; x < unitsXb; x++)
            {
                yGridLines(x, maxPlane, false);
            }
        }
        private void yGridLines(double x,Plane pln, bool filler)
        {
            Vector3d shiftX = new Vector3d();
            if (filler)
            {
                Vector3d shiftPrev = pln.XAxis * (x - 1) * xCell;
                Vector3d shiftFiller =pln.XAxis * (fillerCellX / 2 + xCell / 2);
                shiftX = shiftPrev + shiftFiller;
            }
            else shiftX = pln.XAxis * x * xCell;
            Point3d start = pln.Origin + shiftX;
            yGrid.Add(new Line(start, referencePlane.YAxis * yCell));
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
        private void voxelise()
        {
            for (int z = 0; z <= unitsZ; z++)
            {
                //sideA
                for (int x = 0; x < unitsXa; x++)
                {
                    
                    if (x == unitsXa - 1) voxelTest(x, z, minPlane, "a", true);
                    else voxelTest(x, z, minPlane, "a", false);
                }
                //sideB
                for (int x = 0; x < unitsXb; x++)
                {
                    voxelTest(x, z, maxPlane,  "b", false);
                }
            }
            cullSupports();
        }
        private void cullSupports()
        {
            foreach(List<StructuralCell> sc in voxels)
            {
                foreach(StructuralCell c in sc)
                {
                    if (c.cellType==StructuralCell.CellType.Undefined)
                    {
                        
                        //does it have a non support on one side
                        bool side = hasNonSupportCellLeftOrRight(sc, c);
                        //does it have a non support below
                        bool below = hasNonSupportCellBelow(sc, c);
                        if (side || below) c.cellType = StructuralCell.CellType.PerimeterCell;


                    }
                }
            }
            
            tagInternalCells();
            setVerticalSupports();
        }
        private void tagInternalCells()
        {
            foreach (List<StructuralCell> sc in voxels)
            {
                foreach (StructuralCell c in sc)
                {
                    if (c.cellType == StructuralCell.CellType.Undefined||c.cellType == StructuralCell.CellType.PerimeterCell)
                    {
                        bool inside = cellIsInsideMesh(c);
                        if (inside) c.cellType = StructuralCell.CellType.InsideCell;
                    }
                }
            }
        }
        private void setVerticalSupports()
        {
            foreach (List<StructuralCell> sc in voxels)
            {
                foreach (StructuralCell c in sc)
                {
                    if (c.cellType == StructuralCell.CellType.Undefined)
                    {
                        bool above = hasCellsAbove(sc, c);
                        if (above) c.cellType = StructuralCell.CellType.VerticalFillCell;
                    }
                }
            }
        }
        private bool cellIsInsideMesh(StructuralCell cell)
        {
            //mesh normals towards inside
            MeshPoint mp = slice.ClosestMeshPoint(cell.centroid, 0);
            Vector3d v = cell.centroid - mp.Point;
            if (Vector3d.VectorAngle(v, slice.FaceNormals[mp.FaceIndex]) < Math.PI / 2)
            {
                //inside
                return true;
            }
            return false;
        }
        private bool hasCellsAbove(List<StructuralCell> sc, StructuralCell cell)
        {
            foreach (StructuralCell c in sc)
            {
                //any cells above inuse
                if (c.cellType == StructuralCell.CellType.PerimeterCell||c.cellType== StructuralCell.CellType.SkinCell)
                {
                    Vector3d v = c.centroid - cell.centroid;
                    //x and y small or 0 length length is positive
                    if (Math.Abs(v.X) < 10 && Math.Abs(v.Y) < 10 && v.Z>0)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        private bool hasNonSupportCellBelow(List<StructuralCell> sc, StructuralCell cell)
        {
            
            foreach (StructuralCell c in sc)
            {
                if (c.cellType == StructuralCell.CellType.SkinCell)
                {
                    Vector3d v = c.centroid- cell.centroid ;
                    //x and y small or 0 length is equal to -zcell
                    if (v.X < 10 && v.Y < 10 && Math.Abs(v.Length - zCell) < 10)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        private bool hasNonSupportCellLeftOrRight(List<StructuralCell> sc,StructuralCell cell)
        {
            
            foreach (StructuralCell c in sc)
            {
                if (c.cellType == StructuralCell.CellType.SkinCell)
                {
                    Vector3d v = c.centroid - cell.centroid;
                    if (Math.Abs(v.Length-xCell) < 10)
                    {
                        return true;
                    }
                }
                
            }
            return false;
        }
        private void voxelTest(int x, int z, Plane pln, string ab,bool filler)
        {
            Vector3d shiftX = new Vector3d();
            if (filler) {
                Vector3d shiftPrev = pln.XAxis * (x - 1) * xCell;
                Vector3d shiftFiller = pln.XAxis * (fillerCellX/2+xCell/2);
                shiftX = shiftPrev+shiftFiller;
            }
            else shiftX = pln.XAxis * x * xCell;
            Vector3d shiftY = pln.YAxis * yCell/2;
            Vector3d shiftZ = Vector3d.ZAxis * z * zCell;
            Vector3d shift = shiftX +shiftY+ shiftZ;
            Point3d basePt = new Point3d(pln.OriginX, pln.OriginY, pln.OriginZ);
            Point3d cellCentre = basePt + shift;
            Plane cellpln = new Plane(cellCentre, referencePlane.XAxis, referencePlane.YAxis);
            Mesh testCell = new Mesh();
            Mesh trimCell = new Mesh();
            Interval trimYInterval = new Interval(-yCell/2, yCell);
            if (leader) trimYInterval = new Interval(-yCell, yCell / 2);
           
            if (filler)
            {
                testCell = makeCuboid(cellpln, fillerCellX,new Interval(- yCell/2,yCell/2), zCell);
                trimCell = makeCuboid(cellpln, fillerCellX, trimYInterval, zCell);
            }
            else
            {
                testCell = makeCuboid(cellpln, xCell, new Interval(-yCell / 2, yCell / 2), zCell);
                trimCell = makeCuboid(cellpln, xCell, trimYInterval, zCell);
            }
            
            bool intersect = false;
            

            Mesh caveface = MeshTools.splitMeshWithMesh(slice, trimCell);
            
            if (caveface != null) intersect = true;

            if (intersect)
            {
                MeshTools.matchOrientation(slice, ref caveface);
                StructuralCell structCell = new StructuralCell(testCell, memberSize, caveface.DuplicateMesh(),new int[] { x,z});
                if (ab == "a") voxels[0].Add(structCell);
                else voxels[1].Add(structCell);
            }
            else
            {
                StructuralCell structCell = new StructuralCell(testCell, memberSize, new int[] { x, z });
                if (ab == "a") voxels[0].Add(structCell);
                else voxels[1].Add(structCell);
            }
        }
        private Mesh makeCuboid(Plane pln,double width, Interval depth,double height)
        {
            Mesh cell = new Mesh();
            Box box = new Box(pln, new Interval(-width / 2, width / 2), depth, new Interval(-height / 2, height / 2));
            cell = Mesh.CreateFromBox(box, 1, 1, 1);
            return cell;
        }
        
    }

}
