using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino;


namespace CavernVoxel
{
    class MeshVoxeliser
    {
        
        double xCell;
        double yCell;
        double zCell;
        List<Mesh> meshesToVoxelise;
        public List<StructuralBay> structuralBays = new List<StructuralBay>();
        
        double voxelSphereRad;
        
        Plane gridPlane;
        double width;
        double length;
        double height;
        double memberSize;
        Mesh baseCell = new Mesh();
        public MeshVoxeliser(List<Mesh> meshes,double x, double y,double z,double memberDim, int startBay, int endBay)
        {
            meshesToVoxelise = meshes;
            meshesToVoxelise.ForEach(m => m.Normals.ComputeNormals());
            meshesToVoxelise.ForEach(m => m.FaceNormals.ComputeFaceNormals());
            xCell = x;
            yCell = y;
            zCell = z;
            memberSize = memberDim;

            voxelSphereRad = Math.Sqrt(x * x+ y * y + z * z)/2;
            
            findBBox();
            setupBays(startBay, endBay);
            
        }
        
        
        private void setupBays(int start, int end)
        {

            int unitsY = Convert.ToInt32(Math.Ceiling(length / yCell));
            if (start % 2 != 0) start = start - 1;
            if (end > unitsY) end = unitsY;
            if (start > unitsY) { start = unitsY - 1; end = unitsY; }
            StructuralBay structBayPrev = null;
            for (int y = start; y < end; y++)//unitsY
            {
                if (y % 2 != 0)
                {
                    structuralBays.Add( new StructuralBay(structBayPrev));
                }
                else
                {
                    Vector3d shiftY = gridPlane.YAxis * y * yCell;
                    Point3d basePt = new Point3d(gridPlane.OriginX, gridPlane.OriginY, gridPlane.OriginZ);
                    Point3d origin = basePt + shiftY;
                    Plane boxPln = new Plane(origin, gridPlane.XAxis, gridPlane.YAxis);
                    //containing box with allowance in x and z directions
                    Box box = new Box(boxPln, new Interval(-xCell / 10, width + xCell / 10), new Interval(0, yCell * 2), new Interval(-zCell / 10, height + zCell / 10));
                    Mesh sectionVolume = Mesh.CreateFromBox(box, 1, 1, 1);
                    Mesh slice = MeshTools.splitMeshWithMesh(meshesToVoxelise[0], sectionVolume);
                    if (slice != null)
                    {
                        StructuralBay structBay = new StructuralBay(slice, sectionVolume.DuplicateMesh(), boxPln, xCell, yCell, zCell, memberSize);
                        structuralBays.Add(structBay);
                        structBayPrev = structBay;
                    }
                }
                
            }
        }
        
        private void findBBox()
        {
            BoundingBox minBBox = new BoundingBox();
            double minVol = Double.MaxValue;
            double theta = Math.PI / 100;
            Plane minBBoxPln = new Plane();
            double minBBoxRot = 0;
            for (int i = 0; i < 100; i++)
            {
                Plane pln = Plane.WorldXY;
                pln.Rotate(theta * i, Vector3d.ZAxis);
                List<Point3d> pts = new List<Point3d>();
                foreach (Mesh m in meshesToVoxelise)
                {
                    foreach (Point3d p in m.Vertices)
                    {
                        Point3d remapped = new Point3d();
                        pln.RemapToPlaneSpace(p, out remapped);

                        pts.Add(remapped);
                    }
                }
                BoundingBox bBox = new BoundingBox(pts);
                if (bBox.Volume < minVol)
                {
                    minBBoxPln = pln;
                    minVol = bBox.Volume;
                    minBBox = bBox;
                    minBBoxRot = theta * i;
                }

            }
            width = minBBox.Max.X - minBBox.Min.X;
            length = minBBox.Max.Y - minBBox.Min.Y;
            height = minBBox.Max.Z - minBBox.Min.Z;
            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, minBBoxPln);
            Point3d origin = minBBox.Min;
            origin.Transform(xform);
            gridPlane = new Plane(origin, Vector3d.ZAxis);
            gridPlane.Rotate(minBBoxRot, Vector3d.ZAxis);
        }
        
    }
}
