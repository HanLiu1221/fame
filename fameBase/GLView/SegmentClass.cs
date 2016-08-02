using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Geometry;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using System.Drawing;


namespace Component
{
    public unsafe class SegmentClass
    {
        public List<GuideLine> guideLines;
        public List<Segment> segments;
        public enum GuideLineType
        {
            Random, SimpleCross
        }
        public enum StrokeStyle
        {
            Pencil, Pen1, Pen2, Crayon, Ink1, Ink2
        }
        // apple green: 49,163,84
        // orange red: 255, 153, 102

        public static StrokeStyle strokeStyle = StrokeStyle.Pencil;
        public static GuideLineType GuideLineStyle = GuideLineType.Random;
        public static double StrokeSize = 2;
        public static double GuideLineSize = 2;
        public static Color StrokeColor = Color.FromArgb(110, 110, 110);//(54, 69, 79);
        public static Color sideStrokeColor = Color.FromArgb(120, 120, 120);//(54, 69, 79);
        public static Color VanLineColor = Color.FromArgb(210, 210, 210);
        public static Color HiddenColor = Color.FromArgb(170, 170, 170);
        public static Color HighlightColor = Color.FromArgb(222, 45, 38);
        public static Color FaceColor = Color.FromArgb(253, 205, 172);
        public static Color AnimColor = Color.FromArgb(251, 128, 114);

        public static Color GuideLineWithTypeColor = Color.FromArgb(252, 141, 89);
        public static Color ArrowColor = Color.FromArgb(116, 196, 118);
        public static Color MeshColor = Color.FromArgb(173, 210, 222);//254, 153, 41);

        // for sketch
        public static double PenSize = 2.0;
        public static Color PenColor = Color.FromArgb(60, 60, 60);
        

        public SegmentClass()
        { }

        public bool ReadSegments(string segFolder, string bboxfolder)
        {
            if (!Directory.Exists(segFolder) || !Directory.Exists(bboxfolder))
                return false;
            this.segments = new List<Segment>();
            string[] meshfiles = Directory.GetFiles(segFolder, "*.ply");
            string[] bboxfiles = Directory.GetFiles(bboxfolder, "*.ply");
            if (meshfiles.Length != bboxfiles.Length)
            {
                Console.WriteLine("segments and bounding boxes are not matching.");
                return false;
            }

            for (int i = 0; i < meshfiles.Length; ++i)
            {
                Mesh mesh = new Mesh(meshfiles[i], false);
                Vector3d[] bbox = this.loadPrimitiveBoudingbox(bboxfiles[i]);
                Primitive c = new Primitive(bbox, 0);
                Segment seg = new Segment(mesh, c);
                seg.idx = i;
                this.segments.Add(seg);
            }
            return true;
        }//ReadSegments

        public Vector3d[] loadPrimitiveBoudingbox(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            string line = "";
            char[] separator = new char[] { ' ', '\t' };
            int n = 0;
            while (sr.Peek() > -1)
            {
                line = sr.ReadLine();
                string[] array = line.Split(separator);
                if (array.Length > 0 && array[0].Equals("end_header"))
                {
                    break;
                }
                if (array.Length > 1 && array[0].Equals("element") && array[1].Equals("vertex"))
                {
                    n = Int32.Parse(array[2]);
                }
            }
            Vector3d[] points = new Vector3d[n];
            int[] ids = { 0, 1, 3, 2, 7, 6, 4, 5 };
            for (int i = 0; i < n; ++i)
            {
                line = sr.ReadLine();
                string[] array = line.Split(separator);
                if (array.Length < 3) break;
                points[ids[i]] = new Vector3d(double.Parse(array[0]),
                    double.Parse(array[1]),
                    double.Parse(array[2]));
            }
            return points;
        }//loadPrimitiveBoudingbox

        public bool shadedOrTexture()
        {
            if ((int)SegmentClass.strokeStyle == 3)// || (int)this.strokeStyle == 4)
            {
                return false;
            }
            else
            {
                return true;
            }
        }//shadedOrTexture

        public Vector3d NormalizeSegmentsToBox()
        {
            Vector3d maxCoord = Vector3d.MinCoord();
            Vector3d minCoord = Vector3d.MaxCoord();
            Vector3d m_maxCoord = Vector3d.MinCoord();
            Vector3d m_minCoord = Vector3d.MaxCoord();
            foreach (Segment seg in this.segments)
            {
                if (seg.mesh == null) continue;
                m_maxCoord = Vector3d.Max(m_maxCoord, seg.mesh.MaxCoord);
                m_minCoord = Vector3d.Min(m_minCoord, seg.mesh.MinCoord);
                maxCoord = Vector3d.Max(maxCoord, seg.boundingbox.points[6]);
                minCoord = Vector3d.Min(minCoord, seg.boundingbox.points[0]);
            }
            Vector3d center = (maxCoord + minCoord) / 2;
            Vector3d m_d = m_maxCoord - m_minCoord;
            Vector3d b_d = maxCoord - minCoord;
            double scale = m_d.x > m_d.y ? m_d.x : m_d.y;
            scale = m_d.z > scale ? m_d.z : scale;
            //scale /= 2; // [-1, 1]
            double b_scale = b_d.x > b_d.y ? b_d.x : b_d.y;
            b_scale = b_d.z > b_scale ? b_d.z : b_scale;

            scale = scale / b_scale;
            foreach (Segment seg in this.segments)
            {
                if (seg.mesh == null) continue;
                seg.mesh.normalize(center, scale);
                //seg.boundingbox.normalize(center, scale);
            }
            return center;
        }// NormalizeSegmentsToBox

        public List<Mesh> currMeshes;
        public List<Vector3d> contourPoints;
    }
}
