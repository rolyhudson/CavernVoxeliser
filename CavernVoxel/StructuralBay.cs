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
        Mesh slice = new Mesh();
        public VoxelParameters parameters;
        bool leader = false;
        KDTree<double> tree;
        public int baynum;
        
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
        
        private void voxelise()
        {
            for (int z = 0; z <= parameters.unitsZ; z++)
            {
                //sideA
                for (int x = 0; x < parameters.unitsXa; x++)
                {
                    
                    if (x == parameters.unitsXa - 1) voxelTest(x, z, minPlane, "a", true);
                    else voxelTest(x, z, minPlane, "a", false);
                }
                //sideB
                for (int x = 0; x < parameters.unitsXb; x++)
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
            structuralContinuity();
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
            var skin = col.Find(x => x.cellType == StructuralCell.CellType.SkinCell);
            var t = cells.Find(x => x.id == genMCode(0, 7, 2));
            if(skin!=null)
            {
                int z = cell.rowNum;
                string id = "";
                if (skin.rowNum >= cell.rowNum)
                {
                    //add continuity on filler side upwards
                    var cellsFill = voxels.SelectMany(x => x).ToList().FindAll(c => c.side == cell.side && c.colNum == cell.colNum);
                    while (true)
                    {
                        z++;
                        var next = cellsFill.Find(x=>x.id == genMCode(cell.side, cell.colNum, z));
                        var pair = cells.Find(x => x.rowNum == z);
                        next.cellType = StructuralCell.CellType.PerimeterCell;
                        if (pair.cellType == StructuralCell.CellType.PerimeterCell || pair.cellType == StructuralCell.CellType.VerticalFillCell) break;
                    }
                    
                }
                else
                {
                    //add continuity on non filler side downwards
                    while (true)
                    {
                        var pair = cells.Find(x => x.rowNum == z);
                        if (pair.cellType == StructuralCell.CellType.PerimeterCell || pair.cellType == StructuralCell.CellType.VerticalFillCell) break;
                        else pair.cellType = StructuralCell.CellType.PerimeterCell;
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
                    if (n.cellType == StructuralCell.CellType.VerticalFillCell || n.cellType == StructuralCell.CellType.PerimeterCell) structuralNeighbour = true;
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
                    if (Math.Abs(v.X) < 10 && Math.Abs(v.Y) < 10 && Math.Abs(v.Length - parameters.zCell) < 10)
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
                    if (Math.Abs(v.Length- parameters.xCell) < 10)
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
            Mesh testCell = new Mesh();
            Mesh trimCell = new Mesh();
            Interval trimYInterval = new Interval(-parameters.yCell /2, parameters.yCell);
            if (leader) trimYInterval = new Interval(-parameters.yCell, parameters.yCell / 2);
            
            if (filler)
            {
                testCell = makeCuboid(cellpln, parameters.fillerCellX,new Interval(-parameters.yCell /2, parameters.yCell /2), parameters.zCell);
                trimCell = makeCuboid(cellpln, parameters.fillerCellX, trimYInterval, parameters.zCell);
            }
            else
            {
                testCell = makeCuboid(cellpln, parameters.xCell, new Interval(-parameters.yCell / 2, parameters.yCell / 2), parameters.zCell);
                trimCell = makeCuboid(cellpln, parameters.xCell, trimYInterval, parameters.zCell);
            }
            
            bool intersect = false;
            Mesh caveface = MeshTools.splitMeshWithMesh(slice, trimCell);
            if (caveface != null) intersect = true;
            string mCode = genMCode(ab,x,z);
            if(mCode=="03_00_1_7_2")
            {
                var p =0;
            }
            if (intersect)
            {
                MeshTools.matchOrientation(slice, ref caveface);
                StructuralCell structCell = new StructuralCell(testCell, parameters.memberSize, caveface.DuplicateMesh(),mCode,filler);
                if (ab == "a") voxels[0].Add(structCell);
                else voxels[1].Add(structCell);
            }
            else
            {
                StructuralCell structCell = new StructuralCell(testCell, parameters.memberSize, mCode,filler);
                if (ab == "a") voxels[0].Add(structCell);
                else voxels[1].Add(structCell);
            }
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
        private Mesh makeCuboid(Plane pln,double width, Interval depth,double height)
        {
            Mesh cell = new Mesh();
            Box box = new Box(pln, new Interval(-width / 2, width / 2), depth, new Interval(-height / 2, height / 2));
            cell = Mesh.CreateFromBox(box, 1, 1, 1);
            return cell;
        }
        
    }

}
