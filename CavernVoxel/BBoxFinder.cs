using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace CavernVoxel
{
    public class BBoxFinder : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the BBoxFinder class.
        /// </summary>
        public BBoxFinder()
          : base("BBoxFinder", "BBF",
               "Description",
               "CVox", "CVox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("meshes to box", "mtb", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("boxes", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> meshes = new List<Mesh>();
            List<Box> boxes = new List<Box>();
            if (!DA.GetDataList(0, meshes)) return;
            foreach(Mesh m in meshes)
            {
                boxes.Add(MeshTools.findBBox(m));
            }
            DA.SetDataList(0, boxes);
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
            get { return new Guid("3bf801ee-9393-4215-9114-8fef76adeb2c"); }
        }
    }
}