using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Geometry
{
	public class HalfEdge
	{
		private int fromVertexIndex = -1;
		private int toVertexIndex = -1;
		private int faceIndex = -1;
        public int index = -1;
		public HalfEdge nextHalfEdge = null;
		public HalfEdge prevHalfEdge = null;
		public HalfEdge invHalfEdge = null;
        public bool falg = false;

		public HalfEdge(int from, int to, int findex, int index)
		{
			fromVertexIndex = from;
			toVertexIndex = to;
			faceIndex = findex;
            this.index = index;
		}

        public int FromIndex
        {
            get
            {
                return this.fromVertexIndex;
            }
        }

        public int ToIndex
        {
            get
            {
                return this.toVertexIndex;
            }
        }

        public int FaceIndex
        {
            get
            {
                return this.faceIndex;
            }
        }
	}//HalfEdge

	public class Mesh
	{
		double[] vertexPos = null;
        double[] originVertextPos = null;
		int[] faceVertexIndex = null;
		HalfEdge[] halfEdges = null;
        HalfEdge[] singleHalfEdges = null;
		double[] vertexNormal = null;
		double[] faceNormal = null;
        byte[] vertexColor = null;
        byte[] faceColor = null;
        List<List<int>> vertexFaceIndex = null;
		int vertexCount = 0;
		int faceCount = 0;
        Vector3d _minCoord = Vector3d.MaxCoord;
        Vector3d _maxCoord = Vector3d.MinCoord;
        bool[] flags;
        string sourceFile;
        int[][] _vv; // vertex-vertex adjancency
        int[][] _vf; // vertex-face
        public HalfEdge edgeIter = null;
		
		public Mesh()
		{ }

        public Mesh(Mesh m, double[] vPos)
        {
            this.vertexPos = vPos;
            this.vertexCount = vPos.Length / 3;
            this.faceCount = m.FaceCount;
            this.faceVertexIndex = new int[m.FaceVertexIndex.Length];
            for (int i = 0; i < m.FaceVertexIndex.Length; ++i)
            {
                this.faceVertexIndex[i] = m.FaceVertexIndex[i];
            }
            this.vertexFaceIndex = new List<List<int>>();
            for (int i = 0; i < m.VertexCount; ++i)
            {
                List<int> index = new List<int>();
                for (int j = 0; j < m.vertexFaceIndex[i].Count; ++j)
                {
                    index.Add(m.vertexFaceIndex[i][j]);
                }
                this.vertexFaceIndex.Add(index);
            }
            this.halfEdges = new HalfEdge[m.HalfEdges.Length];
            for (int i = 0; i < m.HalfEdges.Length; ++i)
            {
                HalfEdge edge = m.HalfEdges[i];
                halfEdges[i] = new HalfEdge(edge.FromIndex, edge.ToIndex, edge.FaceIndex, edge.index);
            }
            // build edge relations
            for (int i = 0; i < m.HalfEdges.Length; ++i )
            {
                HalfEdge edge = m.halfEdges[i];
                int idx = -1;
                if (edge.invHalfEdge != null)
                {
                    idx = edge.invHalfEdge.index;
                    this.halfEdges[i].invHalfEdge = this.halfEdges[idx];
                }
                idx = edge.prevHalfEdge.index;
                this.halfEdges[i].prevHalfEdge = this.halfEdges[idx];
                idx = edge.nextHalfEdge.index;
                this.halfEdges[i].nextHalfEdge = this.halfEdges[idx];
            }
            this.collectMeshInfo();
        }

        public Mesh(double[] vPos, int[] fIndex)
        {
            this.vertexCount = vPos.Length / 3;
            this.faceCount = fIndex.Length / 3; // tri mesh
            this.faceVertexIndex = fIndex;
            this.vertexPos = vPos;
            this.vertexFaceIndex = new List<List<int>>();
            for (int i = 0; i < this.vertexCount; ++i)
            {
                this.vertexFaceIndex.Add(new List<int>());
            }
            for (int i = 0, j = 0; i < this.faceCount; ++i)
            {
                this.vertexFaceIndex[fIndex[j++]].Add(i);
                this.vertexFaceIndex[fIndex[j++]].Add(i);
                this.vertexFaceIndex[fIndex[j++]].Add(i);
            }
            this.buildHalfEdge();
            this.collectMeshInfo();
        }

        public Mesh(double[] vPos, byte[] color)
        {
            // point clound
            this.vertexCount = vPos.Length / 3;
            this.vertexPos = vPos;
            this.vertexColor = color;
        }

		public Mesh(string meshFileName, bool normalize)
		{
			if(!File.Exists(meshFileName))
			{
				return;
			}
            sourceFile = meshFileName;
            using (StreamReader sr = new StreamReader(meshFileName))
            {
                // mesh file type
                string extension = Path.GetExtension(meshFileName);
                if (extension.Equals(".off"))
                {
                    loadOffMesh(sr, normalize);
                }
                else if (extension.Equals(".ply"))
                {
                    LoadPlyfile(sr, normalize);
                }
                else // default ".obj"
                {
                    //loadObjMesh_withoutHalfEdge(sr, normalize);
                    loadObjMesh(sr, normalize);
                }
                sr.Close();
            }
            this.collectMeshInfo();
		}

        public Object Clone()
        {
            double[] vPos = vertexPos.Clone() as double[];
            int[] fIndex = faceVertexIndex.Clone() as int[];
            Mesh m = new Mesh();
            m.vertexCount = vPos.Length / 3;
            m.faceCount = fIndex.Length / 3; // tri mesh
            m.faceVertexIndex = fIndex;
            m.vertexPos = vPos;
            m.vertexFaceIndex = new List<List<int>>();
            for (int i = 0; i < this.vertexCount; ++i)
            {
                m.vertexFaceIndex.Add(new List<int>());
            }
            for (int i = 0, j = 0; i < this.faceCount; ++i)
            {
                m.vertexFaceIndex[fIndex[j++]].Add(i);
                m.vertexFaceIndex[fIndex[j++]].Add(i);
                m.vertexFaceIndex[fIndex[j++]].Add(i);
            }
            m.originVertextPos = this.vertexPos.Clone() as double[];
            m._maxCoord = new Vector3d(_maxCoord);
            m._minCoord = new Vector3d(_minCoord);
            m.faceNormal = faceNormal.Clone() as double[];
            m.vertexNormal = vertexNormal.Clone() as double[];
            return m;
        }

        public void Transform(Matrix4d T) 
        {
            _maxCoord = Vector3d.MinCoord;
            _minCoord = Vector3d.MaxCoord;
            for (int i = 0, j = 0; i < this.VertexCount; ++i, j += 3)
            {
                Vector3d ori = new Vector3d(this.vertexPos[j], this.vertexPos[j + 1], this.vertexPos[j + 2]);
                Vector3d transformed = (T * new Vector4d(ori, 1)).ToVector3D();
                for (int k = 0; k < 3; ++k)
                {
                    this.vertexPos[j + k] = transformed[k];
                }
                _maxCoord = Vector3d.Max(_maxCoord, transformed);
                _minCoord = Vector3d.Min(_minCoord, transformed);
            }
            //this.afterUpdatePos();
            this.getBoundary();
        }// Transform

        public void TransformFromOrigin(Matrix4d T)
        {
            _maxCoord = Vector3d.MinCoord;
            _minCoord = Vector3d.MaxCoord;
            for (int i = 0, j = 0; i < this.VertexCount; ++i, j += 3)
            {
                Vector3d ori = new Vector3d(this.originVertextPos[j], this.originVertextPos[j + 1], this.originVertextPos[j + 2]);
                Vector3d transformed = (T * new Vector4d(ori, 1)).ToVector3D();
                for (int k = 0; k < 3; ++k)
                {
                    this.vertexPos[j + k] = transformed[k];
                }
                _maxCoord = Vector3d.Max(_maxCoord, transformed);
                _minCoord = Vector3d.Min(_minCoord, transformed);
            }
            this.afterUpdatePos();
        }// Transform

        public void updateOriginPos()
        {
            this.originVertextPos = this.vertexPos.Clone() as double[];
        }

        private void LoadPlyfile(StreamReader sr, bool normalize)
        {
            List<double> vertexArray = new List<double>();
            List<double> vertexNormalArray = new List<double>();
            List<byte> vertexColorArray = new List<byte>();
            List<int> faceArray = new List<int>();
            char[] separator = new char[] { ' ', '\t' };
            this.vertexCount = 0;
            this.faceCount = 0;
            string line = "";

            int nproperty = 0;
            while (sr.Peek() > -1)
            {
                line = sr.ReadLine().Trim();
                string[] array = line.Split(separator);
                if (array.Length > 0 && array[0].Equals("end_header"))
                {
                    break;
                }
                if (array.Length > 0 && array[0].Equals("property"))
                {
                    ++nproperty;
                }
                if (array.Length > 1 && array[0].Equals("element"))
                {
                    if (array[1].Equals("vertex"))
                    {
                        this.vertexCount = Int32.Parse(array[2]);
                    }
                    else if (array[1].Equals("face"))
                    {
                        this.faceCount = Int32.Parse(array[2]);
                    }
                }
            }
            this.vertexFaceIndex = new List<List<int>>();
            for (int i = 0; i < this.vertexCount; ++i)
            {
                this.vertexFaceIndex.Add(new List<int>());
            }
            for (int i = 0; i < this.vertexCount; ++i)
            {
                line = sr.ReadLine().Trim();
                string[] array = line.Split(separator);
                Vector3d v = new Vector3d();
                for (int j = 0; j < 3; ++j)
                {
                    v[j] = double.Parse(array[j]);
                    vertexArray.Add(v[j]);
                }
                this._minCoord = Vector3d.Min(this._minCoord, v);
                this._maxCoord = Vector3d.Max(this._maxCoord, v);
                if (nproperty >= 20)
                {
                    Vector3d normal = new Vector3d();
                    for (int j = 3; j < 6; ++j)
                    {
                        if (array[j] != "")
                            normal[j - 3] = double.Parse(array[j]);
                    }
                    normal.normalize();
                    for (int j = 0; j < 3; ++j)
                    {
                        vertexNormalArray.Add(normal[j]);
                    }
                    for (int j = 6; j < 10; ++j)
                    {
                        if (array[j] != "")
                            vertexColorArray.Add(byte.Parse(array[j]));
                    }
                }
            }

            for (int i = 0; i < this.faceCount; ++i)
            {
                line = sr.ReadLine().Trim();
                string[] array = line.Split(separator);
                List<int> currFaceArray = new List<int>();
                for (int j = 1; j < 4; ++j)
                {
                    int fv = int.Parse(array[j]);
                    currFaceArray.Add(fv); // face index from 1
                    this.vertexFaceIndex[fv].Add(i);
                }
                faceArray.AddRange(currFaceArray);
            }
            this.buildHalfEdge();
            this.faceVertexIndex = faceArray.ToArray();
            this.vertexPos = vertexArray.ToArray();
            if (vertexNormalArray.Count > 0)
            {
                this.vertexNormal = vertexNormalArray.ToArray();
            }
            this.vertexColor = vertexColorArray.ToArray();
            if (normalize)
            {
                this.normalize();
            }
            this.initializeColor();
            this.calculateFaceNormal();
            //this.calculateFaceVertexNormal();
        }

        private void buildHalfEdge()
        {
            List<HalfEdge> halfEdgeArray = new List<HalfEdge>();
            List<HalfEdge> edgeArray = new List<HalfEdge>();
            Dictionary<int, int> edgeHashTable = new Dictionary<int, int>();
            int halfEdgeIdx = 0;
            for (int i = 0; i < this.faceCount; ++i)
            {
                int[] currFaceArray = { this.faceVertexIndex[i * 3], this.faceVertexIndex[i * 3 + 1], this.faceVertexIndex[i * 3 + 2] };
                List<HalfEdge> currHalfEdgeArray = new List<HalfEdge>();
                // hash map here for opposite halfedge
                for (int j = 0; j < 3; ++j)
                {
                    int v1 = currFaceArray[j];
                    int v2 = currFaceArray[(j + 1) % 3];
                    HalfEdge halfedge = new HalfEdge(v1, v2, i, halfEdgeIdx++);
                    int key = Math.Min(v1, v2) * vertexCount + Math.Max(v1, v2);
                    if (edgeHashTable.ContainsKey(key)) // find a halfedge
                    {
                        HalfEdge oppHalfEdge = halfEdgeArray[edgeHashTable[key]];
                        halfedge.invHalfEdge = oppHalfEdge;
                        oppHalfEdge.invHalfEdge = halfedge;
                    }
                    else
                    {
                        edgeHashTable.Add(key, halfEdgeArray.Count);
                        edgeArray.Add(halfedge);
                    }
                    halfEdgeArray.Add(halfedge);
                    currHalfEdgeArray.Add(halfedge);
                }
                for (int j = 0; j < 3; ++j)
                {
                    currHalfEdgeArray[j].nextHalfEdge = currHalfEdgeArray[(j + 1) % 3];
                    currHalfEdgeArray[(j + 1) % 3].prevHalfEdge = currHalfEdgeArray[j];
                    currHalfEdgeArray[j].prevHalfEdge = currHalfEdgeArray[(j - 1 + 3) % 3];
                    currHalfEdgeArray[(j - 1 + 3) % 3].nextHalfEdge = currHalfEdgeArray[j].prevHalfEdge;
                }
            }

            this.halfEdges = halfEdgeArray.ToArray();
            this.singleHalfEdges = edgeArray.ToArray();
            if (this.halfEdges != null && this.halfEdges.Length > 0)
            {
                this.edgeIter = this.halfEdges[0];
            }
        }// buildHalfEdge

        private void loadObjMesh_withoutHalfEdge(StreamReader sr, bool normalize)
        {
            // for messy .obj file
            List<double> vertexArray = new List<double>();
            List<int> faceArray = new List<int>();
            List<HalfEdge> halfEdgeArray = new List<HalfEdge>();
            List<HalfEdge> edgeArray = new List<HalfEdge>();
            Dictionary<int, int> edgeHashTable = new Dictionary<int, int>();
            char[] separator = new char[] { ' ', '\t', '\\' };
            this.vertexCount = 0;
            this.faceCount = 0;
            while (sr.Peek() > -1)
            {
                string line = sr.ReadLine().Trim();
                line.Replace("  ", " ");
                line.Replace("//", "/");
                string[] array = line.Split(separator);
                if (line == "" || line[0] == '#' || line[0] == 'g')
                    continue;
                if (array[0] == "v")
                {
                    Vector3d v = new Vector3d();
                    int i = 0;
                    int j = 0;
                    while (++i < array.Length)
                    {
                        if (array[i] == "")
                        {
                            continue;
                        }
                        v[j] = double.Parse(array[i]);
                        vertexArray.Add(v[j++]);
                    }
                    if (j > 3)
                    {
                        return;
                    }
                    ++this.vertexCount;
                    this._minCoord = Vector3d.Min(this._minCoord, v);
                    this._maxCoord = Vector3d.Max(this._maxCoord, v);
                }
                else if (array[0] == "f")
                {
                    List<int> currFaceArray = new List<int>();
                    List<HalfEdge> currHalfEdgeArray = new List<HalfEdge>();
                    for (int i = 1; i < array.Length; ++i)
                    {
                        if (array[i] == "") continue;
                        string idStr = array[i];
                        if (array[i].Contains('/'))
                        {
                            // extract only the vertex index
                            idStr = array[i].Substring(0, array[i].IndexOf('/'));
                        }
                        currFaceArray.Add(int.Parse(idStr) - 1); // face index from 1
                    }
                    faceArray.AddRange(currFaceArray);
                    ++faceCount;
                }
            }//while
            this.vertexPos = vertexArray.ToArray();
            this.faceVertexIndex = faceArray.ToArray();
            this.singleHalfEdges = edgeArray.ToArray();

            this.vertexFaceIndex = new List<List<int>>();
            for (int i = 0; i < this.vertexCount; ++i)
            {
                this.vertexFaceIndex.Add(new List<int>());
            }
            for (int i = 0; i < this.faceVertexIndex.Length; i += 3)
            {
                int f = i / 3;
                this.vertexFaceIndex[this.faceVertexIndex[i]].Add(f);
                this.vertexFaceIndex[this.faceVertexIndex[i + 1]].Add(f);
                this.vertexFaceIndex[this.faceVertexIndex[i + 2]].Add(f);
            }

            if (normalize)
            {
                this.normalize();
            }
            this.initializeColor();
        }//loadObjMesh

        private void loadObjMesh(StreamReader sr, bool normalize)
        {
            List<double> vertexArray = new List<double>();
            List<int> faceArray = new List<int>();
            List<HalfEdge> halfEdgeArray = new List<HalfEdge>();
            List<HalfEdge> edgeArray = new List<HalfEdge>();
            Dictionary<int, int> edgeHashTable = new Dictionary<int, int>();
            char[] separator = new char[] { ' ', '\t', '\\' };
            this.vertexCount = 0;
            this.faceCount = 0;
            int halfEdgeIdx = 0;
            while (sr.Peek() > -1)
            {
                string line = sr.ReadLine().Trim();
                line.Replace("  ", " ");
                line.Replace("//", "/");
                string[] array = line.Split(separator);
                if (line == "" || line[0] == '#' || line[0] == 'g')
                    continue;
                if (array[0] == "v")
                {
                    Vector3d v = new Vector3d();
                    int i = 0;
                    int j = 0;
                    while (++i < array.Length)
                    {
                        if (array[i] == "")
                        {
                            continue;
                        }
                        v[j] = double.Parse(array[i]);
                        vertexArray.Add(v[j++]);
                    }
                    if (j > 3)
                    {
                        return;
                    }
                    ++this.vertexCount;
                    this._minCoord = Vector3d.Min(this._minCoord, v);
                    this._maxCoord = Vector3d.Max(this._maxCoord, v);
                }
                else if (array[0] == "f")
                {
                    List<int> currFaceArray = new List<int>();
                    for (int i = 1; i < array.Length; ++i)
                    {
                        if (array[i] == "") continue;
                        string idStr = array[i];
                        if (array[i].Contains('/'))
                        {
                            // extract only the vertex index
                            idStr = array[i].Substring(0, array[i].IndexOf('/'));
                        }
                        currFaceArray.Add(int.Parse(idStr) - 1); // face index from 1
                    }
                    faceArray.AddRange(currFaceArray);
                    ++faceCount;
                }
                else if (line.Length > 1 && line.Substring(0, 2).Equals("vt"))
                {
                }
            }//while
            this.vertexFaceIndex = new List<List<int>>();
            for (int i = 0; i < this.vertexCount; ++i)
            {
                this.vertexFaceIndex.Add(new List<int>());
            }
            // hash map here for opposite halfedge
            for (int f = 0, k = 0; f < faceCount; ++f, k += 3)
            {
                List<int> currFaceArray = new List<int>();
                for (int i = 0; i < 3; ++i)
                {
                    currFaceArray.Add(faceArray[k + i]);
                }
                List<HalfEdge> currHalfEdgeArray = new List<HalfEdge>();
                for (int i = 0; i < 3; ++i)
                {
                    int v1 = currFaceArray[i];
                    int v2 = currFaceArray[(i + 1) % 3];
                    this.vertexFaceIndex[v1].Add(f);
                    HalfEdge halfedge = new HalfEdge(v1, v2, faceCount, halfEdgeIdx++);
                    int key = Math.Min(v1, v2) * vertexCount + Math.Max(v1, v2);
                    if (edgeHashTable.ContainsKey(key)) // find a halfedge
                    {
                        HalfEdge oppHalfEdge = halfEdgeArray[edgeHashTable[key]];
                        halfedge.invHalfEdge = oppHalfEdge;
                        oppHalfEdge.invHalfEdge = halfedge;
                    }
                    else
                    {
                        edgeHashTable.Add(key, halfEdgeArray.Count);
                        edgeArray.Add(halfedge);
                    }
                    halfEdgeArray.Add(halfedge);
                    currHalfEdgeArray.Add(halfedge);
                }

                for (int i = 0; i < 3; ++i)
                {
                    currHalfEdgeArray[i].nextHalfEdge = currHalfEdgeArray[(i + 1) % 3];
                    currHalfEdgeArray[(i + 1) % 3].prevHalfEdge = currHalfEdgeArray[i];
                    currHalfEdgeArray[i].prevHalfEdge = currHalfEdgeArray[(i - 1 + 3) % 3];
                    currHalfEdgeArray[(i - 1 + 3) % 3].nextHalfEdge = currHalfEdgeArray[i].prevHalfEdge;
                }
            }
            this.vertexPos = vertexArray.ToArray();
            this.faceVertexIndex = faceArray.ToArray();
            this.halfEdges = halfEdgeArray.ToArray();
            this.singleHalfEdges = edgeArray.ToArray();
            this.edgeIter = this.halfEdges[0];

            if (normalize)
            {
                this.normalize();
            }
            this.initializeColor();
        }//loadObjMesh

        private void loadOffMesh(StreamReader sr, bool normalize)
		{

		}//loadOffMesh

        private void collectMeshInfo()
        {
            _vf = this.buildFaceVertexAdjancencyMatrix().getColIndex();
            _vv = this.buildVertexToVertexAdjancenyMatrix().getRowIndex();
            this.originVertextPos = this.vertexPos.Clone() as double[];
            this.afterUpdatePos();
            this.flags = new bool[this.vertexCount];
        }

        public void afterUpdatePos()
        {
            this.getBoundary();
            this.calculateFaceVertexNormal();
            this.calculateFaceNormal();
        }
        private void getBoundary()
        {
            _maxCoord = Vector3d.MinCoord;
            _minCoord = Vector3d.MaxCoord;
            for (int i = 0, j = 0; i < this.vertexCount; ++i, j += 3)
            {
                Vector3d v = new Vector3d(this.vertexPos[j], this.vertexPos[j + 1], this.vertexPos[j + 2]);
                _maxCoord = Vector3d.Max(v, _maxCoord);
                _minCoord = Vector3d.Min(v, _minCoord);
            }
        }// getBoundary

        private SparseMatrix buildVertexToVertexAdjancenyMatrix()
        {
            if (faceVertexIndex == null) return null;
            SparseMatrix mat = new SparseMatrix(vertexCount, vertexCount);
            for (int i = 0, j = 0; i < faceCount; ++i)
            {
                int j1 = faceVertexIndex[j++];
                int j2 = faceVertexIndex[j++];
                int j3 = faceVertexIndex[j++];
                mat.AddTriplet(j1, j2, 1);
                mat.AddTriplet(j1, j3, 1);
                mat.AddTriplet(j2, j1, 1);
                mat.AddTriplet(j2, j3, 1);
                mat.AddTriplet(j3, j1, 1);
                mat.AddTriplet(j3, j2, 1);
            }
            mat.sort();
            return mat;
        }// buildVertexToVertexAdjancenyMatrix

        private SparseMatrix buildFaceVertexAdjancencyMatrix()
        {
            if (faceVertexIndex == null) return null;
            SparseMatrix mat = new SparseMatrix(faceCount, vertexCount);
            for (int i = 0, j = 0; i < faceCount; ++i)
            {
                mat.AddTriplet(i, faceVertexIndex[j++], 1);
                mat.AddTriplet(i, faceVertexIndex[j++], 1);
                mat.AddTriplet(i, faceVertexIndex[j++], 1);
            }
            mat.sort();
            return mat;
        }// buildFaceVertexAdjancencyMatrix

        private void initializeColor()
        {
            int n = 3;
            if (this.vertexColor == null || this.vertexColor.Length != this.vertexCount * n)
            {
                this.vertexColor = new byte[this.vertexCount * n];
            }
            this.faceColor = new byte[this.faceCount * n];
            for (int i = 0; i < this.faceCount; ++i)
            {
                int vidx1 = this.faceVertexIndex[3 * i];
                int vidx2 = this.faceVertexIndex[3 * i + 1];
                int vidx3 = this.faceVertexIndex[3 * i + 2];
                int r = 0, g = 0, b = 0, a = 0;
                r = (int)this.vertexColor[vidx1 * n] + (int)this.vertexColor[vidx2 * n] + (int)this.vertexColor[vidx3 * n];
                g = (int)this.vertexColor[vidx1 * n + 1] + (int)this.vertexColor[vidx2 * n + 1] + (int)this.vertexColor[vidx3 * n + 1];
                b = (int)this.vertexColor[vidx1 * n + 2] + (int)this.vertexColor[vidx2 * n + 2] + (int)this.vertexColor[vidx3 * n + 2];
                this.faceColor[i * n] = (byte)(r / 3);
                this.faceColor[i * n + 1] = (byte)(g / 3);
                this.faceColor[i * n + 2] = (byte)(b / 3);
                if (n == 4)
                {
                    a = (int)this.vertexColor[vidx1 * n + 3] + (int)this.vertexColor[vidx2 * n + 3] + (int)this.vertexColor[vidx3 * n + 3];
                    this.faceColor[i * n + 3] = (byte)(a / 3);
                }
            }
        }//initializeColor

        private void calculateFaceNormal()
        {
            if (this.vertexNormal == null || this.vertexNormal.Length == 0)
            {
                this.calculateFaceVertexNormal();
                return;
            }
            this.faceNormal = new double[this.faceCount * 3];
            for (int i = 0; i < this.faceCount; ++i)
            {
                int vidx1 = this.faceVertexIndex[3 * i];
                int vidx2 = this.faceVertexIndex[3 * i + 1];
                int vidx3 = this.faceVertexIndex[3 * i + 2];
                Vector3d v1 = new Vector3d(
                   this.vertexNormal[vidx1 * 3], this.vertexNormal[vidx1 * 3 + 1], this.vertexNormal[vidx1 * 3 + 2]);
                Vector3d v2 = new Vector3d(
                    this.vertexNormal[vidx2 * 3], this.vertexNormal[vidx2 * 3 + 1], this.vertexNormal[vidx2 * 3 + 2]);
                Vector3d v3 = new Vector3d(
                    this.vertexNormal[vidx3 * 3], this.vertexNormal[vidx3 * 3 + 1], this.vertexNormal[vidx3 * 3 + 2]);
                Vector3d normal = v1 + v2 + v3;
                normal /= 3;
                for (int j = 0; j < 3; ++j)
                {
                    this.faceNormal[3 * i + j] = normal[j];
                }
            }
        }

		public void calculateFaceVertexNormal()
		{
			if(this.faceVertexIndex == null || this.faceVertexIndex.Length == 0)
			{
				return;
			}
			this.faceNormal = new double[this.faceCount * 3];
			this.vertexNormal = new double[this.vertexCount * 3];
            this.faceColor = new byte[this.faceCount * 4];
			for (int i = 0; i < this.faceCount; ++i)
			{
				int vidx1 = this.faceVertexIndex[3 * i];
                int vidx2 = this.faceVertexIndex[3 * i + 1];
                int vidx3 = this.faceVertexIndex[3 * i + 2];
                Vector3d v1 = new Vector3d(
                    this.vertexPos[vidx1 * 3], this.vertexPos[vidx1 * 3 + 1], this.vertexPos[vidx1 * 3 + 2]);
                Vector3d v2 = new Vector3d(
                    this.vertexPos[vidx2 * 3], this.vertexPos[vidx2 * 3 + 1], this.vertexPos[vidx2 * 3 + 2]);
                Vector3d v3 = new Vector3d(
                    this.vertexPos[vidx3 * 3], this.vertexPos[vidx3 * 3 + 1], this.vertexPos[vidx3 * 3 + 2]);
                Vector3d v21 = v2 - v1;
                Vector3d v31 = v3 - v1;
                Vector3d normal = v21.Cross(v31);
                normal.normalize();
                for (int j = 0; j < 3; ++j)
                {
                    this.faceNormal[3 * i + j] = normal[j];
                    this.vertexNormal[vidx1 * 3 + j] += normal[j];
                    this.vertexNormal[vidx2 * 3 + j] += normal[j];
                    this.vertexNormal[vidx3 * 3 + j] += normal[j];
                }
			}
            this.initializeColor();
            //for (int i = 0; i < this.vertexCount; ++i)
            //{
            //    Vector3d vn = new Vector3d(this.vertexNormal[3 * i],
            //        this.vertexNormal[3 * i + 1],
            //        this.vertexNormal[3 * i + 2]);
            //    vn.normalize();
            //    this.vertexNormal[3 * i] = vn.x;
            //    this.vertexNormal[3 * i + 1] = vn.y;
            //    this.vertexNormal[3 * i + 2] = vn.z;
            //}
            for (int i = 0; i < this.vertexCount; ++i)
            {
                Vector3d normal = new Vector3d();
                for (int j = 0; j < this.vertexFaceIndex[i].Count; ++j)
                {
                    int fidx = this.vertexFaceIndex[i][j];

                    Vector3d nor = new Vector3d(this.faceNormal[3 * fidx],
                        this.faceNormal[3 * fidx + 1],
                        this.faceNormal[3 * fidx + 2]);
                    normal += nor;
                }
                Vector3d vn = normal / this.vertexFaceIndex[i].Count;
                vn.normalize();
                this.vertexNormal[3 * i] = vn.x;
                this.vertexNormal[3 * i + 1] = vn.y;
                this.vertexNormal[3 * i + 2] = vn.z;
            }
		}//calculateFaceVertexNormal

        private void normalize()
        {
            Vector3d c = (this._maxCoord + this._minCoord) / 2;
            Vector3d d = this._maxCoord - this._minCoord;
            double scale = d.x > d.y ? d.x : d.y;
            scale = d.z > scale ? d.z : scale;
            scale /= 2; // [-1, 1]
            for (int i = 0, j = 0; i < this.VertexCount; ++i, j += 3)
            {
                for (int k = 0; k < 3; ++k)
                {
                    //this.vertexPos[j + k] /= scale;
                    this.vertexPos[j + k] -= c[k];
                    this.vertexPos[j + k] /= scale;
                }
            }
        }

        public void normalize(Vector3d center, double scale)
        {
            for (int i = 0, j = 0; i < this.VertexCount; ++i, j += 3)
            {
                for (int k = 0; k < 3; ++k)
                {
                    this.vertexPos[j + k] /= scale;
                    this.vertexPos[j + k] -= center[k];
                }
            }
            this.afterUpdatePos();
        }

        public Vector3d getFaceCenter(int fidx)
        {
            int i1 = this.faceVertexIndex[fidx * 3];
            int i2 = this.faceVertexIndex[fidx * 3 + 1];
            int i3 = this.faceVertexIndex[fidx * 3 + 2];
            Vector3d v1 = new Vector3d(this.vertexPos[i1 * 3], this.vertexPos[i1 * 3 + 1], this.vertexPos[i1 * 3 + 2]);
            Vector3d v2 = new Vector3d(this.vertexPos[i2 * 3], this.vertexPos[i2 * 3 + 1], this.vertexPos[i2 * 3 + 2]);
            Vector3d v3 = new Vector3d(this.vertexPos[i3 * 3], this.vertexPos[i3 * 3 + 1], this.vertexPos[i3 * 3 + 2]);
            return (v1 + v2 + v3) / 3;
        }

        public Vector3d getFaceNormal(int fidx)
        {
            return new Vector3d(this.faceNormal[fidx * 3], this.faceNormal[fidx * 3 + 1], this.faceNormal[fidx * 3 + 2]);
        }

        public Vector3d getVertexPos(int vidx)
        {
            return new Vector3d(this.vertexPos[vidx * 3], this.vertexPos[vidx * 3 + 1], this.vertexPos[vidx * 3 + 2]);
        }

        public void setVertextPos(int i, Vector3d v)
        {
            this.vertexPos[3 * i] = v.x;
            this.vertexPos[3 * i + 1] = v.y;
            this.vertexPos[3 * i + 2] = v.z;
        }

        public void setVertexColor(byte[] colors)
        {
            this.vertexColor = colors;
            this.initializeColor();
        }

        // to avoid changing the count of vertex/face
        public int VertexCount
        {
            get
            {
                return vertexCount;
            }
        }

        public int FaceCount
        {
            get
            {
                return faceCount;
            }
        }

        public double[] VertexPos
        {
            get
            {
                return this.vertexPos;
            }
        }

        public double[,] VertexArray
        {
            get
            {
                double[,] array = new double[this.vertexCount, 3];
                for (int i = 0, j = 0; i < this.vertexCount; ++i, j += 3)
                {
                    array[i, 0] = this.vertexPos[j];
                    array[i, 1] = this.vertexPos[j + 1];
                    array[i, 2] = this.vertexPos[j + 2];
                }
                return array;
            }
        }

        public Vector3d[] VertexVectorArray
        {
            get
            {
                Vector3d[] array = new Vector3d[this.vertexCount];
                for (int i = 0, j = 0; i < this.vertexCount; ++i, j += 3)
                {
                    array[i] = new Vector3d(this.vertexPos[j], this.vertexPos[j + 1], this.vertexPos[j + 2]);
                }
                return array;
            }
        }

        public double[] VertexNormal
        {
            get
            {
                return this.vertexNormal;
            }
        }

        public int[] FaceVertexIndex
        {
            get
            {
                return this.faceVertexIndex;
            }
        }

        public HalfEdge[] HalfEdges
        {
            get
            {
                return this.halfEdges;
            }
        }

        public HalfEdge[] Edges
        {
            get
            {
                return singleHalfEdges;
            }
        }

        public double[] FaceNormal
        {
            get
            {
                return this.faceNormal;
            }
        }

        public byte[] FaceColor
        {
            get
            {
                return this.faceColor;
            }
        }

        public byte[] VertexColor
        {
            get
            {
                return this.vertexColor;
            }
        }

        public Vector3d MaxCoord
        {
            get
            {
                return this._maxCoord;
            }
        }

        public Vector3d MinCoord
        {
            get
            {
                return this._minCoord;
            }
        }

        public List<List<int>> VertexFaceIndex
        {
            get
            {
                return this.vertexFaceIndex;
            }
        }

        public int[][] _VF
        {
            get
            {
                return _vf;
            }
        }

        public int[][] _VV
        {
            get
            {
                return _vv;
            }
        }

        public bool[] Flags
        {
            get
            {
                return this.flags;
            }
            set
            {
                this.flags = value;
            }
        }
	}//Mesh

}
