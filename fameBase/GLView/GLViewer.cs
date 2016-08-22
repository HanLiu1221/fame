using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;
using System.IO;
using System.Drawing;

using Tao.OpenGl;
using Tao.Platform.Windows;

using Geometry;
using Component;

using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;

namespace FameBase
{
    public class GLViewer :  SimpleOpenGlControl
    {
        /******************** Initialization ********************/
        public GLViewer() 
        {
            this.InitializeComponent();
            this.InitializeContexts();

            this.initScene();
        }

        public void Init()
        {
            this.initializeVariables();
            //// glsl shaders
            //this.shader = new Shader(
            //    @"shaders\vertexshader.glsl",
            //    @"shaders\fragmentshader.glsl");
            //this.shader.Link();

            this.LoadTextures();
        }

        private void InitializeComponent() 
        {
            this.SuspendLayout();

            this.Name = "GlViewer";

            this.ResumeLayout(false);
        }

        private void initializeVariables()
        {
            this.meshClasses = new List<MeshClass>();
            this._currModelTransformMatrix = Matrix4d.IdentityMatrix();
            this.arcBall = new ArcBall(this.Width, this.Height);
            this.camera = new Camera();

            _axes = new Vector3d[18] { new Vector3d(-1.2, 0, 0), new Vector3d(1.2, 0, 0),
                                      new Vector3d(1, 0.2, 0), new Vector3d(1.2, 0, 0), 
                                      new Vector3d(1, -0.2, 0), new Vector3d(1.2, 0, 0), 
                                      new Vector3d(0, -1.2, 0), new Vector3d(0, 1.2, 0), 
                                      new Vector3d(-0.2, 1, 0), new Vector3d(0, 1.2, 0),
                                      new Vector3d(0.2, 1, 0), new Vector3d(0, 1.2, 0),
                                      new Vector3d(0, 0, -1.2), new Vector3d(0, 0, 1.2),
                                      new Vector3d(-0.2, 0, 1), new Vector3d(0, 0, 1.2),
                                      new Vector3d(0.2, 0, 1), new Vector3d(0, 0, 1.2)};
            this.startWid = this.Width;
            this.startHeig = this.Height;
            Vector3d[] groundPoints = new Vector3d[4] {
                new Vector3d(-1, -0.5, -1), new Vector3d(-1, -0.5, 1),
                new Vector3d(1, -0.5, 1), new Vector3d(1, -0.5, -1)};
            _groundPlane = new Plane3D(groundPoints);
        }

        // modes
        public enum UIMode 
        {
            // !Do not change the order of the modes --- used in the current program to retrieve the index (Integer)
            Viewing, VertexSelection, EdgeSelection, FaceSelection, BoxSelection, BodyNodeEdit, Translate, Scale, Rotate, NONE
        }

        private bool drawVertex = false;
        private bool drawEdge = false;
        private bool drawFace = true;
        private bool drawBbox = true;
        private bool isDrawAxes = false;
        private bool isDrawQuad = false;

        public bool showSketchyEdges = true;
        public bool showSketchyContour = false;
        public bool showGuideLines = false;
        public bool showAllGuides = true;
        public bool showOcclusion = false;
        public bool enableDepthTest = true;
        public bool showVanishingLines = true;
        public bool lockView = false;
        public bool showFaceToDraw = true;

        public bool showSharpEdge = false;
        public bool enableHiddencheck = true;
		public bool condition = true;

        public bool showSegSilhouette = false;
        public bool showSegContour = false;
        public bool showSegSuggestiveContour = false;
        public bool showSegApparentRidge = false;
        public bool showSegBoundary = false;
        public bool showLineOrMesh = true;

        public bool showDrawnStroke = true;

        public bool showBlinking = false;

        private static Vector3d eyePosition3D = new Vector3d(0, 0, 1.5);
        private static Vector3d eyePosition2D = new Vector3d(0, 1, 1.5);
        Vector3d eye = new Vector3d(0, 0, 1.5);
        private float[] _material = { 0.62f, 0.74f, 0.85f, 1.0f };
        private float[] _ambient = { 0.2f, 0.2f, 0.2f, 1.0f };
        private float[] _diffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
        private float[] _specular = { 1.0f, 1.0f, 1.0f, 1.0f };
        private float[] _position = { 1.0f, 1.0f, 1.0f, 0.0f };

        /******************** Variables ********************/
        private UIMode currUIMode = UIMode.Viewing;
        private Matrix4d _currModelTransformMatrix = Matrix4d.IdentityMatrix();
        private Matrix4d _modelTransformMatrix = Matrix4d.IdentityMatrix();
        private Matrix4d _fixedModelView = Matrix4d.IdentityMatrix();
        private Matrix4d scaleMat = Matrix4d.IdentityMatrix();
        private Matrix4d transMat = Matrix4d.IdentityMatrix();
        private Matrix4d rotMat = Matrix4d.IdentityMatrix();
        private ArcBall arcBall = new ArcBall();
        private Vector2d mouseDownPos;
        private Vector2d prevMousePos;
        private Vector2d currMousePos;
        private bool isMouseDown = false;
        private List<MeshClass> meshClasses;
        private MeshClass currMeshClass;
        private Quad2d highlightQuad;
        private Camera camera;
        private Shader shader;
        public static uint pencilTextureId, crayonTextureId, inkTextureId, waterColorTextureId, charcoalTextureId,
            brushTextureId;
        
        public string foldername;
        private Vector3d objectCenter = new Vector3d();
        private enum Depthtype
        {
            opacity, hidden, OpenGLDepthTest, none, rayTracing // test 
        }

        public bool showVanishingRay1 = true;
        public bool showVanishingRay2 = true;
        public bool showVanishingPoints = true;
        public bool showBoxVanishingLine = true;
        public bool showGuideLineVanishingLine = true;
        private List<int> boxShowSequence = new List<int>();

        public bool zoonIn = false;

        //########## sketch vars ##########//
        private List<Vector2d> currSketchPoints = new List<Vector2d>();
        

        /******************** Vars ********************/
        Model _currModel;
        List<Model> _models = new List<Model>();
        List<Part> _selectedParts = new List<Part>();
        List<ModelViewer> _modelViewers = new List<ModelViewer>();
        List<ModelViewer> _partViewers = new List<ModelViewer>();
        HumanPose _humanPose;
        BodyNode _selectedNode;
        public bool _unitifyMesh = true;
        bool _showEditAxes = false;
        public bool drawGround = false;
        private Vector3d[] _axes;
        private Vector3d[] _editAxes;
        private Plane3D _groundPlane;
        int _hightlightAxis = -1;
        ArcBall _editArcball;

        /******************** Functions ********************/

        public UIMode CurrentUIMode
        {
            get
            {
                return this.currUIMode;
            }
            set
            {
                this.currUIMode = value;
            }
        }

        private void LoadTextures()					// load textures for canvas and brush
        {
            this.CreateTexture(@"data\pencil.png", out GLViewer.pencilTextureId);
            this.CreateTexture(@"data\crayon.png", out GLViewer.crayonTextureId);
            this.CreateTexture(@"data\ink.jpg", out GLViewer.inkTextureId);
            this.CreateTexture(@"data\watercolor.png", out GLViewer.waterColorTextureId);
            this.CreateTexture(@"data\charcoal.jpg", out GLViewer.charcoalTextureId);
            this.CreateGaussianTexture(32);
        }

        private void CreateTexture(string imagefile, out uint textureid)
        {
            Bitmap image = new Bitmap(imagefile);

            // to gl texture
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            //	image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            System.Drawing.Imaging.BitmapData bitmapdata = image.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Gl.glGenTextures(1, out textureid);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureid);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_R, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, 4, image.Width, image.Height, 0, Gl.GL_BGRA,
                Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);
        }

        public byte[] CreateGaussianTexture(int size)
        {
            int w = size * 2, size2 = size * size;
            Bitmap bitmap = new Bitmap(w, w);
            byte[] alphas = new byte[w * w * 4];
            for (int i = 0; i < w; ++i)
            {
                int dx = i - size;
                for (int j = 0; j < w; ++j)
                {
                    int J = j * w + i;

                    int dy = j - size;
                    double dist2 = (dx * dx + dy * dy);

                    byte alpha = 0;
                    if (dist2 <= size2)	// -- not necessary actually, similar effects
                    {
                        // set gaussian values for the alphas
                        // modify the denominator to get different over-paiting effects
                        double gau_val = Math.Exp(-dist2 / (2 * size2 / 2)) / Math.E / 2;
                        alpha = Math.Min((byte)255, (byte)((gau_val) * 255));
                        //	alpha = 100; // Math.Min((byte)255, (byte)((gau_val) * 255));
                    }

                    byte beta = (byte)(255 - alpha);
                    alphas[J * 4] = (byte)(beta);
                    alphas[J * 4 + 1] = (byte)(beta);
                    alphas[J * 4 + 2] = (byte)(beta);
                    alphas[J * 4 + 3] = (byte)(alpha);

                    bitmap.SetPixel(i, j, System.Drawing.Color.FromArgb(alpha, beta, beta, beta));
                }
            }
            bitmap.Save(@"data\output.png");

            // create gl texture
            uint[] txtid = new uint[1];
            // -- create texture --
            Gl.glGenTextures(1, txtid);				// Create The Texture
            GLViewer.brushTextureId = txtid[0];

            // to gl texture
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            //	image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            System.Drawing.Imaging.BitmapData bitmapdata = bitmap.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, txtid[0]);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_R, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, 4, bitmap.Width, bitmap.Height, 0, Gl.GL_RGBA,
                Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

            return alphas;
        }

        public void clearContext()
        {
            this.currMeshClass = null;
            _selectedParts = new List<Part>();
            _models = new List<Model>();
            _modelViewers = new List<ModelViewer>();
            _partViewers = new List<ModelViewer>();
            this.meshClasses.Clear();
        }

        private void clearHighlights()
        {
            _selectedParts.Clear();
            _selectedNode = null;
            _hightlightAxis = -1;
        }// clearHighlights

        public void loadMesh(string filename)
        {
            this.clearContext();

            Mesh m = new Mesh(filename, _unitifyMesh);
            MeshClass mc = new MeshClass(m);
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;
            _currModel = new Model(m);
            _models = new List<Model>();
            _models.Add(_currModel);
        }// loadMesh

        public void importMesh(string filename, bool multiple)
        {
            // if import multiple meshes, do not unify each mesh
            Mesh m = new Mesh(filename, multiple ? false : _unitifyMesh); 
            MeshClass mc = new MeshClass(m);
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;
        }// importMesh

        public string getStats()
        {
            if (_currModel == null)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("#part:   ");
            sb.Append(_currModel._NPARTS.ToString());

            if (_currModel._MESH != null)
            {
                sb.Append("\n#vertex:   ");
                sb.Append(_currModel._MESH.VertexCount.ToString());
                sb.Append("\n#edge:     ");
                sb.Append(_currModel._MESH.Edges.Length.ToString());
                sb.Append("\n#facee:    ");
                sb.Append(_currModel._MESH.FaceCount.ToString());
            }
            return sb.ToString();
        }// getStats

        public void loadTriMesh(string filename)
        {
            //MessageBox.Show("Trimesh is not activated in this version.");
            //return;

            this.clearContext();
            MeshClass mc = new MeshClass();
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;
            this.Refresh();
        }// loadTriMesh

        public void saveObj(Mesh mesh, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (mesh == null)
            {
                // save a mesh for the current model
                if (_currModel != null)
                {
                    this.saveObj(_currModel._MESH, filename);
                }
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                // vertex
                string s = "";
                for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                {
                    s = "v";
                    s += " " + mesh.VertexPos[j++].ToString();
                    s += " " + mesh.VertexPos[j++].ToString();
                    s += " " + mesh.VertexPos[j++].ToString();
                    sw.WriteLine(s);
                }
                // face
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    s = "f";
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    sw.WriteLine(s);
                }
            }
        }// saveObj

        public void saveMergedObj(string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (this.meshClasses == null)
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (MeshClass mc in this.meshClasses)
                {
                    Mesh mesh = mc.Mesh;

                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = "v";
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                    // face
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "f";
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveMergedObj

        public void swithcXY()
        {
            foreach (Model md in _models)
            {
                Mesh m = md._MESH;
                for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
                {
                    double x = m.VertexPos[j];
                    double y = m.VertexPos[j + 1];
                    double z = m.VertexPos[j + 2];
                    m.setVertextPos(i, new Vector3d(y, x, z));
                }
            }
            this.Refresh();
        }// swithcXY

        public void swithcXZ()
        {
            foreach (Model md in _models)
            {
                Mesh m = md._MESH;
                for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
                {
                    double x = m.VertexPos[j];
                    double y = m.VertexPos[j + 1];
                    double z = m.VertexPos[j + 2];
                    m.setVertextPos(i, new Vector3d(z, y, x));
                }
            }
            this.Refresh();
        }// swithcXZ

        public void swithcYZ()
        {
            foreach (Model md in _models)
            {
                Mesh m = md._MESH;
                for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
                {
                    double x = m.VertexPos[j];
                    double y = m.VertexPos[j + 1];
                    double z = m.VertexPos[j + 2];
                    m.setVertextPos(i, new Vector3d(x, -z, y));
                }
                m.calculateFaceVertexNormal();
                foreach (Part p in md._PARTS)
                {
                    Mesh pm = p._MESH;
                    for (int i = 0, j = 0; i < pm.VertexCount; i++, j += 3)
                    {
                        double x = pm.VertexPos[j];
                        double y = pm.VertexPos[j + 1];
                        double z = pm.VertexPos[j + 2];
                        pm.setVertextPos(i, new Vector3d(x, -z, y));
                    }
                    pm.calculateFaceVertexNormal();
                }                
                md.setMesh(m);
            }
            this.Refresh();
        }// swithcYZ

        public void saveAPartBasedModel(string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (this._currModel == null)
            {
                return;
            }
            // Data fromat:
            // n Parts
            // Part #i:
            // bbox vertices
            // mesh file loc
            // comments start with "%"
            string meshDir = filename.Substring(0, filename.LastIndexOf('.')) + "\\";
            int loc = filename.LastIndexOf('\\');
            string meshFolder = filename.Substring(loc, filename.LastIndexOf('.') - loc);
            if (!Directory.Exists(meshDir))
            {
                Directory.CreateDirectory(meshDir);
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(_currModel._NPARTS.ToString() + " parts");
                for (int i = 0; i < _currModel._NPARTS; ++i)
                {
                    sw.WriteLine("% Part #" + i.ToString());
                    // bounding box
                    Part ipart = _currModel._PARTS[i];
                    foreach (Vector3d v in ipart._BOUNDINGBOX._POINTS3D)
                    {
                        sw.Write(string.Format("{0:0.###}", v.x) + " " +
                            string.Format("{0:0.###}", v.y) + " " +
                            string.Format("{0:0.###}", v.z) + " ");
                    }
                    sw.WriteLine();
                    // save mesh
                    string meshName = "part_" + i.ToString() + ".obj";
                    this.saveObj(ipart._MESH, meshDir + meshName);
                    sw.WriteLine(meshFolder + "\\" + meshName);
                }
            }
        }// saveAPartBasedModel

        public void loadAPartBasedModel(string filename)
        {
            if (!File.Exists(filename))
            {
                MessageBox.Show("File does not exist!");
                return;
            }
            this.clearHighlights();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = "%";
                while (s.Length > 0 && s[0] == '%')
                {
                    s = sr.ReadLine().Trim();
                }
                string[] strs = s.Split(separator);
                int n = 0;
                try {
                    n = Int16.Parse(strs[0]);
                }
                catch (System.FormatException)
                {
                    MessageBox.Show("Wrong data format - need to know #n parts.");
                    return;
                }
                List<Part> parts = new List<Part>();
                string folder = filename.Substring(0, filename.LastIndexOf('\\'));
                for (int i = 0; i < n; ++i)
                {
                    // read a part
                    // bbox vertices:
                    s = sr.ReadLine().Trim(); // description of #i part
                    while (s.Length > 0 && s[0] == '%')
                    {
                        s = sr.ReadLine().Trim();
                    }
                    strs = s.Split(separator);
                    if (strs.Length != Common._nPrimPoint * 3)
                    {
                        MessageBox.Show("Need " + Common._nPrimPoint.ToString() + " vertices for bounding box #" + i.ToString() + ".");
                        return;
                    }
                    Vector3d[] pnts = new Vector3d[Common._nPrimPoint];
                    for (int j = 0, k = 0; j < Common._nPrimPoint; ++j)
                    {
                        pnts[j] = new Vector3d(double.Parse(strs[k++]), double.Parse(strs[k++]), double.Parse(strs[k++])); 
                    }
                    Prim prim = new Prim(pnts);
                    // mesh loc:
                    s = sr.ReadLine().Trim();
                    while (s.Length > 0 && s[0] == '%')
                    {
                        s = sr.ReadLine().Trim();
                    }
                    string meshFile = folder + s;
                    if (!File.Exists(meshFile))
                    {
                        MessageBox.Show("Mesh does not exist at #" + i.ToString() + ".");
                    }
                    Mesh mesh = new Mesh(meshFile, false);
                    Part part = new Part(mesh, prim);
                    parts.Add(part);
                }
                _currModel = new Model(parts);
                this.setCurrentModel(_currModel);
            }
            this.Refresh();
        }// loadAPartBasedModel

        public List<ModelViewer> loadPartBasedModels(string segfolder)
        {
            if (!Directory.Exists(segfolder))
            {
                MessageBox.Show("Directory does not exist!");
                return null;
            }
            this.clearContext();
            string[] files = Directory.GetFiles(segfolder);
            foreach (string file in files)
            {
                loadAPartBasedModel(file);
                ModelViewer modelViewer = new ModelViewer(_currModel, this);
                _modelViewers.Add(modelViewer);
            }
            return _modelViewers;
        }// loadPartBasedModels

        private void refreshModelViewers()
        {
            // view the same as the main view
            foreach (ModelViewer mv in _modelViewers)
            {
                mv.Refresh();
            }
            foreach (ModelViewer mv in _partViewers)
            {
                mv.Refresh();
            }
        }// refreshModelViewers

        public void setCurrentModel(Model m)
        {
            _currModel = m;
            _selectedParts.Clear();
            _models.Clear();
            _models.Add(_currModel);
            this.cal2D();
            this.Refresh();
        }
     
        private void cal2D()
        {
            // otherwise when glViewe is initialized, it will run this function from MouseUp()
            //if (this.currSegmentClass == null) return;

            // reset the current 3d transformation again to check in the camera info, projection/modelview
            Gl.glViewport(0, 0, this.Width, this.Height);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();


            double aspect = (double)this.Width / this.Height;
            if (this.nPointPerspective == 3)
            {
                Glu.gluPerspective(90, aspect, 0.1, 1000);
            }
            else
            {
                Glu.gluPerspective(45, aspect, 0.1, 1000);
            }
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Matrix4d transformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective);
            Matrix4d m = transformMatrix * this._currModelTransformMatrix;

            m = Matrix4d.TranslationMatrix(this.objectCenter) * m * Matrix4d.TranslationMatrix(
                new Vector3d() - this.objectCenter);

            this.calculatePoint2DInfo();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);

            Gl.glPushMatrix();
            Gl.glMultMatrixd(m.Transpose().ToArray());
        }//cal2D

        private void calculatePoint2DInfo()
        {
            this.updateCamera();
            if (_humanPose != null)
            {
                foreach (BodyNode bn in _humanPose._bodyNodes)
                {
                    Vector2d v2 = this.camera.Project(bn._POS).ToVector2d();
                    bn._pos2 = new Vector2d(v2.x, this.Height - v2.y);
                }
            }
            if (this._currModel == null || this._currModel._PARTS == null) 
                return;
            Vector2d max_coord = Vector2d.MinCoord();
            Vector2d min_coord = Vector2d.MaxCoord();
            foreach (Part p in this._currModel._PARTS)
            {
                Prim box = p._BOUNDINGBOX;
                for (int i = 0; i < box._POINTS3D.Length; ++i)
                {
                    Vector2d v2 = this.camera.Project(box._POINTS3D[i]).ToVector2d();
                    p._BOUNDINGBOX._POINTS2D[i] = new Vector2d(v2.x, this.Height - v2.y); 
                }
            }
        }// calculatePoint2DInfo

        public void writeModelViewMatrix(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < 4; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        sw.Write(this._currModelTransformMatrix[i, j].ToString() + " ");
                    }
                }
            }
        }

        public void readModelModelViewMatrix(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ' };
                string s = sr.ReadLine();
                s.Trim();
                string[] strs = s.Split(separator);
                double[] arr = new double[strs.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    if (strs[i] == "") continue;
                    arr[i] = double.Parse(strs[i]);
                }
                this._currModelTransformMatrix = new Matrix4d(arr);
                this._fixedModelView = new Matrix4d(arr);
            }
        }

        public void captureScreen(int idx)
        {
            Size newSize = new System.Drawing.Size(0,0); 
            var bmp = new Bitmap(newSize.Width, newSize.Height);
            var gfx = Graphics.FromImage(bmp);
            gfx.CopyFromScreen((int)(this.Location.X - 60),
                Screen.PrimaryScreen.Bounds.Y + 110, 0, 0, newSize, CopyPixelOperation.SourceCopy);
            string imageFolder = foldername + "\\screenCapture";
            
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }
            string name = imageFolder + "\\seq_" + idx.ToString() + ".png";
            bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
        }

        private Vector3d getCurPos(Vector3d v)
        {
            return (this._currModelTransformMatrix * new Vector4d(v, 1)).ToVector3D();
        }

        private Vector3d getOriPos(Vector3d v)
        {
            return (this._modelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
        }

		public void renderToImage(string filename)
		{
			//uint FramerbufferName = 0;
			//Gl.glGenFramebuffersEXT(1, out FramerbufferName);
			//Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, FramerbufferName);
			this.Draw3D();
			int w = this.Width, h = this.Height;
			Bitmap bmp = new Bitmap(w, h);
			Rectangle rect = new Rectangle(0,0,w,h);
			System.Drawing.Imaging.BitmapData data =
				bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				//System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

			Gl.glReadPixels(0, 0, w, h, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, data.Scan0);
			bmp.UnlockBits(data);
			bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
			bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
		}

		private void writeALine(StreamWriter sw, Vector2d u, Vector2d v, float width, Color color)
		{
			sw.WriteLine("newpath");
			double x = u.x;
			double y = u.y;
			sw.Write(x.ToString() + " ");
			sw.Write(y.ToString() + " ");
			sw.WriteLine("moveto ");
			x = v.x;
			y = v.y;
			sw.Write(x.ToString() + " ");
			sw.Write(y.ToString() + " ");
			sw.WriteLine("lineto ");
			//sw.WriteLine("gsave");
			sw.WriteLine(width.ToString() + " " + "setlinewidth");
			float[] c = { (float)color.R / 255, (float)color.G / 255, (float)color.B / 255 };
			sw.WriteLine(c[0].ToString() + " " +
				c[1].ToString() + " " +
				c[2].ToString() + " setrgbcolor");
			sw.WriteLine("stroke");
		}

		private void writeATriangle(StreamWriter sw, Vector2d u, Vector2d v, Vector2d w, float width, Color color)
		{
			sw.WriteLine("newpath");
			double x = u.x;
			double y = u.y;
			sw.Write(x.ToString() + " ");
			sw.Write(y.ToString() + " ");
			sw.WriteLine("moveto ");
			x = v.x;
			y = v.y;
			sw.Write(x.ToString() + " ");
			sw.Write(y.ToString() + " ");
			sw.WriteLine("lineto ");
			sw.Write(w.x.ToString() + " ");
			sw.Write(w.y.ToString() + " ");
			sw.WriteLine("lineto ");
			sw.WriteLine("closepath");
			sw.WriteLine("gsave");
			
			float[] c = { (float)color.R / 255, (float)color.G / 255, (float)color.B / 255 };
			sw.WriteLine("grestore");
			sw.WriteLine(width.ToString() + " " + "setlinewidth");
			sw.WriteLine(c[0].ToString() + " " +
				c[1].ToString() + " " +
				c[2].ToString() + " setrgbcolor");
			sw.WriteLine("stroke");
		}

		private void writeACircle(StreamWriter sw, Vector2d center, float radius, Color color, float width)
		{			
			float[] c = { (float)color.R / 255, (float)(float)color.G / 255, (float)(float)color.B / 255 };
			sw.WriteLine(width.ToString() + " " + "setlinewidth");
			sw.Write(center.x.ToString() + " ");
			sw.Write(center.y.ToString() + " ");
			sw.Write(radius.ToString());
			sw.WriteLine(" 0 360 arc closepath");
			sw.WriteLine(c[0].ToString() + " " +
						c[1].ToString() + " " +
						c[2].ToString() + " setrgbcolor fill");
			sw.WriteLine("stroke");
		}

        //########## set modes ##########//
        public void setTabIndex(int i)
        {
            this.currMeshClass.tabIndex = i;
        }

        public void setUIMode(int i)
        {
            switch (i)
            {
                case 1:
                    this.currUIMode = UIMode.VertexSelection;
                    break;
                case 2:
                    this.currUIMode = UIMode.EdgeSelection;
                    break;
                case 3:
                    this.currUIMode = UIMode.FaceSelection;
                    break;
                case 4:
                    this.currUIMode = UIMode.BoxSelection;
                    break;
                case 6:
                    this.currUIMode = UIMode.Translate;
                    break;
                case 7:
                    this.currUIMode = UIMode.Scale;
                    break;
                case 8:
                    this.currUIMode = UIMode.Rotate;
                    break;
                case 0:
                default:
                    this.currUIMode = UIMode.Viewing;
                    break;
            }
            if (i >= 6 && i <= 8)
            {
                this.calEditAxesLoc();
                _showEditAxes = true;
                this.Refresh();
            }
        }// setUIMode

        public void setRenderOption(int i)
        {
            switch (i)
            {
                case 1:
                    this.drawVertex = !this.drawVertex;
                    break;
                case 2:
                    this.drawEdge = !this.drawEdge;
                    break;
                case 4:
                    this.drawBbox = !this.drawBbox;
                    break;
                case 3:
                default:
                    this.drawFace = !this.drawFace;
                    break;
            }
            this.Refresh();
        }//setRenderOption

        public void displayAxes(bool isShow)
        {
            this.isDrawAxes = isShow;
            this.Refresh();
        }

        private void calEditAxesLoc()
        {
            Vector3d center = new Vector3d();
            double ad = 0;
            foreach (Part p in _selectedParts)
            {
                center += p._BOUNDINGBOX.CENTER;
                double d = (p._BOUNDINGBOX.MaxCoord - p._BOUNDINGBOX.MinCoord).Length();
                ad = ad > d ? ad : d;
            }
            center /= _selectedParts.Count;
            ad /= 2;
            if (ad == 0)
            {
                ad = 0.5;
            }
            double arrow_d = ad / 6;
            _editAxes = new Vector3d[18];
            _editAxes[0] = center - ad * Vector3d.XCoord;
            _editAxes[1] = center + ad * Vector3d.XCoord;
            _editAxes[2] = _editAxes[1] - arrow_d * Vector3d.XCoord + arrow_d * Vector3d.YCoord;
            _editAxes[3] = new Vector3d(_editAxes[1]);
            _editAxes[4] = _editAxes[1] - arrow_d * Vector3d.XCoord - arrow_d * Vector3d.YCoord;
            _editAxes[5] = new Vector3d(_editAxes[1]);

            _editAxes[6] = center - ad * Vector3d.YCoord;
            _editAxes[7] = center + ad * Vector3d.YCoord;
            _editAxes[8] = _editAxes[7] - arrow_d * Vector3d.YCoord + arrow_d * Vector3d.XCoord;
            _editAxes[9] = new Vector3d(_editAxes[7]);
            _editAxes[10] = _editAxes[7] - arrow_d * Vector3d.YCoord - arrow_d * Vector3d.XCoord;
            _editAxes[11] = new Vector3d(_editAxes[7]);

            _editAxes[12] = center - ad * Vector3d.ZCoord;
            _editAxes[13] = center + ad * Vector3d.ZCoord;
            _editAxes[14] = _editAxes[13] - arrow_d * Vector3d.ZCoord + arrow_d * Vector3d.XCoord;
            _editAxes[15] = new Vector3d(_editAxes[13]);
            _editAxes[16] = _editAxes[13] - arrow_d * Vector3d.ZCoord - arrow_d * Vector3d.XCoord;
            _editAxes[17] = new Vector3d(_editAxes[13]);
        }// calEditAxesLoc

        public void resetView()
        {
            this.arcBall.reset();
            if (this.nPointPerspective == 2)
            {
                this.eye = new Vector3d(eyePosition2D);
            }
            else
            {
                this.eye = new Vector3d(eyePosition3D);
            }
            this._currModelTransformMatrix = Matrix4d.IdentityMatrix();
            this._modelTransformMatrix = Matrix4d.IdentityMatrix();
            this.rotMat = Matrix4d.IdentityMatrix();
            this.scaleMat = Matrix4d.IdentityMatrix();
            this.transMat = Matrix4d.IdentityMatrix();
            this.cal2D();
            this.Refresh();
        }
        
        public void reloadView()
        {
            this.arcBall.reset();
            if (this.nPointPerspective == 2)
            {
                this.eye = new Vector3d(eyePosition2D);
            }
            else
            {
                this.eye = new Vector3d(eyePosition3D);
            }
            this._currModelTransformMatrix = new Matrix4d(this._fixedModelView);
            this._modelTransformMatrix = Matrix4d.IdentityMatrix();
            this.cal2D();
            this.Refresh();
        }

        public void reloadView2d()
        {
            this.arcBall.reset();
            this.eye = new Vector3d(eyePosition2D);
            double[] arr = { 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1 };
            // camera model
            //double[] arr = { -1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1 };
            this._currModelTransformMatrix = new Matrix4d(arr);
            this._modelTransformMatrix = Matrix4d.IdentityMatrix();
            this.cal2D();
            this.Refresh();
        }

        private void updateCamera()
        {
            if (this.camera == null) return;
            Matrix4d m = this._currModelTransformMatrix;
            double[] ballmat =  m.Transpose().ToArray();	// matrix applied with arcball
            this.camera.SetBallMatrix(ballmat);
            this.camera.Update();
        }

        //########## Mouse ##########//
        private void viewMouseDown(MouseEventArgs e)
        {
            //if (this.currMeshClass == null) return;
            this.arcBall = new ArcBall(this.Width, this.Height);
            //this._currModelTransformMatrix = Matrix4d.IdentityMatrix();
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Middle:
                    {
                        this.arcBall.mouseDown(e.X, e.Y, ArcBall.MotionType.Pan);
                        break;
                    }
                case System.Windows.Forms.MouseButtons.Right:
                    {
                        this.arcBall.mouseDown(e.X, e.Y, ArcBall.MotionType.Scale);
                        break;
                    }
                case System.Windows.Forms.MouseButtons.Left:
                default:
                    {
                        this.arcBall.mouseDown(e.X, e.Y, ArcBall.MotionType.Rotate);
                        break;
                    }
            }
            this._showEditAxes = false;
            this.clearHighlights();
        }// viewMouseDown

        private void viewMouseMove(int x, int y)
        {
            if (!this.isMouseDown) return;
            this.arcBall.mouseMove(x, y);
        }// viewMouseMove

		private int nPointPerspective = 3;

        private void viewMouseUp()
        {
            this._currModelTransformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
            if (this.arcBall.motion == ArcBall.MotionType.Pan)
            {
				this.transMat = this.arcBall.getTransformMatrix(this.nPointPerspective) * this.transMat;
            }else if  (this.arcBall.motion == ArcBall.MotionType.Rotate)
            {
				this.rotMat = this.arcBall.getTransformMatrix(this.nPointPerspective) * this.rotMat;
            }
            else
            {
				this.scaleMat = this.arcBall.getTransformMatrix(this.nPointPerspective) * this.scaleMat;
            }
            this.arcBall.mouseUp();
            //this._modelTransformMatrix = this.transMat * this.rotMat * this.scaleMat;

            this._modelTransformMatrix = this._currModelTransformMatrix.Transpose();
            this.cal2D();
        }// viewMouseUp

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.mouseDownPos = new Vector2d(e.X, e.Y);
            this.currMousePos = new Vector2d(e.X, e.Y);
            this.isMouseDown = true;
            this.highlightQuad = null;

            switch (this.currUIMode)
            {
                case UIMode.VertexSelection:
                case UIMode.EdgeSelection:
                case UIMode.FaceSelection:
                    {
                        if (this.currMeshClass != null)
                        {
							Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPushMatrix();
                            Gl.glMultMatrixd(m.Transpose().ToArray());

                            this.currMeshClass.selectMouseDown((int)this.currUIMode, 
                                Control.ModifierKeys == Keys.Shift,
                                Control.ModifierKeys == Keys.Control);

                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPopMatrix();

                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.BoxSelection:
                    {
                        if (this._currModel != null)
                        {
                            Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPushMatrix();
                            Gl.glMultMatrixd(m.Transpose().ToArray());

                            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                            {
                                this.ContextMenuStrip = Program.GetFormMain().getRightButtonMenu();
                                this.ContextMenuStrip.Show();
                            }
                            else
                            {
                                this.selectMouseDown((int)this.currUIMode,
                                    Control.ModifierKeys == Keys.Shift,
                                    Control.ModifierKeys == Keys.Control);
                            }
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPopMatrix();

                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.BodyNodeEdit:
                    {
                        this.cal2D();
                    }
                    break;
                case UIMode.Translate:
                    {
                        this.editMouseDown(1, this.mouseDownPos);
                    }
                    break;
                case UIMode.Scale:
                    {
                        this.editMouseDown(2, this.mouseDownPos);
                    }
                    break;
                case UIMode.Rotate:
                    {
                        this.editMouseDown(3, this.mouseDownPos);
                    }
                    break;
                case UIMode.Viewing:
                default:
                    {
                        this.viewMouseDown(e);
                        break;
                    }
            }
            this.Refresh();
        }// OnMouseDown

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            this.prevMousePos = this.currMousePos;
            this.currMousePos = new Vector2d(e.X, e.Y);

            switch (this.currUIMode)
            {
                case UIMode.VertexSelection:
                case UIMode.EdgeSelection:
                case UIMode.FaceSelection:
                    {
                        if (this.currMeshClass != null && this.isMouseDown)
                        {
                            this.highlightQuad = new Quad2d(this.mouseDownPos, this.currMousePos);
                            this.currMeshClass.selectMouseMove((int)this.currUIMode, this.highlightQuad);
                            this.isDrawQuad = true;
                            this.Refresh();
                        }
                        break;
                    }
                case UIMode.BoxSelection:
                    {
                        if (this._currModel != null && this.isMouseDown)
                        {
                            this.highlightQuad = new Quad2d(this.mouseDownPos, this.currMousePos);
                            this.selectMouseMove((int)this.currUIMode, this.highlightQuad,
                                Control.ModifierKeys == Keys.Control);
                            this.isDrawQuad = true;
                            this.Refresh();
                        }
                        break;
                    }
                case UIMode.BodyNodeEdit:
                    {
                        if (this.isMouseDown)
                        {
                            this.EditBodyNode(this.currMousePos);
                        }
                        else
                        {
                            this.SelectBodyNode(this.currMousePos);
                        }
                    }
                    this.Refresh();
                    break;
                case UIMode.Translate:
                case UIMode.Scale:
                case UIMode.Rotate:
                    {
                        if (this.isMouseDown)
                        {
                            this.transformSelectedParts(this.currMousePos);
                        }
                        else
                        {
                            this.selectAxisWhileMouseMoving(this.currMousePos);
                        }
                        this.Refresh();
                    }
                    break;
                case UIMode.Viewing:
                    //default:
                    {
                        if (!this.lockView)
                        {
                            this.viewMouseMove(e.X, e.Y);
                            this.Refresh();
                            this.refreshModelViewers();
                        }
                    }
                    break;
            }
        }// OnMouseMove

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            this.prevMousePos = this.currMousePos;
            this.currMousePos = new Vector2d(e.X, e.Y);
            this.isMouseDown = false;

            switch (this.currUIMode)
            {
                case UIMode.VertexSelection:
                case UIMode.EdgeSelection:
                case UIMode.FaceSelection:
                    {
                        this.isDrawQuad = false;
                        if (this.currMeshClass != null)
                        {
                            this.currMeshClass.selectMouseUp();
                        }
                        //this.Refresh();
                        break;
                    }
                case UIMode.BoxSelection:
                    {
                        this.isDrawQuad = false;
                        if (this._currModel != null && e.Button != System.Windows.Forms.MouseButtons.Right)
                        {
                            this.selectMouseUp(this.highlightQuad, 
                                Control.ModifierKeys == Keys.Shift,
                                Control.ModifierKeys == Keys.Control);
                        }
                        break;
                    }
                case UIMode.BodyNodeEdit:
                    {
                        this.updateBodyBones();
                        this.Refresh();
                    }
                    break;
                case UIMode.Translate:
                case UIMode.Scale:
                case UIMode.Rotate:
                    {
                        this.editMouseUp();
                        this.Refresh();
                    }
                    break;
                case UIMode.Viewing:
                default:
                    {
                        this.viewMouseUp();
                        //this.Refresh();
                        break;
                    }
            }
            this.Refresh();
        }// OnMouseUp

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // to get the correct 2d info of the points3d
            //this.Refresh();
            this.cal2D();
            this.Refresh();
        }

        public void selectMouseDown(int mode, bool isShift, bool isCtrl)
        {
            switch (mode)
            {
                default:
                    break;
            }
        }

        public void selectMouseMove(int mode, Quad2d q, bool isCtrl)
        {
        }

        public void selectMouseUp(Quad2d q, bool isShift, bool isCtrl)
        {
            switch (this.currUIMode)
            {
                case UIMode.BoxSelection:
                    {
                        if (!isShift && !isCtrl)
                        {
                            _selectedParts = new List<Part>();
                        }
                        this.selectBbox(q, isCtrl);
                        break;
                    }
                default:
                    break;
            }
            this.isDrawQuad = false;
        }

        public void acceptKeyData(KeyEventArgs e)
        {
            SendKeys.Send(e.KeyData.ToString());
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control == true && e.KeyCode == Keys.C)
            {
                this.clearContext();
                this.Refresh();
                return;
            }
            switch (e.KeyData)
            {
                case System.Windows.Forms.Keys.V:
                    {
                        this.currUIMode = UIMode.Viewing;
                        break;
                    }
                case Keys.R:
                    {
                        this.reloadView();
                        break;
                    }
                case Keys.I:
                    {
                        this.resetView(); // Identity
                        break;
                    }
                case Keys.B:
                    {
                        this.currUIMode = UIMode.BodyNodeEdit;
                        this.cal2D();
                        break;
                    }
                case Keys.S:
                    {
                        this.currUIMode = UIMode.BoxSelection;
                        break;
                    }
                case Keys.Space:
                    {
                        this.lockView = !this.lockView;
                        break;
                    }
                case Keys.PageDown:
                case Keys.Right:
                    {
                        if (!e.Shift)
                        {
                        }
                        break;
                    }
                case Keys.PageUp:
                case Keys.Left:
                    {
                        break;
                    }
                default:
                    break;
            }
            this.Refresh();
        }// OnKeyDown        

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
        }//OnMouseWheel

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            this.MakeCurrent();

            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            this.Draw3D();
            this.Draw2D();

            this.SwapBuffers();
			
        }// onPaint

        //######### Part-based #########//
        public void selectBbox(Quad2d q, bool isCtrl)
        {
            if (this._currModel == null || q == null) return;
            foreach (Part p in this._currModel._PARTS)
            {
                if (p._BOUNDINGBOX == null) continue;
                if (!isCtrl && _selectedParts.Contains(p))
                {
                    continue;
                }
                foreach (Vector2d v in p._BOUNDINGBOX._POINTS2D)
                {
                    Vector2d v2 = new Vector2d(v);
                    //v2.y = this.Height - v2.y;
                    if (Quad2d.isPointInQuad(v2, q))
                    {
                        if (isCtrl)
                        {
                            _selectedParts.Remove(p);
                            break;
                        }
                        else
                        {
                            this._selectedParts.Add(p);
                        }
                        break;
                    }
                }
            }
        }//selectBbox

        public void setMeshColor(Color c)
        {
        }

        public void groupParts()
        {
            if (_currModel == null)
            {
                return;
            }
            Part newPart = _currModel.groupParts(_selectedParts);
            _selectedParts.Clear();
            _selectedParts.Add(newPart);
            this.cal2D();
            this.Refresh();
        }// groupParts

        public ModelViewer addSelectedPartsToBasket()
        {
            if (_selectedParts == null || _selectedParts.Count == 0)
            {
                return null;
            }
            List<Part> cloneParts = new List<Part>();
            foreach (Part p in _selectedParts)
            {
                Part np = p.Clone() as Part;
                cloneParts.Add(np);
            }
            Model m = new Model(cloneParts);
            ModelViewer mv = new ModelViewer(m, this);
            _partViewers.Add(mv);
            return mv;
        }// addSelectedPartsToBasket

        private void editMouseDown(int mode, Vector2d mousePos)
        {
            _editArcball = new ArcBall(this.Width, this.Height);
            switch (mode)
            {
                case 1: // Translate
                    _editArcball.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Pan);
                    break;
                case 2: // Scaling
                    _editArcball.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Scale);
                    break;
                case 3: // Rotate
                    _editArcball.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Rotate);
                    break;
            }
        }// editMouseDown

        private Matrix4d editMouseMove(int x, int y)
        {
            if (!this.isMouseDown) return Matrix4d.IdentityMatrix();
            _editArcball.mouseMove(x, y);
            Matrix4d T = _editArcball.getTransformMatrix(3);
            return T;
        }// editMouseMove

        private void editMouseUp()
        {
            _hightlightAxis = -1;
            foreach (Part p in _selectedParts)
            {
                p.updateOriginPos();
            }
        }// editMouseUp

        private void transformSelectedParts(Vector2d mousePos)
        {
            if (_selectedParts.Count == 0)
            {
                return;
            }
            Matrix4d T = editMouseMove((int)mousePos.x, (int)mousePos.y);
            // use a fixed axis
            switch (this.currUIMode)
            {
                case UIMode.Translate:
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            if (_hightlightAxis != -1 && i != _hightlightAxis)
                            {
                                T[i, 3] = 0;
                            }
                        }
                        break;
                    }
                case UIMode.Scale:
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            if (_hightlightAxis != -1 && i != _hightlightAxis)
                            {
                                T[i, i] = 1;
                            }
                        }
                        break;
                    }
                case UIMode.Rotate:
                    {
                        if (_hightlightAxis != -1)
                        {
                            T = _editArcball.getRotationMatrixAlongAxis(_hightlightAxis);
                        }
                        break;
                    }
                default:
                    {
                        T = Matrix4d.IdentityMatrix();
                        break;
                    }
            }
            // original center
            Vector3d ori = getCenter(_selectedParts);
            foreach (Part p in _selectedParts)
            {
                p.TransformFromOrigin(T);
            }
            if (this.currUIMode != UIMode.Translate)
            {
                Vector3d after = getCenter(_selectedParts);
                Matrix4d TtoCenter = Matrix4d.TranslationMatrix(ori - after);
                foreach (Part p in _selectedParts)
                {
                    p.Transform(TtoCenter);
                }
            }
        }// transformSelectedParts

        private Vector3d getCenter(List<Part> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return new Vector3d();
            }
            Vector3d center = new Vector3d();
            foreach (Part p in parts)
            {
                center += p._BOUNDINGBOX.CENTER;
            }
            center /= parts.Count;
            return center;
        }// parts

        public void deleteParts()
        {
            foreach (Part p in _selectedParts)
            {
                _currModel.removeAPart(p);
            }
            _selectedParts.Clear();
            this.Refresh();
        }// deleteParts

        public void duplicateParts()
        {
            int n = _selectedParts.Count;
            Matrix4d shift = Matrix4d.TranslationMatrix(new Vector3d(0.2, 0, 0));
            for (int i = 0; i < n; ++i)
            {
                Part p = _selectedParts[i];
                Part pclone = p.Clone() as Part;
                pclone.TransformFromOrigin(shift);
                _currModel.addAPart(pclone);
                _selectedParts.Add(pclone);
            }
            this.Refresh();
        }// deleteParts

        public void composeSelectedParts()
        {
            if (_partViewers == null || _partViewers.Count == 0)
            {
                return;
            }
            _selectedParts.Clear();
            List<Part> parts = new List<Part>();
            foreach (ModelViewer mv in _partViewers)
            {
                List<Part> mv_parts = mv.getParts();
                foreach (Part p in mv_parts)
                {
                    Part pclone = p.Clone() as Part;
                    parts.Add(pclone);
                }
            }            
            _currModel = new Model(parts);
            this.cal2D();
            this.Refresh();
        }// composeSelectedParts

        private void selectAxisWhileMouseMoving(Vector2d mousePos)
        {
            Vector2d s = this.camera.Project(_editAxes[0]).ToVector2d();
            Vector2d e = this.camera.Project(_editAxes[1]).ToVector2d();
            Line2d xline = new Line2d(s, e);
            double xd = Polygon2D.PointDistToLine(mousePos, xline);

            s = this.camera.Project(_editAxes[6]).ToVector2d();
            e = this.camera.Project(_editAxes[7]).ToVector2d();
            Line2d yline = new Line2d(s, e);
            double yd = Polygon2D.PointDistToLine(mousePos, yline);

            s = this.camera.Project(_editAxes[12]).ToVector2d();
            e = this.camera.Project(_editAxes[13]).ToVector2d();
            Line2d zline = new Line2d(s, e);
            double zd = Polygon2D.PointDistToLine(mousePos, zline);

            _hightlightAxis = 0;
            if (yd < xd && yd < zd)
            {
                _hightlightAxis = 1;
            }
            if (zd < xd && zd < yd)
            {
                _hightlightAxis = 2;
            }
        }// selectAxisWhileMouseMoving

        private Matrix4d calTranslation(Vector2d prev, Vector2d curr)
        {
            Vector3d moveDir = new Vector3d();
            moveDir[_hightlightAxis] = 1;

            // distance
            Vector3d u = this.camera.ProjectPointToPlane(prev, _groundPlane.center, _groundPlane.normal);
            Vector3d v = this.camera.ProjectPointToPlane(curr, _groundPlane.center, _groundPlane.normal);
            Vector3d move = (v - u).Length() * moveDir;

            return Matrix4d.TranslationMatrix(move);
        }// calTranslation

        private void calScaling(Vector2d prev, Vector2d curr)
        {
            double sideLen = this.Width > this.Height ? this.Width : this.Height;
            double ratio = (curr - prev).Length() / this.Width;

        }// calScaling

        private void calRotation()
        {

        }

        public void loadHuamPose(string filename)
        {
            _humanPose = new HumanPose();
            _humanPose.loadPose(filename);
            // test
            Matrix4d T = Matrix4d.TranslationMatrix(new Vector3d(0, -0.2, -0.4));
            foreach (BodyNode bn in _humanPose._bodyNodes)
            {
                bn.TransformOrigin(T);
                bn.Transform(T);
            }
            foreach (BodyBone bb in _humanPose._bodyBones)
            {
                bb.updateEntity();
            }
        }// loadHuamPose

        public void saveHumanPose(string name)
        {
            if (_humanPose != null)
            {
                _humanPose.savePose(name);
            }
        }// saveHumanPose

        private void SelectBodyNode(Vector2d mousePos)
        {
            if (_humanPose == null)
            {
                return;
            }
            _selectedNode = null;
            foreach (BodyNode bn in _humanPose._bodyNodes)
            {
                double d = (bn._pos2 - mousePos).Length();
                if (d < Common._thresh2d)
                {
                    _selectedNode = bn;
                    break;
                }
            }
        }// SelectBodyNode

        private void EditBodyNode(Vector2d mousePos)
        {
            if (_selectedNode == null || _selectedNode._PARENT == null)
            {
                return;
            }
            Vector3d originPos = _selectedNode._ORIGIN;
            Vector3d projPos = this.camera.Project(originPos);
            Vector3d p1 = this.camera.UnProject(new Vector3d(mousePos.x, this.Height - mousePos.y, projPos.z));
            Vector3d p2 = this.camera.UnProject(new Vector3d(mousePos.x, this.Height - mousePos.y, projPos.z + 0.5));
            Vector3d dir = (p2 - p1).normalize();

            Vector3d u = p1 - originPos;
            Vector3d q = p1 - u.Dot(dir) * dir;				// the point on the plane parallel to screen
            Vector3d o = _selectedNode._PARENT._POS;
            double length = (originPos - o).Length();
            Vector3d t = o + length * (q - o).normalize();			// the target point
            Matrix4d T = Matrix4d.TranslationMatrix(t - originPos);

            DeformBodyNode(_selectedNode, T);
            DeformBodyNodePropagation(_selectedNode, T);
        }// EditBodyNode

        private void DeformBodyNode(BodyNode node, Matrix4d T)
        {
            if (node == null) return;
            Vector3d pos = node._ORIGIN;
            node._POS = (T * new Vector4d(pos, 1)).ToVector3D();
        }// DeformBodyNode

        private void DeformBodyNodePropagation(BodyNode node, Matrix4d T)
        {
            if (node == null) return;
            List<BodyNode> children = node.getDescendents();
            foreach (BodyNode bn in children)
            {
                Vector3d pos = bn._ORIGIN;
                bn._POS = (T * new Vector4d(pos, 1)).ToVector3D();
            }
        }// DeformBodyNodePropagation

        private void updateBodyBones()
        {
            if (_selectedNode == null) return;
            foreach (BodyBone bn in _humanPose._bodyBones)
            {
                bn.updateEntity();
            }
        }// DeformBodyNodePropagation

        //######### end-Part-based #########//

        private void setViewMatrix()
        {
            int w = this.Width;
            int h = this.Height;
            if (h == 0)
            {
                h = 1;
            }

            Gl.glViewport(0, 0, w, h);

            double aspect = (double)w / h;

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();

            Glu.gluPerspective(90, aspect, 0.1, 1000);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            //Gl.glPushMatrix();

            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Matrix4d transformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective);
            Matrix4d m = transformMatrix * this._currModelTransformMatrix;

            m = Matrix4d.TranslationMatrix(this.objectCenter) * m * Matrix4d.TranslationMatrix(
                new Vector3d() - this.objectCenter);

            foreach (ModelViewer mv in _modelViewers)
            {
                mv.setModelViewMatrix(m);
            }
            foreach (ModelViewer mv in _partViewers)
            {
                mv.setModelViewMatrix(m);
            }

            Gl.glMatrixMode(Gl.GL_MODELVIEW);

            Gl.glPushMatrix();
            Gl.glMultMatrixd(m.Transpose().ToArray());
        }

        private int startWid = 0, startHeig = 0;

        private void initScene()
        {
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            SetDefaultMaterial();

            Gl.glLoadIdentity();
            SetDefaultLight();
        }

        private void DrawLight()
        {
            for (int i = 0; i < lightPositions.Count; ++i)
            {
                Vector3d pos3 = new Vector3d(lightPositions[i][0],
                    lightPositions[i][1],
                    lightPositions[i][2]);
                Vector3d pos2 = this.camera.Project(pos3.x, pos3.y, pos3.z);
                GLDrawer.DrawCircle2(new Vector2d(pos2.x, pos2.y), Color.Yellow, 0.2f);
            }
        }

        private void Draw2D()
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Glu.gluOrtho2D(0, this.Width, this.Height, 0);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();


            if (this.isDrawQuad && this.highlightQuad != null)
            {
                GLDrawer.drawQuadTransparent2d(this.highlightQuad, GLDrawer.SelectionColor);
            }

            //this.DrawHighlight2D();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }

        private void Draw3D()
        {
            this.setViewMatrix();

            /***** Draw *****/

            if (this.isDrawAxes)
            {
                this.drawAxes(_axes, 3.0f);
            }

            if (this.drawGround)
            {
                GLDrawer.drawPlane(_groundPlane, Color.LightGray);
            }

            //Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);

            if (this.enableDepthTest)
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
            }

            // Draw all meshes
            if (_currModel != null)
            {
                this.drawParts();
            }

            this.drawAllMeshes();

            this.drawHumanPose();

            this.DrawHighlight3D();

            

            if (this.enableDepthTest)
            {
                Gl.glDisable(Gl.GL_DEPTH_TEST);
            }

            Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            
        }// Draw3D   

        private void drawAllMeshes()
        {
            if (this.meshClasses == null || _currModel != null)
            {
                return;
            }
            foreach (MeshClass meshclass in this.meshClasses)
            {
                if (this.drawFace)
                {
                    GLDrawer.drawMeshFace(meshclass.Mesh, GLDrawer.MeshColor, false);
                }
                if (this.drawEdge)
                {
                    meshclass.renderWireFrame();
                }
                if (this.drawVertex)
                {
                    meshclass.renderVertices();
                }
                meshclass.drawSelectedVertex();
                meshclass.drawSelectedEdges();
                meshclass.drawSelectedFaces();
            }
        }

        private void drawParts()
        {
            if (this._currModel == null || this._currModel._PARTS == null)
            {
                return;
            }

            foreach (Part part in this._currModel._PARTS)
            {
                if (_selectedParts.Contains(part))
                {
                    continue;
                }
                if (this.drawFace)
                {
                    GLDrawer.drawMeshFace(part._MESH, part._COLOR, false);
                    //GLDrawer.drawMeshFace(part._MESH, GLDrawer.MeshColor, false);
                }
                if (this.drawEdge)
                {
                    GLDrawer.drawMeshEdge(part._MESH);
                }
                if (this.drawBbox)
                {
                    GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, part._COLOR);
                    GLDrawer.drawBoundingboxEdges(part._BOUNDINGBOX, part._COLOR);
                }
            }
        }//drawParts

        private void drawHumanPose()
        {
            if (_humanPose == null)
            {
                return;
            }
            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);
            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);
            if (iNumSamples == 0)
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

                Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
                Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);


                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_SMOOTH);
                Gl.glDepthMask(Gl.GL_FALSE);
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }
            foreach (BodyBone bb in _humanPose._bodyBones)
            {
                GLDrawer.drawCylinder(bb._SRC._POS, bb._DST._POS, bb._RADIUS, GLDrawer.BodeyBoneColor);
                //GLDrawer.drawCylinderTransparent(bb._SRC._POS, bb._DST._POS, Common._bodyNodeRadius/2, GLDrawer.BodyColor);
                for (int i = 0; i < bb._FACEVERTICES.Length; i += 4)
                {
                    GLDrawer.drawQuadTransparent3d(bb._FACEVERTICES[i], bb._FACEVERTICES[i + 1],
                        bb._FACEVERTICES[i + 2], bb._FACEVERTICES[i + 3], GLDrawer.BodyColor);
                }
            }
            foreach (BodyNode bn in _humanPose._bodyNodes)
            {
                if (bn != _selectedNode)
                {
                    GLDrawer.drawSphere(bn._POS, bn._RADIUS, GLDrawer.BodyNodeColor);
                }
            }
            if (iNumSamples == 0)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                Gl.glDepthMask(Gl.GL_TRUE);
                Gl.glDisable(Gl.GL_DEPTH_TEST);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }
            Gl.glPopAttrib();
        }// drawHumanPose

        private void DrawHighlight2D()
        {
        }

        private void DrawHighlight3D()
        {
            if (this._selectedParts != null)
            {
                foreach (Part part in _selectedParts)
                {
                    if (this.drawFace)
                    {
                        GLDrawer.drawMeshFace(part._MESH, GLDrawer.SelectionColor, false);
                    }
                    GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, GLDrawer.SelectionColor);
                    GLDrawer.drawBoundingboxEdges(part._BOUNDINGBOX, GLDrawer.SelectionColor);
                }
            }
            if (_selectedNode != null)
            {
                GLDrawer.drawSphere(_selectedNode._POS, _selectedNode._RADIUS, GLDrawer.SelectedBodyNodeColor);
            }

            if (_showEditAxes)
            {
                this.drawAxes(_editAxes, 4.0f);
            }
        }// DrawHighlight3D

        private void drawAxes(Vector3d[] axes, float wid)
        {
            // draw axes with arrows
            for (int i = 0; i < 6; i += 2)
            {
                GLDrawer.drawLines3D(axes[i], axes[i + 1], _hightlightAxis == 0 ? Color.Yellow : Color.Red, wid);
            }

            for (int i = 6; i < 12; i += 2)
            {
                GLDrawer.drawLines3D(axes[i], axes[i + 1], _hightlightAxis == 1 ? Color.Yellow : Color.Green, wid);
            }

            for (int i = 12; i < 18; i += 2)
            {
                GLDrawer.drawLines3D(axes[i], axes[i + 1], _hightlightAxis == 2 ? Color.Yellow : Color.Blue, wid);
            }
        }// drawAxes

        // Lights & Materials
        public static float[] matAmbient = { 0.1f, 0.1f, 0.1f, 1.0f };
        public static float[] matDiffuse = { 0.7f, 0.7f, 0.5f, 1.0f };
        public static float[] matSpecular = { 1.0f, 1.0f, 1.0f, 1.0f };
        public static float[] shine = { 120.0f };

        private static void SetDefaultLight()
        {
            float[] pos1 = new float[4] { 0.1f, 0.1f, -0.02f, 0.0f };
            float[] pos2 = new float[4] { -0.1f, 0.1f, -0.02f, 0.0f };
            float[] pos3 = new float[4] { 0.0f, 0.0f, 0.1f, 0.0f };
            float[] col1 = new float[4] { 0.7f, 0.7f, 0.7f, 1.0f };
            float[] col2 = new float[4] { 0.8f, 0.7f, 0.7f, 1.0f };
            float[] col3 = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };


            Gl.glEnable(Gl.GL_LIGHT0);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, pos1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, col1);


            Gl.glEnable(Gl.GL_LIGHT1);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, pos2);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_DIFFUSE, col2);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_SPECULAR, col2);


            Gl.glEnable(Gl.GL_LIGHT2);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_POSITION, pos3);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_DIFFUSE, col3);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_SPECULAR, col3);
        }

        public void AddLight(Vector3d pos, Color col)
        {
            int lightID = lightPositions.Count + 16387;
            float[] posA = new float[4] { (float)pos.x, (float)pos.y, (float)pos.z, 0.0f };
            lightPositions.Add(posA);
            float[] colA = new float[4] { col.R / 255.0f, col.G / 255.0f, col.B / 255.0f, 1.0f };
            lightcolors.Add(colA);
            lightIDs.Add(lightID);
        }
        private static void SetDefaultMaterial()
        {
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, matAmbient);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, matDiffuse);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, matSpecular);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shine);
        }

        public static List<float[]> lightPositions = new List<float[]>();
        public static List<float[]> lightcolors = new List<float[]>();
        public static List<int> lightIDs = new List<int>();
        private static void SetAdditionalLight()
        {
            if (lightPositions.Count == 0)
            {
                return;
            }
            for (int i = 0; i < lightPositions.Count; ++i)
            {
                Gl.glEnable(lightIDs[i]);
                Gl.glLightfv(lightIDs[i], Gl.GL_POSITION, lightPositions[i]);
                Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, lightcolors[i]);
                Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, lightcolors[i]);
            }
        }
    }// GLViewer
}// namespace
