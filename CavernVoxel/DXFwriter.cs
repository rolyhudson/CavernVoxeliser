using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CavernVoxel
{
    class DXFwriter
    {
        StringBuilder outputSB = new StringBuilder();
        public DXFwriter()
        {
            dxfSetUp();
        }
        public void finishAndWrite(string filepath)
        {
            dxfFinishOff();
            StreamWriter sw = new StreamWriter(filepath);
            sw.Write(outputSB.ToString());
            sw.Close();
        }
        private void dxfSetUp()
        {
            outputSB.Append("0\n");
            outputSB.Append("SECTION\n");
            outputSB.Append("2\n");
            outputSB.Append("ENTITIES\n");
            
        }
        public void DXFPolyfaceMesh(Mesh m,string layer,int colour)
        {
            outputSB.Append("0\n");
            outputSB.Append("POLYLINE\n");
            outputSB.Append("8\n");
            outputSB.Append(layer + "\n");//layer name
            outputSB.Append("62\n");
            outputSB.Append(colour + "\n");//color
            outputSB.Append("10\n"); outputSB.Append("0.0\n");

            outputSB.Append("20\n"); outputSB.Append("0.0\n");

            outputSB.Append("30\n"); outputSB.Append("0.0\n");
            
            outputSB.Append("70\n");
            outputSB.Append("64\n");//this is a polyface mesh
            outputSB.Append("71\n");//optional num vertices
            outputSB.Append(m.Vertices.Count+"\n");
            outputSB.Append("72\n");//optional num faces
            outputSB.Append(m.Faces.Count + "\n");
            foreach(Point3d p in m.Vertices)
            {
                DXFvertex(p, layer);
            }
            foreach(MeshFace f in m.Faces)
            {
                DXFmeshface(f.A+1, f.B+1, f.C+1, f.D+1, layer);
            }
            DXFseqend(layer);
        }
        private void DXFvertex(Point3d p1,string layer)
        {
            outputSB.Append("0\n");
            outputSB.Append("VERTEX\n");
            outputSB.Append("8\n");
            outputSB.Append(layer + "\n");//layer name
            outputSB.Append("10\n"); outputSB.Append(p1.X + "\n");
            outputSB.Append("20\n"); outputSB.Append(p1.Y + "\n");
            outputSB.Append("30\n"); outputSB.Append(p1.Z + "\n");
            outputSB.Append("70\n");
            outputSB.Append("192\n");//this is a polyface mesh vertex
        }
        private void DXFmeshface(int a,int b,int c,int d,string layer)
        {//this is a polyface mesh face
            outputSB.Append("0\n");
            outputSB.Append("VERTEX\n");
            outputSB.Append("8\n");
            outputSB.Append(layer + "\n");//layer name
            outputSB.Append("10\n"); outputSB.Append("0\n");
            outputSB.Append("20\n"); outputSB.Append("0\n");
            outputSB.Append("30\n"); outputSB.Append("0\n");
            outputSB.Append("70\n");
            outputSB.Append("128\n");//this is a polyface mesh face
            //vertex refs
            outputSB.Append("71\n"); outputSB.Append(a + "\n");
            outputSB.Append("72\n"); outputSB.Append(b + "\n");
            outputSB.Append("73\n"); outputSB.Append(c + "\n");
            outputSB.Append("74\n"); outputSB.Append(d + "\n");
        }
        private void DXFseqend(string layer)
        {
            outputSB.Append("0\n");
            outputSB.Append("SEQEND\n");
            outputSB.Append("8\n");
            outputSB.Append(layer+"\n");
        }
        public void DXFquadPanel(Point3d p1, Point3d p2, Point3d p3, Point3d p4,string layer,int colour)
        {
            outputSB.Append("0\n");
            outputSB.Append("3DFACE\n");
            outputSB.Append("8\n");
            outputSB.Append(layer + "\n");//layer name
            outputSB.Append("62\n");
            outputSB.Append(colour + "\n");//color
            outputSB.Append("10\n"); outputSB.Append(p1.X + "\n");
            outputSB.Append("20\n"); outputSB.Append(p1.Y + "\n");
            outputSB.Append("30\n"); outputSB.Append(p1.Z + "\n");
            outputSB.Append("11\n"); outputSB.Append(p2.X + "\n");
            outputSB.Append("21\n"); outputSB.Append(p2.Y + "\n");
            outputSB.Append("31\n"); outputSB.Append(p2.Z + "\n");
            outputSB.Append("12\n"); outputSB.Append(p3.X + "\n");
            outputSB.Append("22\n"); outputSB.Append(p3.Y + "\n");
            outputSB.Append("32\n"); outputSB.Append(p3.Z + "\n");
            outputSB.Append("13\n"); outputSB.Append(p4.X + "\n");
            outputSB.Append("23\n"); outputSB.Append(p4.Y + "\n");
            outputSB.Append("33\n"); outputSB.Append(p4.Z + "\n");
            
        }
        public void DXFtriPanel(Point3d p1, Point3d p2, Point3d p3,string layer,int colour)
        {
            outputSB.Append("0\n");
            outputSB.Append("3DFACE\n");
            outputSB.Append("8\n");
            outputSB.Append(layer + "\n");//layer name
            outputSB.Append("62\n");
            outputSB.Append(colour + "\n");//color
            outputSB.Append("10\n"); outputSB.Append(p1.X + "\n");
            outputSB.Append("20\n"); outputSB.Append(p1.Y + "\n");
            outputSB.Append("30\n"); outputSB.Append(p1.Z + "\n");
            outputSB.Append("11\n"); outputSB.Append(p2.X + "\n");
            outputSB.Append("21\n"); outputSB.Append(p2.Y + "\n");
            outputSB.Append("31\n"); outputSB.Append(p2.Z + "\n");
            outputSB.Append("12\n"); outputSB.Append(p3.X + "\n");
            outputSB.Append("22\n"); outputSB.Append(p3.Y + "\n");
            outputSB.Append("32\n"); outputSB.Append(p3.Z + "\n");
            
        }
        private void dxfFinishOff()
        {
            outputSB.Append("0\n");
            outputSB.Append("ENDSEC\n");
            outputSB.Append("0\n");
            outputSB.Append("EOF");
            
        }
        public void DXFLines(Point3d p1, Point3d p2,string layer,int colour)
        {
            outputSB.Append("0\n");
            outputSB.Append("LINE\n");
            outputSB.Append("8\n");
            outputSB.Append(layer+"\n");//layer name
            outputSB.Append("62\n");
            outputSB.Append(colour+"\n");//color
            outputSB.Append("10\n"); outputSB.Append(p1.X + "\n");
            outputSB.Append("20\n"); outputSB.Append(p1.Y + "\n");
            outputSB.Append("30\n"); outputSB.Append(p1.Z + "\n");
            outputSB.Append("11\n"); outputSB.Append(p2.X + "\n");
            outputSB.Append("21\n"); outputSB.Append(p2.Y + "\n");
            outputSB.Append("31\n"); outputSB.Append(p2.Z + "\n");
            
           
        }
        public void DXFText(string name,double x, double y, double z,string layer,int height,int colour,double rotation)
        {
            outputSB.Append("0\n");
            outputSB.Append("TEXT\n");
            outputSB.Append("8\n");
            outputSB.Append(layer + "\n");//layer name
            outputSB.Append("62\n");
            outputSB.Append(colour+"\n");//color
            outputSB.Append("10\n");
            outputSB.Append(x + "\n");//text origin
            outputSB.Append("20\n");
            outputSB.Append(y + "\n"); //text origin
            outputSB.Append("30\n");
            outputSB.Append(z + "\n");//text origin
            outputSB.Append("40\n");
            outputSB.Append(height+"\n");//text height
            outputSB.Append("1\n");
            outputSB.Append(name + "\n");//text height
            outputSB.Append("50\n");
            outputSB.Append(rotation + "\n");//text rotation


        }
    }
}
