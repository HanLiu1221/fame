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
        // test
        public List<Node> selectedNodes;

        public Graph() { }

        public Graph(Model m, bool auto)
        {
            _model = m;
            if (auto)
            {
                buildGraph();
            }
            markGroundTouchingNodes();
        }// Graph

        public void setModel(Model m)
        {
            _model = m;
        }

        public Object Clone()
        {
            List<Part> parts = new List<Part>();
            Graph cloned = new Graph();
            for (int i = 0; i < _model._NPARTS; ++i)
            {
                Node cn = _nodes[i].Clone() as Node;
                cloned.addANode(cn);
                parts.Add(cn._PART);
            }
            foreach (Edge e in _edges)
            {
                int i = e._start._INDEX;
                int j = e._end._INDEX;
                Edge ec = new Edge(cloned._nodes[i], cloned._nodes[j], new Vector3d(e._contact));
                cloned.addEdge(ec);
            }
            Model m = new Model(parts);
            cloned.setModel(m);
            return cloned;
        }

        public void replaceNodes(List<Node> oldNodes, List<Node> newNodes)
        {
            foreach (Node old in oldNodes)
            {
                _nodes.Remove(old);
                _model.removeAPart(old._PART);
            }
            foreach (Node node in newNodes)
            {
                _nodes.Add(node);
                _model.addAPart(node._PART);
            }
        }// replaceNodes

        private void markGroundTouchingNodes()
        {
            foreach (Node node in _nodes)
            {
                double ydist = node._PART._MESH.MinCoord.y;
                if (Math.Abs(ydist) < Common._thresh)
                {
                    node._isGroundTouching = true;
                }
            }
        }// markGroundTouchingNodes

        public void updateANodeMesh(Part p, int idx)
        {
            if (idx < 0 || idx > _nodes.Count)
            {
                return;
            }
            _nodes[idx].setPart(p);
            _model.setPart(p, idx);
        }// updateANode

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
            for (int i = 0; i < _model._NPARTS - 1; ++i)
            {
                Part ip = _model._PARTS[i];
                for (int j = i + 1; j < _model._NPARTS; ++j)
                {
                    Part jp = _model._PARTS[j];
                    // measure the relation between ip and jp
                    Vector3d contact;
                    double mind = getDistBetweenMeshes(ip._MESH, jp._MESH, out contact);
                    if (mind < Common._thresh)
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

        public List<Edge> collectOutgoingEdges(List<Node> nodes)
        {
            // for the substructure, find out the edges that are to be connected
            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Edge e in node._edges)
                {
                    if (edges.Contains(e))
                    {
                        continue;
                    }
                    Node other = e._start == node ? e._end : e._start;
                    if (!nodes.Contains(other))
                    {
                        edges.Add(e);
                    }
                }
            }
            return edges;
        }// collectOutgoingEdges

        public void resetUpdateStatus()
        {
            foreach (Node node in _nodes)
            {
                node.updated = false;
                node._allNeigborUpdated = false;
            }
            foreach (Edge e in _edges)
            {
                e._contactUpdated = false;
            }
        }// resetEdgeContactStatus

        public Model _MODEL
        {
            get
            {
                return _model;
            }
        }
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
        public bool _isGroundTouching = false;
        public bool updated = false;
        public bool _allNeigborUpdated = false;

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
            Part p = _part.Clone() as Part;
            Node cloned = new Node(p, _index);
            cloned._isGroundTouching = _isGroundTouching;
            return cloned;
        }// Clone

        public void Transform(Matrix4d T)
        {
            _part.Transform(T);
            _pos = _part._BOUNDINGBOX.CENTER;
        }

        public void setPart(Part p)
        {
            _part = p;
            _pos = p._BOUNDINGBOX.CENTER;
        }

        public bool isAllNeighborsUpdated()
        {
            if (_allNeigborUpdated)
            {
                return true;
            }
            foreach (Node node in _adjNodes)
            {
                if (!node.updated)
                {
                    return false;
                }
            }
            _allNeigborUpdated = true;
            return true;
        }// isAllNeighborsUpdated

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
        public Vector3d _originContact;
        public Vector3d _contact;
        public bool _contactUpdated = false;
        public Common.NodeRelationType _type;
        
        public Edge(Node a, Node b, Vector3d contact)
        {
            _start = a;
            _end = b;
            _originContact = new Vector3d(contact);
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

        public void TransformContact(Matrix4d T)
        {
            _originContact = new Vector3d(_contact);
            _contact = (T * new Vector4d(_contact, 1)).ToVector3D();
            _contactUpdated = true;
        }


    }// Edge
}// namespace
