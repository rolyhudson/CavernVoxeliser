using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace CavernVoxel
{
    public class CavernVoxelComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CavernVoxelComponent()
           : base("CavernVoxel", "CVox",
               "Description",
               "CVox", "CVox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "", GH_ParamAccess.list);
            pManager.AddNumberParameter("xCell", "x", "", GH_ParamAccess.item, 1000);
            pManager.AddNumberParameter("yCell", "y", "", GH_ParamAccess.item, 1000);
            pManager.AddNumberParameter("zCell", "z", "", GH_ParamAccess.item, 1000);
            pManager.AddNumberParameter("member thickness", "mt", "", GH_ParamAccess.item, 60);
            pManager.AddIntegerParameter("number of bays to display", "nbd", "", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("start bay", "sb", "", GH_ParamAccess.item, 10);
            pManager.AddBooleanParameter("explore mode", "em", "", GH_ParamAccess.item, true);
            pManager.AddPlaneParameter("reference plane", "rp", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("building grid", "bg", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("slabs", "slbs", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("wall boundary", "wb", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("roof", "r", "", GH_ParamAccess.list);
            pManager[8].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.AddGenericParameter("trim cells", "tc", "", GH_ParamAccess.tree);
            pManager.AddGenericParameter("perimeter cells", "pc", "", GH_ParamAccess.tree);
            pManager.AddGenericParameter("vertical support cells", "vsc", "", GH_ParamAccess.tree);
            
            
            pManager.AddMeshParameter("cave slices", "cs", "", GH_ParamAccess.tree);
            pManager.AddMeshParameter("section boxes", "sb", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("grid", "g", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("links", "l", "", GH_ParamAccess.tree);
            pManager.AddGenericParameter("undefined cells", "udc", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> meshes = new List<Mesh>();
            List<Curve> bldGrid = new List<Curve>();
            List<Brep> slabs = new List<Brep>();
            List<Brep> roof = new List<Brep>();
            double xcell = 0;
            double ycell = 0;
            double zcell = 0;
            double memberT = 0;
            int numBays = 0;
            int startBay = 0;
            bool exploreMode=true;
            List<Brep> walls = new List<Brep>();
            Plane refPlane = new Plane();
            
            if (!DA.GetDataList(0, meshes)) return;
            if (!DA.GetData(1, ref xcell)) return;
            if (!DA.GetData(2, ref ycell)) return;
            if (!DA.GetData(3, ref zcell)) return;
            if (!DA.GetData(4, ref memberT)) return;
            if (!DA.GetData(5, ref numBays)) return;
            if (!DA.GetData(6, ref startBay)) return;
            if (!DA.GetData(7, ref exploreMode)) return;
            DA.GetData(8, ref refPlane);
            if (!DA.GetDataList(9, bldGrid)) return;
            if(!DA.GetDataList(10,slabs)) return;
            if (!DA.GetDataList(11, walls)) return;
            if (!DA.GetDataList(12, roof)) return;

            VoxelParameters parameters = new VoxelParameters(xcell, ycell, zcell, memberT, exploreMode, startBay, slabs,walls,roof);
            MeshVoxeliser mvox = new MeshVoxeliser(meshes, startBay,numBays, refPlane,parameters);
            VoxelDocumenter vDoc = new VoxelDocumenter();
            vDoc.writeSection3d(mvox, bldGrid);
            vDoc.moduleSchedule(mvox);
            vDoc.map3dToWorldXY(mvox);
            DataTree<StructuralCell> perimeterCells = new DataTree<StructuralCell>();
            DataTree<StructuralCell> skinCells = new DataTree<StructuralCell>();
            DataTree<StructuralCell> verticalSupportCells = new DataTree<StructuralCell>();
            DataTree<StructuralCell> undefinedCells = new DataTree<StructuralCell>();
            GH_Structure<GH_Mesh> caveSlices = new GH_Structure<GH_Mesh>();
            GH_Structure<GH_Mesh> sectionBoxes = new GH_Structure<GH_Mesh>();
            GH_Structure<GH_Curve> grid = new GH_Structure<GH_Curve>();
            GH_Structure<GH_Curve> links = new GH_Structure<GH_Curve>();

            getSlices(mvox, ref caveSlices,ref sectionBoxes, ref grid,ref links);
            getModules(mvox, ref perimeterCells, ref skinCells, ref verticalSupportCells, ref undefinedCells);
            
            
            DA.SetDataTree(0, skinCells);
            DA.SetDataTree(1, perimeterCells);
            DA.SetDataTree(2, verticalSupportCells);
            DA.SetDataTree(3, caveSlices);
            DA.SetDataTree(4, sectionBoxes);
            DA.SetDataTree(5, grid);
            DA.SetDataTree(6, links);
            DA.SetDataTree(7, undefinedCells);
        }

        private void getModules(MeshVoxeliser mvox, ref DataTree<StructuralCell> perimeterCells, ref DataTree<StructuralCell> trimCells,
            ref DataTree<StructuralCell> verticalCells, ref DataTree<StructuralCell> undefinedCells)
        {
            int bay = 0;
            int side = 0;
            int cell = 0;
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                foreach(StructuralBay sb in sp.structuralBays)
                {
                    side = 0;
                    foreach (List<StructuralCell> sc in sb.voxels)
                    {
                        cell = 0;
                        foreach (StructuralCell c in sc)
                        {
                            if (c.cellType != StructuralCell.CellType.InsideCell)
                            {
                                GH_Path path = new GH_Path(new int[] { bay, side, cell });
                                switch (c.cellType)
                                {
                                    case StructuralCell.CellType.SkinCell:
                                        trimCells.Add(c, path);
                                        break;
                                    case StructuralCell.CellType.PerimeterCell:
                                        perimeterCells.Add(c, path);
                                        break;
                                    case StructuralCell.CellType.VerticalFillCell:
                                        verticalCells.Add(c, path);
                                        break;
                                    case StructuralCell.CellType.Undefined:
                                        undefinedCells.Add(c, path);
                                        break;

                                }

                                cell++;
                            }

                        }
                        side++;
                    }
                    bay++;
                }
                
            }
        }
        private void getSlices(MeshVoxeliser mvox, ref GH_Structure<GH_Mesh> caveslices, ref GH_Structure<GH_Mesh> sectionboxes,ref GH_Structure<GH_Curve> grid,ref GH_Structure<GH_Curve> links)
        {
            foreach (Mesh b in mvox.spanBoxes) sectionboxes.Append(new GH_Mesh(b));
            foreach (StructuralSpan sp in mvox.structuralSpans)
            {
                caveslices.Append(new GH_Mesh(sp.slice));
                foreach (Line l in sp.xGrid) grid.Append(new GH_Curve(l.ToNurbsCurve()));
                foreach (Line l in sp.yGrid) grid.Append(new GH_Curve(l.ToNurbsCurve()));
                foreach (Line l in sp.linkElements) links.Append(new GH_Curve(l.ToNurbsCurve()));
            }
        }
        

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("520f4576-28d6-42e1-a3b9-bcc5dad0e98e"); }
        }
    }
}
