using System;
using System.Collections.Generic;

using Geometry;

namespace Component
{
    public class Graph
    {
        Model _model;
        List<Node> _nodes = new List<Node>();
        List<Edge> _edges = new List<Edge>();

        public Graph(Model m, bool auto)
        {
            _model = m;
            if (auto)
            {
                buildGraph();
            }
        }// Graph

        public Graph(List<Node> nodes, List<Edge> edges)
        {
            _nodes = nodes;
            _edges = edges;
        }// Graph

        public void setModel(Model m)
        {
            _model = m;
        }

        public Object Clone()
        {
            List<Node> nodes = new List<Node>();
            List<Edge> edges = new List<Edge>();
            Graph cloned = new Graph(nodes, edges);
            return cloned;
        }

        private void buildGraph()
        {
            // 
            if (_model == null || _model._NPARTS == 0)
            {
                return;
            }
            _nodes = new List<Node>();
            for (int i = 0; i < _model._NPARTS; ++i)
            {
                _nodes.Add(new Node(_model._PARTS[i], i));
            }
            double thr = 0.1;
            for (int i = 0; i < _model._NPARTS - 1; ++i)
            {
                Part ip = _model._PARTS[i];
                for (int j = i + 1; j < _model._NPARTS; ++j)
                {
                    Part jp = _model._PARTS[j];
                    // measure the relation between ip and jp
                    Vector3d contact;
                    double mind = getDistBetweenMeshes(ip._MESH, jp._MESH, out contact);
                    if (mind < thr)
                    {
                        Edge e = new Edge(_nodes[i], _nodes[j], contact);
                        _edges.Add(e);
                    }
                }
            }
        }// buildGraph

        public void addANode(Node node)
        {
            _nodes.Add(node);
        }

        private double getDistBetweenMeshes(Mesh m1, Mesh m2, out Vector3d contact)
        {
            contact = new Vector3d();
            double mind = double.MaxValue;
            Vector3d[] v1 = m1.VertexVectorArray;
            Vector3d[] v2 = m2.VertexVectorArray;
            for (int i = 0; i < v1.Length; ++i)
            {
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d = (v1[i] - v2[j]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        contact = (v1[i] + v2[j]) / 2;
                    }
                }
            }
            return mind;
        }// getDistBetweenMeshes

        public void addAnEdge(Part p1, Part p2)
        {
            int i = _model._PARTS.IndexOf(p1);
            int j = _model._PARTS.IndexOf(p2);
            if (i != -1 && j != -1)
            {
                Vector3d contact;
                double mind = getDistBetweenMeshes(p1._MESH, p2._MESH, out contact);
                Edge e = new Edge(_nodes[i], _nodes[j], contact);
                if (!isEdgeExist(e))
                {
                    addEdge(e);
                }
            }
        }// addAnEdge

        public void deleteAnEdge(Part p1, Part p2)
        {
            int i = _model._PARTS.IndexOf(p1);
            int j = _model._PARTS.IndexOf(p2);
            if (i != -1 && j != -1)
            {
                Node inode = _nodes[i];
                Node jnode = _nodes[j];
                Edge e = isEdgeExist(inode, jnode);
                if (e != null)
                {
                    deleteEdge(e);
                }
            }
        }// deleteAnEdge

        private bool isEdgeExist(Edge edge)
        {
            foreach (Edge e in _edges)
            {
                if ((e._start == edge._start && e._end == edge._end) ||
                    (e._start == edge._end && e._end == edge._start))
                {
                    return true;
                }
            }
            return false;
        }// isEdgeExist

        private Edge isEdgeExist(Node i, Node j)
        {
            foreach (Edge e in _edges)
            {
                if ((e._start == i && e._end == j) || (e._start == j && e._end == i))
                {
                    return e;
                }
            }
            return null;
        }// isEdgeExist

        private void addEdge(Edge e)
        {
            _edges.Add(e);
            e._start.addAdjNode(e._end);
            e._end.addAdjNode(e._start);
            e._start._edges.Add(e);
            e._end._edges.Add(e);
        }// addEdge

        private void deleteEdge(Edge e)
        {
            _edges.Remove(e);
            e._start._adjNodes.Remove(e._end);
            e._end._adjNodes.Remove(e._start);
            e._start._edges.Remove(e);
            e._end._edges.Remove(e);
        }// deleteEdge

        public List<Node> findReplaceableNodes(List<Node> nodes)
        {
            // nodes: from another graph
            // return: nodes that match the structure
            List<Node> res = new List<Node>();

            return res;
        }// findReplaceableNodes



        public List<Node> _NODES
        {
            get
            {
                return _nodes;
            }
        }

        public List<Edge> _EDGES
        {
            get
            {
                return _edges;
            }
        }
    }// Graph

    public class Node
    {
        Part _part;
        private int _index = -1;
        public List<Edge> _edges;
        public List<Node> _adjNodes;
        public Vector3d _pos;

        public Node(Part p, int idx)
        {
            _part = p;
            _index = idx;
            _edges = new List<Edge>();
            _adjNodes = new List<Node>();
            _pos = p._BOUNDINGBOX.CENTER;
        }

        public void addAdjNode(Node adj)
        {
            if (!_adjNodes.Contains(adj))
            {
                _adjNodes.Add(adj);
            }
        }// addAdjNode        

        public Object Clone()
        {
            Part clonePart = _part.Clone() as Part;
            Node cloned = new Node(clonePart, _index);
            return cloned;
        }// Clone

        public Part _PART
        {
            get
            {
                return _part;
            }
        }

        public int _INDEX
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }
    }// Node

    public class Edge
    {
        public Node _start;
        public Node _end;
        public Vector3d _contact;
        public Common.NodeRelationType _type;
        
        public Edge(Node a, Node b, Vector3d contact)
        {
            _start = a;
            _end = b;
            _contact = contact;
        }

        private void analyzeEdgeType()
        {
            Vector3d ax1 = _start._PART._BOUNDINGBOX.coordSys[0];
            Vector3d ax2 = _end._PART._BOUNDINGBOX.coordSys[0];
            double acos = ax1.Dot(ax2);
            double thr = Math.PI / 18;
            this._type = Common.NodeRelationType.None;
            if (Math.Abs(acos) < thr)
            {
                this._type = Common.NodeRelationType.Orthogonal;
            }
        }// analyzeEdgeType
    }// Edge
}// namespace
