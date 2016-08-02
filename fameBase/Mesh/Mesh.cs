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
		private double[] vertexPos = null;
		private int[] faceVertexIndex = null;
		private HalfEdge[] halfEdges = null;
        private HalfEdge[] singleHalfEdges = null;
		private double[] vertexNormal = null;
		private double[] faceNormal = null;
        private byte[] vertexColor = null;
        private byte[] faceColor = null;
        private List<List<int>> vertexFaceIndex = null;
		private int vertexCount = 0;
		private int faceCount = 0;
        private Vector3d minCoord = Vector3d.MaxCoord();
        private Vector3d maxCoord = Vector3d.MinCoord();
        private bool[] flags;

        public HalfEdge edgeIter = null;
		
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

        public Vector3d MaxCoord
        {
            get
            {
                return this.maxCoord;
            }
        }

        public Vector3d MinCoord
        {
            get
            {
                return this.minCoord;
            }
        }

        public List<List<int>> VertexFaceIndex
        {
            get
            {
                return this.vertexFaceIndex;
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
            
            this.calculateFaceVertexNormal();
            this.flags = new bool[this.vertexCount];
        }

		public Mesh(string meshFileName, bool normalize)
		{
			if(!File.Exists(meshFileName))
			{
				return;
			}
			StreamReader sr = new StreamReader(meshFileName);
			// mesh file type
			string extension = Path.GetExtension(meshFileName); 
			if(extension.Equals(".off"))
			{
				loadOffMesh(sr, normalize);
			} else if (extension.Equals(".ply"))
            {
                LoadPlyfile(sr, normalize);
            }
            else // default ".obj"
            {
                loadObjMesh(sr, normalize);
            }
			sr.Close();
            this.flags = new bool[this.vertexCount];
		}

        private void LoadPlyfile(StreamReader sr, bool normalize)
        {
            List<double> vertexArray = new List<double>();
            List<double> vertexNormalArray = new List<double>();
            List<byte> vertexColorArray = new List<byte>();
            List<int> faceArray = new List<int>();
            List<HalfEdge> halfEdgeArray = new List<HalfEdge>();
            List<HalfEdge> edgeArray = new List<HalfEdge>();
            Dictionary<int, int> edgeHashTable = new Dictionary<int, int>();
            char[] separator = new char[] { ' ', '\t' };
            this.vertexCount = 0;
            this.faceCount = 0;
            string line = "";

            int nproperty = 0;
            while (sr.Peek() > -1)
            {
                line = sr.ReadLine();
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
                line = sr.ReadLine();
                string[] array = line.Split(separator);
                Vector3d v = new Vector3d();
                for (int j = 0; j < 3; ++j)
                {
                    v[j] = double.Parse(array[j]);
                    vertexArray.Add(v[j]);
                }
                this.minCoord = Vector3d.Min(this.minCoord, v);
                this.maxCoord = Vector3d.Max(this.maxCoord, v);
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
            int halfEdgeIdx = 0;
            for (int i = 0; i < this.faceCount; ++i)
            {
                line = sr.ReadLine();
                string[] array = line.Split(separator);
                List<int> currFaceArray = new List<int>();
                List<HalfEdge> currHalfEdgeArray = new List<HalfEdge>();
                for (int j = 1; j < 4; ++j)
                {
                    currFaceArray.Add(int.Parse(array[j])); // face index from 1
                }
                faceArray.AddRange(currFaceArray);
                // hash map here for opposite halfedge
                for (int j = 0; j < 3; ++j)
                {
                    int v1 = currFaceArray[j];
                    int v2 = currFaceArray[(j + 1) % 3];

                    this.vertexFaceIndex[v1].Add(i);

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

            this.vertexPos = vertexArray.ToArray();
            if (vertexNormalArray.Count > 0)
            {
                this.vertexNormal = vertexNormalArray.ToArray();
            }
            this.faceVertexIndex = faceArray.ToArray();
            this.halfEdges = halfEdgeArray.ToArray();
            this.vertexColor = vertexColorArray.ToArray();
            this.singleHalfEdges = edgeArray.ToArray();
            this.edgeIter = this.halfEdges[0];
            if (normalize)
            {
                this.normalize();
            }
            this.initializeColor();
            this.calculateFaceNormal();
            //this.calculateFaceVertexNormal();
        }

        private void loadObjMesh(StreamReader sr, bool normalize)
		{
			List<double> vertexArray = new List<double>();
			List<int> faceArray = new List<int>();
			List<HalfEdge> halfEdgeArray = new List<HalfEdge>();
            List<HalfEdge> edgeArray = new List<HalfEdge>();
			Dictionary<int, int> edgeHashTable = new Dictionary<int, int>();
			char[] separator = new char[]{' ', '\t'};
			this.vertexCount = 0;
			this.faceCount = 0;
			int halfEdgeIdx = 0;
			while(sr.Peek() > -1)
			{
				string line = sr.ReadLine();
                line.Replace("  ", " ");
				string[] array = line.Split(separator);
                if (line == "" || line[0] == '#' || line[0] == 'g') 
                    continue;
                //if (array.Length != 4)
                //{
                //    Console.WriteLine(line);
                //    Console.WriteLine("Vertex/Face read error.");
                //    return;
                //}
				if(line[0] == 'v')
				{
                    Vector3d v = new Vector3d();
                    for (int i = 1; i < 4; ++i) 
                    {
                        if (array[i] == "") continue;
                        v[i - 1] = double.Parse(array[i]);
                        vertexArray.Add(v[i - 1]);
                    }
					++this.vertexCount;
                    this.minCoord = Vector3d.Min(this.minCoord, v);
                    this.maxCoord = Vector3d.Max(this.maxCoord, v);
				}
				else if(line[0] == 'f')
				{
                    if (this.vertexFaceIndex == null)
                    {
                        this.vertexFaceIndex = new List<List<int>>();
                        for (int i = 0; i < this.vertexCount; ++i)
                        {
                            this.vertexFaceIndex.Add(new List<int>());
                        }
                    }
					List<int> currFaceArray = new List<int>();
					List<HalfEdge> currHalfEdgeArray = new List<HalfEdge>();
                    for (int i = 1; i < array.Length; ++i)
                    {
                        if (array[i] == "") continue;
						currFaceArray.Add(int.Parse(array[i]) - 1); // face index from 1
					}
					faceArray.AddRange(currFaceArray);
					// hash map here for opposite halfedge
					for (int i = 0; i < 3; ++i)
					{
						int v1 = currFaceArray[i];
						int v2 = currFaceArray[(i + 1) % 3];
                        this.vertexFaceIndex[v1].Add(faceCount);
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
                    
					for (int i = 0; i < 3;++i )
					{
						currHalfEdgeArray[i].nextHalfEdge = currHalfEdgeArray[(i + 1) % 3];
						currHalfEdgeArray[(i + 1) % 3].prevHalfEdge = currHalfEdgeArray[i];
						currHalfEdgeArray[i].prevHalfEdge = currHalfEdgeArray[(i - 1 + 3) % 3];
						currHalfEdgeArray[(i - 1 + 3) % 3].nextHalfEdge = currHalfEdgeArray[i].prevHalfEdge;
					}
					++faceCount;
				} 
				else if(line.Length > 1 && line.Substring(0,2).Equals("vt"))
				{
				}
			}//while
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
			this.calculateFaceVertexNormal();
		}//loadObjMesh

        private void loadOffMesh(StreamReader sr, bool normalize)
		{

		}//loadOffMesh

        private void initializeColor()
        {
            if (this.vertexColor == null || this.vertexColor.Length != this.vertexCount * 4)
            {
                this.vertexColor = new byte[this.vertexCount * 4];
            }
            this.faceColor = new byte[this.faceCount * 4];
            for (int i = 0; i < this.faceCount; ++i)
            {
                int vidx1 = this.faceVertexIndex[3 * i];
                int vidx2 = this.faceVertexIndex[3 * i + 1];
                int vidx3 = this.faceVertexIndex[3 * i + 2];
                int r = 0, g = 0, b = 0, a = 0;
                r = (int)this.vertexColor[vidx1 * 4] + (int)this.vertexColor[vidx2 * 4] + (int)this.vertexColor[vidx3 * 4];
                g = (int)this.vertexColor[vidx1 * 4 + 1] + (int)this.vertexColor[vidx2 * 4 + 1] + (int)this.vertexColor[vidx3 * 4 + 1];
                b = (int)this.vertexColor[vidx1 * 4 + 2] + (int)this.vertexColor[vidx2 * 4 + 2] + (int)this.vertexColor[vidx3 * 4 + 2];
                a = (int)this.vertexColor[vidx1 * 4 + 3] + (int)this.vertexColor[vidx2 * 4 + 3] + (int)this.vertexColor[vidx3 * 4 + 3];
                this.faceColor[i * 4] = (byte)(r / 3);
                this.faceColor[i * 4 + 1] = (byte)(g / 3);
                this.faceColor[i * 4 + 2] = (byte)(b / 3);
                this.faceColor[i * 4 + 3] = (byte)(a / 3);
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

		private void calculateFaceVertexNormal()
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
            Vector3d c = (this.maxCoord + this.minCoord) / 2;
            Vector3d d = this.maxCoord - this.minCoord;
            double scale = d.x > d.y ? d.x : d.y;
            scale = d.z > scale ? d.z : scale;
            //scale /= 1.5; 
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
            this.calculateFaceVertexNormal();
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
	}//Mesh

}
