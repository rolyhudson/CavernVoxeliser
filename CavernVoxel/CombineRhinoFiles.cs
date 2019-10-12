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
        public void CombineRhinoFilesAsInstance(string folder,string outfilepath,string search,List<Brep> refgeo)
        {
            var files = Directory.GetFiles(folder, "*.3dm", SearchOption.TopDirectoryOnly);
            
            File3dm outfile = new File3dm();
            
            Layer layer = new Layer();
            layer.Name = "envelope";
            outfile.AllLayers.Add(layer);
            var transform = Transform.Identity;
            foreach (string s in files)
            {
                
                if (s.Contains(search))
                {

                    string reffile = Path.GetFileNameWithoutExtension(s);
                    int index = outfile.AllInstanceDefinitions.AddLinked(s, reffile, reffile);
                    InstanceDefinitionGeometry instance = outfile.AllInstanceDefinitions.FindName(s);
                    outfile.Objects.AddInstanceObject(index, transform);
                }
            }
            foreach(Brep b in refgeo)
            {
                ObjectAttributes att = new ObjectAttributes();
                att.LayerIndex = 0;
                outfile.Objects.AddBrep(b,att);
            }
            outfile.Write(folder+"\\"+ outfilepath, 5);


        }
        public void CombineRhinoFilesAsGeo(string folder, string outfilepath, string search, List<Brep> refgeo)
        {
            var files = Directory.GetFiles(folder, "*.3dm", SearchOption.TopDirectoryOnly);

            File3dm outfile = new File3dm();

            Layer layer = new Layer();
            layer.Name = "envelope";
            outfile.AllLayers.Add(layer);
            var transform = Transform.Identity;
            foreach (string s in files)
            {
                
                if (s.Contains(search))
                {
                    File3dm read = File3dm.Read(s);
                    
                    foreach(File3dmObject o in read.Objects)
                    {
                        outfile.Objects.Add(o);
                    }
                }
            }
            foreach (Brep b in refgeo)
            {
                ObjectAttributes att = new ObjectAttributes();
                att.LayerIndex = 0;
                outfile.Objects.AddBrep(b, att);
            }
            outfile.Write(folder + "\\" + outfilepath, 5);


        }
    }
}
