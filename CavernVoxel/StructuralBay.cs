using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Accord.MachineLearning;
using Accord.Collections;
using System.Drawing;

namespace CavernVoxel
{
    class StructuralBay
    {
        public List<List<StructuralCell>> voxels = new List<List<StructuralCell>>();
        
        public Plane minPlane;
        public Plane maxPlane;
        Mesh slice = new Mesh();
        public VoxelParameters parameters;
        bool leader = false;
        KDTree<double> tree;
        public int baynum;
        Random r = new Random();
        public StructuralBay(Mesh sliceToVoxelise, Plane minpln,Plane maxpln, VoxelParameters voxelParameters,bool firstBay, int num)
        {
            voxels.Add(new List<StructuralCell>());
            voxels.Add(new List<StructuralCell>());
            slice = sliceToVoxelise;
            minPlane = minpln;
            maxPlane = maxpln;
            parameters = voxelParameters;
            leader = firstBay;
            baynum = num;
            
            if (!parameters.explore) voxelise();
        }
        public StructuralBay(StructuralBay previous)
        {
            voxels.Add(new List<StructuralCell>());
            voxels.Add(new List<StructuralCell>());
            leader = false;
            slice = previous.slice;
            parameters = previous.parameters;
            baynum = previous.baynum + 1;
            minPlane = previous.minPlane;
            maxPlane = previous.maxPlane;
            //shift in the y direction
            minPlane.Origin = minPlane.Origin + (minPlane.YAxis * parameters.yCell);
            maxPlane.Origin = maxPlane.Origin + (maxPlane.YAxis * parameters.yCell);
            if (!parameters.explore) voxelise();
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
        private Color setColor(Random r)
        {
            int red = r.Next(255);
            int green = r.Next(255);
            int blue = r.Next(255);
            return Color.FromArgb(red, green, blue);
        }
        private void voxelise()
        {
            for (int z = 0; z <= parameters.unitsZ; z++)
            {
                //sideA
                for (int x = 0; x < parameters.unitsXa; x++)
                {
                    
                    if (x == parameters.unitsXa - 1) buildVoxel(x, z, minPlane, "a", true);
                    else buildVoxel(x, z, minPlane, "a", false);
                }
                //sideB
                for (int x = 0; x < parameters.unitsXb; x++)
                {
                    buildVoxel(x, z, maxPlane,  "b", false);
                }
            }
            //determine and adjust the modules laterally
            boundaryChecks();
            //remove nulls either too small or not fitting
            foreach (List<StructuralCell> sc in voxels) sc.RemoveAll(x => x == null);
            //add or adjust modules down to slab
            buildBaseModules();
            //remove nulls either too small or not fitting
            foreach (List<StructuralCell> sc in voxels) sc.RemoveAll(x => x == null);

            defineSkinCells();
            setSupports();
            
        }
        private void defineSkinCells()
        {
            foreach (List<StructuralCell> sc in voxels)
            {
                foreach (StructuralCell c in sc)
                {
                    if(c.id== "02_08_0_4_2")
                    {
                        int g = 0;
                    }
                    Interval yint = new Interval(-c.yDim / 2, c.yDim);
                    if(baynum%2==0) yint = new Interval(-c.yDim, c.yDim/2);
                    //extend splitter to avoid edge alignment
                    Mesh extendSplitter = MeshTools.makeCuboid(c.cellPlane, c.xDim, yint, c.zDim);
                    var intersectCurve = Rhino.Geometry.Intersect.Intersection.MeshMeshAccurate(slice, extendSplitter, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    if (intersectCurve!=null)
                    {
                        if (intersectCurve.Length > 0)
                        {
                            //we know there is defintely an intersection if we have result from MeshMesh
                            Mesh caveface = MeshTools.findIntersection(slice, c, yint);
                            if (caveface == null) return;
                            MeshTools.matchOrientation(slice, ref caveface);
                            c.setSkinCell(caveface);
                        }
                        
                    }
                }
            }
        }
        
        private void setSupports()
        {
            foreach(List<StructuralCell> sc in voxels)
            {
                for(int i=0;i< sc.Count;i++)
                {
                    if (sc[i].cellType==StructuralCell.CellType.Undefined)
                    {
                        //does it have a non support on one side
                        bool side = hasNonSupportCellLeftOrRight(sc, sc[i]);
                        //does it have a non support below
                        bool below = hasNonSupportCellBelow(sc, sc[i]);
                        if (side || below) sc[i].cellType = StructuralCell.CellType.PerimeterCell;

                    }
                }
            }
            tagInternalCells();
            setVerticalSupports();
            structuralContinuity();
            //reduce top row if nothing above set top row h
            topCellReduction();
            removeOutsiders();
        }
        private void topCellReduction()
        {
            foreach (List<StructuralCell> sc in voxels)
            {
                for (int i = 0; i < sc.Count; i++)
                {
                    if(sc[i].cellType == StructuralCell.CellType.PerimeterCell || sc[i].cellType == StructuralCell.CellType.VerticalFillCell)
                    {
                        if (noCellsAbove(sc[i]))
                        {
                            Plane cellPlane = new Plane(sc[i].centroid - Vector3d.ZAxis * parameters.topCellH / 2, minPlane.XAxis, minPlane.YAxis);
                            sc[i] = new StructuralCell(cellPlane, sc[i].xDim, sc[i].yDim, parameters.topCellH, parameters.memberSize, sc[i].id, false, setColor(r));
                            sc[i].cellType = StructuralCell.CellType.PerimeterCell;
                        }
                    }
                }
            }
            
        }
        private bool noCellsAbove(StructuralCell cell)
        {
            bool nocells = true;
            var cellsFill = voxels.SelectMany(x => x).ToList().FindAll(c => 
            c.side == cell.side && 
            c.colNum == cell.colNum && 
            c.rowNum>cell.rowNum && 
            c.cellType!= StructuralCell.CellType.Undefined);
            if (cellsFill.Count > 0) nocells = false;
            return nocells;
        }
        private void removeOutsiders()
        {
            foreach (List<StructuralCell> sc in voxels)
            {
                foreach (StructuralCell c in sc)
                {
                    if(c.cellType!= StructuralCell.CellType.Undefined)
                    {
                        int outsideCount = 0;
                        foreach (Brep w in parameters.wall)
                        {
                            bool inside = w.IsPointInside(c.centroid, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true);
                            if (!inside) outsideCount++;
                        }
                        //cells should be inside one of the wall breps
                        if (outsideCount == parameters.wall.Count) c.cellType = StructuralCell.CellType.Undefined;
                    }
                }
            }
        }
        private void boundaryChecks()
        {
            Curve[] cInter;
            Point3d[] pInter;
            Plane insidePlane = new Plane();
            foreach (List<StructuralCell> sc in voxels)
            {
                for(int i=0;i< sc.Count;i++)
                {
                    StructuralCell c = sc[i];
                    foreach (Brep w in parameters.wall)
                    {
                        bool inter = Rhino.Geometry.Intersect.Intersection.BrepBrep(c.outerBoundary, w, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out cInter, out pInter);
                        if (cInter.Length > 0)
                        {
                            //which cell end is inside
                            bool end0 = MeshTools.curveInBrep(c.boundCurve0, w);
                            bool end1 = MeshTools.curveInBrep(c.boundCurve1, w);
                            if (!end0 && !end1)
                            {
                                // both ends intersect, we won't use it, move to next cell
                                sc[i] = null;
                                break;
                            }

                            if (end0) insidePlane = c.boundPlane0;

                            if (end1) insidePlane = c.boundPlane1;

                            reduceModuleYDirection(ref c, insidePlane, cInter[0]);
                            sc[i] = c;
                            break;//move to next cell
                        }
                    }
                    
                }
            }

        }
        private void fillToSlab(StructuralCell c, double dist, ref List<StructuralCell> cellszero,ref List<StructuralCell> cellsone)
        {
            double nfills = dist / parameters.zCell;
            double fillCell = nfills % 1 * parameters.zCell;
            double lastCell = 0;
            int nCells = Convert.ToInt32(Math.Floor(nfills));
            //check last cell size
            if (fillCell < parameters.fillerMinimum)
            {
                //oversized last cell
                lastCell = parameters.zCell + fillCell;
            }
            else
            {
                //undersized extra last cell
                lastCell = fillCell;
                nCells++;
            }
            double height = parameters.zCell;
            Vector3d shft = new Vector3d();
            for (int i = 0; i < nCells; i++)
            {
                if (i == nCells - 1)
                {
                    //spacing on lastcell
                    shft = shft + Vector3d.ZAxis * lastCell / 2 + Vector3d.ZAxis * parameters.zCell / 2;
                    height = lastCell;
                }
                else
                {
                    shft = Vector3d.ZAxis * ((i + 1) * parameters.zCell);

                }
                Plane cellPlane = new Plane(c.centroid - shft, minPlane.XAxis, minPlane.YAxis);
                string mcode = genMCode(c.side, c.colNum, c.rowNum - (i+1));
                StructuralCell structCell = new StructuralCell(cellPlane, c.xDim, c.yDim, height, parameters.memberSize, mcode, false, setColor(r));
                structCell.cellType = StructuralCell.CellType.VerticalFillCell;
                
                //store and add after/
                if (c.side == 0) cellszero.Add(structCell);
                else cellsone.Add(structCell);
            }
        }
        private void reduceModuleYDirection(ref StructuralCell c, Plane boundPlane, Curve intersection)
        {
            c.cellType = StructuralCell.CellType.Undefined;
            double newY = Double.MaxValue;
            NurbsCurve nc = intersection.ToNurbsCurve();
            foreach (ControlPoint cp in nc.Points)
            {
                Point3d plnPt = boundPlane.ClosestPoint(cp.Location);
                if (plnPt.DistanceTo(cp.Location) < newY) newY = plnPt.DistanceTo(cp.Location);
            }
            if (newY < parameters.fillerMinimum)
            {
                c = null;
                return;
            }

            //set the new module
            Point3d bPoint = boundPlane.ClosestPoint(c.centroid);
            Vector3d shft = c.centroid - bPoint;
            shft.Unitize();
            Plane cellPlane = new Plane(bPoint + shft * newY / 2, minPlane.XAxis, minPlane.YAxis);

            c = new StructuralCell(cellPlane, c.xDim,newY,c.zDim, parameters.memberSize, c.id, false, setColor(r));
        }
        private void adjustModuleVertical(ref StructuralCell c,Point3d slabPt)
        {
            double newH = 0;
            if (slabPt.Z > c.centroid.Z)
            {
                newH = parameters.zCell/2 - (slabPt.Z - c.centroid.Z);
            }
            else
            {
                double d = (c.centroid.Z - slabPt.Z);
                newH = parameters.zCell/2 + d;
            }
            if (newH < parameters.fillerMinimum)
            {
                //maybe flag as undersized
                c = null;
                return;
            }
            Point3d origin = new Point3d(c.centroid.X, c.centroid.Y, slabPt.Z);
            Plane cellPlane = new Plane(origin + Vector3d.ZAxis*newH/2, minPlane.XAxis, minPlane.YAxis);
            
            c = new StructuralCell(cellPlane, c.xDim,c.yDim,newH, parameters.memberSize, c.id, false, setColor(r));
        }
        private bool findClosestSlabPoint(StructuralCell cell,ref Point3d slabPt)
        {
            
            List<Point3d> inters = new List<Point3d>();
            
            foreach(Point3d p in cell.basePoints)
            {
                Line down = new Line(p - Vector3d.ZAxis * 50000, Vector3d.ZAxis, 100000);
                foreach (Brep s in parameters.slabs)
                {
                    Curve[] curves;
                    Point3d[] points;
                    var inter = Rhino.Geometry.Intersect.Intersection.CurveBrep(down.ToNurbsCurve(), s, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out curves, out points);
                    if (points.Length > 0)
                    {
                        inters.Add(points[0]);
                        
                        break;
                    }
                }
            }
            if (inters.Count == 0) return false;
            var sorted = inters.OrderByDescending(p => p.Z).ToList();
            
            slabPt = sorted[0];
            return true;
        }
        private void buildBaseModules()
        {
            List<StructuralCell> cellsZero = new List<StructuralCell>();
            List<StructuralCell> cellsOne = new List<StructuralCell>();
            foreach (List<StructuralCell> sc in voxels)
            {
                for(int i =0;i< sc.Count;i++)
                {
                    StructuralCell c = sc[i];
                    if (c.rowNum == 0)
                    {
                        if (c.id == "03_02_0_2_0")
                        {
                            int g = 0;
                        }
                        Point3d slabPt = new Point3d();
                        
                        bool foundslab = findClosestSlabPoint(sc[i],ref slabPt);
                        
                        if (foundslab)
                        {
                            //centroid to closest slab distance
                            Vector3d diff = slabPt - c.centroid;
                            double dist = Math.Abs(diff.Z);
                            if (dist < parameters.zCell/2+parameters.fillerMinimum)
                            {
                                //smaller than half module
                                //replace with smaller module
                                adjustModuleVertical(ref c, slabPt);
                                sc[i] = c;
                                
                            }
                            else
                            {
                                //fill missing modules
                                //adjust dist to be underside last module to slab
                                //only fill if module above slab
                                if(slabPt.Z<c.centroid.Z) fillToSlab(c, dist - parameters.zCell / 2, ref cellsZero, ref cellsOne);
                            }
                                
                        }
                        else
                        {
                            //no slab below found either we are below slab level or no slab exists
                            // sc[i] = null;
                        }
                        
                    }
                }
            }
            voxels[0].AddRange(cellsZero);
            voxels[1].AddRange(cellsOne);
        }
        private void structuralContinuity()
        {
            foreach (List<StructuralCell> sc in voxels)
            {
                foreach (StructuralCell c in sc)
                {
                    if (c.cellType == StructuralCell.CellType.VerticalFillCell || c.cellType == StructuralCell.CellType.PerimeterCell)
                    {
                        if (hasDiagonalNeighbour(c))
                        {
                            //we need to find the diagonals and try to infill
                            fillFromDiags(c);
                        }
                        if (c.fillerCell)
                        {
                            //check interface with other side
                            interfaceContinuity(c);
                        }
                    }
                }

            }
        }
        private void interfaceContinuity(StructuralCell cell)
        {
            //get all the cells on the other side
            var cells = voxels[0];
            if(cell.side==0) cells = voxels[1];
            //get the row of this cell
            var row = cells.FindAll(c => c.rowNum == cell.rowNum).OrderByDescending(x=>x.colNum).ToList();
            //closest is the first in the list
            StructuralCell closest = row[0];
            if (closest.cellType == StructuralCell.CellType.PerimeterCell || closest.cellType == StructuralCell.CellType.VerticalFillCell) return;
            //get the column
            var col = cells.FindAll(c => c.colNum == closest.colNum).ToList();
            //get the skin cell if any
            var skin = col.FindAll(x => x.cellType == StructuralCell.CellType.SkinCell).OrderByDescending(x => x.rowNum).ToList();

            if (skin.Count>0)
            {
                int z = cell.rowNum;
                var topSkin = skin[0];
                if (topSkin.rowNum >= cell.rowNum)
                {
                    //add continuity on filler side upwards
                    var cellsFill = voxels.SelectMany(x => x).ToList().FindAll(c => c.side == cell.side && c.colNum == cell.colNum);
                    while (true)
                    {
                        z++;
                        var next = cellsFill.Find(x=>x.id == genMCode(cell.side, cell.colNum, z));
                        var pair = col.Find(x => x.rowNum == z);
                        if (pair == null || next == null)
                        {
                            break;
                        }
                        next.cellType = StructuralCell.CellType.PerimeterCell;
                        if (pair.cellType == StructuralCell.CellType.PerimeterCell || pair.cellType == StructuralCell.CellType.VerticalFillCell) break;
                    }
                    
                }
                else
                {
                    //add continuity on non filler side downwards
                    while (true)
                    {
                        
                        var pair = col.Find(x => x.rowNum == z);
                        if (pair == null) break;
                        if (pair.cellType == StructuralCell.CellType.PerimeterCell || pair.cellType == StructuralCell.CellType.VerticalFillCell) break;
                        else pair.cellType = StructuralCell.CellType.PerimeterCell;
                        z--;
                    }
                }
            }
            else
            {
                // no skin cell found...
            }
            

        }
        private bool hasDiagonalNeighbour(StructuralCell cell)
        {
            //does the cell have a neighbour on diagonal that is marked as structural?
            List<string> n4 = fourDiagNeighbours(cell);
            var cells = voxels.SelectMany(x => x).ToList();
            bool structuralNeighbour = false;
            foreach (string code in n4)
            {
                var n = cells.Find(x => x.id == code);
                if (n != null)
                {
                    if (n.cellType == StructuralCell.CellType.VerticalFillCell || n.cellType == StructuralCell.CellType.PerimeterCell)
                    {
                        structuralNeighbour = true;
                    }
                }
            }
            return structuralNeighbour;
        }
        private bool check4Neighbours(StructuralCell cell)
        {
            bool structuralNeighbour = false;
            List<string> n4 = fourDiagNeighbours(cell);
            var cells = voxels.SelectMany(x => x).ToList();
            //if one of the 4 neighbours is structural cell all is good
            foreach(string code in n4)
            {
                var n = cells.Find(x => x.id == code);
                if (n != null)
                {
                    if (n.cellType == StructuralCell.CellType.VerticalFillCell || n.cellType == StructuralCell.CellType.PerimeterCell) structuralNeighbour = true;
                }
            }
            return structuralNeighbour;
        }
        private List<string> fourNeighbours(StructuralCell cell)
        {
            List<string> n4 = new List<string>();
            n4.Add(genMCode(cell.side, cell.colNum - 1, cell.rowNum));
            n4.Add(genMCode(cell.side, cell.colNum + 1, cell.rowNum));
            n4.Add(genMCode(cell.side, cell.colNum, cell.rowNum - 1));
            n4.Add(genMCode(cell.side, cell.colNum, cell.rowNum + 1));
            return n4;
        }
        private bool tryFillCell(string code, List<StructuralCell> cells)
        {
            bool filled = false;
            var c = cells.Find(x => x.id == code);
            if (c == null) return filled;
            if (c.cellType == StructuralCell.CellType.PerimeterCell) return true;
            if (c.cellType == StructuralCell.CellType.Undefined)
            {
                c.cellType = StructuralCell.CellType.PerimeterCell;
                return true;
            }
            return filled;
        }
        private void fillFromDiags(StructuralCell cell)
        {
            List<string> n4 = fourDiagNeighbours(cell);
            var cells = voxels.SelectMany(x => x).ToList();
            
            foreach (string code in n4)
            {
                var n = cells.Find(x => x.id == code);
                if (n != null)
                {
                    if (n.cellType == StructuralCell.CellType.VerticalFillCell || n.cellType == StructuralCell.CellType.PerimeterCell)
                    {
                        //try and fill at lower level first
                        //break after fill as we only need a single filler
                        if (n.rowNum < cell.rowNum)
                        {
                            //row below current cell
                            var id = genMCode(cell.side, cell.colNum, cell.rowNum - 1);
                            if (tryFillCell(id, cells)) break;
                            
                            else
                            {
                                //same row as current cell
                                id = genMCode(cell.side, n.colNum, cell.rowNum);
                                if (tryFillCell(id, cells)) break;
                            }
                        }
                        else
                        {
                            //same row as current cell
                            var id = genMCode(cell.side, n.colNum, cell.rowNum);
                            if (tryFillCell(id, cells)) break;
                            else
                            {
                                //row above current cell
                                id = genMCode(cell.side, cell.colNum, cell.rowNum + 1);
                                if (tryFillCell(id, cells)) break;
                            }
                        }
                    }
                }
            }
        }
        private List<string> fourDiagNeighbours(StructuralCell cell)
        {
            List<string> n4 = new List<string>();
            n4.Add(genMCode(cell.side, cell.colNum - 1, cell.rowNum - 1 ));
            n4.Add(genMCode(cell.side, cell.colNum + 1, cell.rowNum - 1));
            n4.Add(genMCode(cell.side, cell.colNum - 1, cell.rowNum + 1));
            n4.Add(genMCode(cell.side, cell.colNum + 1, cell.rowNum + 1));
            return n4;
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
            List<StructuralCell> cellsAbove = sc.FindAll(c =>
            c.colNum == cell.colNum &&//same column
            c.rowNum > cell.rowNum//row number greater
            ).ToList();
            foreach (StructuralCell c in cellsAbove)
            {
                //any cells above inuse
                if (c.cellType == StructuralCell.CellType.PerimeterCell||c.cellType== StructuralCell.CellType.SkinCell)
                {
                    return true;
                }

            }
            return false;
        }
        private bool hasNonSupportCellBelow(List<StructuralCell> sc, StructuralCell cell)
        {
            StructuralCell neighbourbelow = sc.Find(c =>
            c.cellType == StructuralCell.CellType.SkinCell &&//only skin
            c.rowNum == cell.rowNum-1 && //row below
            c.colNum == cell.colNum//same column
            );
            
            if (neighbourbelow != null) return true;
            return false;
        }
        private bool hasNonSupportCellLeftOrRight(List<StructuralCell> sc,StructuralCell cell)
        {
            StructuralCell neighbour1 = sc.Find(c =>
            c.cellType == StructuralCell.CellType.SkinCell &&//only skin
            c.rowNum == cell.rowNum && //same row
            c.colNum == cell.colNum+1 // next column
            );
            if (neighbour1 != null) return true;

            StructuralCell neighbour2 = sc.Find(c =>
            c.cellType == StructuralCell.CellType.SkinCell &&//only skin
            c.rowNum == cell.rowNum && //same row
            c.colNum == cell.colNum - 1
            );
            if (neighbour2 != null) return true;
            return false;
        }
        private void buildVoxel(int x, int z, Plane pln, string ab,bool filler)
        {
            Vector3d shiftX = new Vector3d();
            if (filler) {
                Vector3d shiftPrev = pln.XAxis * (x - 1) * parameters.xCell;
                Vector3d shiftFiller = pln.XAxis * (parameters.fillerCellX /2+ parameters.xCell /2);
                shiftX = shiftPrev+shiftFiller;
            }
            else shiftX = pln.XAxis * x * parameters.xCell;
            Vector3d shiftY = pln.YAxis * parameters.yCell /2;
            Vector3d shiftZ = Vector3d.ZAxis * z * parameters.zCell;
            Vector3d shift = shiftX +shiftY+ shiftZ;
            Point3d basePt = new Point3d(pln.OriginX, pln.OriginY, pln.OriginZ);
            Point3d cellCentre = basePt + shift;
            Plane cellpln = new Plane(cellCentre, minPlane.XAxis, maxPlane.YAxis);
            string mCode = genMCode(ab, x, z);
            if(mCode== "06_00_0_19_0")
            {
                int g = 0;
            }
            StructuralCell structCell;
            if (filler)
            {
                structCell = new StructuralCell(cellpln, parameters.fillerCellX, parameters.yCell, parameters.zCell, parameters.memberSize, mCode, filler,setColor(r));
            }
            else
            {
                structCell = new StructuralCell(cellpln, parameters.xCell, parameters.yCell, parameters.zCell, parameters.memberSize, mCode, filler, setColor(r));
            }
            if (ab == "a") voxels[0].Add(structCell);
            else voxels[1].Add(structCell);

        }
        private string genMCode(string ab,int x, int z)
        {
            string section = parameters.sectionNum.ToString();
            if (parameters.sectionNum < 10) section = "0" + section;
            
            string side = "0";
            if (ab == "b") side = "1";
            
            string bay = baynum.ToString();
            if (baynum < 10) bay = "0" + bay;

            string mCode = section + "_" + bay + "_" + side + "_" + x + "_" + z;
            return mCode;
        }
        private string genMCode(int s, int x, int z)
        {
            string section = parameters.sectionNum.ToString();
            if (parameters.sectionNum < 10) section = "0" + section;

            string side = s.ToString();
            
            string bay = baynum.ToString();
            if (baynum < 10) bay = "0" + bay;

            string mCode = section + "_" + bay + "_" + side + "_" + x + "_" + z;
            return mCode;
        }
        
        
    }

}
