using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometry;

namespace Component
{
    // part based modeling
    // a part includes:
    // - bounding box info, e.g., vertices
    // - mesh
    class Part
    {
        int _idx = -1;
        Mesh _mesh = null;
        Prim _boundingbox = null;
        Color _color = Color.Black;

        public Part(Mesh m)
        {
            _mesh = m;
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
    }

    public class Model
    {
        List<Part> _parts;

        public Model(Mesh m, Prim bbox)
        {
            // read models
        }
    }
}
