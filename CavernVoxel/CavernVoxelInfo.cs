using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace CavernVoxel
{
    public class CavernVoxelInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "CavernVoxel";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("f9bc52f4-a469-4d2b-a466-662ac109dfcd");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
