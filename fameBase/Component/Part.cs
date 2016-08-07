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
        Color _color = Color.LightBlue;
        List<Part> _jointParts = new List<Part>();
        List<Joint> _joints = new List<Joint>();

        public Part(Mesh m)
        {
            _mesh = m;
        }

        public Part(Mesh m, Prim bbox)
        {
            _mesh = m;
            _boundingbox = bbox;
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

        private void calculateBbox()
        {
            if (_mesh == null)
            {
                return;
            }
            Vector3d maxv = Vector3d.MaxCoord;
            Vector3d minv = Vector3d.MinCoord;
            for (int i = 0, j = 0; i < _mesh.VertexCount; ++i)
            {
                Vector3d v = new Vector3d(_mesh.VertexPos[j++], _mesh.VertexPos[j++], _mesh.VertexPos[j]);
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
        public Model(List<Part> parts)
        {
            _parts = parts;
        }

        private void normalize()
        {
            // normalize the whole model to a unit box for cross-model analysis
            Vector3d minCoord = Vector3d.MaxCoord;
            Vector3d maxCoord = Vector3d.MinCoord;
            foreach (Part part in _parts)
            {
                minCoord = Vector3d.Min(minCoord, part._BOUNDINGBOX.MinCoord);

            }
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
        }

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
        }

        private void initializeParts()
        {
            // find parts by measuring point distance
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
