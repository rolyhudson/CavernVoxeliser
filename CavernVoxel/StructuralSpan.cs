using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CavernVoxel
{
    class StructuralSpan
    {
        public List<StructuralBay> structuralBays = new List<StructuralBay>();
        public Mesh slice = new Mesh();
        public Mesh container = new Mesh();
        public StructuralSpan()
        {

        }
    }
}
