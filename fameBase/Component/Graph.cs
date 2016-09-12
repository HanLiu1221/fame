using System;
using System.Collections.Generic;

using Geometry;

namespace Component
{
    /* topological information of a model */
    public class Graph
    {
        List<Node> _nodes = new List<Node>();
        List<Edge> _edges = new List<Edge>();
        public int _NNodes = 0;
        public int _NEdges = 0;

        double _maxNodeBboxScale; // max scale of a box
        double _minNodeBboxScale; // min scale of a box
        double _maxAdjNodesDist; // max distance between two nodes
        // test
        public List<Node> selectedNodes;

        public Graph() { }

        public Graph(List<Part> parts)
        {
            _nodes = new List<Node>();
            _NNodes = parts.Count;
            for (int i = 0; i < _NNodes; ++i)
            {
                _nodes.Add(new Node(parts[i], i));
            }
            markGroundTouchingNodes();
            buildGraph();
        }

        public Object Clone(List<Part> parts)
        {
            Graph cloned = new Graph();
            cloned._NNodes = parts.Count;
            for (int i = 0; i < _NNodes; ++i)
            {
                Node cn = _nodes[i].Clone(parts[i]) as Node;
                cloned.addANode(cn);
            }
            for (int i = 0; i < _nodes.Count; ++i)
            {
                if (_nodes[i].symmetry != null)
                {
                    int idx = _nodes[i].symmetry._INDEX;
                    if (idx > i)
                    {
                        cloned.markSymmtry(cloned._nodes[i], cloned._nodes[idx]);
                    }
                }
            }
            foreach (Edge e in _edges)
            {
                int i = e._start._INDEX;
                int j = e._end._INDEX;
                List<Contact> contacts = new List<Contact>();
                foreach (Contact pnt in e._contacts)
                {
                    contacts.Add(pnt.Clone() as Contact);
                }
                Edge ec = new Edge(cloned._nodes[i], cloned._nodes[j], contacts);
                cloned.addEdge(ec);
            }
            cloned._NNodes = cloned._nodes.Count;
            cloned._NEdges = cloned._edges.Count;
            cloned._maxAdjNodesDist = _maxAdjNodesDist;
            cloned._minNodeBboxScale = _minNodeBboxScale;
            return cloned;
        }// clone

        public void analyzeScale()
        {
            double[] vals = calScale();
            _maxAdjNodesDist = vals[0];
            _minNodeBboxScale = vals[1];
            _maxNodeBboxScale = vals[2];
        }

        private double[] calScale()
        {
            double[] vals = new double[3];
            double maxd = double.MinValue;
            foreach (Edge e in _edges)
            {
                Node n1 = e._start;
                Node n2 = e._end;
                Vector3d contact;
                double dist = getDistBetweenMeshes(n1._PART._MESH, n2._PART._MESH, out contact);
                if (dist > maxd)
                {
                    maxd = dist;
                }
            }
            double minScale = double.MaxValue;
            double maxScale = double.MinValue;
            foreach (Node node in _nodes)
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (minScale > node._PART._BOUNDINGBOX._scale[i])
                    {
                        minScale = node._PART._BOUNDINGBOX._scale[i];
                    }
                    if (maxScale < node._PART._BOUNDINGBOX._scale[i])
                    {
                        maxScale = node._PART._BOUNDINGBOX._scale[i];
                    }
                }
            }
            vals[0] = maxd;
            vals[1] = minScale;
            vals[2] = maxScale;
            return vals;
        }// calScale

        public void replaceNodes(List<Node> oldNodes, List<Node> newNodes)
        {
            foreach (Node old in oldNodes)
            {
                _nodes.Remove(old);
            }
            foreach (Node node in newNodes)
            {
                _nodes.Add(node);
            }
            _NNodes = _nodes.Count;
            // update edges

        }// replaceNodes

        private void updateNodeIndex()
        {
            int idx = 0;
            foreach (Node node in _nodes)
            {
                node._INDEX = idx++;
            }
        }// updateNodeIndex

        public void markGroundTouchingNodes()
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

        private void buildGraph()
        {
            if (_NNodes == 0)
            {
                return;
            }
            for (int i = 0; i < _NNodes - 1; ++i)
            {
                Part ip = _nodes[i]._PART;
                for (int j = i + 1; j < _NNodes; ++j)
                {
                    Part jp = _nodes[j]._PART;
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
            _NNodes = _nodes.Count;
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

        public void addAnEdge(Node n1, Node n2)
        {
            Vector3d contact;
            double mind = getDistBetweenMeshes(n1._PART._MESH, n2._PART._MESH, out contact);
            Edge e = new Edge(n1, n2, contact);
            if (!isEdgeExist(e))
            {
                addEdge(e);
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void addAnEdge(Node n1, Node n2, Vector3d c)
        {
            Edge e = new Edge(n1, n2, c);
            if (!isEdgeExist(e))
            {
                addEdge(e);
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void addAnEdge(Node n1, Node n2, List<Contact> contacts)
        {
            Edge e = new Edge(n1, n2, contacts);
            if (!isEdgeExist(e))
            {
                addEdge(e);
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void deleteAnEdge(Node n1, Node n2)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e != null)
            {
                deleteEdge(e);
            }
            _NEdges = _edges.Count;
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

        public Edge isEdgeExist(Node i, Node j)
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
            _NEdges = _edges.Count;
        }// addEdge

        private void deleteEdge(Edge e)
        {
            _edges.Remove(e);
            e._start._adjNodes.Remove(e._end);
            e._end._adjNodes.Remove(e._start);
            e._start._edges.Remove(e);
            e._end._edges.Remove(e);
            _NEdges = _edges.Count;
        }// deleteEdge

        public void markSymmtry(Node a, Node b)
        {
            a.symmetry = b;
            b.symmetry = a;

            Vector3d symm_center = (a._pos + b._pos) / 2;
            Vector3d symm_axis = (a._pos - b._pos).normalize();
            Symmetry symm = new Symmetry(symm_center, symm_axis);

            a.symm = symm;
            b.symm = symm;
        }// markSymmtry

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

        public List<Node> getGroundTouchingNodes()
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node._isGroundTouching)
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// getGroundTouchingNodes

        private Node getKeyNodes()
        {
            int nMaxConn = 0;
            Node key = null;
            foreach (Node node in _nodes)
            {
                if (node._edges.Count > nMaxConn)
                {
                    nMaxConn = node._edges.Count;
                    key = node;
                }
            }
            return key;
        }// getKeyNodes

        public List<List<Node>> splitAlongKeyNode()
        {
            List<List<Node>> splitNodes = new List<List<Node>>();
            // key node(s)
            Node key = getKeyNodes();
            List<Node> keyNodes = new List<Node>();
            keyNodes.Add(key);
            bool[] added = new bool[_NNodes];
            added[key._INDEX] = true;
            if (key.symmetry != null)
            {
                keyNodes.Add(key.symmetry);
                added[key.symmetry._INDEX] = true;
            }
            splitNodes.Add(keyNodes);
            // split 1
            List<Node> split1 = new List<Node>();
            // dfs
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(key._adjNodes[0]);
            added[key._adjNodes[0]._INDEX] = true;
            bool containGround1 = false;
            while (queue.Count > 0)
            {
                Node cur = queue.Dequeue();
                split1.Add(cur);
                if (!containGround1 && cur._isGroundTouching)
                {
                    containGround1 = true;
                }
                foreach (Node node in cur._adjNodes)
                {
                    if (!added[node._INDEX])
                    {
                        queue.Enqueue(node);
                        added[node._INDEX] = true;
                    }
                }
            }
            
            List<Node> split2 = new List<Node>();
            bool containGround2 = false;
            foreach (Node node in _nodes)
            {
                if (!added[node._INDEX])
                {
                    split2.Add(node);
                    if (!containGround2 && node._isGroundTouching)
                    {
                        containGround2 = true;
                    }
                }
            }
            bool add_split1_first = true;
            if (containGround1 && !containGround2) {
                add_split1_first = true;
            } else if (!containGround1 && containGround2)
            {
                add_split1_first = false;
            }
            else if (collectOutgoingEdges(split2).Count > collectOutgoingEdges(split1).Count)
            {
                add_split1_first = false;
            }

            if (add_split1_first)
            {
                splitNodes.Add(split1);
                if (split2.Count > 0)
                {
                    splitNodes.Add(split2);
                }
            }
            else
            {
                if (split2.Count > 0)
                {
                    splitNodes.Add(split2);
                }
                splitNodes.Add(split1);
            }
            return splitNodes;
        }// splitAlongKeyNode

        public List<List<Node>> getSymmetryPairs()
        {
            List<List<Node>> symPairs = new List<List<Node>>();
            for (int i = 0; i < _NNodes; ++i)
            {
                if (_nodes[i].symmetry != null && _nodes[i]._INDEX < _nodes[i].symmetry._INDEX)
                {
                    List<Node> syms = new List<Node>();
                    syms.Add(_nodes[i]);
                    syms.Add(_nodes[i].symmetry);
                    symPairs.Add(syms);
                }
            }
            return symPairs;
        }// getSymmetryPairs

        public bool isGeometryViolated()
        {
            // geometry filter
            double[] vals = calScale();
            double thr = 2.2;
            if (vals[0] > _maxAdjNodesDist * thr || vals[1] < _minNodeBboxScale / thr || vals[2] > _maxNodeBboxScale * thr)
            {
                return true;
            }
            return false;
        }// isGeometryViolated

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
        public Node symmetry = null;
        public Symmetry symm = null;

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

        public Object Clone(Part p)
        {
            Node cloned = new Node(p, _index);
            cloned._isGroundTouching = _isGroundTouching;
            return cloned;
        }// Clone

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
        public List<Contact> _contacts;
        public bool _contactUpdated = false;
        public Common.NodeRelationType _type;
        
        public Edge(Node a, Node b, Vector3d c)
        {
            _start = a;
            _end = b;
            _contacts = new List<Contact>();
            _contacts.Add(new Contact(c));
        }

        public Edge(Node a, Node b, List<Contact> contacts)
        {
            _start = a;
            _end = b;
            _contacts = contacts;            
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
            foreach (Contact p in _contacts)
            {
                p.TransformFromOrigin(T);
                p.updateOrigin();
            }            
            _contactUpdated = true;
        }

        public List<Vector3d> getOriginContactPoints()
        {
            List<Vector3d> pnts = new List<Vector3d>();
            foreach (Contact p in _contacts)
            {
                pnts.Add(p._originPos3d);
            }
            return pnts;
        }// getOriginContactPoints

        public List<Vector3d> getContactPoints()
        {
            List<Vector3d> pnts = new List<Vector3d>();
            foreach (Contact p in _contacts)
            {
                pnts.Add(p._pos3d);
            }
            return pnts;
        }// getContactPoints
    }// Edge

    public class Symmetry
    {
        public Vector3d _center;
        public Vector3d _axis;
        public Symmetry(Vector3d c, Vector3d a)
        {
            _center = c;
            _axis = a;
        }
    }// Symmetry
}// namespace
