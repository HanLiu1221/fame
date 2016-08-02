using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using TrimeshWrapper;
using Geometry;

namespace Component
{
    public unsafe class Segment
    {
        public int idx = -1;
        public Mesh mesh = null;
        public Primitive boundingbox = null;
        public Color color = Color.Black;
        public bool active = true;
        public bool drawn = false;
        public List<List<int>> contours;
        private MyTriMesh2 triMesh;
        public List<Vector3d> contourPoints;
        public List<Vector3d> silhouettePoints;
        public List<Vector3d> suggestiveContourPoints;
        public List<Vector3d> ridgePoints;
        public List<Vector3d> boundaryPoints;
        private string meshFileName;
        public Contour contour;
        public List<DrawStroke2d> sketch;

        public Segment(Mesh m, Primitive c)
        {
            this.mesh = m;
            this.boundingbox = c;
        }

        static private void ArrayConvCtoSB(ref sbyte[] to_sbyte, char[] from_char)
        {
            for (int i = 0; i < from_char.Length; i++)
            {
                Array.Resize(ref to_sbyte, to_sbyte.Length + 1);
                to_sbyte[i] = (sbyte)from_char[i];
            }
        }

        public void loadTrieMesh(string filename){
            sbyte[] fn = new sbyte[0];
            ArrayConvCtoSB(ref fn, filename.ToCharArray());
            List<Vector3d> contourPoints = new List<Vector3d>();
            fixed (sbyte* meshName = fn)
            {
                this.triMesh = new MyTriMesh2(meshName);
                int npoints = triMesh.vertextCount();
            }
            this.meshFileName = filename;
        }//loadTrieMesh

        private void reloadMesh()
        {
            sbyte[] fn = new sbyte[0];
            ArrayConvCtoSB(ref fn, this.meshFileName.ToCharArray());
            List<Vector3d> contourPoints = new List<Vector3d>();
            fixed (sbyte* meshName = fn)
            {
                this.triMesh = new MyTriMesh2(meshName);
                int npoints = triMesh.vertextCount();
            }
        }

        public int getTriMeshVertexCount()
        {
            if (this.triMesh == null) return -1;
            int npoints = triMesh.vertextCount();
            return npoints;
        }


        public void updateVertex(Matrix4d T)
        {
            // tag = 1: exterior silhouette
            // tag = 2: contour
            // tag = 3: suggestive contour
            // tag = 4: apparent ridge
            if (this.mesh == null || this.triMesh == null) return;

            this.reloadMesh();

            double[] vertexPos = new double[this.mesh.VertexPos.Length];
            int nverts = this.triMesh.vertextCount();
            for (int i = 0, j = 0; i < mesh.VertexCount; ++i, j += 3)
            {
                Vector3d v0 = new Vector3d(mesh.VertexPos[j],
                    mesh.VertexPos[j + 1],
                    mesh.VertexPos[j + 2]);
                Vector3d v1 = (T * new Vector4d(v0, 1)).ToVector3D();
                vertexPos[j] = v1.x;
                vertexPos[j + 1] = v1.y;
                vertexPos[j + 2] = v1.z;
            }
            fixed (double* vertexPos_ = vertexPos)
            {
                triMesh.set_transformed_Vertices(vertexPos_);
            }
            
            nverts = this.triMesh.vertextCount();
        }// computeContour

        public void computeSihouette(Matrix4d Tv, Vector3d eye)
        {
            if (this.mesh == null || this.triMesh == null) return;
            double[] eyepos = eye.ToArray();
            double[] contour = new double[80000];
            this.silhouettePoints = new List<Vector3d>();
            fixed (double* eyepos_ = eyepos)
            fixed (double* contour_ = contour)
            {
                int nps = triMesh.get_silhouette(eyepos_, 0, contour_);
                for (int i = 0; i < nps; i += 3)
                {
                    Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                    Vector3d vt = (Tv * new Vector4d(v, 1)).ToVector3D();
                    this.silhouettePoints.Add(vt);
                }
            }
        }// computeSihouette

        public void computeContour(Matrix4d Tv, Vector3d eye)
        {
            if (this.mesh == null || this.triMesh == null) return;
            double[] eyepos = eye.ToArray();
            double[] contour = new double[30000];
            int nverts = this.triMesh.vertextCount();
            this.contourPoints = new List<Vector3d>();
            fixed (double* eyepos_ = eyepos)
            fixed (double* contour_ = contour)
            {
                int nps = triMesh.get_contour(eyepos_, 0, contour_);
                for (int i = 0; i < nps; i += 3)
                {
                    Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                    Vector3d vt = (Tv * new Vector4d(v, 1)).ToVector3D();
                    this.contourPoints.Add(vt);
                }
            }
        }// computeContour

        public void computeSuggestiveContour(Matrix4d Tv, Vector3d eye)
        {
            if (this.mesh == null || this.triMesh == null) return;
            double[] eyepos = eye.ToArray();
            double[] contour = new double[30000];
            this.suggestiveContourPoints = new List<Vector3d>();
            fixed (double* eyepos_ = eyepos)
            fixed (double* contour_ = contour)
            {
                int nps = triMesh.get_suggestive_contour(eyepos_, 0, contour_);
                for (int i = 0; i < nps; i += 3)
                {
                    Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                    Vector3d vt = (Tv * new Vector4d(v, 1)).ToVector3D();
                    this.suggestiveContourPoints.Add(vt);
                }
            }
        }// computeSuggestiveContour

        public void computeApparentRidge(Matrix4d Tv, Vector3d eye)
        {
            if (this.mesh == null || this.triMesh == null) return;
            double[] eyepos = eye.ToArray();
            double[] contour = new double[50000];
            this.ridgePoints = new List<Vector3d>();
            fixed (double* eyepos_ = eyepos)
            fixed (double* contour_ = contour)
            {
                int nps = triMesh.get_apparent_ridges(eyepos_, 0, contour_);
                for (int i = 0; i < nps; i += 3)
                {
                    Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                    Vector3d vt = (Tv * new Vector4d(v, 1)).ToVector3D();
                    this.ridgePoints.Add(vt);
                }
            }
        }// computeApparentRidge

        public void computeBoundary(Matrix4d Tv, Vector3d eye)
        {
            if (this.mesh == null || this.triMesh == null) return;
            double[] eyepos = eye.ToArray();
            double[] contour = new double[30000];
            this.boundaryPoints = new List<Vector3d>();
            fixed (double* eyepos_ = eyepos)
            fixed (double* contour_ = contour)
            {
                int nps = triMesh.get_boundary(eyepos_, 0, contour_);
                for (int i = 0; i < nps; i += 3)
                {
                    Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                    Vector3d vt = (Tv * new Vector4d(v, 1)).ToVector3D();
                    this.boundaryPoints.Add(vt);
                }
            }
        }// computeApparentRidge

        public void regionGrowingContours(List<int> labeled)
        {
            if (this.mesh == null) return;
            this.contours = new List<List<int>>();
            int ndist = 10;
            while (labeled.Count > 0)
            {
                int i = labeled[0];
                labeled.RemoveAt(0);
                if (!this.mesh.Flags[i])
                {
                    continue;
                }
                this.mesh.Flags[i] = false;
                List<int> vids = new List<int>();
                List<int> queue = new List<int>();
                queue.Add(i);
                vids.Add(i);
                int s = 0;
                int d = 0;
                while (s < queue.Count && d < ndist)
                {
                    int j = queue[s];
                    for (int k = 0; k < mesh.VertexFaceIndex[j].Count; ++k)
                    {
                        int f = mesh.VertexFaceIndex[j][k];
                        if (f == -1) continue;
                        for (int fi = 0; fi < 3; ++fi)
                        {
                            int kv = mesh.FaceVertexIndex[f * 3 + fi];
                            if (mesh.Flags[kv])
                            {
                                vids.Add(kv);
                                mesh.Flags[kv] = false;
                                d = 0;
                            }
                            if (!queue.Contains(kv))
                            {
                                queue.Add(kv);
                            }
                        }
                        ++s;
                        ++d;
                    }
                }
                if (vids.Count > 5)
                {
                    this.contours.Add(vids);
                }
            }
        }//regionGrowingContours

    }
}
