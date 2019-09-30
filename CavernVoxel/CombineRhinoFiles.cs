using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace CavernVoxel
{
    class CombineRhinoFiles
    {
        public CombineRhinoFiles(string folder,string outfile,string search,List<Brep> refgeo)
        {
            var files = Directory.GetFiles(folder, "*.3dm", SearchOption.TopDirectoryOnly);
            
            File3dm file = new File3dm();
            
            Layer layer = new Layer();
            layer.Name = "envelope";
            file.AllLayers.Add(layer);
            var transform = Transform.Identity;
            foreach (string s in files)
            {
                
                if (s.Contains(search))
                {

                    string reffile = Path.GetFileNameWithoutExtension(s);
                    int index = file.AllInstanceDefinitions.AddLinked(s, reffile, reffile);
                    InstanceDefinitionGeometry instance = file.AllInstanceDefinitions.FindName(s);
                    file.Objects.AddInstanceObject(index, transform);
                }
            }
            foreach(Brep b in refgeo)
            {
                ObjectAttributes att = new ObjectAttributes();
                att.LayerIndex = 0;
                file.Objects.AddBrep(b,att);
            }
            file.Write(folder+"\\"+outfile, 5);


        }
    }
}
