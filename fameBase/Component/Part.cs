using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Geometry;
using Accord.Statistics.Analysis;

namespace Component
{
    // part based modeling
    // a part includes:
    // - bounding box info, e.g., vertices
    // - mesh
    public class Part
    {
        Mesh _mesh = null;
        Prism _boundingbox = null;
        List<Part> _jointParts = new List<Part>();
        List<Joint> _joints = new List<Joint>();
        int[] _vertexIndexInParentMesh;
        int[] _faceVIndexInParentMesh;
        double[] _vertexPosInParentMesh;
        Vector3d[] axes;

        public Color _COLOR = Color.LightBlue;

        public Part(Mesh m)
        {
            _mesh = m;

            this.fitProxy(-1);

            setRandomColorToNodes();
        }

        public Part(Mesh m, Prism p)
        {
            _mesh = m;
            _boundingbox = p;
            _boundingbox.setMaxMinScaleFromMesh(m.MaxCoord, m.MinCoord);
            _boundingbox.initInfo();
            setRandomColorToNodes();
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
            setRandomColorToNodes();
            this.fitProxy(-1);
        }

        public Part(Mesh m, Prism bbox, bool fit)
        {
            _mesh = m;
            _boundingbox = bbox;
            _COLOR = Color.FromArgb(Common.rand.Next(255), Common.rand.Next(255), Common.rand.Next(255));
            if (fit)
            {
                this.fitProxy(-1);
            }
        }

        public Object Clone()
        {
            Mesh m = _mesh.Clone() as Mesh;
            Prism prism = _boundingbox.Clone() as Prism;
            Part p = new Part(m, prism);
            p._COLOR = this._COLOR;
            return p;
        }

        public void setRandomColorToNodes()
        {
            _COLOR = Color.FromArgb(Common.rand.Next(255), Common.rand.Next(255), Common.rand.Next(255));
        }

        public void fitProxy(int option)
        {
            axes = new Vector3d[3];

            //axes[0] = Vector3d.XCoord;
            //axes[1] = Vector3d.YCoord;
            //axes[2] = Vector3d.ZCoord;
            //this.calculateAxisAlignedBbox();
            //return;

            int n = this._mesh.VertexCount;
            double[,] vArray = new double[n, 3];
            Vector3d center = new Vector3d();
            for (int i = 0, j = 0; i < n; ++i, j += 3)
            {
                vArray[i, 0] = this._mesh.VertexPos[j];
                vArray[i, 1] = this._mesh.VertexPos[j + 1];
                vArray[i, 2] = this._mesh.VertexPos[j + 2];
                center += new Vector3d(vArray[i, 0], vArray[i, 1], vArray[i, 2]);
            }
            center /= n;           

            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(vArray, AnalysisMethod.Center);
            pca.Compute();
            
            if (pca.Components.Count < 3)
            {
                // using axis aligned axes
                axes[0] = Vector3d.XCoord;
                axes[1] = Vector3d.YCoord;
                axes[2] = Vector3d.ZCoord;
                this.calculateAxisAlignedBbox();
            }
            else
            {
                axes[0] = new Vector3d(pca.Components[0].Eigenvector).normalize();
                axes[1] = new Vector3d(pca.Components[1].Eigenvector).normalize();
                axes[2] = new Vector3d(pca.Components[2].Eigenvector).normalize();
                double[,] trans = pca.Transform(this._mesh.VertexArray, 3);
                double max_x = double.MinValue;
                double max_y = double.MinValue;
                double max_z = double.MinValue;
                double min_x = double.MaxValue;
                double min_y = double.MaxValue;
                double min_z = double.MaxValue;
                for (int i = 0, j = 0; i < n; ++i, j += 3)
                {
                    // compute the min max x,y,z
                    if (max_x < trans[i, 0]) max_x = trans[i, 0];
                    if (min_x > trans[i, 0]) min_x = trans[i, 0];
                    if (max_y < trans[i, 1]) max_y = trans[i, 1];
                    if (min_y > trans[i, 1]) min_y = trans[i, 1];
                    if (max_z < trans[i, 2]) max_z = trans[i, 2];
                    if (min_z > trans[i, 2]) min_z = trans[i, 2];
                }

                // shift the center point
                double shiftX = (max_x + min_x) / 2;
                double scaleX = (max_x - min_x) / 2;
                double shiftY = (max_y + min_y) / 2;
                double scaleY = (max_y - min_y) / 2;
                double shiftZ = (max_z + min_z) / 2;
                double scaleZ = (max_z - min_z) / 2;

                center += (axes[0] * shiftX + axes[1] * shiftY + axes[2] * shiftZ);
                Vector3d scale = new Vector3d(scaleX, scaleY, scaleZ);

                Prism cuboid = fitCuboid(center, scale, axes);
                Prism cylinder = fitCylinder(center, scale, axes);
                //_boundingbox = cuboid;
                if (option == -1)
                {
                    if (cuboid.fittingError >= cylinder.fittingError && (cuboid.fittingError - cylinder.fittingError) / Math.Abs(cylinder.fittingError) > 0.2)
                    {
                        _boundingbox = cylinder;
                    }
                    else
                    {
                        _boundingbox = cuboid;
                    }
                }
                else
                {
                    if (option == 0)
                    {
                        _boundingbox = cuboid;
                    }
                    else if (option == 1)
                    {
                        _boundingbox = cylinder;
                    }
                }
            }
        }// calPrincipalAxes

        public Prism fitCuboid(Vector3d center, Vector3d scale, Vector3d[] axes)
        {
            ConvexHull hull = new ConvexHull(this._mesh);
            // compute box fitting error
            double boxVolume = scale.x * scale.y * scale.z * 8; // scale.x * 2, etc.
            double boxFitError = 1 - hull.Volume / boxVolume;

            if (axes[2].Dot(axes[0].Cross(axes[1])) < 0)
                axes[2] = (new Vector3d() - axes[2]).normalize();

            Prism proxy = Prism.CreateCuboid(new Vector3d(), new Vector3d(1, 1, 1));

            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(scale.x, scale.y, scale.z);

            Matrix3d r = new Matrix3d(axes[0], axes[1], axes[2]);
            Matrix4d R = new Matrix4d(r);
            R[3, 3] = 1.0;

            proxy.setMaxMinScaleFromMesh(_mesh.MaxCoord, _mesh.MinCoord);
            proxy.Transform(T * R * S);
            proxy.coordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy.originCoordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy._originScale = proxy._scale = scale;
            proxy.fittingError = boxFitError;
            proxy.type = Common.PrimType.Cuboid;
            proxy.updateOrigin();
            return proxy;
        }// FitCuboid

        public Prism fitCylinder(Vector3d center, Vector3d scale, Vector3d[] axes)
        {
            ConvexHull hull = new ConvexHull(this._mesh);
            // compute cylinder fitting error, iterate through each axis
            int whichaxis = 0;
            double minCylFitError = 1e8;
            double idealClyRadius = 0, clyAxisLen = 0;
            for (int ax = 0; ax < 3; ++ax)
            {
                int i = (ax + 1) % 3;
                int j = (ax + 2) % 3;
                double cylRadius = Math.Max(scale[i], scale[j]);

                // volume
                double cylVolume = cylRadius * cylRadius * Math.PI * scale[ax] * 2;
                double cylFitError = 1 - hull.Volume / cylVolume;
                if (minCylFitError > cylFitError)
                {
                    minCylFitError = cylFitError;
                    whichaxis = ax;
                    idealClyRadius = cylRadius;
                    clyAxisLen = scale[ax];
                }
            }
            Prism proxy = Prism.CreateCylinder(20); // upright unit-scale cylinder
            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(idealClyRadius, idealClyRadius, clyAxisLen);
            Matrix4d R = Matrix4d.IdentityMatrix();
            Vector3d zaxis = new Vector3d(0, 0, 1);
            Vector3d rot_axis = zaxis.Cross(axes[whichaxis]).normalize();
            if (!double.IsNaN(rot_axis.x))
            {
                double angle = Math.Acos(zaxis.Dot(axes[whichaxis]));
                R = Matrix4d.RotationMatrix(rot_axis, angle);
            }

            proxy.setMaxMinScaleFromMesh(_mesh.MaxCoord, _mesh.MinCoord);
            proxy.Transform(T * R * S);
            proxy.coordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy.originCoordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy._originScale = proxy._scale = scale;
            proxy.fittingError = minCylFitError;
            proxy.type = Common.PrimType.Cylinder;
            proxy.updateOrigin();
            return proxy;
        }// FitCylinder

        public void Transform(Matrix4d T)
        {
            _mesh.Transform(T);
            _boundingbox.setMaxMinScaleFromMesh(_mesh.MaxCoord, _mesh.MinCoord);
            _boundingbox.Transform(T);
        }

        public void TransformFromOrigin(Matrix4d T)
        {
            _mesh.TransformFromOrigin(T);
            _boundingbox.TransformFromOrigin(T);
        }

        public void updateOriginPos()
        {
            _mesh.updateOriginPos();
            _boundingbox.updateOrigin();
        }

        public void calculateAxisAlignedBbox()
        {
            if (_mesh == null)
            {
                return;
            }
            Vector3d maxv = _mesh.MaxCoord;
            Vector3d minv = _mesh.MinCoord;
            _boundingbox = new Prism(minv, maxv);

            _boundingbox.coordSys = new CoordinateSystem((_mesh.MaxCoord + _mesh.MinCoord)/2, axes[0], axes[1], axes[2]);
            _boundingbox.originCoordSys = new CoordinateSystem((_mesh.MaxCoord + _mesh.MinCoord) / 2, axes[0], axes[1], axes[2]);
            _boundingbox._originScale = _boundingbox._scale = _mesh.MaxCoord - _mesh.MinCoord;
        }

        public void addAJoint(Part p, Joint j)
        {
            if (!_jointParts.Contains(p))
            {
                _jointParts.Add(p);
            }
            _joints.Add(j);
        }

        public Mesh _MESH
        {
            get
            {
                return _mesh;
            }
        }

        public Prism _BOUNDINGBOX
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

    }// Part

    public class Model
    {
        List<Part> _parts;
        Mesh _mesh; // the whole mesh
        public Graph _GRAPH;

        public string _path = "";
        public string _model_name = "";

        public Model()
        {
            _parts = new List<Part>();
        }

        public Model(List<Part> parts)
        {
            _parts = parts;
        }

        public Model(Mesh mesh)
        {
            _mesh = mesh;
            this.initializeParts();
            //this.mergeNearbyParts();
        }

        public void initialize()
        {
            // process a new model without loaded graph info
            unify();
            initializeGraph();
        }

        public void initializeGraph()
        {
            if (_GRAPH == null)
            {
                _GRAPH = new Graph(_parts);
            }
        }

        public void setGraph(Graph g)
        {
            _GRAPH = g;
        }

        public void unify()
        {
            Vector3d maxCoord = Vector3d.MinCoord;
            Vector3d minCoord = Vector3d.MaxCoord;
            foreach (Part p in _parts)
            {
                maxCoord = Vector3d.Max(maxCoord, p._MESH.MaxCoord);
                minCoord = Vector3d.Min(minCoord, p._MESH.MinCoord);
            }
            Vector3d scale = maxCoord - minCoord;
            double maxS = scale.x > scale.y ? scale.x : scale.y;
            maxS = maxS > scale.z ? maxS : scale.z;
            maxS = 1.0 / maxS;
            Vector3d center = (maxCoord + minCoord) / 2;
            center.y = minCoord.y;
            center = new Vector3d() - center;
            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(new Vector3d(maxS, maxS, maxS));
            Matrix4d Q = T * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
            foreach (Part p in _parts)
            {
                p.Transform(Q);
            }
        }// unify

        public void setPart(Part p, int idx)
        {
            if (idx < 0 || idx > _parts.Count)
            {
                return;
            }
            _parts[idx] = p;
        }

        public void replaceNodes(List<Node> oldNodes, List<Node> newNodes)
        {
            foreach (Node node in oldNodes)
            {
                _parts.Remove(node._PART);
            }
            foreach (Node node in newNodes)
            {
                node._PART.updateOriginPos();
                _parts.Add(node._PART);
            }
            // topology
            _GRAPH.replaceNodes(oldNodes, newNodes);            
        }// replaceNodes

        public Object Clone()
        {
            List<Part> parts = new List<Part>();
            foreach (Part p in _parts)
            {
                parts.Add(p.Clone() as Part);
            }
            Model m = new Model(parts);
            m._GRAPH = _GRAPH.Clone(parts) as Graph;
            return m;
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
                if (_mesh.VertexCount < k)
                {
                    return;
                }
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
            if (parts[0]._VERTEXINDEX == null)
            {
                return groupPartsSeparately(parts);
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

        public Part groupPartsSeparately(List<Part> parts)
        {
            List<double> vPos = new List<double>();
            List<int> fIndex = new List<int>();
            // merge meshes
            int start = 0;
            foreach (Part part in parts)
            {
                Mesh m = part._MESH;
                vPos.AddRange(m.VertexPos.ToList());
                for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
                {
                    fIndex.Add(start + m.FaceVertexIndex[j]);
                    fIndex.Add(start + m.FaceVertexIndex[j + 1]);
                    fIndex.Add(start + m.FaceVertexIndex[j + 2]);
                }
                start += m.VertexCount;
                _parts.Remove(part);
            }
            Mesh merged = new Mesh(vPos.ToArray(), fIndex.ToArray());
            Part newPart = new Part(merged);
            _parts.Add(newPart);
            return newPart;
        }// group parts

        public void addAPart(Part p)
        {
            _parts.Add(p);
        }

        public void removeAPart(Part p)
        {
            _parts.Remove(p);
        }

        public void setMesh(Mesh m)
        {
            _mesh = m;
        }

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
