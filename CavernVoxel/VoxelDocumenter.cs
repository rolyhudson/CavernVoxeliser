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
            List<Color> colors =new List<Color> {Color.Black,Color.DarkBlue,Color.DarkGray,Color.DarkGray,Color.DarkOliveGreen,Color.DarkOrange,Color.DarkOrchid };
            foreach(string l in layers)
            {
                Layer layer = new Layer();
                layer.Name = l;
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

            File3dm file = new File3dm();
            addLayers(file, layers);
            return file;
        }
        private void makeBayView(File3dm file,StructuralBay bay)
        {
            //File3dm file = setupFile();
            RhinoViewport viewport = new RhinoViewport();
            viewport.SetProjection(DefinedViewportProjection.Front, "bay_"+bay.baynum.ToString()+"section", false);
            
            Vector3d shiftTarget = bay.maxPlane.Origin - bay.minPlane.Origin;

            file.AllViews.Add(new ViewInfo(viewport));
            //shift to mid point of module
            Plane plnA = new Plane(bay.minPlane.Origin + bay.minPlane.YAxis * bay.parameters.yCell/2, bay.minPlane.YAxis);
            
            Plane plnB = new Plane(plnA);
            //shift to end of module
            plnB.Origin = plnB.Origin + bay.minPlane.YAxis * bay.parameters.yCell;
            plnB.Flip();
            //set camera and target
            viewport.SetCameraTarget(bay.maxPlane.Origin + shiftTarget / 2 + Vector3d.ZAxis * bay.parameters.unitsZ / 2 * bay.parameters.zCell, false);
            viewport.SetCameraLocation(viewport.CameraTarget + bay.minPlane.YAxis * -50000,false);
            //Clip plane A
            file.Objects.AddClippingPlane(plnA,bay.parameters.width,bay.parameters.height,viewport.Id);
            //Clip plane B
            file.Objects.AddClippingPlane(plnB, bay.parameters.width, bay.parameters.height, viewport.Id);
            //make a layout
            //add detail view
            //set detail view viewport to perpendicular to clip plane
            //set clip planes only active in this view
        }
        public void writeSection(MeshVoxeliser mvox)
        {
            File3dm file = setupFile();
            Random r = new Random();
            
            ObjectAttributes attCavepanels = objectAttributes.Find(o => o.Name == "cavepanels");
            attCavepanels.ColorSource = ObjectColorSource.ColorFromObject;
            ObjectAttributes attGrid = objectAttributes.Find(o => o.Name == "grid");
            ObjectAttributes attBasegrid = objectAttributes.Find(o => o.Name == "basegrid");
            ObjectAttributes attCentrelines = objectAttributes.Find(o => o.Name == "centrelines");
            ObjectAttributes attDiagonals = objectAttributes.Find(o => o.Name == "diagonals");
            ObjectAttributes attGsamesh = objectAttributes.Find(o => o.Name == "gsamesh");
            ObjectAttributes attAnnotation = objectAttributes.Find(o => o.Name == "annotation");

            file.Objects.AddText(mvox.sectionNum,attAnnotation);
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                foreach (Line xg in sp.xGrid) file.Objects.AddLine(xg, attGrid);
                foreach (Line yg in sp.yGrid) file.Objects.AddLine(yg, attGrid);
                foreach(Line bg in sp.baseGrid) file.Objects.AddLine(bg, attBasegrid);
                foreach(Text3d text in sp.txt) file.Objects.AddText(text,attAnnotation);
                foreach (StructuralBay sb in sp.structuralBays)
                {
                    makeBayView(file, sb);
                    foreach (List<StructuralCell> sc in sb.voxels)
                    {
                        foreach (StructuralCell c in sc)
                        {
                            if (c.cellType != StructuralCell.CellType.InsideCell && c.cellType != StructuralCell.CellType.Undefined)
                            {
                                int red = r.Next(255);
                                int green = r.Next(255);
                                int blue = r.Next(255);
                                attCavepanels.ObjectColor = System.Drawing.Color.FromArgb(red, green, blue);
                                
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
            file.Write(@"C:\Users\r.hudson\Documents\WORK\projects\passageProjects\sections\section"+section+".3dm", 5);
        }
        public static void create3dmTest()
        {
            File3dm file = new File3dm();
            file.Objects.AddLine(new Line(new Point3d(0, 0, 0), new Point3d(0, 10, 0)));


            
        }
    }
    
}
