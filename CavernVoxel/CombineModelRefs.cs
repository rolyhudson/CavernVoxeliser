using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace CavernVoxel
{
    public class CombineModelRefs : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CombineModelRefs class.
        /// </summary>
        public CombineModelRefs()
          : base("CombineModelRefs", "CVox",
               "Description",
               "CVox", "CVox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("folder with files to reference", "fwf", "", GH_ParamAccess.item);
            pManager.AddTextParameter("search string", "ss", "", GH_ParamAccess.item);
            pManager.AddTextParameter("output file name", "ofn", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("run", "r", "", GH_ParamAccess.item, false);
            pManager.AddBrepParameter("reference geometry", "rg", "", GH_ParamAccess.list);
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
            string folder = "";
            string search = "";
            string outfile = "";
            bool run = false;
            List<Brep> refgeometry = new List<Brep>();
            if (!DA.GetData(0, ref folder)) return;
            if (!DA.GetData(1, ref search)) return;
            if (!DA.GetData(2, ref outfile)) return;
            if (!DA.GetData(3, ref run)) return;
            DA.GetDataList(4, refgeometry);
            if (run)
            {
                CombineRhinoFiles rhinoFile = new CombineRhinoFiles(folder, outfile, search,refgeometry);
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
            get { return new Guid("2442b74e-6f84-4ab0-a136-badb60567157"); }
        }
    }
}