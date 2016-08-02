using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Tao.OpenGl;
using Tao.Platform.Windows;

using Geometry;

namespace Component
{
    public class Stroke
    {
        public static Vector3d yNormal = new Vector3d(0, 1, 0);
        //endpoints
        public Vector3d u3;
        public Vector3d v3;
        public Vector2d u2;
        public Vector2d v2;
        private int npoints;
        //stroke points
        public List<StrokePoint> strokePoints;
        public List<Vector3d> meshVertices3d;
        public List<Vector2d> meshVertices2d;
        public List<int> faceIndex;
        private int facecount = 0;
        private double size2 = SegmentClass.StrokeSize;
        private double size3 = (double)SegmentClass.StrokeSize / 500;
        public Color strokeColor = SegmentClass.StrokeColor;
        public int opacity = 0;
        public Plane hostPlane;
        public double depth = 1.0;
        public int ncapoints = 5;
        private static readonly Random rand = new Random();
        private bool isBoxEdge = true;
        public double weight = SegmentClass.StrokeSize; // for line drawing

        public Stroke() { }
        public Stroke(Vector3d v1, Vector3d v2, bool isBoxEdge)
        {
            this.u3 = v1;
            this.v3 = v2;
            this.npoints = (int)((v1 - v2).Length() / 0.01);
            this.npoints = this.npoints > 0 ? this.npoints : 1;
            this.isBoxEdge = isBoxEdge;
            this.sampleStrokePoints();
        }

        public Stroke(Vector2d v1, Vector2d v2, bool isBoxEdge, int n)
        {
            this.size2 = SegmentClass.StrokeSize / 2;
            this.u2 = v1;
            this.v2 = v2;
            this.npoints = n;
            this.isBoxEdge = isBoxEdge;
            this.sampleStrokePoints2d();
        }

        public int FaceCount
        {
            get
            {
                return this.facecount;
            }
        }

        public void setStrokeSize(double s)
        {
            this.size2 = s;
            this.size3 = s / 500;
            this.weight = s;
        }


        private void sampleStrokePoints()
        {
            Vector3d dir = (this.v3 - this.u3).normalize();
            this.strokePoints = new List<StrokePoint>();
            double step = (this.v3 - this.u3).Length() / this.npoints;
            for (int i = 0; i < this.npoints; ++i)
            {
                Vector3d p = new Vector3d(this.u3 + step * i * dir);
                this.strokePoints.Add(new StrokePoint(p));
            }
        }//sampleStrokePoints

        private void sampleStrokePoints2d()
        {
            Vector2d dir = (this.v2 - this.u2).normalize();
            this.strokePoints = new List<StrokePoint>();
            double step = (this.v2 - this.u2).Length() / this.npoints;
            for (int i = 0; i < this.npoints; ++i)
            {
                Vector2d p = new Vector2d(this.u2 + step * i * dir);
                this.strokePoints.Add(new StrokePoint(p));
            }
            this.changeStyle2d((int)SegmentClass.strokeStyle);
        }//sampleStrokePoints

        public void setStrokeMeshPoints(List<Vector2d> points, List<Vector2d> normals)
        {
            this.meshVertices2d = new List<Vector2d>();
            double radius = this.size2;
            this.npoints = points.Count;
            for (int i = 0; i < this.npoints; ++i)
            {
                Vector2d p = points[i];
                Vector2d nor = normals[i].normalize();
                Vector2d v1 = p + nor * radius;
                Vector2d v2 = p - nor * radius;
                this.meshVertices2d.Add(v1);
                this.meshVertices2d.Add(v2);
            }
            this.buildStrokeMeshFace();
        }// setStrokeMeshPoints

        public Stroke(List<Vector2d> points, double size2)
        {
            this.meshVertices2d = new List<Vector2d>();
            this.size2 = size2;
            double radius = this.size2 / 2;
            this.npoints = points.Count;
            this.strokePoints = new List<StrokePoint>();
            this.size3 = size2 / 500;
            for (int i = 0; i < this.npoints; ++i)
            {
                Vector2d p = points[i];
                this.strokePoints.Add(new StrokePoint(p));
                Vector2d nor = new Vector2d();
                if (i + 1 < this.npoints)
                {
                    Vector2d d = points[i + 1] - points[i];
                    nor = new Vector2d(-d.y, d.x);
                }
                else
                {
                    Vector2d d = points[i] - points[i - 1];
                    nor = new Vector2d(-d.y, d.x);
                }
                nor.normalize();
                Vector2d v1 = p + nor * radius;
                Vector2d v2 = p - nor * radius;
                this.meshVertices2d.Add(v1);
                this.meshVertices2d.Add(v2);
            }
            
            this.changeStyle2d((int)SegmentClass.strokeStyle);
        }// setStrokeMeshPoints

        public double get2DLength()
        {
            double len = 0;
            for(int i = 0; i < this.strokePoints.Count - 1; ++i)
            {
                len += (this.strokePoints[i].pos2 - this.strokePoints[i + 1].pos2).Length();
            }
            return len;
        }

        public void setStrokeMeshPoints2D(Vector2d normal)
        {
            this.meshVertices2d = new List<Vector2d>();
            double radius = this.size2 / 2;
            int n = this.strokePoints.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2d p = this.strokePoints[i].pos2;
                Vector2d v1 = p + normal * radius;
                Vector2d v2 = p - normal * radius;
                this.meshVertices2d.Add(v1);
                this.meshVertices2d.Add(v2);
            }
            this.buildStrokeMeshFace();
        }// setStrokeMeshPoints

        private void buildStrokeMeshFace()
        {
            // face
            this.faceIndex = new List<int>();
            for (int i = 0, j = 0; i < this.npoints - 1; ++i, j += 2)
            {
                this.faceIndex.Add(j);
                this.faceIndex.Add(j + 1);
                this.faceIndex.Add(j + 3);
                this.faceIndex.Add(j);
                this.faceIndex.Add(j + 3);
                this.faceIndex.Add(j + 2);
            }
            this.facecount = this.faceIndex.Count / 3;
        }

        public void addCapfrom2D(int tag)
        {
            // tag = 1: add one head and one tail
            // tag = 2: round cap
            if (tag == 1)
            {
                this.addHeadTail2D();
            }
            else
            {
                this.addRoundCap2D();
            }
            this.addHeadTailFaceIndex(tag);
        }

        public void addCap(int tag)
        {
            // tag = 1: add one head and one tail
            // tag = 2: round cap
            if (tag == 1)
            {
                this.addHeadTail2D();
                this.addHeadTail3D();
            }
            else
            {
                this.addRoundCap2D();
                this.addRoundCap3D();
            }
            this.addHeadTailFaceIndex(tag);
        }

        public void addHeadTail2D()
        {            
            Vector2d u = this.meshVertices2d[1];
            Vector2d v = this.meshVertices2d[0];
            Vector2d d = (u - v).normalize();
            double len = (u - v).Length();
            Vector2d n = new Vector2d(-d.y, d.x).normalize();
            n = -1.0 * n;
            Vector2d c = (u + v) / 2;
            Vector2d p = c + n * len / 2;
            //head
            this.meshVertices2d.Insert(0, p);            
            //tail
            int tailIdx = this.meshVertices2d.Count;
            u = this.meshVertices2d[tailIdx - 1];
            v = this.meshVertices2d[tailIdx - 2];
            d = (u - v).normalize();
            len = (u - v).Length();
            n = new Vector2d(-d.y, d.x).normalize();
            c = (u + v) / 2;
            p = c + n * len;
            this.meshVertices2d.Add(p);
        }// addHeadTail2D

        public void addHeadTail3D()
        {
            if (this.hostPlane == null)
            {
                return;
            }
            Vector3d u = this.meshVertices3d[1];
            Vector3d v = this.meshVertices3d[0];
            Vector3d d = (u - v).normalize();
            double len = (u - v).Length();
            Vector3d n = d.Cross(this.hostPlane.normal).normalize();
            Vector3d c = (u + v) / 2;
            Vector3d p = c + n * len / 2;
            //head
            this.meshVertices3d.Insert(0, p);
            //tail
            int tailIdx = this.meshVertices3d.Count;
            u = this.meshVertices3d[tailIdx - 1];
            v = this.meshVertices3d[tailIdx - 2];
            d = (u - v).normalize();
            len = (u - v).Length();
            n = this.hostPlane.normal.Cross(d).normalize();
            c = (u + v) / 2;
            p = c + n * len;
            this.meshVertices3d.Add(p);
        }// addHeadTail3D

        private void addRoundCap2D()
        {
            //if (this.meshVertices2d == null || this.meshVertices2d.Count == 0) return;
            //head
            int N = this.ncapoints; // odd number
            Vector2d u = this.meshVertices2d[1];
            Vector2d v = this.meshVertices2d[0];
            Vector2d d = (u - v).normalize();
            double len = (u - v).Length() / 2;
            Vector2d n = new Vector2d(-d.y, d.x).normalize();
            n = -1.0 * n;
            Vector2d c = (u + v) / 2;
            Vector2d p = c + n * len;
            int N2 = N / 2;
            Vector2d cv = (v - c).normalize();
            Vector2d vp = (p - v).normalize();
            double vp_len = (p - v).Length() / (N2 + 1);
            int loc = 0;
            for (int i = 0; i < N2; ++i)
            {
                Vector2d m = v + vp * (i + 1) * vp_len;
                Vector2d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices2d.Insert(loc++, p2);
            }
            this.meshVertices2d.Insert(loc++, p);
            Vector2d up = (p - u).normalize();
            double up_len = (p - u).Length() / (N2 + 1);
            for (int i = N2 - 1; i >= 0; --i)
            {
                Vector2d m = u + up * (i + 1) * up_len;
                Vector2d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices2d.Insert(loc++, p2);
            }
            this.meshVertices2d.Insert(loc++, c);
            // tail
            int tailIdx = this.meshVertices2d.Count;
            u = this.meshVertices2d[tailIdx - 1];
            v = this.meshVertices2d[tailIdx - 2];
            d = (u - v).normalize();
            len = (u - v).Length() / 2;
            n = new Vector2d(-d.y, d.x).normalize();
            c = (u + v) / 2;
            p = c + n * len;
            cv = (v - c).normalize();
            vp = (p - v).normalize();
            vp_len = (p - v).Length() / (N2 + 1);
            for (int i = 0; i < N2; ++i)
            {
                Vector2d m = v + vp * (i + 1) * vp_len;
                Vector2d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices2d.Add(p2);
            }
            this.meshVertices2d.Add(p);
            up = (p - u).normalize();
            up_len = (p - u).Length() / (N2 + 1);
            for (int i = N2 - 1; i >= 0; --i)
            {
                Vector2d m = u + up * (i + 1) * up_len;
                Vector2d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices2d.Add(p2);
            }
            this.meshVertices2d.Add(c);
        }// addRoundCap2D

        private void addRoundCap3D()
        {
            //head
            int N = this.ncapoints; // odd number
            Vector3d u = this.meshVertices3d[1];
            Vector3d v = this.meshVertices3d[0];
            Vector3d d = (u - v).normalize();
            double len = (u - v).Length() / 2;
            Vector3d normal = Stroke.yNormal;
            if (this.hostPlane != null)
            {
                normal = this.hostPlane.normal;
            }
            Vector3d n = d.Cross(normal).normalize();
            Vector3d c = (u + v) / 2;
            Vector3d p = c + n * len;
            int N2 = N / 2;
            Vector3d cv = (v - c).normalize();
            Vector3d vp = (p - v).normalize();
            double vp_len = (p - v).Length() / (N2 + 1);
            int loc = 0;
            for (int i = 0; i < N2; ++i)
            {
                Vector3d m = v + vp * (i + 1) * vp_len;
                Vector3d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices3d.Insert(loc++, p2);
            }
            this.meshVertices3d.Insert(loc++, p);
            Vector3d up = (p - u).normalize();
            double up_len = (p - u).Length() / (N2 + 1);
            for (int i = N2 - 1; i >= 0; --i)
            {
                Vector3d m = u + up * (i + 1) * up_len;
                Vector3d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices3d.Insert(loc++, p2);
            }
            this.meshVertices3d.Insert(loc++, c);
            // tail
            int tailIdx = this.meshVertices3d.Count;
            u = this.meshVertices3d[tailIdx - 1];
            v = this.meshVertices3d[tailIdx - 2];
            d = (u - v).normalize();
            len = (u - v).Length() / 2;
            n = normal.Cross(d).normalize();
            c = (u + v) / 2;
            p = c + n * len;
            cv = (v - c).normalize();
            vp = (p - v).normalize();
            vp_len = (p - v).Length() / (N2 + 1);
            for (int i = 0; i < N2; ++i)
            {
                Vector3d m = v + vp * (i + 1) * vp_len;
                Vector3d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices3d.Add(p2);
            }
            this.meshVertices3d.Add(p);
            up = (p - u).normalize();
            up_len = (p - u).Length() / (N2 + 1);
            for (int i = N2 - 1; i >= 0; --i)
            {
                Vector3d m = u + up * (i + 1) * up_len;
                Vector3d p2 = (m - c).normalize();
                p2 = c + p2 * len;
                this.meshVertices3d.Add(p2);
            }
            this.meshVertices3d.Add(c);
        }// addRoundCap3D

        private void addHeadTailFaceIndex(int tag)
        {
            if (tag == 1)
            {
                // head
                this.InsertTriFace(1, 2, 0);
                // add one more mesh vertex in front
                for (int i = 3; i < this.faceIndex.Count; ++i)
                {
                    this.faceIndex[i]++;
                }
                // tail
                int tailIdx = this.meshVertices2d.Count - 1;
                this.AddTriFace(tailIdx - 2, tailIdx - 1, tailIdx);
            }
            else
            {
                //head
                int N = this.ncapoints; // odd number 
                for (int i = 0; i < this.faceIndex.Count; ++i)
                {
                    this.faceIndex[i] += N + 1;
                }                               
                this.InsertTriFace(N, N + 2, N - 1);
                for (int i = N - 2; i >= 0; --i)
                {
                    this.InsertTriFace(N, i + 1, i);
                }
                this.InsertTriFace(N, 0, N + 1);                
                //tail
                int tailIdx = this.meshVertices2d.Count - 1;
                this.AddTriFace(tailIdx - N - 1, tailIdx, tailIdx - 1);
                for (int i = 1; i < N; ++i)
                {
                    this.AddTriFace(tailIdx - i, tailIdx, tailIdx - i - 1);
                }
                this.AddTriFace(tailIdx - N, tailIdx, tailIdx - N - 2);
            }
            this.facecount = this.faceIndex.Count / 3;
        }// addHeadTailFaceIndex

        private void InsertTriFace(int i, int j, int k)
        {
            this.faceIndex.Insert(0, i);
            this.faceIndex.Insert(0, j);
            this.faceIndex.Insert(0, k);
        }
        private void AddTriFace(int i, int j, int k)
        {
            this.faceIndex.Add(i);
            this.faceIndex.Add(j);
            this.faceIndex.Add(k);
        }

        public void changeStyle(int type)
        {
            this.meshVertices2d = new List<Vector2d>();
            this.meshVertices3d = new List<Vector3d>();
            int N = this.npoints;
            float start = (float)N;
            
            double r_size2 = Polygon.getRandomDoubleInRange(rand, this.size2 / 4, this.size2);
            double r_size3 = Polygon.getRandomDoubleInRange(rand, this.size3 / 4, this.size3);
            if (this.isBoxEdge)
            {
                r_size2 = this.size2;
                r_size3 = this.size3;
            }
            double isize2 = r_size2 / 2;
            double isize3 = r_size3 / 2;
            for (int i = 0; i < N; ++i)
            {
                int I = i - 1 >= 0 ? i - 1 : i;
                int J = i + 1 < N ? i + 1 : i;
                Vector2d u2 = this.strokePoints[I].pos2;
                Vector2d v2 = this.strokePoints[J].pos2;
                Vector2d o2 = this.strokePoints[i].pos2;
                Vector2d d2 = (v2 - u2).normalize();	// dir
                Vector2d n2 = new Vector2d(-d2.y, d2.x).normalize();

                Vector3d u3 = this.strokePoints[I].pos3;
                Vector3d v3 = this.strokePoints[J].pos3;
                Vector3d o3 = this.strokePoints[i].pos3;
                Vector3d d3 = (v3 - u3).normalize();	// dir
                Vector3d n3 = Stroke.yNormal;
                if (this.hostPlane != null)
                {
                    n3 = this.hostPlane.normal.Cross(d3).normalize();
                }

                int op = 255;
                switch (type)
                {
                    case 0: // pencil
                        {
                            op = 255;
                            break;
                        }
                    case 1: // pen
                        {
                            isize2 = (start - i + 1) / start * r_size2;
                            isize3 = (start - i + 1) / start * r_size3;
                            op = 255; //(int)((start - i) / start * 255.0);
                            break;
                        }
                    case 2:// pen-2
                        {
                            isize2 = (i + 1) / start * r_size2;
                            isize3 = (i + 1) / start * r_size3;
                            op = 255; // (int)(i / start * 255.0);
                            break;
                        }
                    case 3: // crayon
                        {
                            op = 155;
                            break;
                        }                 
                    case 4://ink - 1
                        {
                            double diff = (i - start / 2) / start * 8;
                            diff = Math.Pow(Math.E, -(diff * diff) / 10);
                            isize2 = diff * r_size2;
                            isize3 = diff * r_size3;
                            op = (int)((diff + 0.1) * 255.0);
                            if (op > 255)
                                op = 255;
                            break;
                        }
                    case 5: //ink-2 watercolor
                        {
                            double diff = (i - start / 2) / start * 8;
                            diff = Math.Pow(Math.E, -(diff * diff) / 10);
                            if (diff < 1)
                                diff = 1 - diff;
                            else
                                diff = diff - 1;
                            diff += 0.2;
                            if (diff > 1) diff = 1.0;
                            isize2 = diff * r_size2;
                            isize3 = diff * r_size3;
                            op = (int)(diff * 255.0);
                            break;
                        }
                }
                this.strokePoints[i].opacity = (byte)(op);
                this.meshVertices2d.Add(o2 + isize2 * n2);
                this.meshVertices2d.Add(o2 - isize2 * n2);
                this.meshVertices3d.Add(o3 + isize3 * n3);
                this.meshVertices3d.Add(o3 - isize3 * n3);
            }
            this.buildStrokeMeshFace();
            this.addCap(2);
            //this.addCapfrom2D(1);
            //this.addCapfrom2D(2);
            //this.meshVertices3d = new List<Vector3d>();
            //Matrix4d invMat =  T.Inverse();
            //Plane plane = this.hostPlane.clone() as Plane;
            //plane.Transform(T);
            //foreach (Vector2d v2 in this.meshVertices2d)
            //{
            //    Vector3d v3 = camera.ProjectPointToPlane(v2, plane.center, plane.normal);
            //    Vector4d v4 = (invMat * new Vector4d(v3, 1));
            //    v3 = v4.ToVector3D();
            //    this.meshVertices3d.Add(v3);
            //}
        }// changeStyle

        public void changeStyle2d(int type)
        {
            this.meshVertices2d = new List<Vector2d>();
            int N = this.strokePoints.Count;
            float start = (float)N;

            double r_size2 = Polygon.getRandomDoubleInRange(rand, this.size2, this.size2 * 2);
            if (this.isBoxEdge)
            {
                r_size2 = this.size2 * 2;
            }
            double isize2 = r_size2 / 2;
            for (int i = 0; i < N; ++i)
            {
                int I = i - 1 >= 0 ? i - 1 : i;
                int J = i + 1 < N ? i + 1 : i;
                Vector2d u2 = this.strokePoints[I].pos2;
                Vector2d v2 = this.strokePoints[J].pos2;
                Vector2d o2 = this.strokePoints[i].pos2;
                Vector2d d2 = (v2 - u2).normalize();	// dir
                Vector2d n2 = new Vector2d(-d2.y, d2.x).normalize();


                int op = 255;
                switch (type)
                {
                    case 0: // pencil
                        {
                            op = 255;
                            break;
                        }
                    case 1: // pen
                        {
                            isize2 = (start - i + 1) / start * r_size2;
                            op = 255; //(int)((start - i) / start * 255.0);
                            break;
                        }
                    case 2:// pen-2
                        {
                            isize2 = (i + 1) / start * r_size2;
                            op = 255; // (int)(i / start * 255.0);
                            break;
                        }
                    case 3: // crayon
                        {
                            op = 155;
                            break;
                        }
                    case 4://ink - 1
                        {
                            double diff = (i - start / 2) / start * 8;
                            diff = Math.Pow(Math.E, -(diff * diff) / 10);
                            isize2 = diff * r_size2;
                            op = (int)((diff + 0.1) * 255.0);
                            if (op > 255)
                                op = 255;
                            break;
                        }
                    case 5: //ink-2 watercolor
                        {
                            double diff = (i - start / 2) / start * 8;
                            diff = Math.Pow(Math.E, -(diff * diff) / 10);
                            if (diff < 1)
                                diff = 1 - diff;
                            else
                                diff = diff - 1;
                            diff += 0.2;
                            if (diff > 1) diff = 1.0;
                            isize2 = diff * r_size2;
                            op = (int)(diff * 255.0);
                            break;
                        }
                }
                this.strokePoints[i].opacity = (byte)(op);
                this.meshVertices2d.Add(o2 + isize2 * n2);
                this.meshVertices2d.Add(o2 - isize2 * n2);
            }
            this.buildStrokeMeshFace();
            this.addRoundCap2D();
            this.addHeadTailFaceIndex(2);
        }// changeStyle2d

        public void smooth()
        {
            int nloop = 5;
            int n = 0;
            int step = 3;
            while (n < nloop)
            {
                for (int i = n; i < this.strokePoints.Count - step; i += step)
                {
                    Vector2d v1 = this.strokePoints[i].pos2;
                    Vector2d v2 = this.strokePoints[i + step].pos2;
                    double len = (v1 - v2).Length() / step;
                    Vector2d dir = (v2 - v1).normalize();
                    for (int j = i; j < i + step; ++j)
                    {
                        this.strokePoints[j].pos2 = v1 + ((j - i) * len) * dir;
                    }

                }
                ++n;
            }
            this.changeStyle2d((int)SegmentClass.strokeStyle);
        }

		public void TryRectifyToLine()
		{
			double thres = 0.02 * this.get2DLength();
			Vector2d p, q;
			if (!this.LineRegression(out p, out  q))
				return;

			List<StrokePoint> new_stroke_points = new List<StrokePoint>();
			bool error_detected = false;
			double error = 0;
			foreach (StrokePoint pt in this.strokePoints)
			{
				Vector2d foot = Polygon.FindPointTolineFootPrint(pt.pos2,p,q);
				new_stroke_points.Add(new StrokePoint(foot));
				if (double.IsNaN(foot.x) || double.IsNaN(foot.y))
					error_detected = true;
				error += (foot - pt.pos2).Length();
			}
			error /= this.strokePoints.Count;
			if (!error_detected && error < 3)
			{
				this.strokePoints = new_stroke_points;
				this.u2 = p;
				this.v2 = q;
				this.changeStyle2d((int)SegmentClass.strokeStyle);
			}
		}

		public bool LineRegression(out Vector2d p, out Vector2d q) // returns the regression error
		{
			p = new Vector2d();
			q = new Vector2d();
			if (this.strokePoints.Count < 2) return false;
			double thresh = 1e-4;
			// regress a line to fit the stroke points
			int npoints = this.strokePoints.Count;
			int nvars = 1;
			double[,] xy = new double[npoints, nvars + 1];
			int i = 0;
			List<Vector2d> points2 = new List<Vector2d>();
			
			foreach (StrokePoint v in this.strokePoints)
			{
				xy[i, 0] = v.pos2.x;
				xy[i, 1] = v.pos2.y;
				points2.Add(v.pos2);
				i++;
			}

			// perform linear regression
			int info;
			alglib.linearmodel lrmodel;
			alglib.lrreport lrreport;
			alglib.lrbuild(xy, npoints, nvars, out info, out lrmodel, out lrreport);
			if (info == 1)
			{ // successful 
				double[] coef;
				alglib.lrunpack(lrmodel, out coef, out nvars);
				double a = coef[0], b = coef[1], c = 0;	 // y = ax + b  is the line formula

				// using non-linear optimization -- for case when there is a vetical line which causes the linear regression unstable
				LineOptimizer opt = new LineOptimizer();
				opt.Init(points2, new double[3] { a, -1, b });
				double[] abc = opt.Optimize();
				a = abc[0]; b = abc[1]; c = abc[2];
				double error = opt.GetFittingError(abc);

				if (a == 0 && b == 0)
					throw new ArgumentException("line regression failed!");

				Vector2d pt = new Vector2d(0, -c / b);				// a point on line
				if (b == 0) pt = new Vector2d(-c / a, 0);		// a point on line
				Vector2d d = new Vector2d(-b, a).normalize();	// line direction
				double min = double.MaxValue, max = double.MinValue;

				foreach (StrokePoint v in this.strokePoints)
				{
					double x = (v.pos2 - pt).Dot(d);
					if (x > max)
					{
						max = x;
						p = pt + d * x;
					}
					if (x < min)
					{
						min = x;
						q = pt + d * x;
					}
				}
				// for the type of x = c
				double dist_max = (this.strokePoints[0].pos2 - this.strokePoints[this.strokePoints.Count - 1].pos2).Length();
				double dist = (p - q).Length();
				dist_max *= 2;
				if (dist > thresh && dist < dist_max)
				{
					return true;
				}
			}

			Console.WriteLine("line regression failed, will use start - end points");
			
			return false;
		}
       
    }//Stroke

    public class GuideLine
    {
        public Vector3d u;
        public Vector3d v;
        public Vector2d u2;
        public Vector2d v2;
        private int nSketch = 0;
        public List<Stroke> strokes;
        public Plane hostPlane;
        private static readonly Random rand = new Random();
        public Arrow3D guideArrow;
        private bool isBoxEdge = true; // false -> guide line, less random strokes
        public bool active = true;
        public Color color = SegmentClass.StrokeColor;
        public bool isGuide = false;
        public Line3d[][] vanLines;
        public bool makeVisible = false;
        public double strokeGap = 0.05; // for overshotting
        // 1: normal 
        // 2: 1/2
        // 3: 1/3
        // 4: 1/4
        // 5: reflection
        public int type = 1;
        public double weight = SegmentClass.StrokeSize;

        public GuideLine() { }

        public GuideLine(Vector3d v1, Vector3d v2, Plane plane, bool isBoxEdge)
        {
            this.u = v1;
            this.v = v2;
            this.hostPlane = plane;
            this.isBoxEdge = isBoxEdge;
            //if (!isBoxEdge)
            //{
            //    this.DefineGuideLineStroke();
            //}
            //else
            //{
            //    //this.DefineRandomStrokes();
            //    this.DefineCrossStrokes();
            //}
            this.DefineCrossStrokes();
            if (plane != null)
            {
                this.guideArrow = new Arrow3D(v1, v2, plane.normal);
            }
        }

        public GuideLine(Stroke stroke)
        {
            // draw strokes
            this.strokes = new List<Stroke>();
            strokes.Add(stroke);
        }

        public void setHostPlane(Plane plane)
        {
            this.hostPlane = plane.clone() as Plane;
            if (this.strokes != null && this.strokes.Count > 0)
            {
                this.guideArrow = new Arrow3D(this.strokes[0].u3, this.strokes[0].v3, plane.normal);
            }
            else
            {
                this.guideArrow = new Arrow3D(u, v, plane.normal);
            }
        }

        private double angleTransform(int degree)
        {
            return Math.PI * (double)degree / 180.0;
        }

        private double getRandomDoubleInRange(Random rand, double s, double e)
        {
            return s + (e - s) * rand.NextDouble();
        }

        public void DefineRandomStrokes()
        {
            if (!this.isBoxEdge)
            {
                this.DefineGuideLineStroke();
                return;
            }
            this.strokes = new List<Stroke>();
            double strokeLen = (v - u).Length();
            double len = strokeGap * strokeLen;
            Vector3d lineDir = (v - u).normalize();
            if (!this.isBoxEdge)
            {
				this.nSketch = 2;// rand.Next(1, 2);
            }
            else
            {
                this.nSketch = rand.Next(3, 4);
            }
            // now the first one is always the correct one without errors
            double dis = this.getRandomDoubleInRange(rand, -len/4, len);
            if (!this.isBoxEdge)
            {
                dis = this.getRandomDoubleInRange(rand, 0, len / 2);
            }
            Stroke line = new Stroke(u - dis * lineDir, v + dis * lineDir, this.isBoxEdge);
            this.strokes.Add(line);
			
            for (int i = 1; i < this.nSketch; ++i)
            {
                Vector3d[] endpoints = new Vector3d[2];
                for (int j = 0; j < 2; ++j)
                {
                    // find an arbitrary point
                    dis = this.getRandomDoubleInRange(rand, -len, len);
                    // find a random normal
                    Vector3d normal1 = new Vector3d();
                    for (int k = 0; k < 3; ++k)
                    {
                        normal1[k] = this.getRandomDoubleInRange(rand, -1, 1);
                    }
                    normal1.normalize();
					double dirfloating = this.getRandomDoubleInRange(rand, 0, len / 2);

					if (!this.isBoxEdge)
					{
						dis = this.getRandomDoubleInRange(rand, -len *0.6, len *0.6);
						dirfloating = this.getRandomDoubleInRange(rand, 0, len / 3);
					}


                    Vector3d step1 = this.getRandomDoubleInRange(rand, -dirfloating, dirfloating) * normal1;
                    Vector3d normal2 = new Vector3d();
                    for (int k = 0; k < 3; ++k)
                    {
                        normal2[k] = this.getRandomDoubleInRange(rand, -1, 1);
                    }
                    normal2.normalize();
                    //Vector3d step2 = this.getRandomDoubleInRange(rand, -dirfloating, dirfloating) * normal2;
                    //double checkDir = step1.normalize().Dot(step2.normalize());
                    //if (checkDir > 0)
                    //{
                    //    // different dir
                    //    step2 = new Vector3d() - step2;
                    //}    
                    Vector3d step2 = new Vector3d() - step1;

                   
                    if (j == 0)
                    {
                        endpoints[j] = u + dis * lineDir;
                        endpoints[j] += step1;
						//if (!this.isBoxEdge)
						//{
						//	endpoints[j] = u - dis * lineDir + step1;
						//}
                    }
                    else
                    {
                        endpoints[j] = v + dis * lineDir;
                        endpoints[j] += step2;
                    }
                }
                line = new Stroke(endpoints[0], endpoints[1], this.isBoxEdge);
                line.weight *= 0.7;
                line.strokeColor = SegmentClass.sideStrokeColor;
                this.strokes.Add(line);
            }
        }//DefineRandomStrokes

        public void DefineCrossStrokes()
        {
            //if (!this.isBoxEdge)
            //{
            //    this.DefineGuideLineStroke();
            //    return;
            //}
            this.strokes = new List<Stroke>();
            double len = strokeGap *(v - u).Length();
            if (!this.isBoxEdge)
            {
                len /= 3;
            }
            Vector3d lineDir = (v - u).normalize();
            this.nSketch = 1;
            Vector3d[] endpoints = new Vector3d[2];
            double dis = this.getRandomDoubleInRange(rand, 0, len);
            endpoints[0] = u - dis * lineDir;
            endpoints[1] = v + dis * lineDir;
            Stroke line = new Stroke(endpoints[0], endpoints[1], this.isBoxEdge);
            this.strokes.Add(line);
        }

        public void DefineGuideLineStroke()
        {
            this.strokes = new List<Stroke>();
            Stroke line = new Stroke(this.u, this.v, this.isBoxEdge);
            this.strokes.Add(line);
        }

        public void buildVanishingLines(Vector3d v, int vidx)
        {
            double ext = 0.1;
            if (this.vanLines == null)
            {
                this.vanLines = new Line3d[2][];
            }
            this.vanLines[vidx] = new Line3d[2];
            Vector3d[] points = new Vector3d[2] { this.u, this.v };
            for (int i = 0; i < points.Length; ++i)
            {
                Vector3d vi = points[i];
                Vector3d d = (vi - v).normalize();
                ext = getRandomDoubleInRange(rand, 0, 0.2);
                Vector3d v1 = vi + ext * d;
                ext = getRandomDoubleInRange(rand, 0, 0.2);
                Vector3d v2 = v - ext * d;
                Line3d line = new Line3d(v1, v2);
                this.vanLines[vidx][i] = line;
            }
        }

        public void buildVanishingLines2d(Vector2d v, int vidx)
        {
            double ext = 0.1;
            if (this.vanLines == null)
            {
                this.vanLines = new Line3d[2][];
            }
            this.vanLines[vidx] = new Line3d[2];
            Vector2d[] points = new Vector2d[2] { this.u2, this.v2 };
            Vector2d dir =  (this.v2-this.u2).normalize();
            for (int i = 0; i < points.Length; ++i)
            {
                Vector2d vi = points[i];
                Vector2d d = (vi - v).normalize();
                ext = getRandomDoubleInRange(rand, 0, 20);
                Vector2d v1 = vi + ext * d;
                ext = getRandomDoubleInRange(rand, 0, 20);
                Vector2d v2 = v - ext * d;
                Line3d line = new Line3d(v1, v2);
                // check if the line dir aligns with the vanishing dir
                double acos = dir.Dot(d);
                if (Math.Abs(Math.Abs(acos) - 1) > 0.1)
                {
                    line.active = false;
                }
                this.vanLines[vidx][i] = line;
            }
        }

    }//GuideLine


    public class DrawStroke2d
    {
        //endpoints
        public Vector2d u2;
        public Vector2d v2;
        //stroke points
        public List<Stroke> strokes;
        public double strokeGap = 0.1;

        private static readonly Random rand = new Random();

        public DrawStroke2d() { }

        public DrawStroke2d(Stroke stroke)
        {
            this.strokes = new List<Stroke>();
            this.strokes.Add(stroke);

            Vector2d p, q;
            bool isLine = this.linearRegression(stroke, out p, out q);
            if (isLine)
            {
                this.u2 = p;
                this.v2 = q;
                this.DefineRandomCurves();
            }
            else
            {
                this.u2 = new Vector2d(stroke.strokePoints[0].pos2);
                this.v2 = new Vector2d(stroke.strokePoints[stroke.strokePoints.Count - 1].pos2);
                this.DefineRandomCurves();
            }
        }

        public void DefineRandomLines()
        {
            Vector2d v2 = this.strokes[0].strokePoints[0].pos2;
            Vector2d u2 = this.strokes[0].strokePoints[this.strokes[0].strokePoints.Count - 1].pos2;
            double strokeLen = (v2 - u2).Length();
            double len = strokeGap * strokeLen;
            Vector2d lineDir = (v2 - u2).normalize();
            int  n = rand.Next(1, 4);
            // now the first one is always the correct one without errors
            double dirfloating = 10;
            for (int i = 1; i < n; ++i)
            {
                Vector2d[] endpoints = new Vector2d[2];
                for (int j = 0; j < 2; ++j)
                {
                    // find an arbitrary point
                    double dis = this.getRandomDoubleInRange(rand, -1, 1) * len;
                    // find a random normal
                    Vector2d normal1 = new Vector2d();
                    for (int k = 0; k < 2; ++k)
                    {
                        normal1[k] = this.getRandomDoubleInRange(rand, -1, 1);
                    }
                    normal1.normalize();
                    Vector2d step1 = this.getRandomDoubleInRange(rand, -1, 1) * dirfloating * normal1;
                    Vector2d step2 = new Vector2d() - step1;

                    if (j == 0)
                    {
                        endpoints[j] = u2 + dis * lineDir;
                        endpoints[j] += step1;
                    }
                    else
                    {
                        endpoints[j] = v2 + dis * lineDir;
                        endpoints[j] += step2;
                    }
                }
                Stroke stroke = new Stroke(endpoints[0], endpoints[1], false, this.strokes[0].strokePoints.Count);
                stroke.weight *= 0.7;
                stroke.strokeColor = SegmentClass.sideStrokeColor;
                this.strokes.Add(stroke);
            }
        }//Define random lines (strokes)

        public void DefineRandomCurves()
        {
            double strokeLen = (v2 - u2).Length();
            double len = strokeLen / 10;
            Vector2d lineDir = (v2 - u2).normalize();
            int n = rand.Next(2, 4);
			
            //return;
            //n = n > 2 ? 2 : n;
            //n = 1;
            // now the first one is always the correct one without errors
            double angle = 0.1;
            int npoints = this.strokes[0].strokePoints.Count;
            for (int i = 1; i < n; ++i)
            {
                // 1. rotate angle
                double theta = this.getRandomDoubleInRange(rand, -angle, angle);
                // 2. rotate from which point
                int idx = rand.Next(1, this.strokes[0].strokePoints.Count - 1);
                List<Vector2d> points = new List<Vector2d>();

                // left: rotate theta
                // right: rotate -theta
                //for (int j = 0; j < npoints; ++j)
                //{
                //    Vector2d vr = this.strokes[0].strokePoints[j].pos2.rotate(theta);
                //    points.Add(vr);
                //}
                double dist = this.getRandomDoubleInRange(rand, 0, len/2);
                double d = dist / idx;
                for (int j = 0; j <= idx; ++j)
                {
                    Vector2d dir = (this.strokes[0].strokePoints[j + 1].pos2 - this.strokes[0].strokePoints[j].pos2).normalize();
                    Vector2d norm = new Vector2d(-dir.y, dir.x);
                    norm.normalize();
                    Vector2d trans = this.strokes[0].strokePoints[j].pos2 + d * (idx - j) * norm;
                    points.Add(trans);
                }
                dist = this.getRandomDoubleInRange(rand, 0, len/2);
                d = dist / (this.strokes[0].strokePoints.Count - idx);
                for (int j = idx + 1; j < this.strokes[0].strokePoints.Count; ++j)
                {
                    Vector2d dir = new Vector2d();
                    if (j < this.strokes[0].strokePoints.Count - 1)
                        dir = (this.strokes[0].strokePoints[j + 1].pos2 - this.strokes[0].strokePoints[j].pos2).normalize();
                    else
                        dir = (this.strokes[0].strokePoints[j].pos2 - this.strokes[0].strokePoints[j - 1].pos2).normalize();

                    Vector2d norm = -1 * new Vector2d(-dir.y, dir.x);
                    norm.normalize();
                    Vector2d trans = this.strokes[0].strokePoints[j].pos2 + d * (j - idx) * norm;
                    points.Add(trans);
                }

                Stroke stroke = new Stroke(points, SegmentClass.PenSize/2);
                stroke.strokeColor = SegmentClass.sideStrokeColor;
                this.strokes.Add(stroke);
            }
        }//Define random curves (strokes)

        private double getRandomDoubleInRange(Random rand, double s, double e)
        {
            return s + (e - s) * rand.NextDouble();
        }

        private bool linearRegression(Stroke stroke, out Vector2d p, out Vector2d q)
        {
            // check if a stroke is a line or curve, to decide which perturb method to use
            int n = stroke.strokePoints.Count;
            double[,] xy = new double[n, 2];
            int i = 0;
            int nvar = 1;
            List<Vector2d> points = new List<Vector2d>();
            p = new Vector2d();
            q = new Vector2d();
            foreach (StrokePoint sp in stroke.strokePoints)
            {
                xy[i, 0] = sp.pos2.x;
                xy[i, 1] = sp.pos2.y;
                points.Add(sp.pos2);
                ++i;
            }
            alglib.linearmodel lrmodel;
            alglib.lrreport lrreport;
            int info;
            alglib.lrbuild(xy, n, nvar, out info, out lrmodel, out lrreport);
            if (info == 1)
            {
                double[] coef;
                alglib.lrunpack(lrmodel, out coef, out nvar);
                double a = coef[0], b = coef[1], c = 0;	 // y = ax + b  is the line formula

                // using non-linear optimization -- for case when there is a vetical line which causes the linear regression unstable
                LineOptimizer opt = new LineOptimizer();
                opt.Init(points, new double[3] { a, -1, b });
                double[] abc = opt.Optimize();
                a = abc[0]; b = abc[1]; c = abc[2];
                double error = opt.GetFittingError(abc);

                if (a == 0 && b == 0)
                    throw new ArgumentException("line regression failed!");

                Vector2d pt = new Vector2d(0, -c / b);				// a point on line
                if (b == 0) pt = new Vector2d(-c / a, 0);		// a point on line
                Vector2d d = new Vector2d(-b, a).normalize();	// line direction
                double min = double.MaxValue, max = double.MinValue;

                foreach (StrokePoint v in stroke.strokePoints)
                {
                    double x = (v.pos2 - pt).Dot(d);
                    if (x > max)
                    {
                        max = x;
                        p = pt + d * x;
                    }
                    if (x < min)
                    {
                        min = x;
                        q = pt + d * x;
                    }
                }
                // for the type of x = c
                double dist_max = (stroke.strokePoints[0].pos2 - stroke.strokePoints[stroke.strokePoints.Count - 1].pos2).Length();
                double dist = (p - q).Length();
                dist_max *= 2;
                //if (dist > 1e-6 && dist < dist_max)
                //{
                //    return true;
                //}
            }

            double err = 0.0;
            foreach (StrokePoint pt in stroke.strokePoints)
            {
                Vector2d foot = Polygon.FindPointTolineFootPrint(pt.pos2,
                    stroke.strokePoints[0].pos2, stroke.strokePoints[stroke.strokePoints.Count - 1].pos2
                    );
                err += (foot - pt.pos2).Length();
            }
            err /= stroke.strokePoints.Count;
            if (err < 10)
                return true;
            return false;
        }
    } // Draw stroke 2d

    public class UserLevel
    {
        public List<int> index;
        public List<int> previous_guides;

        public UserLevel(List<int> index, List<int> previous_guides)
        {
            this.index = index;
            this.previous_guides = previous_guides;
        }
    }

    public class Primitive
    {
        public Vector3d[] points = null;
        public Vector2d[] points2 = null;
        public Plane[] planes = null;
        public GuideLine[] edges = null;
        public List<List<GuideLine>> guideLines = null;
        public List<Circle3D> circles = null;
        public Line3d[][] vanLines;
        public Line3d[][] vanEllipses;
        private static readonly Random rand = new Random();
        public List<Plane> facesToDraw;
        public List<Plane> facesToHighlight;
        public int activeFaceIndex = -1;
        public int highlightFaceIndex = -1;
        public int guideBoxIdx = -1;
        public int guideBoxSeqGroupIdx = -1;
        public List<int> guideBoxSeqenceIdx;
        public List<Arrow3D> arrows;
        public String type = "";
        public List<userLevel> users = new List<userLevel>();
        public List<String> type_texts = new List<string>();

        public Primitive()
        { }

        public Primitive(Vector3d[] vs, int type)
        {
            if (vs == null) { return; }
            switch (type)
            {
                case 2:
                    this.createPlanePrimitive(vs);
                    break;
                case 3:
                    this.createLinePrimitive(vs);
                    break;
                case 0:
                case 1:
                default:
                    this.createBoxPrimitive(vs);
                    break;
            }
            this.guideLines = new List<List<GuideLine>>();
            this.facesToDraw = new List<Plane>();
            this.facesToHighlight = new List<Plane>();
            this.circles = new List<Circle3D>();
        }

        private void createBoxPrimitive(Vector3d[] vs)
        {
            this.type = "Box";
            this.points = new Vector3d[vs.Length];
            for (int i = 0; i < vs.Length; ++i)
            {
                this.points[i] = new Vector3d(vs[i]);
            }
            // faces
            this.planes = new Plane[6];
            List<Vector3d> vslist = new List<Vector3d>();
            for (int i = 0; i < 4; ++i)
            {
                vslist.Add(this.points[i]);
            }
            this.planes[0] = new Plane(vslist);
            vslist = new List<Vector3d>();
            for (int i = 4; i < 8; ++i)
            {
                vslist.Add(this.points[i]);
            }
            this.planes[1] = new Plane(vslist);
            int r = 2;
            for (int i = 0; i < 4; ++i)
            {
                vslist = new List<Vector3d>();
                vslist.Add(this.points[i]);
                vslist.Add(this.points[(i + 1) % 4]);
                vslist.Add(this.points[((i + 1) % 4 + 4) % 8]);
                vslist.Add(this.points[(i + 4) % 8]);
                this.planes[r++] = new Plane(vslist);
            }
            this.edges = new GuideLine[12];
            int s = 0;
            Plane plane = new Plane();
            int[] series = { 0, 3, 0, 5 };

			for (int i = 0; i < 4; ++i)
			{
				plane = this.planes[series[i]].clone() as Plane;
				edges[s++] = new GuideLine(this.points[i], this.points[(i + 1) % 4], plane, true);
			}
			series = new int[] { 5, 3, 3, 5 };
			for (int i = 0; i < 4; ++i)
			{
				plane = this.planes[series[i]].clone() as Plane;
				edges[s++] = new GuideLine(this.points[i], this.points[i + 4], plane, true);
			}
			series = new int[] { 1, 3, 1, 5 };
			for (int i = 0; i < 4; ++i)
			{
				plane = this.planes[series[i]].clone() as Plane;
				edges[s++] = new GuideLine(this.points[i + 4], this.points[4 + (i + 1) % 4], plane, true);
			}

            
        }

        private void createPlanePrimitive(Vector3d[] vs)
        {
            this.type = "Plane";
            this.points = new Vector3d[vs.Length];
            for (int i = 0; i < vs.Length; ++i)
            {
                this.points[i] = new Vector3d(vs[i]);
            }
            // face
            this.planes = new Plane[1];
            List<Vector3d> vslist = new List<Vector3d>();
            for (int i = 0; i < 4; ++i)
            {
                vslist.Add(this.points[i]);
            }
            this.planes[0] = new Plane(vslist);

            this.edges = new GuideLine[4];
            Plane plane = new Plane();
            for (int i = 0; i < 4; ++i)
            {
                plane = this.planes[0].clone() as Plane;
                edges[i] = new GuideLine(this.points[i], this.points[(i + 1) % 4], plane, true);
            }
        }

        private void createLinePrimitive(Vector3d[] vs)
        {
            this.type = "Line";
            this.points = new Vector3d[vs.Length];
            for (int i = 0; i < vs.Length; ++i)
            {
                this.points[i] = new Vector3d(vs[i]);
            }
            int nlines = vs.Length / 2;
            this.edges = new GuideLine[nlines];
            Plane plane = new Plane();
            for (int i = 0; i < nlines; ++i)
            {
                plane = new Plane(
                    (this.points[2 * i] + this.points[2 * i + 1]) / 2,
                    new Vector3d(0, 0, 1));
                edges[i] = new GuideLine(this.points[2 * i], this.points[2 * i + 1], plane, true);
            }
        }

        public List<GuideLine> getAllLines()
        {
            List<GuideLine> allLines = new List<GuideLine>();
            foreach (GuideLine edge in this.edges)
            {
                allLines.Add(edge);
            }
            for (int g = 0; g < this.guideLines.Count; ++g)
            {
                allLines.AddRange(this.guideLines[g]);
            }
            return allLines;
        }

        public void normalize(Vector3d center, double scale)
        {
            for (int i = 0;i < points.Length;++i)
            {
                points[i] /= scale;
                points[i] -= center;
            }
            foreach (GuideLine line in this.edges)
            {
                line.u /= scale;
                line.v /= scale;
                line.u -= center;
                line.v -= center;
                foreach (Stroke s in line.strokes)
                {
                    s.u3 /= scale;
                    s.u3 -= center;
                    s.v3 /= scale;
                    s.v3 -= center;
                }
            }
        }

        public void buildVanishingLines(Vector3d v, int vidx)
        {
            double ext = 0.1;
            if (this.vanLines == null)
            {
                this.vanLines = new Line3d[2][];
            }
            this.vanLines[vidx] = new Line3d[this.points.Length];
            for (int i = 0; i < this.points.Length; ++i)
            {
                Vector3d vi = this.points[i];
                Vector3d d = (vi - v).normalize();
                ext = Polygon.getRandomDoubleInRange(rand, 0, 0.2);
                Vector3d v1 = vi + ext * d;
                ext = Polygon.getRandomDoubleInRange(rand, 0, 0.2);
                Vector3d v2 = v - ext * d;
                Line3d line = new Line3d(v1, v2);
                this.vanLines[vidx][i] = line;
            }
        }

        public void buildVanishingLines2d(Vector2d v, int vidx)
        {
            double ext = 0.1;
            if (this.vanLines == null)
            {
                this.vanLines = new Line3d[2][];
            }
            this.vanLines[vidx] = new Line3d[this.points.Length];
            for (int i = 0; i < this.points.Length; ++i)
            {
                Vector2d vi = this.points2[i];
                Vector2d d = (vi - v).normalize();
                ext = Polygon.getRandomDoubleInRange(rand, 0, 20);
                Vector2d v1 = vi + ext * d;
                ext = Polygon.getRandomDoubleInRange(rand, 0, 20);
                Vector2d v2 = v - ext * d;
                Line3d line = new Line3d(v1, v2);
                this.vanLines[vidx][i] = line;
            }
        }

        public Plane[] GetYPairFaces()
        {
            return new Plane[2] {
				this.planes[0], this.planes[2]
			};
        }
        public Plane[] GetXPairFaces()
        {
            return new Plane[2] {
				this.planes[3], this.planes[5]
			};
        }
        public Plane[] GetZPairFaces()
        {
            return new Plane[2] {
				this.planes[1], this.planes[4]
			};
        }

        public Object softClone()
        {
            Primitive cloneBox = new Primitive();
            // only change the active value
            // for convenience, copy other values
            cloneBox.points = this.points;
            cloneBox.planes = this.planes;
            cloneBox.vanLines = this.vanLines;
            cloneBox.edges = this.edges;
            cloneBox.guideLines = new List<List<GuideLine>>();
            cloneBox.arrows = this.arrows;
            cloneBox.facesToHighlight = this.facesToHighlight;
            cloneBox.guideBoxSeqGroupIdx = this.guideBoxSeqGroupIdx;
            cloneBox.guideBoxSeqenceIdx = this.guideBoxSeqenceIdx;
            cloneBox.guideBoxIdx = this.guideBoxIdx;

            foreach (List<GuideLine> lines in this.guideLines)
            {
                List<GuideLine> cloneLines = new List<GuideLine>();
                foreach (GuideLine line in lines)
                {
                    GuideLine cloneLine = new GuideLine();
                    cloneLine.hostPlane = line.hostPlane;
                    cloneLine.strokes = line.strokes;
                    cloneLine.type = line.type;
                    cloneLine.active = false;
                    cloneLine.isGuide = line.isGuide;
                    cloneLines.Add(cloneLine);
                }
                cloneBox.guideLines.Add(cloneLines);
            }

            return cloneBox;
        }
    }// Primitive

    public class StrokePoint
    {
        // contorl the representation of stroke mesh
        public Vector2d pos2;
        public Vector3d pos3;
        public Vector2d pos2_origin;
        public Vector2d pos2_local;
        public Vector3d pos3_origin;
        public byte opacity = 255;
        public Color color = Color.Black;
        public double depth = 1.0;
        public double speed = 1.0;

        public StrokePoint(Vector2d p)
        {
            this.pos2 = new Vector2d(p);
            this.pos2_local = new Vector2d(this.pos2);
        }

        public StrokePoint(Vector3d p)
        {
            this.pos3 = new Vector3d(p);
        }

        public void Transform(Matrix4d T)
        {
            this.pos3 = (T * new Vector4d(pos3, 1)).ToVector3D();
        }
        public void Transform_from_origin(Matrix4d T)
        {
            this.pos3 = (T * new Vector4d(pos3_origin, 1)).ToVector3D();
        }
        public void Transform_to_origin(Matrix4d T)
        {
            this.pos3_origin = (T * new Vector4d(this.pos3, 1)).ToVector3D();
        }

    }// StrokePoint

    public class LineOptimizer
    {
        private int multivariate = 1;
        private double[] initialx = null;	// a*x + b*y +c = 0; this is a more general line equation
        private List<Vector2d> points = null;

        public void Init(List<Vector2d> points, double[] init_x)
        {
            int n = points.Count;
            this.initialx = init_x;
            this.points = points;
            this.multivariate = n;
        }
        public double[] Optimize()
        {
            double[] x = this.initialx;
            double epsg = 0.0000000001;
            double epsf = 0;
            double epsx = 0;
            int maxits = 100;

            alglib.minlmstate state;
            alglib.minlmreport rep;

            //	this.OutputEnergy(x, "before:");

            // with jacobi
            alglib.minlmcreatevj(this.multivariate, x, out state);
            alglib.minlmsetcond(state, epsg, epsf, epsx, maxits);
            alglib.minlmoptimize(state, function, jacobi, null, null);
            alglib.minlmresults(state, out x, out rep);

            //// without jacobi
            //alglib.minlmcreatev(this.multivariate, x, 0.0001, out state);
            //alglib.minlmsetcond(state, epsg, epsf, epsx, maxits);
            //alglib.minlmoptimize(state, function, null, null);
            //alglib.minlmresults(state, out x, out rep);

            //	this.OutputEnergy(x, "after:");

            return x;
        }
        public double GetFittingError(double[] x)
        {
            double error = 0;
            foreach (Vector2d p in this.points)
            {
                double a = x[0], b = x[1], c = x[2];
                double d = Math.Abs(a * p.x + b * p.y + c);
                d /= Math.Sqrt(a * a + b * b);
                error += d;
            }
            return error / this.points.Count;
        }
        private void function(double[] x, double[] func, object obj)
        {
            int _functionIndex = 0;
            foreach (Vector2d pt in this.points)
            {
                double a = x[0], b = x[1], c = x[2];
                double d = (a * pt.x + b * pt.y + c);
                //		d /= Math.Sqrt(a * a + b * b);
                func[_functionIndex] = d;
                _functionIndex++;
            }
        }//function
        private void jacobi(double[] x, double[] func, double[,] jacoi, object obj)
        {
            int _functionIndex = 0;
            foreach (Vector2d pt in this.points)
            {
                double a = x[0], b = x[1], c = x[2];
                double d = (a * pt.x + b * pt.y + c);
                func[_functionIndex] = d;
                jacoi[_functionIndex, 0] = pt.x;
                jacoi[_functionIndex, 1] = pt.y;
                jacoi[_functionIndex, 2] = 1;
                _functionIndex++;
            }
        }
        private void OutputEnergy(double[] x, string prefix_text)
        {
            Console.Write("x = [");
            for (int i = 0; i < x.Length; ++i)
            {
                Console.Write(x[i].ToString("f3") + ",");
            }
            Console.WriteLine("]");

            double error = this.GetFittingError(x);
            Console.WriteLine(prefix_text + " energy = " + error);
        }
    }
}
