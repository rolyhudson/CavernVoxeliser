using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Display;
using System.Drawing;

namespace CavernVoxel
{
    class VoxelDocumenter
    {
        List<ObjectAttributes> objectAttributes = new List<ObjectAttributes>();
        ObjectAttributes attCavepanels;
        
        ObjectAttributes attGrid;
        ObjectAttributes attBasegrid;
        ObjectAttributes attCentrelines;
        ObjectAttributes attDiagonals;
        ObjectAttributes attGsamesh;
        ObjectAttributes attAnnotation;
        ObjectAttributes attClippingplanes;
        public static void moduleSchedule(MeshVoxeliser mvox)
        {
            int bayNum = 0;
            int modulesCount = 0;
            string section = mvox.parameters.sectionNum.ToString();
            if (mvox.parameters.sectionNum < 10) section ="0"+ section;
            StreamWriter sw = new StreamWriter(@"C:\Users\r.hudson\Documents\WORK\projects\passageProjects\sections\" + section + "modules.csv");
            sw.WriteLine("module code, type, disjoint cave panel");
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                foreach (StructuralBay sb in sp.structuralBays)
                {
                    foreach (List<StructuralCell> sc in sb.voxels)
                    {
                        foreach (StructuralCell c in sc)
                        {
                            if (c.cellType != StructuralCell.CellType.InsideCell && c.cellType != StructuralCell.CellType.Undefined)
                            {

                                int flag = 0;
                                if (c.cellType == StructuralCell.CellType.SkinCell) flag = c.caveFace.DisjointMeshCount - 1;
                                sw.WriteLine(c.id + "," + c.cellType.ToString() + "," + flag);
                                modulesCount++;
                            }
                        }
                    }
                    bayNum++;
                }
            }
            sw.Close();

            StreamWriter sw2 = new StreamWriter(@"C:\Users\r.hudson\Documents\WORK\projects\passageProjects\sections\modulesSummary.csv", true);
            sw2.WriteLine("section" + section + ",total bays:," + bayNum + ",total modules all types:," + modulesCount);
            sw2.Close();
        }
        private void addLayers(File3dm file,List<string> layers)
        {
            int index = 0;
            List<Color> colors =new List<Color> {Color.Black,Color.DarkBlue,Color.DarkGray,Color.DarkGray,Color.DarkOliveGreen,Color.DarkOrange,Color.DarkOrchid,Color.DarkSeaGreen };
            foreach(string l in layers)
            {
                Layer layer = new Layer();
                layer.Name = l;
                if(l=="clippingplanes") layer.IsVisible = false;
                layer.Color = System.Drawing.Color.Black;
                file.AllLayers.Add(layer);
                
                ObjectAttributes oa = new ObjectAttributes();
                oa.LayerIndex = index;
                
                oa.ObjectColor = colors[index];
                oa.Name = l;
                objectAttributes.Add(oa);
                index++;
            }
            
        }
        private File3dm setupFile()
        {
            List<string> layers = new List<string>();
            layers.Add("cavepanels");

            layers.Add("grid");
            layers.Add("basegrid");

            layers.Add("gsamesh");
            layers.Add("centrelines");
            layers.Add("diagonals");
            layers.Add("annotation");
            layers.Add("clippingplanes");

            File3dm file = new File3dm();
            addLayers(file, layers);
            setAttributes();
            return file;
        }//
        private void setAttributes()
        {
            attCavepanels = objectAttributes.Find(o => o.Name == "cavepanels");
            attCavepanels.ColorSource = ObjectColorSource.ColorFromObject;
            attGrid = objectAttributes.Find(o => o.Name == "grid");
            attBasegrid = objectAttributes.Find(o => o.Name == "basegrid");
            attCentrelines = objectAttributes.Find(o => o.Name == "centrelines");
            attDiagonals = objectAttributes.Find(o => o.Name == "diagonals");
            attGsamesh = objectAttributes.Find(o => o.Name == "gsamesh");
            attAnnotation = objectAttributes.Find(o => o.Name == "annotation");
            attClippingplanes = objectAttributes.Find(o => o.Name == "clippingplanes");
        }
        private bool curveInHalfSpace(Curve c,Plane p)
        {
            bool inhalfspace = false;
            Vector3d v1 = c.PointAtStart - p.Origin;
            Vector3d v2 = c.PointAtEnd - p.Origin;
            if(Vector3d.VectorAngle(v1,p.Normal)<Math.PI/2 && Vector3d.VectorAngle(v2, p.Normal) < Math.PI / 2)
            {
                inhalfspace = true;
            }
                return inhalfspace;
        }
        private void addBaySection2d(File3dm file,StructuralBay bay, Point3d location)
        {
            int pWidth = 35000;
            Point3d origin = new Point3d(bay.minPlane.Origin);
            Plane plnA = new Plane(origin + bay.minPlane.YAxis * bay.parameters.yCell / 2, bay.minPlane.XAxis, Vector3d.ZAxis);
            Vector3d shift = new Vector3d(location);
            Vector3d panelShift = new Vector3d(location.X+ pWidth, location.Y,location.Z);
            Plane titlepln = Plane.WorldXY;
            titlepln.Transform(Transform.Translation(shift ));
            string baytitle = bay.baynum.ToString();
            if (bay.baynum < 10) baytitle = "0" + baytitle;
            Text3d title = new Text3d("bay_" + baytitle, titlepln, 300);
            var id = file.Objects.AddText(title);
            int col = 0;
            int row = 0;
            foreach (List<StructuralCell> scs in bay.voxels)
            {
                foreach(StructuralCell sc in scs)
                {
                    //intersect the cave face with the plan
                    if (sc.cellType != StructuralCell.CellType.InsideCell && sc.cellType != StructuralCell.CellType.Undefined)
                    {
                        if(sc.cellType== StructuralCell.CellType.SkinCell)
                        {
                            
                            attCavepanels.ObjectColor = sc.displayColor;
                            var intersect = Rhino.Geometry.Intersect.Intersection.MeshPlane(sc.caveFace, plnA);
                            if (intersect != null)
                            {
                                foreach (Polyline pl in intersect)
                                {
                                    Polyline mapped = new Polyline(mapAndTranslatePts(plnA, pl.ToList(), shift));
                                    file.Objects.AddPolyline(mapped, attCavepanels);
                                }

                                panelShift = new Vector3d(col * 2500, row * 2500, 0);
                                panelShift.X = panelShift.X + pWidth / 2;
                                Plane txtpln = Plane.WorldXY;
                                txtpln.Transform(Transform.Translation(shift + panelShift));

                                Text3d modulecode = new Text3d(sc.id, txtpln, 100);
                                var g = file.Objects.AddText(modulecode, attAnnotation);
                                Mesh cavepanel2d = new Mesh();
                                cavepanel2d.Vertices.AddVertices(mapAndTranslatePts(sc.midPlane, sc.caveFace.Vertices.ToPoint3dArray().ToList(), shift + panelShift));
                                cavepanel2d.Faces.AddFaces(sc.caveFace.Faces);
                                file.Objects.AddMesh(cavepanel2d, attCavepanels);
                                col++;
                                if (col > 5)
                                {
                                    col = 0;
                                    row++;
                                }
                            }
                            
                        }
                        //diagonals 
                        addCurves(file, sc.diagonals, plnA, attDiagonals, shift);
                        //centrelines
                        addCurves(file, sc.centreLines, plnA, attCentrelines, shift);

                    }
                    
                }
            }
        }
       
        private void addCurves(File3dm file, List<Curve> curves,Plane p, ObjectAttributes att,Vector3d t)
        {
            foreach (Curve c in curves)
            {
                if (curveInHalfSpace(c, p))
                {
                    
                    var pts = mapAndTranslatePts(p, new List<Point3d> { c.PointAtStart, c.PointAtEnd }, t);
                    file.Objects.AddLine(new Line(pts[0], pts[1]),att);
                }
            }
        }
        private List<Point3d> mapAndTranslatePts(Plane mapPlane, List<Point3d> originalPts, Vector3d transV)
        {
            List<Point3d> mappedPts = new List<Point3d>();
            Transform xform = Transform.Translation(transV);
            foreach (Point3d pt in originalPts)
            {
                Point3d onPlane = mapPlane.ClosestPoint(pt);
                Point3d remapped = new Point3d();
                mapPlane.RemapToPlaneSpace(onPlane, out remapped);
                remapped.Transform(xform);
                mappedPts.Add(remapped);
            }
            return mappedPts;
        }
        private void makeBayView(File3dm file,StructuralBay bay)
        {
            //File3dm file = setupFile();
            RhinoViewport viewport = new RhinoViewport();

            //shift to mid point of module
            Point3d origin = new Point3d(bay.minPlane.Origin);
            Plane plnA = new Plane(origin + bay.minPlane.YAxis * bay.parameters.yCell / 2, bay.minPlane.XAxis,Vector3d.ZAxis);
            //shift to end of module
            Plane plnB = new Plane(origin + bay.minPlane.YAxis * bay.parameters.yCell, bay.minPlane.XAxis, Vector3d.ZAxis);
            plnA.Flip();
            
            
            viewport.SetProjection(DefinedViewportProjection.Perspective, "bay_" + bay.baynum.ToString() + "section", false);
            
            viewport.ChangeToParallelProjection(true);
            
            //set camera and target
            Vector3d shiftTarget = bay.maxPlane.Origin - bay.minPlane.Origin;
            viewport.SetCameraTarget(bay.minPlane.Origin + shiftTarget / 2, false);
            viewport.SetCameraLocation(viewport.CameraTarget + bay.minPlane.YAxis * -50000,false);
            viewport.SetCameraDirection(viewport.CameraTarget - viewport.CameraTarget, false);
            viewport.CameraUp = Vector3d.ZAxis;
            //viewport.ZoomExtents();
            viewport.PushViewProjection();
            file.AllViews.Add(new ViewInfo(viewport));
            
            //Clip plane A
            file.Objects.AddClippingPlane(plnA,bay.parameters.width,bay.parameters.height,viewport.Id);
            //Clip plane B
            file.Objects.AddClippingPlane(plnB, bay.parameters.width, bay.parameters.height, viewport.Id);

        }
        public void writeSection2d(MeshVoxeliser mvox)
        {
            File3dm file = setupFile();
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                
                Point3d location = new Point3d();
                foreach (StructuralBay sb in sp.structuralBays)
                {
                    location.X = sb.baynum * 40000;
                    addBaySection2d(file, sb, location);
                }
            }
            RhinoViewport viewport = new RhinoViewport();
            viewport.SetProjection(DefinedViewportProjection.Top, "2d sections", false);
            viewport.ZoomExtents();
            file.AllViews.Add(new ViewInfo(viewport));
            string section = mvox.parameters.sectionNum.ToString();
            if (mvox.parameters.sectionNum < 10) section = "0" + section;
            file.Write(@"C:\Users\r.hudson\Documents\WORK\projects\passageProjects\sections\section" + section + "_2d.3dm", 5);
        }
        private void setClippingPlanes(Plane a, Plane b,File3dm file,string vpName,double w,double h)
        {
            RhinoViewport viewport = new RhinoViewport();
            viewport.SetProjection(DefinedViewportProjection.Top, vpName, false);
            
            
            viewport.SetCameraTarget(new Point3d(13817.498, 7162.495, -6025.053),true);
            var vInfo = new ViewInfo(viewport);
            file.AllViews.Add(vInfo);
            //Clip plane A
            file.Objects.AddClippingPlane(a, w, h,new List<Guid> { viewport.Id },attClippingplanes);
            //Clip plane B
            file.Objects.AddClippingPlane(b, w, h, new List<Guid> { viewport.Id }, attClippingplanes);
            
        }
        private void addcavepanels2d(List<StructuralCell> scs,File3dm file,Plane plnA)
        {
            int col = 0;
            int row = 0;
            int pWidth = 35000;
            Vector3d shift = new Vector3d(plnA.Origin);
            Vector3d panelShift = new Vector3d(pWidth, 0, 0);
            foreach (StructuralCell sc in scs)
            {
                //intersect the cave face with the plan
                
                    if (sc.cellType == StructuralCell.CellType.SkinCell)
                    {
                        if (sc.caveFace != null)
                        {
                            attCavepanels.ObjectColor = sc.displayColor;
                            panelShift = new Vector3d(col * 2500, row * 2500, 0);
                            panelShift.X = panelShift.X + pWidth / 2;
                            Plane txtpln = Plane.WorldXY;
                            txtpln.Transform(Transform.Translation(shift+panelShift));

                            Text3d modulecode = new Text3d(sc.id, txtpln, 100);
                            var g = file.Objects.AddText(modulecode, attAnnotation);
                            Mesh cavepanel2d = new Mesh();
                            cavepanel2d.Vertices.AddVertices(mapAndTranslatePts(sc.midPlane, sc.caveFace.Vertices.ToPoint3dArray().ToList(), shift+panelShift));
                            cavepanel2d.Faces.AddFaces(sc.caveFace.Faces);
                            file.Objects.AddMesh(cavepanel2d, attCavepanels);
                            col++;
                            if (col > 5)
                            {
                                col = 0;
                                row++;
                            }
                        }

                }

            }
        }
        public void map3dToWorldXY(MeshVoxeliser mvox)
        {
            File3dm file = setupFile();
            Plane mapFrom = new Plane(mvox.structuralSpans[0].minPlane.Origin, mvox.structuralSpans[0].minPlane.XAxis, Vector3d.ZAxis);
            Transform transform = Transform.PlaneToPlane(mapFrom,Plane.WorldXY);
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                foreach (StructuralBay sb in sp.structuralBays)
                {
                    Point3d origin = new Point3d(sb.minPlane.Origin);
                    Plane plnA = new Plane(origin + sb.minPlane.YAxis * sb.parameters.yCell / 2, sb.minPlane.XAxis, Vector3d.ZAxis);
                    //shift to end of module
                    Plane plnB = new Plane(origin + sb.minPlane.YAxis * sb.parameters.yCell, sb.minPlane.XAxis, Vector3d.ZAxis);
                    plnA.Flip();
                    plnA.Transform(transform);
                    plnB.Transform(transform);
                    string baytitle = sb.baynum.ToString();
                    if (sb.baynum < 10) baytitle = "0" + baytitle;
                    setClippingPlanes(plnA, plnB, file, "bay_" + baytitle, sb.parameters.width, sb.parameters.height);
                    Text3d title = new Text3d("bay_" + baytitle, new Plane(plnA.Origin,Vector3d.ZAxis), 300);
                    file.Objects.AddText(title, attAnnotation);
                    
                    
                    foreach (List<StructuralCell> sc in sb.voxels)
                    {
                        foreach (StructuralCell c in sc)
                        {
                            if (c.cellType != StructuralCell.CellType.InsideCell && c.cellType != StructuralCell.CellType.Undefined)
                            {
                                attCavepanels.ObjectColor = c.displayColor;
                                if (c.caveFace != null)
                                {
                                    c.caveFace.Transform(transform);
                                    c.midPlane.Transform(transform);
                                    file.Objects.AddMesh(c.caveFace, attCavepanels);
                                }
                                foreach (Curve cl in c.centreLines)
                                {
                                    cl.Transform(transform);
                                    file.Objects.AddCurve(cl, attCentrelines);
                                }

                                foreach (Curve d in c.diagonals)
                                {
                                    d.Transform(transform);
                                    file.Objects.AddCurve(d, attDiagonals);
                                }
                                if (c.GSAmesh != null)
                                {
                                    c.GSAmesh.Transform(transform);
                                    file.Objects.AddMesh(c.GSAmesh, attGsamesh);
                                }

                            }
                        }
                    }
                    addcavepanels2d(sb.voxels.SelectMany(x => x).ToList(), file, plnA);
                }
            }
            string section = mvox.parameters.sectionNum.ToString();
            if (mvox.parameters.sectionNum < 10) section = "0" + section;
            file.Write(@"C:\Users\r.hudson\Documents\WORK\projects\passageProjects\sections\section" + section + "_ClippedSections.3dm", 5);
        }
        public void writeSection3d(MeshVoxeliser mvox)
        {
            File3dm file = setupFile();

            file.Objects.AddText(mvox.sectionNum,attAnnotation);
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                foreach (Line xg in sp.xGrid) file.Objects.AddLine(xg, attGrid);
                foreach (Line yg in sp.yGrid) file.Objects.AddLine(yg, attGrid);
                foreach(Line bg in sp.baseGrid) file.Objects.AddLine(bg, attBasegrid);
                foreach(Text3d text in sp.txt) file.Objects.AddText(text,attAnnotation);
                foreach (StructuralBay sb in sp.structuralBays)
                {
                    
                    foreach (List<StructuralCell> sc in sb.voxels)
                    {
                        foreach (StructuralCell c in sc)
                        {
                            if (c.cellType != StructuralCell.CellType.InsideCell && c.cellType != StructuralCell.CellType.Undefined)
                            {
                                attCavepanels.ObjectColor = c.displayColor;
                                if (c.caveFace!=null)file.Objects.AddMesh(c.caveFace, attCavepanels);
                                foreach(Curve cl in c.centreLines) file.Objects.AddCurve(cl, attCentrelines);
                                foreach (Curve d in c.diagonals) file.Objects.AddCurve(d, attDiagonals);
                                if(c.GSAmesh!=null)file.Objects.AddMesh(c.GSAmesh,attGsamesh);
                            }
                        }
                    }
                }
            }
          
            
            RhinoViewport viewport = new RhinoViewport();
            viewport.SetProjection(DefinedViewportProjection.Perspective, "cavern view", false);
            viewport.ZoomExtents();
            file.AllViews.Add(new ViewInfo(viewport));
            string section = mvox.parameters.sectionNum.ToString();
            if (mvox.parameters.sectionNum < 10) section = "0" + section;
            file.Write(@"C:\Users\r.hudson\Documents\WORK\projects\passageProjects\sections\section"+section+ "_3d.3dm", 5);
        }
        
    }
    
}
