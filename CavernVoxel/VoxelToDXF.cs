using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace CavernVoxel
{
    public class VoxelToDXF : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the VoxelToDXF class.
        /// </summary>
        public VoxelToDXF()
          : base("VoxelToDXF", "CVox",
               "Description",
               "CVox", "CVox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("centrelines", "cl", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("gsamesh", "gsa", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("cave panels", "cp", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("grid", "g","", GH_ParamAccess.list);
            pManager.AddCurveParameter("base grid", "bg", "", GH_ParamAccess.list);
            pManager.AddTextParameter("filepath", "fp", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("write", "w", "", GH_ParamAccess.item);
            pManager.AddTextParameter("text", "t", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("text locations", "tl", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> centerlines = new List<Curve>();
            List<Mesh> gsa = new List<Mesh>();
            List<Mesh> cavepanels = new List<Mesh>();
            List<Curve> grid = new List<Curve>();
            List<Curve> basegrid = new List<Curve>();
            List<string> text = new List<string>();
            List<Curve> textlocations = new List<Curve>();
            string path = "";
            bool run = false;
            if(!DA.GetDataList(0, centerlines))return;
            if (!DA.GetDataList(1, gsa)) return;
            if (!DA.GetDataList(2, cavepanels)) return;
            if (!DA.GetDataList(3, grid)) return;
            if (!DA.GetDataList(4, basegrid)) return;
            if (!DA.GetData(5, ref path)) return;
            if (!DA.GetData(6, ref run)) return;
            if (!DA.GetDataList(7, text)) return;
            if (!DA.GetDataList(8, textlocations)) return;
            if (run)
            {
                DXFwriter writer = new DXFwriter();
                writeLines(writer, centerlines, "centrelines",1);
                writeLines(writer, grid, "grid",60);
                writeLines(writer, basegrid, "basegrid",78);
                writeMeshes(writer, gsa, "gsamesh",150);
                writeMeshes(writer, cavepanels, "cavepanels",250);
                writeText(writer, text, textlocations);
                writer.finishAndWrite(path);
            }
            

        }
        private void writeText(DXFwriter writer, List<string> text,List<Curve> locations)
        {
            for(int t=0;t<text.Count;t++)
            {
                Vector3d v = locations[t].PointAtEnd - locations[t].PointAtStart;
                double rot = Vector3d.VectorAngle(Vector3d.XAxis, v)* 57.2958;
                writer.DXFText(text[t], locations[t].PointAtStart.X, locations[t].PointAtStart.Y, locations[t].PointAtStart.Z, "text",1000, 0,rot);
            }
        }
        private void writeLines(DXFwriter writer,List<Curve> lines,string layer,int col)
        {
            foreach(Curve c in lines)
            {
                writer.DXFLines(c.PointAtStart, c.PointAtEnd, layer,col);
            }
        }
        private void writeMeshes(DXFwriter writer, List<Mesh> meshes, string layer, int col)
        {
            foreach(Mesh m in meshes)
            {
                if (m != null)
                {
                    writer.DXFPolyfaceMesh(m, layer,col);
                }
                
            }
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ab35a7b8-35f4-4cc9-a557-93ab18b12d0f"); }
        }
    }
}