using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Geometry;

namespace Component
{
    // part based modeling
    // a part includes:
    // - bounding box info, e.g., vertices
    // - mesh
    public class Part
    {
        Mesh _mesh = null;
        Prim _boundingbox = null;
        List<Part> _jointParts = new List<Part>();
        List<Joint> _joints = new List<Joint>();
        int[] _vertexIndexInParentMesh;
        int[] _faceVIndexInParentMesh;
        double[] _vertexPosInParentMesh;

        public Color _COLOR = Color.LightBlue;

        public Part(Mesh m)
        {
            _mesh = m;
            this.calculateBbox();
        }

        public Part(Mesh m, int[] vIndex, double[] vPos, int[] fIndex)
        {
            // create a mesh part from a large mesh
            // re-order the index of vertex, face
            Dictionary<int, int> d = new Dictionary<int, int>();
            _vertexIndexInParentMesh = vIndex;
            _faceVIndexInParentMesh = fIndex;
            _vertexPosInParentMesh = vPos;
            int k = 0;
            foreach (int idx in vIndex)
            {
                d.Add(idx, k++);
            }
            int[] faceVertexIndex = new int[fIndex.Length * 3];
            for (int i = 0, j= 0; i < fIndex.Length; ++i)
            {
                int fv1 = m.FaceVertexIndex[fIndex[i] * 3];
                int fv2 = m.FaceVertexIndex[fIndex[i] * 3 + 1];
                int fv3 = m.FaceVertexIndex[fIndex[i] * 3 + 2];
                faceVertexIndex[j++] = d[fv1];
                faceVertexIndex[j++] = d[fv2];
                faceVertexIndex[j++] = d[fv3];
            }
            _mesh = new Mesh(vPos, faceVertexIndex);
            _COLOR = Color.FromArgb(Common.rand.Next(255), Common.rand.Next(255), Common.rand.Next(255));
            this.calculateBbox();
        }

        public Part(Mesh m, Prim bbox)
        {
            _mesh = m;
            _boundingbox = bbox;
            _COLOR = Color.FromArgb(Common.rand.Next(255), Common.rand.Next(255), Common.rand.Next(255));
        }

        public Mesh _MESH
        {
            get
            {
                return _mesh;
            }
        }

        public Prim _BOUNDINGBOX
        {
            get
            {
                return _boundingbox;
            }
        }

        public List<Joint> _JOINTS
        {
            get
            {
                return _joints;
            }
        }

        public List<Part> _JOINTPARTS
        {
            get
            {
                return _jointParts;
            }
        }

        public int[] _VERTEXINDEX
        {
            get
            {
                return _vertexIndexInParentMesh;
            }
        }

        public int[] _FACEVERTEXINDEX
        {
            get
            {
                return _faceVIndexInParentMesh;
            }
        }

        public double[] _VERTEXPOS
        {
            get
            {
                return _vertexPosInParentMesh;
            }
        }

        private void calculateBbox()
        {
            if (_mesh == null)
            {
                return;
            }
            Vector3d maxv = Vector3d.MinCoord;
            Vector3d minv = Vector3d.MaxCoord;
            for (int i = 0, j = 0; i < _mesh.VertexCount; ++i)
            {
                Vector3d v = new Vector3d(_mesh.VertexPos[j++], _mesh.VertexPos[j++], _mesh.VertexPos[j++]);
                maxv = Vector3d.Max(v, maxv);
                minv = Vector3d.Min(v, minv);
            }
            _boundingbox = new Prim(minv, maxv);
        }

        public void addAJoint(Part p, Joint j)
        {
            if (!_jointParts.Contains(p))
            {
                _jointParts.Add(p);
            }
            _joints.Add(j);
        }
    }// Part

    public class Model
    {
        List<Part> _parts;
        Mesh _mesh; // the whole mesh

        public Model(List<Part> parts)
        {
            _parts = parts;
        }

        public Model(Mesh mesh)
        {
            _mesh = mesh;
            this.initializeParts();
            this.mergeNearbyParts();
        }

        private void calPartRelations()
        {
            if (_parts == null || _parts.Count == 0)
            {
                return;
            }
            for (int i = 0; i < _parts.Count - 1; ++i)
            {
                for (int j = i + 1; j < _parts.Count; ++j)
                {
                    Vector3d jointPoint;
                    double d = calClosestPointBetweenMeshes(_parts[i]._MESH, _parts[j]._MESH, out jointPoint);
                    if (d < Common._thresh)
                    {
                        // has a contact
                        Joint joint = new Joint(_parts[i], _parts[j], jointPoint);
                        _parts[i].addAJoint(_parts[j], joint);
                        _parts[j].addAJoint(_parts[i], joint);
                    }
                }
            }
        }// calPartRelations

        private double calClosestPointBetweenMeshes(Mesh m1, Mesh m2, out Vector3d jointPoint)
        {
            double min_dist = double.MaxValue;
            jointPoint = new Vector3d();
            for (int i = 0; i < m1.VertexCount; ++i)
            {
                Vector3d v1 = new Vector3d(m1.VertexPos, i * 3);
                for (int j = 0; j < m2.VertexCount; ++j)
                {
                    Vector3d v2 = new Vector3d(m2.VertexPos, j * 3);
                    double d = (v1 - v2).Length();
                    if (d < min_dist)
                    {
                        min_dist = d;
                        jointPoint = (v1 + v2) / 2;
                    }
                }
            }
            return min_dist;
        }// calClosestPointBetweenMeshes

        private void initializeParts()
        {
            if (_mesh == null) return;
            // find point cluster to form initial parts
            int n = _mesh.VertexCount;
            bool[] visited = new bool[n];
            _parts = new List<Part>();
            for (int k = 0; k < n; ++k)
            {
                if (visited[k]) continue;
                Queue<int> q = new Queue<int>();
                List<int> vIndex = new List<int>();
                q.Enqueue(k);
                visited[k] = true;
                // region growing from i
                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    vIndex.Add(cur);
                    int[] curRow = _mesh._VV[cur];
                    foreach (int j in curRow)
                    {
                        if (visited[j]) continue;
                        q.Enqueue(j);
                        visited[j] = true;
                    }
                }
                // vertex position, face
                double[] vPos = new double[vIndex.Count * 3];
                for (int i = 0, j = 0; i < vIndex.Count; ++i)
                {
                    int idx = vIndex[i] * 3;
                    vPos[j++] = _mesh.VertexPos[idx++];
                    vPos[j++] = _mesh.VertexPos[idx++];
                    vPos[j++] = _mesh.VertexPos[idx++];
                }
                HashSet<int> fIndex = new HashSet<int>();
                foreach (int i in vIndex)
                {
                    foreach (int f in _mesh._VF[i])
                    {
                        fIndex.Add(f);
                    }
                }
                Part part = new Part(_mesh, vIndex.ToArray(), vPos, fIndex.ToArray());
                _parts.Add(part);
            }
        }// initializeParts

        private void mergeNearbyParts()
        {
            // In some meshes, though a vertex is shared across triangles, however, multiple vertices 
            // may be defined at the same position, causing inaccurate initial part clusters
            alglib.kdtree kdt;
            double[,] xy = new double[_mesh.VertexCount, 3];
            int[] tags = new int[_mesh.VertexCount];
            for (int i = 0, j = 0; i < _mesh.VertexCount; ++i)
            {
                xy[i, 0] = _mesh.VertexPos[j++];
                xy[i, 1] = _mesh.VertexPos[j++];
                xy[i, 2] = _mesh.VertexPos[j++];
                tags[i] = i;
            }
            int nx = 3;
            int ny = 0;
            int normtype = 2;
            alglib.kdtreebuildtagged(xy, tags, nx, ny, normtype, out kdt);
            while (true)
            {
                int[] labels = new int[_mesh.VertexCount];
                int l = 0;
                foreach (Part part in _parts)
                {
                    foreach (int idx in part._VERTEXINDEX)
                    {
                        labels[idx] = l;
                    }
                    ++l;
                }
                // search
                Part pa = null;
                Part pb = null;
                int k = 5;
                double[,] x = new double[0,0];
                int[] tag = new int[0];
                foreach (Part p1 in _parts)
                {
                    bool found = false;
                    foreach (int v in p1._VERTEXINDEX)
                    {
                        double[] vpos = { _mesh.VertexPos[v * 3], _mesh.VertexPos[v * 3 + 1], _mesh.VertexPos[v * 3 + 2] };
                        int res = alglib.kdtreequeryknn(kdt, vpos, k, true); // true for overlapping points
                        alglib.kdtreequeryresultsx(kdt, ref x);
                        alglib.kdtreequeryresultstags(kdt, ref tag);
                        Vector3d v1 = new Vector3d(vpos[0],vpos[1],vpos[2]);
                        for (int i = 0; i < k; ++i)
                        {
                            Part p2 = _parts[labels[tag[i]]];
                            if (p1 == p2) continue;
                            double d = (v1 - new Vector3d(x[i, 0], x[i, 1], x[i, 2])).Length();
                            if (d < Common._thresh)
                            {
                                pa = p1;
                                pb = p2;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (pa != null && pb != null)
                {
                    // group parts
                    List<Part> parts = new List<Part>();
                    parts.Add(pa);
                    parts.Add(pb);
                    groupParts(parts);
                }
                else
                {
                    break; // couldn't find any parts for grouping
                }
            }// while
        }// mergeNearbyParts

        public Part groupParts(List<Part> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return null;
            }
            List<int> vIndex = new List<int>();
            List<double> vPos = new List<double>();
            List<int> fIndex = new List<int>();
            foreach (Part part in parts)
            {
                vIndex.AddRange(part._VERTEXINDEX.ToList());
                vPos.AddRange(part._VERTEXPOS.ToList());
                fIndex.AddRange(part._FACEVERTEXINDEX.ToList());
                _parts.Remove(part);
            }
            Part newPart = new Part(_mesh, vIndex.ToArray(), vPos.ToArray(), fIndex.ToArray());
            _parts.Add(newPart);
            return newPart;
        }// group parts

        // Global get
        public int _NPARTS
        {
            get
            {
                return _parts == null ? 0 : _parts.Count;
            }
        }

        public List<Part> _PARTS
        {
            get
            {
                return _parts;
            }
        }

        public Mesh _MESH
        {
            get
            {
                return _mesh;
            }
        }
    }// Model

    public class Joint
    {
        Vector3d _point;
        Part _part1;
        Part _part2;
        public Joint() { }

        public Joint(Part p1, Part p2, Vector3d v)
        {
            _part1 = p1;
            _part2 = p2;
            _point = v;
        }
    }// Joint
}
