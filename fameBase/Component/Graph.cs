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
        
        public List<Node> selectedNodes = new List<Node>();
        private List<Common.Functionality> _origin_funcs = new List<Common.Functionality>();

        // test
        public List<List<Node>> selectedNodePairs = new List<List<Node>>();

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

        public List<Common.Functionality> getGraphFuncs()
        {
            List<Common.Functionality> funcs = new List<Common.Functionality>();
            foreach (Node node in _nodes)
            {
                foreach (Common.Functionality f in node._funcs)
                {
                    if (!funcs.Contains(f))
                    {
                        funcs.Add(f);
                    }
                }
            }
            return funcs;
        }// getGraphFuncs

        public Object Clone(List<Part> parts)
        {
            if (_nodes.Count != parts.Count)
            {
                return null;
            }
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
                    contacts.Add(new Contact(pnt._pos3d));
                }
                Edge ec = new Edge(cloned._nodes[i], cloned._nodes[j], contacts);
                cloned.addEdge(ec);
            }
            cloned._NNodes = cloned._nodes.Count;
            cloned._NEdges = cloned._edges.Count;
            cloned._maxAdjNodesDist = _maxAdjNodesDist;
            cloned._minNodeBboxScale = _minNodeBboxScale;
            cloned._maxNodeBboxScale = _maxNodeBboxScale;
            cloned._origin_funcs = new List<Common.Functionality>(_origin_funcs);
            return cloned;
        }// clone

        public void analyzeOriginFeatures()
        {
            double[] vals = calScale();
            _maxAdjNodesDist = vals[0];
            _minNodeBboxScale = vals[1];
            _maxNodeBboxScale = vals[2];
            _origin_funcs = this.getGraphFuncs();
        }// analyzeOriginFeatures

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

        public List<Node> getNodesByFunctionality(Common.Functionality func)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node._funcs.Contains(func))
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// getNodesByFunctionality

        public List<Node> getNodesByFunctionality(List<Common.Functionality> funcs)
        {
            // sample functional parts (connected!)
            List<Node> nodes = new List<Node>();
            foreach (Common.Functionality f in funcs)
            {
                List<Node> cur = getNodesByFunctionality(f);
                foreach (Node node in cur)
                {
                    if (!nodes.Contains(node))
                    {
                        nodes.Add(node);
                    }
                }
            }
            return nodes;
        }// getNodesByFunctionality

        public void replaceNodes(List<Node> oldNodes, List<Node> newNodes)
        {
            // replace the old nodes from this graph by newNodes
            // UPDATE nodes first            
            foreach (Node old in oldNodes)
            {
                _nodes.Remove(old);
            }
            List<Node> nodes_in_oppo_list = new List<Node>(_nodes);
            foreach (Node node in newNodes)
            {
                _nodes.Add(node);
            }
            _NNodes = _nodes.Count;
            resetNodeIndex();

            // UPDATE edges
            // 1.1 remove inner edges from oldNodes
            List<Edge> inner_edges_old = GetInnerEdges(oldNodes);
            foreach (Edge e in inner_edges_old)
            {
                this.deleteEdge(e);
            }
            
            List<Edge> out_old_edges = getOutgoingEdges(oldNodes);
            List<Edge> out_new_edges = getOutgoingEdges(newNodes);
            List<Node> out_nodes = new List<Node>();
            List<Edge> out_edges = new List<Edge>();
            if (out_new_edges.Count > out_old_edges.Count)
            {
                out_nodes = newNodes;
                out_edges = out_new_edges;
            }
            else {
                out_nodes = oldNodes;
                out_edges = out_old_edges;
                nodes_in_oppo_list = new List<Node>(newNodes);
            }
            // 1.2 remove out edges from oldNodes
            foreach (Edge e in out_old_edges)
            {
                this.deleteEdge(e);
            }
            // 2. handle edges from newNodes
            List<Edge> inner_edges_new = GetInnerEdges(newNodes);
            // 2.1 remove all edges from newNodes
            foreach (Node node in newNodes)
            {
                node._edges.Clear();
                node._adjNodes.Clear();
            }
            // 2.2 add inner edges from newNodes
            foreach (Edge e in inner_edges_new)
            {
                this.addAnEdge(e._start, e._end, e._contacts);
            }
            // 3. connect  
            if (nodes_in_oppo_list.Count > 0)
            {
                foreach (Edge e in out_edges)
                {
                    // find the nearest node depending on contacts
                    Node cur = null;
                    if (_nodes.Contains(e._start) && !_nodes.Contains(e._end))
                    {
                        cur = e._start;
                    }
                    else if (!_nodes.Contains(e._start) && _nodes.Contains(e._end))
                    {
                        cur = e._end;
                    }
                    else
                    {
                        throw new Exception();
                    }
                    foreach (Contact c in e._contacts)
                    {
                        Node closest = getNodeNearestToContact(nodes_in_oppo_list, c);
                        this.addAnEdge(cur, closest, c._pos3d);
                    }
                }
            }
            // 4. adjust contacts for new nodes
            this.resetUpdateStatus();
            foreach (Node node in newNodes)
            {
                adjustContacts(node);
            }
            this.resetUpdateStatus();
            _NEdges = _edges.Count;
        }// replaceNodes

        private void adjustContacts(Node node)
        {
            double thr = 0.1;
            foreach (Edge e in node._edges)
            {
                if (e._contactUpdated)
                {
                    continue;
                }
                Node n1 = e._start;
                Node n2 = e._end;
                foreach (Contact c in e._contacts)
                {
                    Vector3d v = this.getVertextNearToContactMeshes(n1._PART._MESH, n2._PART._MESH, c._pos3d);
                    c._originPos3d = c._pos3d = v;
                }
                // remove overlapping contacts
                Vector3d cnt;
                double min_d = this.getDistBetweenMeshes(n1._PART._MESH, n2._PART._MESH, out cnt);
                for (int i = 0; i < e._contacts.Count - 1; ++i)
                {
                    for (int j = i + 1; j < e._contacts.Count; ++j)
                    {
                        double d = (e._contacts[i]._pos3d - e._contacts[j]._pos3d).Length();
                        if (d < thr)
                        {
                            // remove either i or j
                            double di = (e._contacts[i]._pos3d - cnt).Length();
                            double dj = (e._contacts[j]._pos3d - cnt).Length();
                            if (di > dj)
                            {
                                e._contacts.RemoveAt(i);
                                --i;
                                break;
                            }
                            else
                            {
                                e._contacts.RemoveAt(j);
                                --j;
                                continue;
                            }
                        }
                    }
                }
                e._contactUpdated = true;
            }
        }// adjustContacts

        private Node getNodeNearestToContact(List<Node> nodes, Contact c)
        {
            Node res = null;
            double min_dis = double.MaxValue;
            // calculation based on meshes (would be more accurate if the vertices on the mesh is uniform)
            foreach (Node node in nodes)
            {
                Mesh mesh = node._PART._MESH;
                Vector3d[] vecs = mesh.VertexVectorArray;
                for (int i = 0; i < vecs.Length; ++i)
                {
                    double d = (vecs[i] - c._pos3d).Length();
                    if (d < min_dis)
                    {
                        min_dis = d;
                        res = node;
                    }
                }
            }
            return res;
        }// getNodeNearestToContact

        public static List<Edge> GetInnerEdges(List<Node> nodes)
        {
            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Edge edge in node._edges)
                {
                    Node other = edge._start == node ? edge._end : edge._start;
                    if (nodes.Contains(other) && !edges.Contains(edge))
                    {
                        edges.Add(edge);
                    }
                }
            }
            return edges;
        }// GetInnerEdges

        public static List<Edge> GetAllEdges(List<Node> nodes)
        {
            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Edge edge in node._edges)
                {
                    if (!edges.Contains(edge))
                    {
                        edges.Add(edge);
                    }
                }
            }
            return edges;
        }// GetInnerEdges

        public static List<Node> GetNodePropagation(List<Node> nodes)
        {
            // propagate the nodes to all inner nodes that only connect to the input #nodes#
            List<Node> inner_nodes = new List<Node>();
            inner_nodes.AddRange(nodes);
            foreach (Node node in nodes)
            {
                foreach (Node adj in node._adjNodes)
                {
                    if (inner_nodes.Contains(adj))
                    {
                        continue;
                    }
                    bool add = true;
                    foreach (Node adjadj in adj._adjNodes)
                    {
                        if (!nodes.Contains(adjadj))
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add)
                    {
                        inner_nodes.Add(adj);
                    }
                }
            }
            return inner_nodes;
        }// GetNodePropagation

        public Vector3d getGroundTouchingNodesCenter()
        {
            Vector3d center = new Vector3d();
            int n = 0;
            foreach (Node node in _nodes)
            {
                if (node._isGroundTouching)
                {
                    center += node._PART._BOUNDINGBOX.CENTER;
                    ++n;
                }
            }
            center /= n;
            center.y = 0;
            return center;
        }// getGroundTouchingNode

        public List<Node> selectFuncNodes(Common.Functionality func)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node._funcs.Contains(func) && !nodes.Contains(node))
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// selectFuncNodes

        public List<Node> selectSymmetryFuncNodes( Common.Functionality func)
        {
            List<Node> sym_nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node.symmetry != null && node._funcs.Contains(func) && !sym_nodes.Contains(node) && !sym_nodes.Contains(node.symmetry))
                {
                    sym_nodes.Add(node);
                    sym_nodes.Add(node.symmetry);
                    break;
                }
            }
            return sym_nodes;
        }// selectSymmetryFuncNodes

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
            return;
            foreach (Node node in _nodes)
            {
                double ydist = node._PART._MESH.MinCoord.y;
                if (Math.Abs(ydist) < Common._thresh)
                {
                    node._isGroundTouching = true;
                    node.addFunctionality(Common.Functionality.GROUND_TOUCHING);
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

        private Vector3d getVertextNearToContactMeshes(Mesh m1, Mesh m2, Vector3d pos)
        {
            Vector3d vertex = pos;
            double mind12 = double.MaxValue;
            double mindc = double.MaxValue;
            double thr = 0.2;
            Vector3d[] v1 = m1.VertexVectorArray;
            Vector3d[] v2 = m2.VertexVectorArray;
            for (int i = 0; i < v1.Length; ++i)
            {
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d_v1_v2 = (v1[i] - v2[j]).Length();
                    Vector3d v0 = (v1[i] + v2[j]) / 2;
                    double dc = (v0 - pos).Length();
                    if (d_v1_v2 < mind12 && dc < thr)
                    {
                        mind12 = d_v1_v2;
                        mindc = dc;
                        vertex = v0;
                    }
                }
            }
            return vertex;
        }// getVertextNearToContactMeshes

        public void addAnEdge(Node n1, Node n2)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e == null)
            {
                Vector3d contact;
                double mind = getDistBetweenMeshes(n1._PART._MESH, n2._PART._MESH, out contact);
                e = new Edge(n1, n2, contact);
                addEdge(e);
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void addAnEdge(Node n1, Node n2, Vector3d c)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e == null)
            {
                e = new Edge(n1, n2, c);
                addEdge(e);
            }
            else if (e._contacts.Count < Common._max_edge_contacts)
            {
                e._contacts.Add(new Contact(c));
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void addAnEdge(Node n1, Node n2, List<Contact> contacts)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e == null)
            {
                e = new Edge(n1, n2, contacts);
                addEdge(e);
            }
            else
            {
                e._contacts = contacts;
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

        private void addEdge(Edge e)
        {
            _edges.Add(e);
            e._start.addAnEdge(e);
            e._end.addAnEdge(e);
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

        public List<Edge> getOutgoingEdges(List<Node> nodes)
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
        }// getOutgoingEdges

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

        public Node getKeyNode()
        {
            int nMaxConn = 0;
            Node key = null;
            foreach (Node node in _nodes)
            {
                if (node._edges.Count > nMaxConn)
                    //&& (node._funcs.Contains(Common.Functionality.HAND_PLACE) || node._funcs.Contains(Common.Functionality.HUMAN_HIP)))
                {
                    nMaxConn = node._edges.Count;
                    key = node;
                }
            }
            return key;
        }// getKeyNode

        private List<Node> getKeyNodes()
        {
            List<Node> keys = new List<Node>();
            int max_funcs = 0;
            Node key = null;
            foreach (Node node in _nodes)
            {
                if (node._funcs.Count > max_funcs)
                {
                    max_funcs = node._funcs.Count;
                    key = node;
                }
            }
            if (key != null && !keys.Contains(key))
            {
                keys.Add(key);
                if (key.symmetry != null && !keys.Contains(key.symmetry))
                {
                    keys.Add(key.symmetry);
                }
            }
            if (keys.Count == 0)
            {
                key = getKeyNode();
                keys.Add(key);
                if (key.symmetry != null)
                {
                    keys.Add(key.symmetry);
                }
            }
            return keys;
        }// getKeyNodes

        public List<List<Node>> splitAlongKeyNode()
        {
            List<List<Node>> splitNodes = new List<List<Node>>();
            // key node(s)
            List<Node> keyNodes = getKeyNodes();
            bool[] added = new bool[_NNodes];
            foreach (Node node in keyNodes)
            {
                added[node._INDEX] = true;
            }
            splitNodes.Add(keyNodes);
            // split 1
            List<Node> split1 = new List<Node>();
            // dfs
            Queue<Node> queue = new Queue<Node>();
            // put an arbitrary not-visited node
            foreach (Node node in keyNodes)
            {
                foreach (Node adj in node._adjNodes)
                {
                    if (!added[adj._INDEX])
                    {
                        queue.Enqueue(adj);
                        added[adj._INDEX] = true;
                        if (adj.symmetry != null)
                        {
                            queue.Enqueue(adj.symmetry);
                            added[adj.symmetry._INDEX] = true;
                        }
                        break;
                    }
                }
                if (queue.Count > 0)
                {
                    break;
                }
            }            
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
                        if (node.symmetry != null)
                        {
                            queue.Enqueue(node.symmetry);
                            added[node.symmetry._INDEX] = true;
                        }
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
            else if (getOutgoingEdges(split2).Count > getOutgoingEdges(split1).Count)
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

        public bool isViolateOrigin()
        {
            // geometry filter
            double[] vals = calScale();
            double max_adj_nodes_dist = Math.Max(_maxAdjNodesDist, 0.1);
            double min_box_scale = Math.Min(_minNodeBboxScale, Common._min_scale);
            double max_box_scale = Math.Max(_maxNodeBboxScale, Common._max_scale);
            // max scale is not reliable, since a large node may replace many small nodes
            if (vals[0] > max_adj_nodes_dist || vals[1] < min_box_scale || vals[2] > max_box_scale)
            {
                return true;
            }
            foreach (Node node in _nodes)
            {
                if (node._PART._BOUNDINGBOX.MinCoord.y < Common._minus_thresh)
                {
                    return true;
                }
            }
            List<Common.Functionality> funs = this.getGraphFuncs();
            foreach (Common.Functionality f in _origin_funcs)
            {
                if (!funs.Contains(f))
                {
                    return true;
                }
            }

            return false;
        }// isViolateOrigin

        private void resetNodeIndex()
        {
            int i = 0;
            foreach (Node node in _nodes)
            {
                node._INDEX = i++;
            }
        }// resetNodeIndex

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
                foreach (Contact c in e._contacts)
                {
                    c.updateOrigin();
                }
            }
        }// resetEdgeContactStatus

        public void unify()
        {
            Vector3d maxCoord = Vector3d.MinCoord;
            Vector3d minCoord = Vector3d.MaxCoord;
            foreach (Node node in _nodes)
            {
                maxCoord = Vector3d.Max(maxCoord, node._PART._MESH.MaxCoord);
                minCoord = Vector3d.Min(minCoord, node._PART._MESH.MinCoord);
            }
            Vector3d scale = maxCoord - minCoord;
            double maxS = scale.x > scale.y ? scale.x : scale.y;
            maxS = maxS > scale.z ? maxS : scale.z;
            maxS = 1.0 / maxS;
            Vector3d center = (maxCoord + minCoord) / 2;
            center = new Vector3d() - center;
            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(new Vector3d(maxS, maxS, maxS));
            Matrix4d Q = T * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
            this.transformAll(Q);
            // y == 0
            minCoord = Vector3d.MaxCoord;
            foreach (Node node in _nodes)
            {
                minCoord = Vector3d.Min(minCoord, node._PART._MESH.MinCoord);
            }
            if (Math.Abs(minCoord.y) > Common._thresh)
            {
                Vector3d t = new Vector3d();
                t.y = -minCoord.y;
                T = Matrix4d.TranslationMatrix(t);
                this.transformAll(T);
            }
        }// unify

        private void transformAll(Matrix4d T)
        {
            foreach (Node node in _nodes)
            {
                node.Transform(T);
            }
            foreach (Edge edge in _edges)
            {
                if (!edge._contactUpdated)
                {
                    edge.TransformContact(T);
                }
            }
            resetUpdateStatus();
        }// transformAll

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
        public List<Common.Functionality> _funcs = new List<Common.Functionality>();

        public Node(Part p, int idx)
        {
            _part = p;
            _index = idx;
            _edges = new List<Edge>();
            _adjNodes = new List<Node>();
            _pos = p._BOUNDINGBOX.CENTER;
        }

        public void addAnEdge(Edge e)
        {
            Node adj = e._start == this ? e._end : e._start;
            if (!_adjNodes.Contains(adj))
            {
                _adjNodes.Add(adj);
            }
            Edge edge = getEdge(adj);
            if (edge == null)
            {
                _edges.Add(e);
            }
            else
            {
                edge._contacts = e._contacts;
            }
        }// addAnEdge  

        public void deleteAnEdge(Edge e)
        {
            _edges.Remove(e);
            Node other = e._start == this ? e._end : e._start;
            _adjNodes.Remove(other);
        }// deleteAnEdge

        private Edge getEdge(Node adj)
        {
            foreach (Edge e in _edges)
            {
                if (e._start == adj || e._end == adj)
                {
                    return e;
                }
            }
            return null;
        }// getEdge

        public void addFunctionality(Common.Functionality func)
        {
            if (!_funcs.Contains(func))
            {
                _funcs.Add(func);
            }
            if (func == Common.Functionality.GROUND_TOUCHING)
            {
                _isGroundTouching = true;
            }
        }// addFunctionality

        public void removeAllFuncs()
        {
            _funcs.Clear();
        }// removeAllFuncs

        public Object Clone(Part p)
        {
            Node cloned = new Node(p, _index);
            cloned._isGroundTouching = _isGroundTouching;
            cloned._funcs = new List<Common.Functionality>(_funcs);
            return cloned;
        }// Clone

        public Object Clone()
        {
            Part p = _part.Clone() as Part;
            Node cloned = new Node(p, _index);
            cloned._isGroundTouching = _isGroundTouching;
            cloned._funcs = new List<Common.Functionality>(_funcs);
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

    public class ReplaceablePair
    {
        public Graph _g1;
        public Graph _g2;
        public List<List<Node>> _pair1;
        public List<List<Node>> _pair2;

        public ReplaceablePair(Graph g1, Graph g2, List<List<int>> idx1, List<List<int>> idx2)
        {
            _g1 = g1;
            _g2 = g2;
            _pair1 = new List<List<Node>>();
            _pair2 = new List<List<Node>>();
            for (int i = 0; i < idx1.Count; ++i)
            {
                List<Node> nodes1 = new List<Node>();
                List<Node> nodes2 = new List<Node>();
                foreach (int j in idx1[i])
                {
                    nodes1.Add(_g1._NODES[j]);
                }
                foreach (int j in idx2[i])
                {
                    nodes2.Add(_g2._NODES[j]);
                }
                _pair1.Add(nodes1);
                _pair2.Add(nodes2);
            }
        }
    }// ReplaceablePair
}// namespace
