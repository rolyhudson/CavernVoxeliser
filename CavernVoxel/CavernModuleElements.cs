using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace CavernVoxel
{
    public class CavernModuleElements : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CavernModuleElements class.
        /// </summary>
        public CavernModuleElements()
          : base("CavernModuleElements", "CEle",
               "Description",
               "CVox", "CVox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("modules", "m", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("nodes", "n", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("centre lines", "cl", "", GH_ParamAccess.tree);
            
            pManager.AddBrepParameter("complete cells", "cc", "", GH_ParamAccess.tree);
            pManager.AddMeshParameter("cave face meshes", "cfm", "", GH_ParamAccess.tree);
            pManager.AddMeshParameter("GSAMeshes", "gsam", "", GH_ParamAccess.tree);
            pManager.AddPointParameter("cell cenroids", "cc", "", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("disjointed cave face", "df", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> obj = new GH_Structure<IGH_Goo>();
            DA.GetDataTree(0, out obj);
            if (obj != null)
            {
                var paths = obj.Paths;
                
                GH_Structure<GH_Brep> cellscomplete = new GH_Structure<GH_Brep>();
                GH_Structure<GH_Mesh> caveFaceMeshes = new GH_Structure<GH_Mesh>();
                GH_Structure<GH_Curve> centreLines = new GH_Structure<GH_Curve>();
                GH_Structure<GH_Mesh> gsameshes = new GH_Structure<GH_Mesh>();
                GH_Structure<GH_Point> nodes = new GH_Structure<GH_Point>();
                GH_Structure<GH_Point> centroids = new GH_Structure<GH_Point>();
                GH_Structure<GH_Boolean> disjoint = new GH_Structure<GH_Boolean>();

                for (int i = 0; i < obj.Branches.Count; i++)
                {
                    GH_Path path = paths[i];
                    for (int j = 0; j < obj.Branches[i].Count; j++)
                    {

                        StructuralCell cell = null;
                        obj[i][j].CastTo(out cell);
                        if (cell.cellType != StructuralCell.CellType.InsideCell)
                        {
                            
                            foreach (Curve cl in cell.centreLines)
                            {
                                centreLines.Append(new GH_Curve(cl.ToNurbsCurve()), path);
                            }
                            foreach (Curve cl in cell.diagonals)
                            {
                                centreLines.Append(new GH_Curve(cl.ToNurbsCurve()), path);
                            }
                            foreach (Point3d p in cell.nodes)
                            {
                                nodes.Append(new GH_Point(p), path);
                            }

                            if (cell.cellType == StructuralCell.CellType.SkinCell)
                            {
                                if(cell.GSAmesh!=null) gsameshes.Append(new GH_Mesh(cell.GSAmesh), path);
                                if(cell.caveFace!=null) caveFaceMeshes.Append(new GH_Mesh(cell.caveFace), path);
                                if (cell.caveFace.DisjointMeshCount > 1) disjoint.Append(new GH_Boolean(true));
                                else disjoint.Append(new GH_Boolean(false));
                            }
                            cellscomplete.Append(new GH_Brep(cell.innerBoundary), path);
                            centroids.Append(new GH_Point(cell.centroid), path);

                        }

                    }
                }
                DA.SetDataTree(0, nodes);
                DA.SetDataTree(1, centreLines);
                
                DA.SetDataTree(2, cellscomplete);
                DA.SetDataTree(3, caveFaceMeshes);
                DA.SetDataTree(4, gsameshes);
                DA.SetDataTree(5, centroids);
                DA.SetDataTree(6, disjoint);
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
            get { return new Guid("f27ddb28-e1ec-4ae7-86dc-2d26037f97c6"); }
        }


    }
}