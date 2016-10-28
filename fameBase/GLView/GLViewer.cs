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
    public class GLViewer : SimpleOpenGlControl
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

            //this.LoadTextures();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Name = "GlViewer";

            this.ResumeLayout(false);
        }

        private void initializeVariables()
        {
            this._meshClasses = new List<MeshClass>();
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
            initializeGround();
        }

        private void initializeGround()
        {
            Vector3d[] groundPoints = new Vector3d[4] {
                new Vector3d(-1, 0, -1), new Vector3d(-1, 0, 1),
                new Vector3d(1, 0, 1), new Vector3d(1, 0, -1)};
            _groundPlane = new Polygon3D(groundPoints);
            int n = 10;
            _groundGrids = new Vector3d[(n + 1) * 2 * 2];
            Vector3d xf = groundPoints[0];
            Vector3d xt = groundPoints[1];
            Vector3d yf = groundPoints[0];
            Vector3d yt = groundPoints[3];
            double xstep = (xt - xf).Length() / n;
            Vector3d xdir = (xt - xf).normalize();
            int k = 0;
            for (int i = 0; i <= n; ++i)
            {
                _groundGrids[k++] = xf + xdir * xstep * i;
                _groundGrids[k++] = yt + xdir * xstep * i;
            }
            double ystep = (yt - yf).Length() / n;
            Vector3d ydir = (yt - yf).normalize();
            for (int i = 0; i <= n; ++i)
            {
                _groundGrids[k++] = xf + ydir * ystep * i;
                _groundGrids[k++] = xt + ydir * ystep * i;
            }
        }

        // modes
        public enum UIMode
        {
            // !Do not change the order of the modes --- used in the current program to retrieve the index (Integer)
            Viewing, VertexSelection, EdgeSelection, FaceSelection, BoxSelection, BodyNodeEdit,
            Translate, Scale, Rotate, Contact, NONE
        }

        private bool drawVertex = false;
        private bool drawEdge = false;
        private bool drawFace = true;
        private bool isDrawBbox = true;
        private bool isDrawGraph = true;
        private bool isDrawAxes = false;
        private bool isDrawQuad = false;
        private bool isDrawFuncSpace = false;
        public bool isDrawSamplePoints = false;

        public bool enableDepthTest = true;
        public bool showVanishingLines = true;
        public bool lockView = false;
        public bool showFaceToDraw = true;

        public bool showSharpEdge = false;
        public bool enableHiddencheck = true;
        public bool condition = true;

        private static Vector3d eyePosition3D = new Vector3d(0, 0.5, 1.5);
        private static Vector3d eyePosition2D = new Vector3d(0, 1, 1.5);
        Vector3d eye = new Vector3d(0, 0.5, 1.5);
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
        private List<MeshClass> _meshClasses = new List<MeshClass>();
        private MeshClass currMeshClass;
        private Quad2d highlightQuad;
        private Camera camera;
        private Shader shader;
        public static uint pencilTextureId, crayonTextureId, inkTextureId, waterColorTextureId, charcoalTextureId, brushTextureId;

        private List<Model> _crossOverBasket = new List<Model>();
        private int _selectedModelIndex = -1;

        public string foldername = "";
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
        private int meshIdx = 0;

        /******************** Vars ********************/
        Model _currModel;
        List<Model> _models = new List<Model>();
        List<Part> _selectedParts = new List<Part>();
        List<Node> _selectedNodes = new List<Node>();
        List<ModelViewer> _ancesterModelViewers = new List<ModelViewer>();
        HumanPose _currHumanPose;
        List<HumanPose> _humanposes = new List<HumanPose>();
        BodyNode _selectedNode;
        public bool _unitifyMesh = true;
        bool _showEditAxes = false;
        public bool isDrawGround = false;
        private Vector3d[] _axes;
        private Contact[] _editAxes;
        private Polygon3D _groundPlane;
        int _hightlightAxis = -1;
        ArcBall _editArcBall;
        bool _isRightClick = false;
        bool _isDrawTranslucentHumanPose = true;

        private Vector3d[] _groundGrids;
        Edge _selectedEdge = null;
        Contact _selectedContact = null;
        private ReplaceablePair[,] _replaceablePairs = null;
        private int _currIter = 0;
        private int _mutateOrCross = -1;
        private bool _showContactPoint = false;

        List<ModelViewer> _partViewers = new List<ModelViewer>();
        List<List<Model>> _mutateGenerations = new List<List<Model>>();
        List<List<Model>> _crossoverGenerations = new List<List<Model>>();
        List<ModelViewer> _currGenModelViewers = new List<ModelViewer>();
        List<Model> _currGen = new List<Model>();
        List<FunctionalityModel> _functionalityModels = new List<FunctionalityModel>();

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

        public Model getCurrModel()
        {
            return _currModel;
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
            _currModel = null;
            _selectedParts = new List<Part>();
            _models.Clear();
            _ancesterModelViewers.Clear();
            _currHumanPose = null;
            this._meshClasses.Clear();
            _humanposes.Clear();
        }

        private void clearHighlights()
        {
            _selectedNode = null;
            _hightlightAxis = -1;
            _selectedEdge = null;
            _selectedContact = null;
            _selectedParts.Clear();
        }// clearHighlights

        /******************** Load & Save ********************/
        public void loadMesh(string filename)
        {
            this.clearContext();
            Mesh m = new Mesh(filename, _unitifyMesh);
            MeshClass mc = new MeshClass(m);
            this._meshClasses.Add(mc);
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
            this._meshClasses.Add(mc);
            this.currMeshClass = mc;
        }// importMesh

        public string getStats()
        {
            if (_currModel == null)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();

            sb.Append(_currModel._model_name + "\n");
            if (_currModel._GRAPH != null && _currModel._GRAPH._ff._cats.Count > 0)
            {
                string catStr = "";
                foreach (Common.Category cat in _currModel._GRAPH._ff._cats)
                {
                    catStr += cat + " ";
                }
                catStr += "\n";
                sb.Append(catStr);
            }

            sb.Append("#part:   ");
            sb.Append(_currModel._NPARTS.ToString());

            if (_currModel._MESH != null)
            {
                sb.Append("\n#vertex:   ");
                sb.Append(_currModel._MESH.VertexCount.ToString());
                sb.Append("\n#edge:     ");
                sb.Append(_currModel._MESH.EdgeCount.ToString());
                sb.Append("\n#face:    ");
                sb.Append(_currModel._MESH.FaceCount.ToString());
            }
            sb.Append("\n#selected parts: ");
            sb.Append(_selectedParts.Count.ToString());
            sb.Append("\n#human poses: ");
            sb.Append(_humanposes.Count.ToString());
            if (_currModel._GRAPH != null)
            {
                sb.Append("\n#nodes: ");
                sb.Append(_currModel._GRAPH._NNodes.ToString());
                sb.Append("\n#edges: ");
                sb.Append(_currModel._GRAPH._NEdges.ToString());
            }
            sb.Append("\n#iter: " + _currIter.ToString() + " ");
            string mc = _mutateOrCross == -1 ? "" : (_mutateOrCross == 0 ? "mutate" : (_mutateOrCross == 1 ? "crossover" : "growth"));
            sb.Append(mc);
            return sb.ToString();
        }// getStats

        public void loadTriMesh(string filename)
        {
            //MessageBox.Show("Trimesh is not activated in this version.");
            //return;

            this.clearContext();
            MeshClass mc = new MeshClass();
            this._meshClasses.Add(mc);
            this.currMeshClass = mc;
            this.Refresh();
        }// loadTriMesh

        public void saveObj(Mesh mesh, string filename, Color c)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (mesh == null)
            {
                // save a mesh for the current model
                this.saveModelObj(filename);
                return;
            }
            string model_name = filename.Substring(filename.LastIndexOf('\\') + 1);
            model_name = model_name.Substring(0, model_name.LastIndexOf('.'));
            string mtl_name = filename.Substring(0, filename.LastIndexOf('.')) + ".mtl";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                // vertex
                sw.WriteLine("usemtl " + model_name);
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
            using (StreamWriter sw = new StreamWriter(mtl_name))
            {
                sw.WriteLine("newmtl " + model_name);
                sw.Write("Ka ");
                sw.WriteLine(colorToString(c, false));
                sw.Write("Kd ");
                sw.WriteLine(colorToString(c, false));
                sw.Write("Ks ");
                sw.WriteLine(colorToString(c, false));
                sw.Write("ke ");
                sw.WriteLine(colorToString(c, false));
            }
        }// saveObj

        public void saveOffFile(Mesh mesh, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (mesh == null)
            {
                saveModelOff(filename);
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("OFF");
                sw.WriteLine(mesh.VertexCount.ToString() + " " + mesh.FaceCount.ToString() + "  0");
                // vertex
                string s = "";
                for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                {
                    s = mesh.VertexPos[j++].ToString() + " " 
                        + mesh.VertexPos[j++].ToString() + " " 
                        + mesh.VertexPos[j++].ToString();
                    sw.WriteLine(s);
                }
                // face
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    s = "3";
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    sw.WriteLine(s);
                }
            }
        }// saveOffFile

        private void saveModelOff(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("OFF");
                int vertexCount = 0;
                int faceCount = 0;
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    vertexCount += mesh.VertexCount;
                    faceCount += mesh.FaceCount;
                }
                sw.WriteLine(vertexCount.ToString() + " " + faceCount.ToString() + "  0");
                
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = mesh.VertexPos[j++].ToString() + " "
                        + mesh.VertexPos[j++].ToString() + " "
                        + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                }
                int start = 0;
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    // face
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "3";
                        s += " " + (start + mesh.FaceVertexIndex[j++] + 1).ToString();
                        s += " " + (start + mesh.FaceVertexIndex[j++] + 1).ToString();
                        s += " " + (start + mesh.FaceVertexIndex[j++] + 1).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveModelOff

        private string colorToString(Color c, bool space)
        {
            double r = (double)c.R / 255.0;
            double g = (double)c.G / 255.0;
            double b = (double)c.B / 255.0;
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0:0.###}", r) + " ");
            sb.Append(string.Format("{0:0.###}", g) + " ");
            sb.Append(string.Format("{0:0.###}", b));
            if (space)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }// colorToString

        private void saveModelObj(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;

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
                }
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    // face
                    string s = "";
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
        }// saveModelObj

        public void saveMergedObj(string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (this._meshClasses == null)
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (MeshClass mc in this._meshClasses)
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

        public void saveMeshForModel(Model model, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (model == null || model._GRAPH == null)
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Node node in model._GRAPH._NODES)
                {
                    Mesh mesh = node._PART._MESH;

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
        }// saveMeshForModel

        public void saveMeshForModel(List<Node> nodes, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Node node in nodes)
                {
                    Mesh mesh = node._PART._MESH;

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
        }// saveMeshForModel

        public void switchXYZ(int mode)
        {
            foreach (Part p in _selectedParts)
            {
                Common.switchXYZ_mesh(p._MESH, mode);
                if (p._partSP != null)
                {
                    Common.switchXYZ_vectors(p._partSP._points, mode);
                    p._partSP.updateNormals(_currModel._MESH);
                }
                p.fitProxy(-1);
                p.updateOriginPos();
            }
            this.Refresh();
        }// switchXYZ

        public void saveAPartBasedModel(Model model, string filename, bool isOriginalModel)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (model == null)
            {
                return;
            }
            // Data fromat:
            // n Parts
            // Part #i:
            // bbox vertices
            // mesh file loc
            // m edges (for graph)
            // id1, id2
            // ..., ...
            // comments start with "%"
            string meshDir = filename.Substring(0, filename.LastIndexOf('.')) + "\\";
            int loc = filename.LastIndexOf('\\');
            string modelName = filename.Substring(loc + 1, filename.LastIndexOf('.') - loc - 1);
            if (!Directory.Exists(meshDir))
            {
                Directory.CreateDirectory(meshDir);
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(model._NPARTS.ToString() + " parts");
                for (int i = 0; i < model._NPARTS; ++i)
                {
                    sw.WriteLine("% Part #" + i.ToString());
                    // bounding box
                    Part ipart = model._PARTS[i];
                    foreach (Vector3d v in ipart._BOUNDINGBOX._POINTS3D)
                    {
                        sw.Write(vector3dToString(v, " ", " "));
                    }
                    sw.WriteLine();
                    // principal axes
                    sw.Write(vector3dToString(ipart._BOUNDINGBOX.coordSys.x, " ", " "));
                    sw.Write(vector3dToString(ipart._BOUNDINGBOX.coordSys.y, " ", " "));
                    sw.WriteLine(vector3dToString(ipart._BOUNDINGBOX.coordSys.z, " ", ""));
                    // save mesh
                    string meshName = "part_" + i.ToString() + ".obj";
                    this.saveObj(ipart._MESH, meshDir + meshName, ipart._COLOR);
                    sw.WriteLine("\\" + modelName + "\\" + meshName);
                }
                if (model._GRAPH != null)
                {
                    string graphName = filename.Substring(0, filename.LastIndexOf('.')) + ".graph";
                    saveAGraph(graphName);
                }
                saveModelInfo(model, meshDir, modelName, isOriginalModel);
            }
        }// saveAPartBasedModel

        private void saveModelInfo(Model model, string foldername, string model_name, bool isOriginalModel)
        {
            if (model == null)
            {
                return;
            }
            // save mesh
            string meshName = model._path +  model_name + "\\" + model_name + ".obj";
            if (model._NPARTS > 0)
            {
                this.saveObj(null, meshName, GLDrawer.MeshColor);
            }
            else
            {
                this.saveObj(model._MESH, meshName, GLDrawer.MeshColor);
            }
            // save mesh sample points & normals & faceindex
            if (model._SP != null)
            {
                string modelSPname = foldername + model_name + ".sp";
                this.saveSamplePointsInfo(model._SP, modelSPname);
                string spColorname = foldername + model_name + ".color";
                this.saveSamplePointsColor(model._SP._blendColors, spColorname);
            }
            for (int i = 0; i < _currModel._NPARTS; ++i)
            {
                Part ipart = _currModel._PARTS[i];
                if (ipart._partSP == null || ipart._partSP._points == null || ipart._partSP._normals == null)
                {
                    MessageBox.Show("No sample point in part #" + i.ToString());
                    return;
                }
                string partSPname = foldername + "part_" + i.ToString() + ".sp";
                this.saveSamplePointsInfo(ipart._partSP, partSPname);
                string spColorname = foldername + "part_" + i.ToString() + ".color";
                this.saveSamplePointsColor(ipart._partSP._blendColors, spColorname);
                // part mesh index info
                if (isOriginalModel)
                {
                    // Note - this is not necessary for new shapes
                    string partMeshIndexName = foldername + "part_" + i.ToString() + ".mi";
                    this.savePartMeshIndexInfo(ipart, partMeshIndexName);
                }
            }
        }// saveModelInfo

        public void savePointFeature()
        {
            if (_currModel == null || _currModel._GRAPH == null)
            {
                return;
            }
            // save .off file & .pts file for shape2pose
            string offname = _currModel._path + _currModel._model_name + ".off";
            string ptsname = _currModel._path + _currModel._model_name + ".pts";
            this.saveModelOff(offname);
            this.saveModelSamplePoints(ptsname);
            string shape2poseDataFolder = _currModel._path + "shape2pose\\";
            if (!Directory.Exists(shape2poseDataFolder))
            {
                Directory.CreateDirectory(shape2poseDataFolder);
            }
            string exeFolder = @"..\..\external\";
            string exePath = Path.GetFullPath(exeFolder);
            _currModel._GRAPH.computeShape2PoseFeatures(_currModel._path, _currModel._model_name, exePath, shape2poseDataFolder);
            //_currModel._GRAPH.computeFeatures();
            //this.writeSampleFeatureFilesForPrediction(this._currModel._GRAPH._NODES, _currModel, "");
        }

        private void saveModelSamplePoints(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    SamplePoints sp = node._PART._partSP;
                    for (int j = 0; j < sp._points.Length; ++j)
                    {
                        Vector3d vpos = sp._points[j];
                        sw.Write(vector3dToString(vpos, " ", " "));
                        Vector3d vnor = sp._normals[j];
                        sw.Write(vector3dToString(vnor, " ", " "));
                        int fidx = start + sp._faceIdx[j];
                        sw.WriteLine(fidx.ToString());
                    }
                    start += node._PART._MESH.FaceCount;
                }
            }
        }// saveModelSamplePoints

        private void saveSamplePointsInfo(SamplePoints sp, string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                //sw.WriteLine(sp._points.Length.ToString());
                for (int j = 0; j < sp._points.Length; ++j)
                {
                    Vector3d vpos = sp._points[j];
                    sw.Write(vector3dToString(vpos, " ", " "));
                    Vector3d vnor = sp._normals[j];
                    sw.Write(vector3dToString(vnor, " ", " "));
                    sw.WriteLine(sp._faceIdx[j].ToString());
                }
            }
        }// saveSamplePointsInfo

        private void saveSamplePointsColor(Color[] colors, string filename)
        {
            if (colors == null || colors.Length == 0)
            {
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int j = 0; j < colors.Length; ++j)
                {
                    Color c = colors[j];
                    sw.WriteLine(c.R.ToString() + " " + c.G.ToString() + " " + c.B.ToString());
                }
            }
        }// saveSamplePointsColor

        private void savePartMeshIndexInfo(Part part, string filename)
        {
            if (part._VERTEXINDEX == null || part._FACEVERTEXINDEX == null)
            {
                MessageBox.Show("The part lack index info from the model mesh.");
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(part._VERTEXINDEX.Length.ToString());
                for (int i = 0; i < part._VERTEXINDEX.Length; ++i)
                {
                    sw.WriteLine(part._VERTEXINDEX[i].ToString());
                }
                sw.WriteLine(part._FACEVERTEXINDEX.Length.ToString());
                for (int i = 0; i < part._FACEVERTEXINDEX.Length; ++i)
                {
                    sw.WriteLine(part._FACEVERTEXINDEX[i].ToString());
                }
            }
        }// savePartMeshIndexInfo

        private void loadPartMeshIndexInfo(string filename, out int[] vertexIndex, out int[] faceVertexIndex)
        {
            vertexIndex = null;
            faceVertexIndex = null;
            if (!File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };

                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separators);
                int nv = int.Parse(strs[0]);
                vertexIndex = new int[nv];
                for (int i = 0; i < nv; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separators);
                    vertexIndex[i] = int.Parse(strs[0]);
                }
                s = sr.ReadLine().Trim();
                strs = s.Split(separators);
                int nf = int.Parse(strs[0]);
                faceVertexIndex = new int[nf];
                for (int i = 0; i < nf; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separators);
                    faceVertexIndex[i] = int.Parse(strs[0]);
                }
            }
        }// loadPartMeshIndexInfo

        private string vector3dToString(Vector3d v, string sep, string tail)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0:0.###}", v.x) + sep +
                            string.Format("{0:0.###}", v.y) + sep +
                            string.Format("{0:0.###}", v.z));
            sb.Append(tail);
            return sb.ToString();
        }// vector3dToString

        private Model loadOnePartBasedModel(string filename)
        {
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
                try
                {
                    n = Int16.Parse(strs[0]);
                }
                catch (System.FormatException)
                {
                    MessageBox.Show("Wrong data format - need to know #n parts.");
                    return null;
                }
                List<Part> parts = new List<Part>();
                string folder = filename.Substring(0, filename.LastIndexOf('\\'));
                string modelName = filename.Substring(filename.LastIndexOf('\\') + 1);
                modelName = modelName.Substring(0, modelName.LastIndexOf('.'));
                string partfolder = filename.Substring(0, filename.LastIndexOf('.'));
                // load mesh
                string meshName = partfolder + "\\" + modelName + ".obj";
                Mesh modelMesh = null;
                if (File.Exists(meshName))
                {
                    modelMesh = new Mesh(meshName, false);
                }
                // mesh sample points
                string modelSPname = partfolder + "\\" + modelName + ".sp";
                SamplePoints sp = this.loadSamplePoints(modelSPname, modelMesh == null ? 0 : modelMesh.FaceCount);
                string spColorname = partfolder + "\\" + modelName + ".color";
                sp._blendColors = this.loadSamplePointsColors(spColorname);
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
                    int nVertices = strs.Length / 3;
                    Vector3d[] pnts = new Vector3d[nVertices];
                    for (int j = 0, k = 0; j < nVertices; ++j)
                    {
                        pnts[j] = new Vector3d(double.Parse(strs[k++]), double.Parse(strs[k++]), double.Parse(strs[k++]));
                    }
                    Prism prim = new Prism(pnts);
                    // coord system
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    bool hasPrism = false;
                    if (strs.Length == 9)
                    {
                        hasPrism = true;
                        pnts = new Vector3d[3];
                        for (int j = 0, k = 0; j < 3; ++j)
                        {
                            pnts[j] = new Vector3d(double.Parse(strs[k++]), double.Parse(strs[k++]), double.Parse(strs[k++]));
                        }
                        prim.coordSys = new CoordinateSystem(prim.CENTER, pnts[0], pnts[1], pnts[2]);
                        s = sr.ReadLine().Trim();
                    }
                    // mesh loc:                    
                    while (s.Length > 0 && s[0] == '%')
                    {
                        s = sr.ReadLine().Trim();
                    }
                    string meshFile = folder + s;
                    if (!File.Exists(meshFile))
                    {
                        MessageBox.Show("Mesh does not exist at #" + i.ToString() + ".");
                        return null;
                    }
                    Mesh mesh = new Mesh(meshFile, false);
                    //Part part = new Part(mesh);
                    Part part = hasPrism ? new Part(mesh, prim) : new Part(mesh);
                    string partSPname = partfolder + "\\part_" + i.ToString() + ".sp";
                    part._partSP = loadSamplePoints(partSPname, mesh.FaceCount);
                    // part mesh index
                    string partMeshIndexInfoName = partfolder + "\\part_" + i.ToString() + ".mi";
                    int[] vertexIndex;
                    int[] faceVertexIndex;
                    this.loadPartMeshIndexInfo(partMeshIndexInfoName, out vertexIndex, out faceVertexIndex);
                    part._VERTEXINDEX = vertexIndex;
                    part._FACEVERTEXINDEX = faceVertexIndex;
                    parts.Add(part);
                }
                Model model = new Model(parts);
                model.setMesh(modelMesh);
                model._SP = sp;
                model._path = filename.Substring(0, filename.LastIndexOf('\\') + 1);
                string name = filename.Substring(filename.LastIndexOf('\\') + 1);
                model._model_name = name.Substring(0, name.LastIndexOf('.'));
                return model;
            }
        }// loadOnePartBasedModel

        public void loadAPartBasedModel(string filename)
        {
            if (!File.Exists(filename))
            {
                MessageBox.Show("File does not exist!");
                return;
            }
            this.foldername = Path.GetDirectoryName(filename);
            this.clearHighlights();
            _currModel = this.loadOnePartBasedModel(filename);
            if (_currModel == null)
            {
                return;
            }
            string graphName = filename.Substring(0, filename.LastIndexOf('.')) + ".graph";
            if (!File.Exists(graphName))
            {
                _currModel.initialize();
            }
            else
            {
                LoadAGraph(_currModel, graphName, true);
            }
            this.setUIMode(0);
            this.Refresh();
        }// loadAPartBasedModel

        public void importPartBasedModel(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
            {
                MessageBox.Show("No model loaded!");
                return;
            }
            this.clearHighlights();
            if (_currModel == null)
            {
                _currModel = new Model();
            }
            foreach (string file in filenames)
            {
                Model m = loadOnePartBasedModel(file);
                foreach (Part p in m._PARTS)
                {
                    _currModel.addAPart(p);
                }
            }
            // rebuild the graph
            _currModel.initialize();
            this.Refresh();
        }// importPartBasedModel

        public List<ModelViewer> loadPartBasedModels(string segfolder)
        {
            if (!Directory.Exists(segfolder))
            {
                MessageBox.Show("Directory does not exist!");
                return null;
            }
            this.foldername = segfolder;
            this.clearContext();
            this.clearHighlights();
            string[] files = Directory.GetFiles(segfolder, "*.pam");
            int idx = 0;
            Program.writeToConsole("Loading all " + files.Length.ToString() + " models...");
            foreach (string file in files)
            {
                Program.writeToConsole("Loading Model info @" + (idx+1).ToString() + "...");
                Model m = loadOnePartBasedModel(file);
                if (m != null)
                {
                    _models.Add(m);
                    string graphName = file.Substring(0, file.LastIndexOf('.')) + ".graph";
                    LoadAGraph(m, graphName, false);
                    ModelViewer modelViewer = new ModelViewer(m, idx++, this, 0); // ancester
                    _ancesterModelViewers.Add(modelViewer);
                }
            }
            if (_ancesterModelViewers.Count > 0)
            {
                this.setCurrentModel(_ancesterModelViewers[_ancesterModelViewers.Count - 1]._MODEL, _ancesterModelViewers.Count - 1);
            }
            this.readModelModelViewMatrix(foldername + "\\view.mat");

            // try to load replaceable pairs
            tryLoadReplaceablePairs();
            this.setUIMode(0);
            return _ancesterModelViewers;
        }// loadPartBasedModels

        public string loadAShapeNetModel(string foldername)
        {
            if (!Directory.Exists(foldername))
            {
                return "";
            }
            // load points
            string points_folder = foldername + "\\points";
            string expert_lable_folder = foldername + "\\expert_verified\\points_label\\";
            string obj_folder = foldername + "\\objs\\";
            string[] points_files = Directory.GetFiles(points_folder);
            int idx = 0;
            int max_idx = 1;
            string points_name = foldername.Substring(foldername.LastIndexOf('\\') + 1);
            this._meshClasses = new List<MeshClass>();
            foreach (String file in points_files)
            {
                if (!file.EndsWith(".pts"))
                {
                    continue;
                }
                // find if there is a .seg file (labeling)
                string mesh_name = file.Substring(file.LastIndexOf('\\') + 1);
                mesh_name = mesh_name.Substring(0, mesh_name.LastIndexOf('.'));
                string seg_file = mesh_name + ".seg";
                seg_file = expert_lable_folder + seg_file;
                string obj_file = obj_folder + mesh_name + "\\" + "model.obj";
                if (!File.Exists(seg_file))
                {
                    continue;
                }
                //Mesh m = loadPointCloud(file, seg_file);
                if (!File.Exists(obj_file))
                {
                    continue;
                }
                Mesh m = loadPointCloud(file, obj_file, seg_file);
                if (m != null)
                {
                    this.currMeshClass = new MeshClass(m);
                    this.currMeshClass._MESHNAME = mesh_name;
                    this._meshClasses.Add(this.currMeshClass);
                    ++idx;
                }
                if (idx >= max_idx)
                {
                    break;
                }
            }
            if (this._meshClasses.Count > 0)
            {
                this.currMeshClass = this._meshClasses[0];
            }
            // only points
            this.setRenderOption(1);
            this.meshIdx = -1;
            string str = nextMeshClass();
            return str;
        }// loadAShapeNetModel

        private Mesh loadPointCloud(string points_file, string seg_file)
        {
            if (!File.Exists(points_file) || !File.Exists(seg_file))
            {
                return null;
            }
            char[] separator = { ' ', '\t' };
            List<double> vertices = new List<double>();
            List<byte> colors = new List<byte>();
            using (StreamReader sr = new StreamReader(points_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length >= 3)
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            vertices.Add(double.Parse(strs[i]));
                        }
                    }
                }
            }
            using (StreamReader sr = new StreamReader(seg_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length > 0)
                    {
                        int label = int.Parse(strs[0]);
                        Color c = GLDrawer.ColorSet[label];
                        colors.Add(c.R);
                        colors.Add(c.G);
                        colors.Add(c.B);
                    }
                }
                Mesh m = new Mesh(vertices.ToArray(), colors.ToArray());
                return m;
            }
        }// loadPointCloud

        private Mesh loadPointCloud(string points_file, string obj_file, string seg_file)
        {
            if (!File.Exists(obj_file) || !File.Exists(seg_file))
            {
                return null;
            }
            char[] separator = { ' ', '\t' };
            List<double> vertices = new List<double>();
            List<byte> colors = new List<byte>();
            using (StreamReader sr = new StreamReader(points_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length >= 3)
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            vertices.Add(double.Parse(strs[i]));
                        }
                    }
                }
            }
            Mesh m = new Mesh(obj_file, false);
            using (StreamReader sr = new StreamReader(seg_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length > 0)
                    {
                        int label = int.Parse(strs[0]);
                        Color c = GLDrawer.ColorSet[label];
                        colors.Add(c.R);
                        colors.Add(c.G);
                        colors.Add(c.B);
                    }
                }
            }
            if (m.VertexCount != vertices.Count / 3)
            {
                return null;
            }
            return m;
        }// loadPointCloud
        /******************** End - Load & Save ********************/

        public string nextModel()
        {
            if (_models.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx + 1) % this._models.Count;
            this._currModel = this._models[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._models.Count.ToString() + ": ";
            str += this._currModel._model_name;
            return str;
        }

        public string prevModel()
        {
            if (_models.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx - 1 + this._models.Count) % this._models.Count;
            this._currModel = this._models[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._models.Count.ToString() + ": ";
            str += this._currModel._model_name;
            return str;
        }

        public string nextMeshClass()
        {
            if (this._meshClasses.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx + 1) % this._meshClasses.Count;
            this.currMeshClass = this._meshClasses[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._meshClasses.Count.ToString() + ": ";
            str += this.currMeshClass._MESHNAME;
            return str;
        }

        public string prevMeshClass()
        {
            if (this._meshClasses.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx - 1 + this._meshClasses.Count) % this._meshClasses.Count;
            this.currMeshClass = this._meshClasses[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._meshClasses.Count.ToString() + ": ";
            str += this.currMeshClass._MESHNAME;
            return str;
        }

        public void refit_by_cylinder()
        {
            if (_selectedParts.Count == 0)
            {
                return;
            }
            foreach (Part p in _selectedParts)
            {
                p.fitProxy(1);
            }
            this.Refresh();
        }// refit_by_cylinder

        public void refit_by_cuboid()
        {
            if (_selectedParts.Count == 0)
            {
                return;
            }
            foreach (Part p in _selectedParts)
            {
                p.fitProxy(0);
            }
        }// refit_by_cuboid

        private bool hasInValidContact(Graph g)
        {
            if (g == null) return false;
            foreach (Edge e in g._EDGES)
            {
                foreach (Contact c in e._contacts)
                {
                    if (double.IsNaN(c._pos3d.x) || double.IsNaN(c._pos3d.y) || double.IsNaN(c._pos3d.z))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void saveReplaceablePairs()
        {
            if (_crossOverBasket.Count < 2)
            {
                return;
            }
            Model model_i = _crossOverBasket[_crossOverBasket.Count - 2];
            Model model_j = _crossOverBasket[_crossOverBasket.Count - 1];
            Graph graph_i = model_i._GRAPH;
            Graph graph_j = model_j._GRAPH;
            if (graph_i == null || graph_j == null || graph_i.selectedNodePairs.Count != graph_j.selectedNodePairs.Count)
            {
                return;
            }
            string filename = model_i._path + model_i._model_name + "_" + model_j._model_name + ".corr";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int n = graph_i.selectedNodePairs.Count;
                sw.WriteLine(n.ToString());
                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < graph_i.selectedNodePairs[i].Count; ++j)
                    {
                        sw.Write(graph_i.selectedNodePairs[i][j]._INDEX.ToString() + " ");
                    }
                    sw.WriteLine();
                    for (int j = 0; j < graph_j.selectedNodePairs[i].Count; ++j)
                    {
                        sw.Write(graph_j.selectedNodePairs[i][j]._INDEX.ToString() + " ");
                    }
                    sw.WriteLine();
                }
            }
        }// saveLoadReplaceablePairs

        private void tryLoadReplaceablePairs()
        {
            if (_ancesterModelViewers.Count == 0)
            {
                return;
            }
            int n = _ancesterModelViewers.Count;
            _replaceablePairs = new ReplaceablePair[n, n];
            for (int i = 0; i < n - 1; ++i)
            {
                Model model_i = _ancesterModelViewers[i]._MODEL;
                Graph graph_i = _ancesterModelViewers[i]._GRAPH;
                for (int j = i + 1; j < n; ++j)
                {
                    Model model_j = _ancesterModelViewers[j]._MODEL;
                    Graph graph_j = _ancesterModelViewers[j]._GRAPH;
                    string filename = model_i._path + model_i._model_name + "_" + model_j._model_name + ".corr";
                    List<List<int>> pairs_i = new List<List<int>>();
                    List<List<int>> pairs_j = new List<List<int>>();
                    loadReplaceablePair(filename, out pairs_i, out pairs_j);
                    _replaceablePairs[i, j] = new ReplaceablePair(graph_i, graph_j, pairs_i, pairs_j);
                }
            }
        }// tryLoadReplacePairs

        private void loadReplaceablePair(string filename, out List<List<int>> pairs_1, out List<List<int>> pairs_2)
        {
            pairs_1 = new List<List<int>>();
            pairs_2 = new List<List<int>>();
            if (!File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                int npairs = int.Parse(strs[0]);
                for (int i = 0; i < npairs; ++i)
                {
                    List<int> p1 = new List<int>();
                    List<int> p2 = new List<int>();
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    for (int j = 0; j < strs.Length; ++j)
                    {
                        p1.Add(int.Parse(strs[j]));
                    }
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    for (int j = 0; j < strs.Length; ++j)
                    {
                        p2.Add(int.Parse(strs[j]));
                    }
                    pairs_1.Add(p1);
                    pairs_2.Add(p2);
                }
            }
        }// loadReplaceablePair

        public void LoadAGraph(Model m, string filename, bool unify)
        {
            if (m == null || !File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                int nNodes = int.Parse(strs[0]);
                if (nNodes != m._NPARTS)
                {
                    MessageBox.Show("Unmatched graph nodes and mesh parts.");
                    return;
                }
                Graph g = new Graph();
                List<int> symGroups = new List<int>();
                bool hasGroundTouching = false;
                for (int i = 0; i < nNodes; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int j = int.Parse(strs[0]);
                    int k = int.Parse(strs[1]);
                    Node node = new Node(m._PARTS[i], j);
                    node._isGroundTouching = k == 1 ? true : false;
                    if (node._isGroundTouching)
                    {
                        hasGroundTouching = true;
                        node.addFunctionality(Common.Functionality.GROUND_TOUCHING);
                    }
                    if (strs.Length > 4)
                    {
                        Color c = Color.FromArgb(int.Parse(strs[2]), int.Parse(strs[3]), int.Parse(strs[4]));
                        node._PART._COLOR = c;
                    }
                    if (strs.Length > 5)
                    {
                        int sym = int.Parse(strs[5]);
                        if (sym > i) // sym != -1
                        {
                            symGroups.Add(i);
                            symGroups.Add(sym);
                        }
                    }
                    if (strs.Length > 6)
                    {
                        for (int f = 6; f < strs.Length; ++f)
                        {
                            Common.Functionality func = getFunctionalityFromString(strs[f]);
                            node.addFunctionality(func);
                        }
                    }
                    g.addANode(node);
                }
                // add symmetry
                for (int i = 0; i < symGroups.Count; i += 2)
                {
                    g.markSymmtry(g._NODES[symGroups[i]], g._NODES[symGroups[i + 1]]);
                }
                if (!hasGroundTouching)
                {
                    g.markGroundTouchingNodes();
                }
                s = sr.ReadLine().Trim();
                strs = s.Split(separator);
                int nEdges = int.Parse(strs[0]);
                for (int i = 0; i < nEdges; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int j = int.Parse(strs[0]);
                    int k = int.Parse(strs[1]);
                    if (strs.Length > 4)
                    {
                        int t = 2;
                        List<Contact> contacts = new List<Contact>();
                        while (t + 2 < strs.Length)
                        {
                            Vector3d v = new Vector3d(double.Parse(strs[t++]), double.Parse(strs[t++]), double.Parse(strs[t++]));
                            Contact c = new Contact(v);
                            contacts.Add(c);
                        }
                        g.addAnEdge(g._NODES[j], g._NODES[k], contacts);
                    }
                    else
                    {
                        g.addAnEdge(g._NODES[j], g._NODES[k]);
                    }
                }
                g._ff = null;
                List<Common.Category> cats = new List<Common.Category>();
                List<double> funvals = new List<double>();
                while (sr.Peek() > -1)
                {
                    // functionality
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    cats.Add(Common.getCategory(strs[0]));
                    funvals.Add(double.Parse(strs[1]));
                }
                if (cats.Count > 0)
                {
                    g._ff = new FunctionalityFeatures(cats, funvals);
                }
                if (unify)
                {
                    g.unify();
                }
                g.init();
                m.setGraph(g);
            }
        }// LoadAGraph

        public void saveAGraph(string filename)
        {
            if (_currModel._GRAPH == null)
            {
                return;
            }
            Graph  g = _currModel._GRAPH;
            // node:
            // idx, isGroundTouching, Color, Sym (-1: no sym, idx)
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(_currModel._GRAPH._NNodes.ToString() + " nodes.");
                for (int i = 0; i < _currModel._GRAPH._NNodes; ++i)
                {
                    Node iNode = _currModel._GRAPH._NODES[i];
                    sw.Write(iNode._INDEX.ToString() + " ");
                    int isGround = iNode._isGroundTouching ? 1 : 0;
                    sw.Write(isGround.ToString() + " ");
                    // color
                    sw.Write(iNode._PART._COLOR.R.ToString() + " " + iNode._PART._COLOR.G.ToString() + " " + iNode._PART._COLOR.B.ToString() + " ");
                    // sym
                    int symIdx = -1;
                    if (iNode.symmetry != null)
                    {
                        symIdx = iNode.symmetry._INDEX;
                    }
                    sw.Write(symIdx.ToString());
                    // functionality
                    if (iNode._funcs != null)
                    {
                        foreach (Common.Functionality func in iNode._funcs)
                        {
                            sw.Write(" " + func.ToString());
                        }
                    }
                    sw.WriteLine();
                }
                sw.WriteLine(_currModel._GRAPH._NEdges.ToString() + " edges.");
                foreach (Edge e in _currModel._GRAPH._EDGES)
                {
                    sw.Write(e._start._INDEX.ToString() + " " + e._end._INDEX.ToString() + " ");
                    foreach (Contact pnt in e._contacts)
                    {
                        sw.Write(this.vector3dToString(pnt._pos3d, " ", " "));
                    }
                    sw.WriteLine();
                }
                if (g._ff != null && g._ff._cats.Count > 0)
                {
                    for (int i = 0; i < g._ff._cats.Count; ++i)
                    {
                        sw.WriteLine(g._ff._cats[i] + " " + g._ff._funvals[i].ToString());
                    }
                }
            }
        }// saveAGraph

        public void refreshModelViewers()
        {
            // view the same as the main view
            foreach (ModelViewer mv in _ancesterModelViewers)
            {
                mv.Refresh();
            }
            foreach (ModelViewer mv in _currGenModelViewers)
            {
                mv.Refresh();
            }
            foreach (ModelViewer mv in _partViewers)
            {
                mv.Refresh();
            }
        }// refreshModelViewers

        public void setCurrentModel(Model m, int idx)
        {
            _currModel = m;
            _selectedParts.Clear();
            _selectedModelIndex = idx;
            _crossOverBasket.Remove(m);
            m._GRAPH.selectedNodePairs.Clear();

            this.cal2D();
            this.Refresh();
        }

        public bool userSelectModel(Model m)
        {
            // from user selction
            if (_currGen.Contains(m))
            {
                _currGen.Remove(m);
                return false;
            }
            else
            {
                _currGen.Add(m);
                return true;
            }
        }// userSelectModel

        private void cal2D()
        {
            // otherwise when glViewe is initialized, it will run this function from MouseUp()
            //if (this.currSegmentClass == null) return;

            // reset the current 3d transformation again to check in the camera info, projection/modelview
            Gl.glViewport(0, 0, this.Width, this.Height);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
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
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Matrix4d transformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective);
            Matrix4d m = transformMatrix * this._currModelTransformMatrix;

            m = Matrix4d.TranslationMatrix(this.objectCenter) * m * Matrix4d.TranslationMatrix(
                new Vector3d() - this.objectCenter);

            this.calculatePoint2DInfo();

            //Gl.glMatrixMode(Gl.GL_MODELVIEW);
            //Gl.glPushMatrix();
            //Gl.glMultMatrixd(m.Transpose().ToArray());

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }//cal2D

        private void calculatePoint2DInfo()
        {
            this.updateCamera();
            if (_currHumanPose != null)
            {
                foreach (BodyNode bn in _currHumanPose._bodyNodes)
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
                Prism box = p._BOUNDINGBOX;
                for (int i = 0; i < box._POINTS3D.Length; ++i)
                {
                    p._BOUNDINGBOX._POINTS2D[i] = getVec2D(box._POINTS3D[i]);
                }
            }
            if (_currModel._GRAPH != null)
            {
                foreach (Edge e in _currModel._GRAPH._EDGES)
                {
                    foreach (Contact pnt in e._contacts)
                    {
                        Vector2d v2 = this.camera.Project(pnt._pos3d).ToVector2d();
                        pnt._pos2d = new Vector2d(v2.x, this.Height - v2.y);
                    }
                }
            }
            if (_editAxes != null)
            {
                foreach (Contact p in _editAxes)
                {
                    p._pos2d = getVec2D(p._pos3d);
                }
            }
        }// calculatePoint2DInfo

        private Vector2d getVec2D(Vector3d v3)
        {
            Vector2d v2 = this.camera.Project(v3).ToVector2d();
            Vector2d v = new Vector2d(v2.x, this.Height - v2.y);
            return v;
        }// getVec2D

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
            if (!File.Exists(filename))
            {
                MessageBox.Show("Default view matrix does not exist.");
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ' };
                string s = sr.ReadLine().Trim();
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
                this.Refresh();
            }
        }

        public void captureScreen(int idx)
        {
            Size newSize = new System.Drawing.Size(this.Width, this.Height);
            var bmp = new Bitmap(newSize.Width, newSize.Height);
            var gfx = Graphics.FromImage(bmp);
            gfx.CopyFromScreen((int)(this.Location.X), (int)(this.Location.Y) + 90,
                0, 0, newSize, CopyPixelOperation.SourceCopy);
            string imageFolder = foldername + "\\screenCapture";

            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }
            string name = imageFolder + "\\seq_" + idx.ToString() + ".png";
            bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void captureScreen(string filename)
        {
            Size newSize = new System.Drawing.Size(this.Width, this.Height);
            var bmp = new Bitmap(newSize.Width, newSize.Height);
            var gfx = Graphics.FromImage(bmp);
            gfx.CopyFromScreen((int)(this.Location.X), (int)(this.Location.Y) + 90,
                0, 0, newSize, CopyPixelOperation.SourceCopy);
            bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
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
            Rectangle rect = new Rectangle(0, 0, w, h);
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

        /*****************Functionality-aware evolution*************************/

        public void markSymmetry()
        {
            if (_selectedNodes.Count != 2)
            {
                return;
            }
            _currModel._GRAPH.markSymmtry(_selectedNodes[0], _selectedNodes[1]);
        }// markSymmetry

        public void markFunctionPart(int i)
        {
            Common.Functionality func = getFunctionalityFromIndex(i);

            foreach (Node node in _selectedNodes)
            {
                node.addFunctionality(func);
            }

        }// markFunctionPart

        private Common.Functionality getFunctionalityFromIndex(int i)
        {
            switch (i)
            {
                case 1:
                    return Common.Functionality.HUMAN_BACK;
                case 2:
                    return Common.Functionality.HUMAN_HIP;
                case 3:
                    return Common.Functionality.HAND_HOLD;
                case 4:
                    return Common.Functionality.HAND_PLACE;
                case 5:
                    return Common.Functionality.SUPPORT;
                case 6:
                    return Common.Functionality.HANG;
                case 0:
                default:
                    return Common.Functionality.GROUND_TOUCHING;
            }
        }// getFunctionalityFromIndex

        private Common.Functionality getFunctionalityFromString(string s)
        {
            switch (s)
            {
                case "HUMAN_BACK":
                    return Common.Functionality.HUMAN_BACK;
                case "HUMAN_HIP":
                    return Common.Functionality.HUMAN_HIP;
                case "HAND_HOLD":
                    return Common.Functionality.HAND_HOLD;
                case "HAND_PLACE":
                    return Common.Functionality.HAND_PLACE;
                case "SUPPORT":
                    return Common.Functionality.SUPPORT;
                case "GROUND_TOUCHING":
                default:
                    return Common.Functionality.GROUND_TOUCHING;
            }
        }// getFunctionalityFromIndex

        public void switchParts(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2)
        {
            List<Edge> edgesToConnect_1 = g1.getOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.getOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_1);
            List<Vector3d> targets = collectPoints(edgesToConnect_2);

            if (sources.Count == targets.Count && sources.Count == 2)
            {

            }
        }// switchParts

        public void collectSnapshotsFromFolder(string folder)
        {
            string snapshot_folder = folder + "\\snapshots";

            this.readModelModelViewMatrix(folder + "\\view.mat");

            this.isDrawBbox = false;
            this.isDrawGraph = false;
            this.isDrawGround = true;
            Program.GetFormMain().setCheckBox_drawBbox(this.isDrawBbox);
            Program.GetFormMain().setCheckBox_drawGraph(this.isDrawGraph);
            Program.GetFormMain().setCheckBox_drawGround(this.isDrawGround);

            // for capturing screen
            this.reloadView();

            dfs_files(folder, snapshot_folder);
        }// collectSnapshotsFromFolder

        private void dfs_files(string folder, string snap_folder)
        {
            string[] files = Directory.GetFiles(folder);
            if (!Directory.Exists(snap_folder))
            {
                Directory.CreateDirectory(snap_folder);
            }
            foreach (string file in files)
            {
                if (!file.EndsWith("pam"))
                {
                    continue;
                }
                Model m = loadOnePartBasedModel(file);
                if (m != null)
                {
                    string graphName = file.Substring(0, file.LastIndexOf('.')) + ".graph";
                    LoadAGraph(m, graphName, false);
                    this.setCurrentModel(m, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(snap_folder + "\\" + m._model_name + ".png");
                }
            }
            string[] folders = Directory.GetDirectories(folder);
            if (folders.Length == 0)
            {
                return;
            }
            foreach (string subfolder in folders)
            {
                string foldername = subfolder.Substring(subfolder.LastIndexOf('\\'));
                dfs_files(subfolder, snap_folder + foldername);
            }
        }// dfs_files

        public int getParentModelNum()
        {
            return _ancesterModelViewers.Count;
        }

        private void convertFunctionalityDescription(Vector3d[] points, double[] weights)
        {
            if (_currModel == null || _currModel._GRAPH == null || points == null || weights == null || points.Length != weights.Length)
            {
                return;
            }
            // TO BE UPDATED AFTER GET THE DATA
            int[] labels = new int[weights.Length];
            foreach (Node node in _currModel._GRAPH._NODES)
            {
                // get a stats of each point
                Mesh m = node._PART._MESH;
                Vector3d[] vecs = m.VertexVectorArray;
                Dictionary<int, int> dict = new Dictionary<int, int>();
                for (int i = 0; i < vecs.Length; ++i)
                {
                    double mind = double.MaxValue;
                    int plabel = -1;
                    for (int j = 0; j < points.Length; ++j)
                    {
                        double d = (vecs[i] - points[j]).Length();
                        if (d < mind)
                        {
                            mind = d;
                            plabel = labels[j];
                        }
                    }
                    int val = 0;
                    if (dict.TryGetValue(plabel, out val))
                    {
                        val++;
                    }
                    else
                    {
                        val = 1;
                    }
                    dict.Add(plabel, val);
                }// for -vertex
                // label by the majority
                Dictionary<int, int>.Enumerator iter = dict.GetEnumerator();
                int part_label = -1;
                int maxnum = 0;
                while (iter.MoveNext())
                {
                    int num = iter.Current.Value;
                    if (num > maxnum)
                    {
                        maxnum = num;
                        part_label = iter.Current.Key;
                    }
                }
                node.addFunctionality(this.getFunctionalityFromIndex(part_label));
            }// for-part
        }// convertFunctionalityDescription

        // save folders
        string userFolder;
        string mutateFolder;
        string crossoverFolder;
        string growthFolder;
        string imageFolder_m;
        string imageFolder_c;
        string imageFolder_g;
        int _userIndex = 1;

        public int registerANewUser()
        {
            string root = this.foldername.Clone() as string;
            _userIndex = 1;
            userFolder = root + "\\User_" + _userIndex.ToString();
            while (Directory.Exists(userFolder))
            {
                // create a new folder for the new user
                ++_userIndex;
                userFolder = root + "\\User_" + _userIndex.ToString();
            }
            Directory.CreateDirectory(userFolder);

            mutateFolder = userFolder + "\\models\\mutate\\";
            crossoverFolder = userFolder + "\\models\\crossover\\";
            growthFolder = userFolder + "\\models\\growth\\";

            createDirectory(mutateFolder);
            createDirectory(crossoverFolder);
            createDirectory(growthFolder);

            imageFolder_m = userFolder + "\\screenCapture\\mutate\\";
            imageFolder_c = userFolder + "\\screenCapture\\crossover\\";
            imageFolder_g = userFolder + "\\screenCapture\\growth\\";

            createDirectory(imageFolder_m);
            createDirectory(imageFolder_c);
            createDirectory(imageFolder_g);

            return _userIndex;
        }// registerANewUser

        private void createDirectory(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        private void saveUserSelections(int gen)
        {
            string dir = userFolder + "\\selections";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string selectionTxt = dir + "\\gen_" + gen.ToString() + ".txt";
            using (StreamWriter sw = new StreamWriter(selectionTxt))
            {
                sw.WriteLine("User " + _userIndex.ToString());
                sw.WriteLine("Generation " + gen.ToString());
                foreach (Model model in _currGen)
                {
                    sw.WriteLine(model._path + model._model_name);
                }
            }
        }// saveUserSelections

        public List<ModelViewer> autoGenerate()
        {
            if (!Directory.Exists(userFolder))
            {
                this.registerANewUser();
            }

            this.isDrawBbox = false;
            this.isDrawGraph = false;
            this.isDrawGround = true;
            Program.GetFormMain().setCheckBox_drawBbox(this.isDrawBbox);
            Program.GetFormMain().setCheckBox_drawGraph(this.isDrawGraph);
            Program.GetFormMain().setCheckBox_drawGround(this.isDrawGround);

            // CALL MATLAB
            MLApp.MLApp matlab = new MLApp.MLApp();
            string exeStr = "cd " + Interface.MATLAB_PATH;
            matlab.Execute(exeStr);
            Object matlabOutput = null;
            matlab.Feval("clearData", 0, out matlabOutput);

            // for capturing screen            
            this.reloadView();

            List<Model> parents = new List<Model>();
            List<Model> prev_parents = new List<Model>(_currGen);
            // always include the ancient models
            foreach (ModelViewer mv in _ancesterModelViewers)
            {
                parents.Add(mv._MODEL);
            }
            _currGenModelViewers.Clear();

            _currIter = 0;
            // add user selected models
            if (_currIter > 0)
            {
                this.saveUserSelections(_currIter);
            }
            parents.AddRange(_currGen);

            int maxIter = 5;
            int start = 0;
            for (int i = 0; i < maxIter; ++i)
            {
                Random rand = new Random();
                _mutateOrCross = runMutateOrCrossover(rand);
                if (i == 0)
                {
                    _mutateOrCross = 1;
                }
                //_mutateOrCross = 1;
                List<Model> cur_par = new List<Model>(parents);
                List<Model> cur_kids = new List<Model>();
                string runstr = "Run ";
                switch (_mutateOrCross)
                {
                    case 0:
                        // mutate
                        runstr += "Mutate @iteration " + i.ToString();
                        Program.GetFormMain().writeToConsole(runstr);
                        cur_kids = runMutate(cur_par, _currIter, imageFolder_m, start);
                        break;
                    case 2:
                        runstr += "Growth @iteration " + i.ToString();
                        Program.GetFormMain().writeToConsole(runstr);
                        cur_kids = runGrowth(cur_par, _currIter, rand, imageFolder_g, start);
                        break;
                    case 1:
                    default:
                        // crossover
                        runstr += "Crossover @iteration " + i.ToString();
                        Program.GetFormMain().writeToConsole(runstr);
                        cur_kids = runCrossover(cur_par, _currIter, rand, imageFolder_c, start);
                        break;
                }
                // functionality test
                if (cur_kids.Count > 0)
                {
                    List<string> filenames = new List<string>();
                    for (int j = 0; j < cur_kids.Count; ++j)
                    {
                        //filenames.Add(cur_kids[j]._model_name);
                        List<string> patchFileNames = this.useSelectedSubsetPatchesForPrediction(cur_kids[j]);
                        filenames.AddRange(patchFileNames);
                    }
                    writeFileNamesForPredictToMatlabFolder(filenames);
                    matlabOutput = null;
                    matlab.Feval("getFunctionalityScore", 1, out matlabOutput);
                    Object[] res = matlabOutput as Object[];
                    double[,] results = res[0] as double[,];
                    // save the scores
                    for (int j = 0; j < cur_kids.Count; ++j)
                    {
                        string filename = cur_kids[j]._path + cur_kids[j]._model_name + ".score";
                        int ncat = results.GetLength(1);
                        double[] scores = new double[ncat];
                        for (int k = 0; k < ncat; ++k)
                        {
                            scores[k] = results[j, k];
                        }
                        this.saveScoreFile(filename, scores);
                    }
                }
                start = _currGen.Count;
                parents.AddRange(cur_kids);
                _currGen.AddRange(cur_kids);
                ++_currIter;
            }
            int n = _ancesterModelViewers.Count;
            foreach (Model m in _currGen)
            {
                ModelViewer mv = new ModelViewer(m, n++, this, 2);
                _currGenModelViewers.Add(mv);
            }
            _currGen = new List<Model>(prev_parents); // for user selection
            return _currGenModelViewers;
        }// autoGenerate

        private void writeFileNamesForPredictToMatlabFolder(List<string> strs)
        {
            string filename = Interface.MATLAB_PATH + "\\shapeFileNames.txt";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < strs.Count; ++i)
                {
                    sw.WriteLine(strs[i]);
                }
            }
        }// writeFileNamesForPredictToMatlabFolder

        private void saveScoreFile(string filename, double[] scores)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < scores.Length; ++i)
                {
                    sw.WriteLine(Common.getCategoryName(i) + " " + scores[i].ToString());
                }
                string tops = Common.getTopPredictedCategories(scores);
                sw.WriteLine(tops);
                Program.writeToConsole(tops);
            }
        }// saveScoreFile

        private List<string> useSelectedSubsetPatchesForPrediction(Model model)
        {
            if (model == null)
            {
                return null;
            }
            List<string> patchFileNamesForPredictions = new List<string>();
            // select those patches with certain functioanlity for a category
            foreach (Common.Category cat in model._GRAPH._ff._cats)
            {
                List<Common.Functionality> funcs = Common.getFunctionalityFromCategory(cat);
                List<Node> subPatches = new List<Node>();
                foreach (Node node in model._GRAPH._NODES)
                {
                    List<Common.Functionality> funcs_node = node._funcs;
                    foreach (Common.Functionality f in funcs_node)
                    {
                        if (funcs.Contains(f))
                        {
                            subPatches.Add(node);
                            break;
                        }
                    }
                }
                // save 
                string subsetStr = "_subPatches_" + cat;
                string patchFileName = model._model_name + subsetStr;
                patchFileNamesForPredictions.Add(patchFileName);
                writeSampleFeatureFilesForPrediction(subPatches, model, subsetStr);

                // test the whole shape
                subPatches = model._GRAPH._NODES;
                subsetStr = "_wholeShape_" + cat;
                patchFileName = model._model_name + subsetStr;
                patchFileNamesForPredictions.Add(patchFileName);
                writeSampleFeatureFilesForPrediction(subPatches, model, subsetStr);
            }
            return patchFileNamesForPredictions;
        }// useSelectedSubsetPatchesForPrediction

        private void writeSampleFeatureFilesForPrediction(List<Node> subPatches, Model model, string subsetStr)
        {
            string meshFileName = model._model_name + subsetStr + ".obj";
            string mesh_file = model._path + meshFileName;
            this.saveMeshForModel(subPatches, mesh_file);
            File.Copy(mesh_file, Interface.MESH_PATH + meshFileName, true);

            string folder = Interface.MATLAB_INPUT_PATH;
            string possionFilename = model._model_name + subsetStr + ".poisson";
            string pois_file = folder + possionFilename;
            using (StreamWriter sw = new StreamWriter(pois_file))
            {
                int start = 0;
                foreach (Node node in subPatches)
                {
                    SamplePoints sp = node._PART._partSP;
                    for (int j = 0; j < sp._points.Length; ++j)
                    {
                        Vector3d vpos = sp._points[j];
                        sw.Write(vector3dToString(vpos, " ", " "));
                        Vector3d vnor = sp._normals[j];
                        sw.Write(vector3dToString(vnor, " ", " "));
                        int fidx = start + sp._faceIdx[j];
                        sw.WriteLine(fidx.ToString());
                    }
                    start += node._PART._MESH.FaceCount;
                }
            }
            File.Copy(pois_file, Interface.POINT_SAMPLE_PATH + possionFilename, true);

            string featureFileName = model._model_name + subsetStr + "_point_feature.csv";
            string feat_file = folder + featureFileName;
            using (StreamWriter sw = new StreamWriter(feat_file))
            {
                int npnts = 0;
                foreach (Node node in subPatches)
                {
                    int n = node._PART._partSP._points.Length;
                    npnts += n;
                    for (int i = 0; i < n; ++i)
                    {
                        StringBuilder sb = new StringBuilder();
                        int d = Common._POINT_FEAT_DIM;
                        for (int j = 0; j < d; ++j)
                        {
                            sb.Append(Common.correct(node.funcFeat._pointFeats[i * d + j]));
                            sb.Append(",");
                        }
                        d = Common._CURV_FEAT_DIM;
                        for (int j = 0; j < d; ++j)
                        {
                            sb.Append(Common.correct(node.funcFeat._curvFeats[i * d + j]));
                            sb.Append(",");
                        }
                        d = Common._PCA_FEAT_DIM;
                        for (int j = 0; j < d; ++j)
                        {
                            sb.Append(Common.correct(node.funcFeat._pcaFeats[i * d + j]));
                            sb.Append(",");
                        }
                        d = Common._RAY_FEAT_DIM;
                        for (int j = 0; j < d; ++j)
                        {
                            sb.Append(Common.correct(node.funcFeat._rayFeats[i * d + j]));
                            sb.Append(",");
                        }
                        d = Common._CONVEXHULL_FEAT_DIM;
                        for (int j = 0; j < d; ++j)
                        {
                            sb.Append(Common.correct(node.funcFeat._conhullFeats[i * d + j]));
                            sb.Append(",");
                        }
                        for (int j = 0; j < d; ++j)
                        {
                            sb.Append(Common.correct(node.funcFeat._cenOfMassFeats[i * d + j]));
                            if (j < d - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sw.WriteLine(sb.ToString());
                    }
                }
            }
            File.Copy(feat_file, Interface.POINT_FEATURE_PATH + featureFileName, true);
        }// writeSampleFeatureFilesForPrediction

        private List<Model> runGrowth(List<Model> models, int gen, Random rand, string imageFolder, int start)
        {
            List<Model> growth = new List<Model>();
            if (models.Count < 2)
            {
                return growth;
            }
            string path = growthFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            for (int i = start; i < models.Count; ++i)
            {
                Model m1 = models[i];
                // jump those have already been visited
                int j = start + 1;
                if (i > start)
                {
                    j = 0;
                }
                int idx = 0;
                int randomj = rand.Next(models.Count - j);
                int maxw = 5;
                int w = 0;
                while (randomj == i && w < maxw)
                {
                    randomj = rand.Next(models.Count - j);
                    ++w;
                }
                j = randomj;
                if (j == i)
                {
                    continue;
                }
                //for (; j < models.Count; ++j)
                //{
                    if (i == j)
                    {
                        continue;
                    }
                    Model m2 = models[j];
                    //Model model = addPlacement(m1, m2._GRAPH, path, idx, rand, gen);
                    Model model = growNewFunctionality(m1, m2, path, idx, rand);
                    if (model != null && model._GRAPH != null && model._GRAPH.isValid())
                    {
                        model._GRAPH.reset();
                        model._GRAPH.recomputeSPnormals();
                        model._GRAPH._ff = this.addFF(m1._GRAPH._ff, m2._GRAPH._ff);
                        growth.Add(model);
                        // screenshot
                        this.setCurrentModel(model, -1);
                        Program.GetFormMain().updateStats();
                        this.captureScreen(imageFolder + model._model_name + ".png");
                        saveAPartBasedModel(model, model._path + model._model_name + ".pam", false);
                        ++idx;
                    }
                //}
            }
            return growth;
        }// runGrowth

        private Model growNewFunctionality(Model m1, Model m2, string path, int idx, Random rand)
        {
            // find the new func in m2 and add to m1
            List<Common.Functionality> funcs1 = m1._GRAPH.getGraphFuncs();
            List<Common.Functionality> funcs2 = m2._GRAPH.getGraphFuncs();
            List<Common.Functionality> cands = new List<Common.Functionality>();
            foreach (Common.Functionality f2 in funcs2)
            {
                if (!funcs1.Contains(f2))
                {
                    cands.Add(f2);
                }
            }
            if (cands.Count == 0) {
                return null;
            }
            int i = rand.Next(cands.Count);
            Common.Functionality addf = cands[i];
            Model m1_clone = m1.Clone() as Model;
            m1_clone._path = path;
            m1_clone._model_name = m1._model_name + "_grow_" + idx.ToString();
            Graph g1_clone = m1_clone._GRAPH;
            List<Node> nodes = m2._GRAPH.getNodesByUniqueFunctionality(addf);
            if (nodes.Count == 0)
            {
                return null;
            }
            List<Edge> outEdges = m2._GRAPH.getOutgoingEdges(nodes);
            List<Node> clone_nodes = new List<Node>();
            List<Edge> clone_edges = new List<Edge>();
            m2._GRAPH.cloneSubgraph(nodes, out clone_nodes, out clone_edges);
            foreach (Node node in clone_nodes)
            {
                m1_clone.addAPart(node._PART);
                g1_clone.addANode(node);
            }
            foreach (Edge e in clone_edges)
            {
                g1_clone.addAnEdge(e._start, e._end, e._contacts);
            }
            // node to attach
            Node attach = g1_clone.getNodeToAttach();
            if (attach == null)
            {
                return null;
            }
            Vector3d sourcePos = new Vector3d();
            int ne = 0;
            foreach (Edge e in outEdges)
            {
                List<Contact> clone_contacts = new List<Contact>();
                foreach (Contact c in e._contacts)
                {
                    sourcePos += c._pos3d;
                    ++ne;
                    clone_contacts.Add(new Contact(new Vector3d(c._pos3d)));
                }
                Node out_node =  nodes.Contains(e._start) ? e._start : e._end;
                Node cnode = clone_nodes[nodes.IndexOf(out_node)];
                g1_clone.addAnEdge(cnode, attach, clone_contacts);
            }
            sourcePos /= ne;
            Vector3d targetPos;
            if (addf == Common.Functionality.HAND_PLACE || addf == Common.Functionality.HUMAN_HIP)
            {
                targetPos = new Vector3d(
                attach._PART._BOUNDINGBOX.CENTER.x,
                attach._PART._BOUNDINGBOX.MaxCoord.y,
                attach._PART._BOUNDINGBOX.CENTER.z);
            }
            else if (addf == Common.Functionality.SUPPORT)
            {
                targetPos = new Vector3d(
                attach._PART._BOUNDINGBOX.CENTER.x,
                attach._PART._BOUNDINGBOX.MinCoord.y,
                attach._PART._BOUNDINGBOX.CENTER.z);
            }
            else
            {
                targetPos = new Vector3d(
                attach._PART._BOUNDINGBOX.CENTER.x,
                attach._PART._BOUNDINGBOX.MaxCoord.y,
                attach._PART._BOUNDINGBOX.MinCoord.z);
            }

            Matrix4d T = Matrix4d.TranslationMatrix(targetPos - sourcePos);
            foreach (Node cnode in clone_nodes)
            {
                deformANodeAndEdges(cnode, T);
            }
            return m1_clone;
        }// growNewFunctionality

        private Model addPlacement(Model m1, Graph g2, string path, int idx, Random rand, int gen)
        {
            // get sth from g2 that can be added to g1
            Graph g1 = m1._GRAPH;
            Node place_g1 = null;
            foreach (Node node in g1._NODES)
            {
                if (node._funcs.Contains(Common.Functionality.HAND_PLACE))
                {
                    place_g1 = node;
                    break;
                }
            }
            if (place_g1 == null)
            {
                return null;
            }
            int option = rand.Next(2);
            if (m1._model_name.Contains("table"))
            {
                option = 0;
            }
            if (gen > 1)
            {
                option = 1;
            }
            Node nodeToAdd = getAddNode(m1._GRAPH, g2, option);
            if (nodeToAdd == null)
            {
                return null;
            }
            // along X-axis
            double hx = (place_g1._PART._BOUNDINGBOX.MaxCoord.x - place_g1._PART._BOUNDINGBOX.MinCoord.x) / 2;
            double xscale = hx / (nodeToAdd._PART._BOUNDINGBOX.MaxCoord.x - nodeToAdd._PART._BOUNDINGBOX.MinCoord.x);
            double y = (nodeToAdd._PART._BOUNDINGBOX.MaxCoord.y - nodeToAdd._PART._BOUNDINGBOX.MinCoord.y);
            double z = (nodeToAdd._PART._BOUNDINGBOX.MaxCoord.z - nodeToAdd._PART._BOUNDINGBOX.MinCoord.z);
            double yscale = 1.0;
            double zscale = (place_g1._PART._BOUNDINGBOX.MaxCoord.z - place_g1._PART._BOUNDINGBOX.MinCoord.z) / z;
            Vector3d center = new Vector3d(place_g1._PART._BOUNDINGBOX.MaxCoord.x - hx / 2,
                place_g1._PART._BOUNDINGBOX.MaxCoord.y + y / 2,
                place_g1._PART._BOUNDINGBOX.CENTER.z);

            Matrix4d S = Matrix4d.ScalingMatrix(xscale, yscale, zscale);
            if (option == 1)
            {
                center = new Vector3d(place_g1._PART._BOUNDINGBOX.CENTER.x,
                place_g1._PART._BOUNDINGBOX.MaxCoord.y + y / 2,
                place_g1._PART._BOUNDINGBOX.MinCoord.z + z / 2);
                S = Matrix4d.ScalingMatrix(xscale * 2, 1.0, 1.0);
            }
            Matrix4d Q = Matrix4d.TranslationMatrix(center) * S * Matrix4d.TranslationMatrix(new Vector3d() - nodeToAdd._PART._BOUNDINGBOX.CENTER);
            Node nodeToAdd_clone = nodeToAdd.Clone() as Node;
            nodeToAdd_clone._INDEX = m1._GRAPH._NNodes;
            int node_idx = m1._GRAPH._NODES.IndexOf(place_g1);
            Model m1_clone = m1.Clone() as Model;
            m1_clone._path = path;
            m1_clone._model_name = m1._model_name + "_grow_" + idx.ToString();
            Graph g1_clone = m1_clone._GRAPH;
            m1_clone.addAPart(nodeToAdd_clone._PART);
            g1_clone.addANode(nodeToAdd_clone);
            List<Contact> clone_contacts = new List<Contact>();
            Vector3d[] contact_points = new Vector3d[4];
            contact_points[0] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MinCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MinCoord.z);
            contact_points[1] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MaxCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MinCoord.z);
            contact_points[2] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MaxCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MaxCoord.z);
            contact_points[3] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MinCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MaxCoord.z);

            for (int i = 0; i < 4; ++i)
            {
                clone_contacts.Add(new Contact(contact_points[i]));
            }
            g1_clone.addAnEdge(g1_clone._NODES[node_idx], nodeToAdd_clone, clone_contacts);
            deformANodeAndEdges(nodeToAdd_clone, Q);
            return m1_clone;
        }// addPlacement

        private Node getAddNode(Graph g1, Graph g2, int option)
        {
            List<Contact> contacts = new List<Contact>();
            int maxSupport = 0;
            Node nodeToAdd = null;
            if (option == 1)
            {
                // add a new functionality
                List<Common.Functionality> funcs1 = g1.getGraphFuncs();
                List<Common.Functionality> funcs2 = g2.getGraphFuncs();
                foreach (Common.Functionality f in funcs1)
                {
                    funcs2.Remove(f);
                }
                if (funcs2.Count > 0)
                {
                    Random rand = new Random();
                    int fidx = rand.Next(funcs2.Count);
                    Common.Functionality func = funcs2[fidx];
                    foreach (Node node in g2._NODES)
                    {
                        if (node._funcs.Contains(func))
                        {
                            if (nodeToAdd != null)
                            {
                                nodeToAdd = null; // only support one node for now
                                break;
                            }
                            nodeToAdd = node;
                            //break;
                        }
                    }
                }
            }
            else
            {
                foreach (Node node in g2._NODES)
                {
                    int ns = 0;
                    List<Contact> cnts = new List<Contact>();
                    for (int i = 0; i < node._edges.Count; ++i)
                    {
                        Node adj = node._edges[i]._start == node ? node._edges[i]._end : node._edges[i]._start;
                        if (adj._funcs.Contains(Common.Functionality.SUPPORT))
                        //&& adj._PART._BOUNDINGBOX.MaxCoord.y < node._PART._BOUNDINGBOX.MaxCoord.y)
                        {
                            ++ns;
                            cnts.AddRange(node._edges[i]._contacts);
                        }
                    }
                    if (ns > maxSupport)
                    {
                        maxSupport = ns;
                        nodeToAdd = node;
                        contacts = cnts;
                    }
                }
            }
            return nodeToAdd;
        }// getAddNode

        private List<Model> runMutate(List<Model> models, int gen, string imageFolder, int start)
        {
            List<Model> mutated = new List<Model>();
            if (models.Count == 0)
            {
                return mutated;
            }
            Random rand = new Random();
            string path = mutateFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            for (int i = start; i < models.Count; ++i)
            {
                // select a node
                Model iModel = models[i];
                int j = rand.Next(iModel._GRAPH._NNodes);
                Model model = iModel.Clone() as Model;
                model._path = path.Clone() as string;
                model._model_name = iModel._model_name + "_g_" + gen.ToString();
                //model._model_name = "gen_" + gen.ToString() + "_" + i.ToString();
                Node updateNode = model._GRAPH._NODES[j];
                if (gen > 1)
                {
                    updateNode = model._GRAPH._NODES[j];
                }
                // mutate
                if (hasInValidContact(model._GRAPH))
                {
                    break;
                }
                mutateANode(updateNode, rand);
                deformPropagation(model._GRAPH, updateNode);
                model._GRAPH.resetUpdateStatus();
                model._GRAPH._ff = iModel._GRAPH._ff.clone() as FunctionalityFeatures;
                if (model._GRAPH.isValid())
                {
                    model._GRAPH.unify();
                    mutated.Add(model);
                    // screenshot
                    this.setCurrentModel(model, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(imageFolder + model._model_name + ".png");
                    saveAPartBasedModel(model, model._path + model._model_name + ".pam", false);
                }
            }
            return mutated;
        }// runMutate

        private void mutateANode(Node node, Random rand)
        {
            double s1 = 1.0;// 0.5; // min
            double s2 = 2.0; // max
            double scale = s1 + rand.NextDouble() * (s2 - s1);
            Vector3d scale_vec = new Vector3d(1, 1, 1);
            Matrix4d R = Matrix4d.IdentityMatrix();
            int axis = rand.Next(3);
            scale_vec[axis] = scale;
            Vector3d ori_axis = new Vector3d();
            ori_axis[axis]=1;
            if (node._PART._BOUNDINGBOX.type == Common.PrimType.Cylinder)
            {
                Vector3d rot_axis = node._PART._BOUNDINGBOX.rot_axis;
                R = Matrix4d.RotationMatrix(rot_axis, Math.Acos(ori_axis.Dot(rot_axis)));
                //scale_vec = scale * rot_axis;
            }
            Matrix4d S = Matrix4d.ScalingMatrix(scale_vec);
            Vector3d center = node._pos;
            Matrix4d Q = R * S;
            Q = Matrix4d.TranslationMatrix(center) * Q * Matrix4d.TranslationMatrix(new Vector3d() - center);
            if (node._isGroundTouching)
            {
                Node cNode = node.Clone() as Node;
                deformANodeAndEdges(cNode, Q);
                Vector3d trans = new Vector3d();
                trans.y = -cNode._PART._BOUNDINGBOX.MinCoord.y;
                Matrix4d T = Matrix4d.TranslationMatrix(trans);
                Q = T * Q;
            }
            deformANodeAndEdges(node, Q);
            deformSymmetryNode(node);
        }// mutateANode

        private List<Model> runCrossover(List<Model> models, int gen, Random rand, string imageFolder, int start)
        {
            List<Model> crossed = new List<Model>();
            if (models.Count < 2)
            {
                return crossed;
            }
            int m_idx = 0;
            for (int i = 0; i < models.Count - 1; ++i)
            {
                Model m1 = models[i];
                // jump those have already been visited
                int j = Math.Max(i + 1, start + 1);
                for (; j < models.Count; ++j)
                {
                    Model m2 = models[j];
                    // select parts for crossover
                    bool isValid = this.selectNodesForCrossover(m1._GRAPH, m2._GRAPH, rand);
                    if (!isValid)
                    {
                        continue;
                    }
                    // include all inner connected nodes
                    List<Node> nodes1 = m1._GRAPH.getNodePropagation(m1._GRAPH.selectedNodes);
                    List<Node> nodes2 = m2._GRAPH.getNodePropagation(m2._GRAPH.selectedNodes);
                    if (nodes1.Count < m1._GRAPH._NNodes)
                    {
                        m1._GRAPH.selectedNodes = nodes1;
                    }
                    if (nodes2.Count < m2._GRAPH._NNodes)
                    {
                        m2._GRAPH.selectedNodes = nodes2;
                    }
                    List<Model> results = this.crossOverOp(m1, m2, gen, m_idx);
                    m_idx += 2;
                    foreach (Model m in results)
                    {
                        if (m._GRAPH.isValid())
                        {
                            m._GRAPH.unify();
                            m._GRAPH._ff = this.addFF(m1._GRAPH._ff, m2._GRAPH._ff);
                            crossed.Add(m);
                            if (crossed.Count > 15) { return crossed; }
                            // screenshot
                            this.setCurrentModel(m, -1);
                            Program.GetFormMain().updateStats();
                            this.captureScreen(imageFolder + m._model_name + ".png");
                            saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
                        }
                    }
                }
            }
            return crossed;
        }// runCrossover

        public FunctionalityFeatures addFF(FunctionalityFeatures f1, FunctionalityFeatures f2)
        {
            List<Common.Category> cats = new List<Common.Category>(f1._cats);
            List<double> vals = new List<double>(f1._funvals);
            for (int i = 0; i < f2._cats.Count; ++i) 
            {
                Common.Category cat = f2._cats[i];
                if (!cats.Contains(cat))
                {
                    cats.Add(cat);
                    vals.Add(f2._funvals[i]);
                }
            }            
            FunctionalityFeatures added = new FunctionalityFeatures(cats, vals);
            return added;
        }// addFF

        private bool selectNodesForCrossover(Graph g1, Graph g2, Random rand)
        {
            // select functionality, one or more
            List<Common.Functionality> funcs = new List<Common.Functionality>();
            int max_func_search = 6;
            int search = 0;
            g1.selectedNodes.Clear();
            g2.selectedNodes.Clear();
            int option = rand.Next(4);
            if (option < 2)
            {
                // select functionality
                while (search < max_func_search && !this.isValidSelection(g1, g2))
                {
                    // only switch 1 functionality at one time
                    funcs = this.selectFunctionality(rand, 1);
                    if (option == 0)
                    {
                        g1.selectedNodes = g1.getNodesByFunctionality(funcs);
                        g2.selectedNodes = g2.getNodesByFunctionality(funcs);
                    }
                    else
                    {
                        g1.selectedNodes = g1.getNodesByUniqueFunctionality(funcs[0]);
                        g2.selectedNodes = g2.getNodesByUniqueFunctionality(funcs[0]);
                    }
                    ++search;
                }
            }
            bool isValid = this.isValidSelection(g1, g2);
            if (option > 1 || !isValid)
            {
                g1.selectedNodes.Clear();
                g2.selectedNodes.Clear();
                // symmetry
                List<int> visitedFuncs = new List<int>();
                int nDiffFuncs = Enum.GetNames(typeof(Common.Functionality)).Length;
                while (!this.isValidSelection(g1, g2) && visitedFuncs.Count < nDiffFuncs)
                {
                    int nf = rand.Next(nDiffFuncs);
                    while (visitedFuncs.Contains(nf))
                    {
                        nf = rand.Next(nDiffFuncs);
                    }
                    visitedFuncs.Add(nf);
                    Common.Functionality func = this.getFunctionalityFromIndex(nf);
                    g1.selectedNodes = g1.selectSymmetryFuncNodes(func);
                    g2.selectedNodes = g2.selectSymmetryFuncNodes(func);
                }
                if (option > 1)
                {
                    // break symmetry
                    List<Node> nodes1 = new List<Node>();
                    List<Node> nodes2 = new List<Node>();
                    foreach (Node node in g1.selectedNodes)
                    {
                        if (!nodes1.Contains(node) && !nodes1.Contains(node.symmetry))
                        {
                            nodes1.Add(node);
                        }
                    }
                    foreach (Node node in g2.selectedNodes)
                    {
                        if (!nodes2.Contains(node) && !nodes2.Contains(node.symmetry))
                        {
                            nodes2.Add(node);
                        }
                    }
                    g1.selectedNodes = nodes1;
                    g2.selectedNodes = nodes2;
                }
            }
            return this.isValidSelection(g1, g2);
        }//selectNodesForCrossover

        private bool isValidSelection(Graph g1, Graph g2)
        {
            if (g1.selectedNodes.Count == g1._NNodes && g2.selectedNodes.Count == g2._NNodes)
            {
                return false;
            }
            if (g1.selectedNodes.Count == 0 || g2.selectedNodes.Count == 0)
            {
                return false;
            }
            // same functionality parts in g1.selected vs. g2.unselected
            List<Node> g1_unselected = new List<Node>(g1._NODES);
            foreach (Node node in g1.selectedNodes)
            {
                g1_unselected.Remove(node);
            }
            List<Node> g2_unselected = new List<Node>(g2._NODES);
            foreach (Node node in g2.selectedNodes)
            {
                g2_unselected.Remove(node);
            }
            if (g1_unselected.Count == 0 || g2_unselected.Count == 0)
            {
                return false;
            }

            //List<Common.Functionality> g1_selected_funcs = getAllFuncs(g1.selectedNodes);
            //List<Common.Functionality> g2_selected_funcs = getAllFuncs(g2.selectedNodes);
            //List<Common.Functionality> g1_unselected_funcs = getAllFuncs(g1_unselected);
            //List<Common.Functionality> g2_unselected_funcs = getAllFuncs(g2_unselected);

            //if (this.hasfunctionIntersection(g1_selected_funcs, g2_unselected_funcs) ||
            //    this.hasfunctionIntersection(g1_unselected_funcs, g2_selected_funcs))
            //{
            //    return false;
            //}            
            return true;
        }// isValidSelection

        private List<Common.Functionality> getAllFuncs(List<Node> nodes)
        {
            List<Common.Functionality> funcs = new List<Common.Functionality>();
            foreach (Node node in nodes)
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
        }// getAllFuncs

        private bool hasfunctionIntersection(List<Common.Functionality> funcs1, List<Common.Functionality> funcs2)
        {
            foreach (Common.Functionality f1 in funcs1)
            {
                if (funcs2.Contains(f1))
                {
                    return true;
                }
            }
            return false;
        }// hasIntersection

        public List<Model> crossOverOp(Model m1, Model m2, int gen, int idx)
        {
            List<Model> crossModels = new List<Model>();
            if (m1 == null || m2 == null)
            {
                return crossModels;
            }

            List<Node> nodes1 = new List<Node>();
            List<Node> nodes2 = new List<Node>();
            //findOneToOneMatchingNodes(g1, g2, out nodes1, out nodes2);

            Model newM1 = m1.Clone() as Model;
            Model newM2 = m2.Clone() as Model;
            newM1._path = crossoverFolder + "gen_" + gen.ToString() + "\\";
            newM2._path = crossoverFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(newM1._path))
            {
                Directory.CreateDirectory(newM1._path);
            }
            if (!Directory.Exists(newM2._path))
            {
                Directory.CreateDirectory(newM2._path);
            }
            // using model names will be too long, exceed the maximum length of file name
            //newM1._model_name = m1._model_name + "_c_" + m2._model_name;
            //newM2._model_name = m2._model_name + "_c_" + m1._model_name;
            newM1._model_name = "gen_" + gen.ToString() + "_" + idx.ToString();
            newM2._model_name = "gen_" + gen.ToString() + "_" + (idx + 1).ToString();

            foreach (Node node in m1._GRAPH.selectedNodes)
            {
                nodes1.Add(newM1._GRAPH._NODES[node._INDEX]);
            }
            foreach (Node node in m2._GRAPH.selectedNodes)
            {
                nodes2.Add(newM2._GRAPH._NODES[node._INDEX]);
            }

            if (nodes1 == null || nodes2 == null)
            {
                return null;
            }

            List<Node> updatedNodes1;
            List<Node> updatedNodes2;
            // switch
            switchNodes(newM1._GRAPH, newM2._GRAPH, nodes1, nodes2, out updatedNodes1, out updatedNodes2);
            newM1.replaceNodes(nodes1, updatedNodes2);
            crossModels.Add(newM1);

            newM2.replaceNodes(nodes2, updatedNodes1);
            crossModels.Add(newM2);

            return crossModels;
        }// crossover

        private void switchNodes(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2,
            out List<Node> updateNodes1, out List<Node> updateNodes2)
        {
            List<Edge> edgesToConnect_1 = g1.getOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.getOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_1);
            List<Vector3d> targets = collectPoints(edgesToConnect_2);

            updateNodes1 = new List<Node>();
            updateNodes2 = new List<Node>();
            Vector3d center1 = new Vector3d();
            Vector3d maxv_s = Vector3d.MinCoord;
            Vector3d minv_s = Vector3d.MaxCoord;
            Vector3d maxv_t = Vector3d.MinCoord;
            Vector3d minv_t = Vector3d.MaxCoord;

            updateNodes1 = cloneNodesAndRelations(nodes1);
            foreach (Node node in nodes1)
            {
                center1 += node._PART._BOUNDINGBOX.CENTER;
                maxv_s = Vector3d.Max(maxv_s, node._PART._BOUNDINGBOX.MaxCoord);
                minv_s = Vector3d.Min(minv_s, node._PART._BOUNDINGBOX.MinCoord);
            }
            center1 /= nodes1.Count;

            Vector3d center2 = new Vector3d();
            updateNodes2 = cloneNodesAndRelations(nodes2);
            foreach (Node node in nodes2)
            {
                center2 += node._PART._BOUNDINGBOX.CENTER;
                maxv_t = Vector3d.Max(maxv_t, node._PART._BOUNDINGBOX.MaxCoord);
                minv_t = Vector3d.Min(minv_t, node._PART._BOUNDINGBOX.MinCoord);
            }
            center2 /= nodes2.Count;

            double sx = (maxv_t.x - minv_t.x) / (maxv_s.x - minv_s.x);
            double sy = (maxv_t.y - minv_t.y) / (maxv_s.y - minv_s.y);
            double sz = (maxv_t.z - minv_t.z) / (maxv_s.z - minv_s.z);
            Vector3d boxScale_1 = new Vector3d(sx, sy, sz);
            sx = (maxv_s.x - minv_s.x) / (maxv_t.x - minv_t.x);
            sy = (maxv_s.y - minv_s.y) / (maxv_t.y - minv_t.y);
            sz = (maxv_s.z - minv_s.z) / (maxv_t.z - minv_t.z);
            Vector3d boxScale_2 = new Vector3d(sx, sy, sz);

            Matrix4d S, T, Q;

            // sort corresponding points
            int nps = sources.Count;
            bool startWithSrc = true;
            List<Vector3d> left = sources;
            List<Vector3d> right = targets;
            if (targets.Count < nps)
            {
                nps = targets.Count;
                startWithSrc = false;
                left = targets;
                right = sources;
            }
            List<Vector3d> src = new List<Vector3d>();
            List<Vector3d> trt = new List<Vector3d>();
            bool[] visited = new bool[right.Count];
            foreach (Vector3d v in left)
            {
                src.Add(v);
                int j = -1;
                double mind = double.MaxValue;
                for (int i = 0; i < right.Count; ++i)
                {
                    if (visited[i]) continue;
                    double d = (v - right[i]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        j = i;
                    }
                }
                trt.Add(right[j]);
                visited[j] = true;
            }
            if (startWithSrc)
            {
                sources = src;
                targets = trt;
            }
            else
            {
                sources = trt;
                targets = src;
            }


            if (sources.Count <= 1)
            {
                sources.Add(center1);
                targets.Add(center2);
            }

            Node ground1 = hasGroundTouchingNode(nodes1);
            Node ground2 = hasGroundTouchingNode(nodes2);
            if (ground1 != null && ground2 != null)
            {
                sources.Add(g1.getGroundTouchingNodesCenter());
                targets.Add(g2.getGroundTouchingNodesCenter());
                //sources.Add(new Vector3d(sources[0].x, 0, sources[0].z));
                //targets.Add(new Vector3d(targets[0].x, 0, targets[0].z));
            }
            getTransformation(sources, targets, out S, out T, out Q, boxScale_1, true);
            this.deformNodesAndEdges(updateNodes1, Q);
            if (ground2 != null)
            {
                g1.resetUpdateStatus();
                adjustGroundTouching(updateNodes1);
            }
            g1.resetUpdateStatus();

            getTransformation(targets, sources, out S, out T, out Q, boxScale_2, true);
            this.deformNodesAndEdges(updateNodes2, Q);
            if (ground1 != null)
            {
                g2.resetUpdateStatus();
                adjustGroundTouching(updateNodes2);
            }
            g2.resetUpdateStatus();
        }// switchOneNode

        private void deformNodesAndEdges(List<Node> nodes, Matrix4d T)
        {
            foreach (Node node in nodes)
            {
                node.Transform(T);
            }
            List<Edge> inner_edges = Graph.GetAllEdges(nodes);
            foreach (Edge e in inner_edges)
            {
                if (e._contactUpdated)
                {
                    continue;
                }
                e.TransformContact(T);
            }
        }// deformNodesAndEdges

        private void adjustGroundTouching(List<Node> nodes)
        {
            double miny = double.MaxValue;
            foreach (Node node in nodes)
            {
                double y = node._PART._BOUNDINGBOX.MinCoord.y;
                miny = miny < y ? miny : y;
            }
            Vector3d trans = new Vector3d(0, -miny, 0);
            Matrix4d T = Matrix4d.TranslationMatrix(trans);

            this.deformNodesAndEdges(nodes, T);

            // in case the nodes are not marked as ground touching
            foreach (Node node in nodes)
            {
                double ydist = node._PART._MESH.MinCoord.y;
                if (Math.Abs(ydist) < Common._thresh)
                {
                    node._isGroundTouching = true;
                    node.addFunctionality(Common.Functionality.GROUND_TOUCHING);
                }
            }
        }// adjustGroundTouching

        private List<Node> cloneNodesAndRelations(List<Node> nodes)
        {
            List<Node> clone_nodes = new List<Node>();
            foreach (Node node in nodes)
            {
                Node cloned = node.Clone() as Node;
                clone_nodes.Add(cloned);
            }
            // edges
            for (int i = 0; i < nodes.Count; ++i)
            {
                Node node = nodes[i];
                foreach (Edge e in node._edges)
                {
                    Node other = e._start == node ? e._end : e._start;
                    int j = nodes.IndexOf(other);
                    Node adjNode = other;
                    if (j != -1)
                    {
                        adjNode = clone_nodes[j];
                    }
                    if (adjNode == other || j > i)
                    {
                        List<Contact> contacts = new List<Contact>();
                        foreach (Contact c in e._contacts)
                        {
                            contacts.Add(new Contact(c._pos3d));
                        }
                        Edge clone_edge = new Edge(clone_nodes[i], adjNode, contacts);
                        clone_nodes[i].addAnEdge(clone_edge);
                        if (adjNode != other)
                        {
                            adjNode.addAnEdge(clone_edge);
                        }
                    }
                }
            }
            return clone_nodes;
        }// cloneNodesAndRelations

        private int runMutateOrCrossover(Random rand)
        {
            int n = 5;
            int r = rand.Next(n);
            if (r >= 3)
            {
                r = 2;
            }
            return r;
        }// runMutateOrCrossover

        private List<Common.Functionality> selectFunctionality(Random rand, int maxNfunc)
        {
            int n = 6;
            int r = rand.Next(n) + 1;
            r = Math.Min(r, maxNfunc);
            List<Common.Functionality> funcs = new List<Common.Functionality>();
            for (int i = 0; i < r; ++i)
            {
                int j = rand.Next(n);
                if (j >= n)
                {
                    j = 2;
                }
                Common.Functionality f = getFunctionalityFromIndex(j);
                if (!funcs.Contains(f))
                {
                    funcs.Add(f);
                }
            }
            return funcs;
        }// selectAFunctionality

        private List<Node> selectSubsetNodes(List<Node> nodes, int n)
        {
            List<Node> selected = new List<Node>();
            List<Node> sym_nodes = new List<Node>();
            Random rand = new Random();
            foreach (Node node in nodes)
            {
                if (node.symmetry != null && !selected.Contains(node) && !selected.Contains(node.symmetry))
                {
                    selected.Add(node);
                    selected.Add(node.symmetry);
                }
            }
            if (n < 2)
            {
                // take symmetry
                int nsym = sym_nodes.Count / 2;
                for (int i = 0; i < nsym; i += 2)
                {
                    int s = rand.Next(2);
                    if (s == 0)
                    {
                        selected.Add(sym_nodes[i]);
                        selected.Add(sym_nodes[i + 1]);
                    }
                }
                if (selected.Count == 0 && sym_nodes.Count != 0)
                {
                    int j = rand.Next(nsym);
                    selected.Add(sym_nodes[j * 2]);
                    selected.Add(sym_nodes[j * 2 + 1]);
                }
            }
            else if (n < 4)
            {
                //// node propagation --- all inner nodes that only connect to the #nodes#
                //selected = Graph.GetNodePropagation(nodes);
                // break symmetry
                if (sym_nodes.Count == 2)
                {
                    int s = rand.Next(2);
                    selected.Add(sym_nodes[s]);
                }
            }
            else
            {
                selected = nodes;
            }
            return selected;
        }// selectSubsetNodes

        private bool hasGroundTouching(List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    return true;
                }
            }
            return false;
        }// hasGroundTouching

        private void selectReplaceableNodesPair(Graph g1, Graph g2, out List<List<Node>> nodeChoices1, out List<List<Node>> nodeChoices2)
        {
            nodeChoices1 = new List<List<Node>>();
            nodeChoices2 = new List<List<Node>>();
            // ground touching
            List<Node> nodes1 = g1.getGroundTouchingNodes();
            List<Node> nodes2 = g2.getGroundTouchingNodes();
            nodeChoices1.Add(nodes1);
            nodeChoices2.Add(nodes2);

            // Key nodes
            List<List<Node>> splitNodes1 = g1.splitAlongKeyNode();
            List<List<Node>> splitNodes2 = g2.splitAlongKeyNode();
            if (splitNodes1.Count < splitNodes2.Count)
            {
                if (hasGroundTouching(splitNodes2[1]))
                {
                    splitNodes2.RemoveAt(2);
                }
                else
                {
                    splitNodes2.RemoveAt(1);
                }
            }
            else if (splitNodes1.Count > splitNodes2.Count)
            {
                if (hasGroundTouching(splitNodes1[1]))
                {
                    splitNodes1.RemoveAt(2);
                }
                else
                {
                    splitNodes1.RemoveAt(1);
                }
            }
            else
            {
                nodeChoices1.AddRange(splitNodes1);
                nodeChoices2.AddRange(splitNodes2);
            }
            // symmetry nodes
            List<List<Node>> symPairs1 = g1.getSymmetryPairs();
            List<List<Node>> symPairs2 = g2.getSymmetryPairs();
            List<int> outEdgeNum1 = new List<int>();
            List<int> outEdgeNum2 = new List<int>();
            foreach (List<Node> nodes in symPairs1)
            {
                outEdgeNum1.Add(g1.getOutgoingEdges(nodes).Count);
            }
            foreach (List<Node> nodes in symPairs2)
            {
                outEdgeNum2.Add(g2.getOutgoingEdges(nodes).Count);
            }
            for (int i = 0; i < symPairs1.Count; ++i)
            {
                bool isGround1 = symPairs1[i][0]._isGroundTouching;
                for (int j = 0; j < symPairs2.Count; ++j)
                {
                    bool isGround2 = symPairs2[j][0]._isGroundTouching;
                    if ((isGround1 && isGround2) || (!isGround1 && !isGround2 && outEdgeNum1[i] == outEdgeNum2[j]))
                    {
                        nodeChoices1.Add(symPairs1[i]);
                        nodeChoices2.Add(symPairs2[j]);
                    }
                }
            }
        }// selectReplaceableNodesPair

        public void setSelectedNodes()
        {
            if (_currModel == null)
            {
                return;
            }
            _currModel._GRAPH.selectedNodes = new List<Node>();
            foreach (Node node in _currModel._GRAPH._NODES)
            {
                if (_selectedParts.Contains(node._PART))
                {
                    _currModel._GRAPH.selectedNodes.Add(node);
                }
            }
            if (!_crossOverBasket.Contains(_currModel))
            {
                _crossOverBasket.Add(_currModel);
            }
            _currModel._GRAPH.selectedNodePairs.Add(_currModel._GRAPH.selectedNodes);
            Program.writeToConsole(_currModel._GRAPH.selectedNodes.Count.ToString() + " nodes in Graph #" + _selectedModelIndex.ToString() + " are selcted.");
        }// setSelectedNodes

        public List<ModelViewer> getMutateViewers()
        {
            List<Model> models = mutate();
            List<ModelViewer> _resViewers = new List<ModelViewer>();
            int i = 0;
            foreach (Model m in models)
            {
                _resViewers.Add(new ModelViewer(m, i++, this, 1));
            }
            return _resViewers;
        }// getMutateViewers

        private List<Model> mutate()
        {
            if (_currModel == null)
            {
                return null;
            }
            List<Model> mutatedModels = new List<Model>();
            int n = _currModel._NPARTS;
            bool[] mutated = new bool[_currModel._NPARTS];
            Random rand = new Random();
            double s1 = 0.5;
            double s2 = 2.0;
            int idx = 0;
            string path = _currModel._path + "mutate_models\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            for (int i = 0; i < n; ++i)
            {
                //int j = rand.Next(n);
                //while (mutated[j])
                //{
                //    j = rand.Next(n);
                //}
                //int axis = rand.Next(3);
                // permute
                int j = i;
                if (_currModel._GRAPH._NODES[j].symmetry != null && _currModel._GRAPH._NODES[j]._INDEX > _currModel._GRAPH._NODES[j].symmetry._INDEX)
                {
                    continue;
                }
                for (int axis = 0; axis < 3; ++axis)
                {
                    ++idx;
                    Model model = _currModel.Clone() as Model;
                    hasInValidContact(model._GRAPH);
                    model._path = path.Clone() as string;
                    model._model_name = _currModel._model_name + "_mutate_" + idx.ToString();
                    Node updateNode = model._GRAPH._NODES[j];
                    double scale = s1 + rand.NextDouble() * (s2 - s1);
                    Vector3d sv = new Vector3d(1, 1, 1);
                    sv[axis] = scale;
                    Matrix4d S = Matrix4d.ScalingMatrix(sv);
                    Vector3d center = model._GRAPH._NODES[j]._pos;
                    Matrix4d Q = Matrix4d.TranslationMatrix(center) * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
                    if (updateNode._isGroundTouching)
                    {
                        Node cNode = updateNode.Clone() as Node;
                        deformANodeAndEdges(cNode, Q);
                        Vector3d trans = new Vector3d();
                        trans.y = -cNode._PART._BOUNDINGBOX.MinCoord.y;
                        Matrix4d T = Matrix4d.TranslationMatrix(trans);
                        Q = T * Q;
                    }
                    deformANodeAndEdges(updateNode, Q);
                    deformSymmetryNode(updateNode);
                    hasInValidContact(model._GRAPH);
                    deformPropagation(model._GRAPH, updateNode);
                    model._GRAPH.resetUpdateStatus();
                    mutatedModels.Add(model);
                    ++idx;
                }// each axis
            }
            return mutatedModels;
        }// mutate

        private void deformANodeAndEdges(Node node, Matrix4d T)
        {
            node.Transform(T);
            node.updated = true;
            foreach (Edge e in node._edges)
            {
                if (!e._contactUpdated)
                {
                    e.TransformContact(T);
                }
            }
        }// deformANodeAndEdges

        private bool deformSymmetryNode(Node node)
        {
            if (node.symmetry == null || node.symmetry.updated)
            {
                return false;
            }

            Node other = node.symmetry;
            Symmetry symm = node.symm;
            // get scales
            Vector3d s2 = node._PART._BOUNDINGBOX._scale;
            Vector3d s1 = other._PART._BOUNDINGBOX._scale;

            Vector3d cc = Matrix4d.GetMirrorSymmetryPoint(node._pos, symm._axis, symm._center);
            Matrix4d T1 = Matrix4d.TranslationMatrix(new Vector3d() - other._pos);
            Matrix4d T2 = Matrix4d.TranslationMatrix(cc);
            Matrix4d S = Matrix4d.ScalingMatrix(s2.x / s1.x, s2.y / s1.y, s2.z / s1.z);

            Matrix4d Q = T2 * S * T1;

            deformANodeAndEdges(other, Q);

            return true;
        }// deformSymmetryNode

        private void deformPropagation(Graph graph, Node edited)
        {
            Node activeNode = edited;
            int time = 0;
            while (activeNode != null)
            {
                int maxNumUpdatedContacts = -1;
                activeNode = null;
                foreach (Node node in graph._NODES)
                {
                    if (node.updated && !node.isAllNeighborsUpdated())
                    {
                        int nUpdatedContacts = 0;
                        foreach (Edge e in node._edges)
                        {
                            if (e._contactUpdated)
                            {
                                ++nUpdatedContacts;
                            }
                        }
                        if (nUpdatedContacts > maxNumUpdatedContacts)
                        {
                            maxNumUpdatedContacts = nUpdatedContacts;
                            activeNode = node;
                        }
                    }
                }// foreach
                if (activeNode != null)
                {
                    // deform all neighbors
                    activeNode._allNeigborUpdated = true;
                    // deoform from the most updated one
                    Node toUpdate = activeNode;
                    while (toUpdate != null)
                    {
                        int nMatxUpdatedContacts = -1;
                        toUpdate = null;
                        foreach (Node node in activeNode._adjNodes)
                        {
                            int nUpdatedContacts = 0;
                            if (node.updated)
                            {
                                continue;
                            }
                            foreach (Edge e in node._edges)
                            {
                                if (e._contactUpdated)
                                {
                                    ++nUpdatedContacts;
                                }
                            }
                            if (nUpdatedContacts > nMatxUpdatedContacts)
                            {
                                nMatxUpdatedContacts = nUpdatedContacts;
                                toUpdate = node;
                            }
                        }
                        if (toUpdate != null)
                        {
                            deformNode(toUpdate);
                            deformSymmetryNode(toUpdate);
                            if (hasInValidContact(graph))
                            {
                                ++time;
                            }
                        }
                    }
                }
            }// while
        }// deformPropagation        

        private void deformNode(Node node)
        {
            List<Vector3d> sources = new List<Vector3d>();
            List<Vector3d> targets = new List<Vector3d>();
            foreach (Edge e in node._edges)
            {
                if (e._contactUpdated)
                {
                    sources.AddRange(e.getOriginContactPoints());
                    targets.AddRange(e.getContactPoints());
                }
            }
            if (sources.Count > 0 && node._isGroundTouching)
            {
                sources.Add(new Vector3d(sources[0].x, 0, sources[0].z));
                targets.Add(new Vector3d(targets[0].x, 0, targets[0].z));
            }
            Matrix4d T, S, Q;
            getTransformation(sources, targets, out S, out T, out Q, null, false);
            node.Transform(Q);
            node.updated = true;
            foreach (Edge e in node._edges)
            {
                if (e._contactUpdated)
                {
                    continue;
                }
                e.TransformContact(Q);
            }
        }// deformNode

        public List<ModelViewer> crossOver()
        {
            if (_crossOverBasket.Count < 2)
            {
                return null;
            }
            List<ModelViewer> _resViewers = new List<ModelViewer>();
            int k = 0;
            for (int i = 0; i < _crossOverBasket.Count - 1; ++i)
            {
                Model m1 = _crossOverBasket[i];
                for (int j = i + 1; j < _crossOverBasket.Count; ++j)
                {
                    Model m2 = _crossOverBasket[j];
                    List<Node> nodes1 = new List<Node>();
                    List<Node> nodes2 = new List<Node>();
                    //findOneToOneMatchingNodes(g1, g2, out nodes1, out nodes2);

                    Model newM1 = m1.Clone() as Model;
                    Model newM2 = m2.Clone() as Model;

                    foreach (Node node in m1._GRAPH.selectedNodes)
                    {
                        nodes1.Add(newM1._GRAPH._NODES[node._INDEX]);
                    }
                    foreach (Node node in m2._GRAPH.selectedNodes)
                    {
                        nodes2.Add(newM2._GRAPH._NODES[node._INDEX]);
                    }

                    if (nodes1 == null || nodes2 == null || nodes1.Count == 0 || nodes2.Count == 0)
                    {
                        return null;
                    }

                    List<Node> updatedNodes1;
                    List<Node> updatedNodes2;
                    switchNodes(m1._GRAPH, m2._GRAPH, nodes1, nodes2, out updatedNodes1, out updatedNodes2);

                    newM1.replaceNodes(nodes1, updatedNodes2);
                    _resViewers.Add(new ModelViewer(newM1, k++, this, 1));
                    newM2.replaceNodes(nodes2, updatedNodes1);
                    _resViewers.Add(new ModelViewer(newM2, k++, this, 1));
                }
            }
            _crossOverBasket.Clear();
            return _resViewers;
        }// crossover

        public List<Model> crossOver(List<Model> models)
        {
            if (models.Count < 2)
            {
                return null;
            }
            List<Model> crossedModels = new List<Model>();
            int k = 0;
            for (int i = 0; i < models.Count - 1; ++i)
            {
                Model m1 = models[i];
                for (int j = i + 1; j < models.Count; ++j)
                {
                    Model m2 = models[j];
                    // set selected nodes
                    List<List<Node>> nodeChoices1;
                    List<List<Node>> nodeChoices2;

                    if (_replaceablePairs != null && _replaceablePairs[i, j] != null && _replaceablePairs[i, j]._pair1.Count != 0)
                    {
                        nodeChoices1 = _replaceablePairs[i, j]._pair1;
                        nodeChoices2 = _replaceablePairs[i, j]._pair2;
                    }
                    else
                    {
                        this.selectReplaceableNodesPair(m1._GRAPH, m2._GRAPH, out nodeChoices1, out nodeChoices2);
                    }
                    for (int t = 0; t < nodeChoices1.Count; ++t)
                    {
                        m1._GRAPH.selectedNodes = nodeChoices1[t];
                        m2._GRAPH.selectedNodes = nodeChoices2[t];
                        List<Model> crossed = this.crossOverOp(m1, m2, t, k);
                        k += 2;
                        crossedModels.AddRange(crossed);
                    }
                }
            }
            return crossedModels;
        }// crossover

        private Node hasGroundTouchingNode(List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    return node;
                }
            }
            return null;
        }// hasGroundTouchingNode

        private List<Node> getGroundTouchingNode(List<Node> nodes)
        {
            List<Node> grounds = new List<Node>();
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    grounds.Add(node);
                }
            }
            return grounds;
        }// getGroundTouchingNode

        private List<Vector3d> collectPoints(List<Edge> edges)
        {
            List<Vector3d> points = new List<Vector3d>();
            foreach (Edge e in edges)
            {
                points.AddRange(e.getContactPoints());
            }
            return points;
        }// collectPoints

        private bool isNaNMat(Matrix4d m)
        {
            for (int i = 0; i < 16; ++i)
            {
                if (double.IsNaN(m[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public void getTransformation(List<Vector3d> srcpts, List<Vector3d> tarpts, out Matrix4d S, out Matrix4d T, out Matrix4d Q, Vector3d boxScale, bool useScale)
        {
            int n = srcpts.Count;
            if (n == 1)
            {
                double ss = 1;
                Vector3d trans = tarpts[0] - srcpts[0];
                S = Matrix4d.ScalingMatrix(ss, ss, ss);
                if (useScale && boxScale.isValidVector())
                {
                    S = Matrix4d.ScalingMatrix(boxScale);
                }
                T = Matrix4d.TranslationMatrix(trans);
                Q = Matrix4d.TranslationMatrix(tarpts[0]) * S * Matrix4d.TranslationMatrix(new Vector3d() - srcpts[0]);
                if (isNaNMat(Q))
                {
                    Q = Matrix4d.IdentityMatrix();
                }
            }
            else if (n == 2)
            {
                Vector3d c1 = (srcpts[0] + srcpts[1]) / 2;
                Vector3d c2 = (tarpts[0] + tarpts[1]) / 2;
                Vector3d v1 = srcpts[1] - srcpts[0];
                Vector3d v2 = tarpts[1] - tarpts[0];
                if (v1.Dot(v2) < 0) v1 = new Vector3d() - v1;
                double ss = v2.Length() / v1.Length();
                if (double.IsNaN(ss))
                {
                    ss = 1.0;
                }
                S = Matrix4d.ScalingMatrix(ss, ss, ss);

                if (useScale && boxScale.isValidVector())
                {
                    S = Matrix4d.ScalingMatrix(boxScale);
                }

                Matrix4d R = Matrix4d.IdentityMatrix();
                double cos = v1.normalize().Dot(v2.normalize());
                if (cos < Math.Cos(1.0 / 18 * Math.PI))
                {
                    Vector3d axis = v1.Cross(v2).normalize();
                    double theta = Math.Acos(cos);
                    R = Matrix4d.RotationMatrix(axis, theta);
                    if (isNaNMat(R))
                    {
                        R = Matrix4d.IdentityMatrix();
                    }
                }
                T = Matrix4d.TranslationMatrix(c2 - c1);
                Q = Matrix4d.TranslationMatrix(c2) * R * S * Matrix4d.TranslationMatrix(new Vector3d() - c1);
                if (isNaNMat(Q))
                {
                    Q = Matrix4d.IdentityMatrix();
                }
            }
            else
            {
                Vector3d t1 = new Vector3d();
                Vector3d t2 = new Vector3d();
                foreach (Vector3d tt in srcpts)
                    t1 += tt;
                foreach (Vector3d tt in tarpts)
                    t2 += tt;
                t1 /= srcpts.Count;
                t2 /= tarpts.Count;

                Vector3d trans = t2 - t1;
                T = Matrix4d.TranslationMatrix(trans);

                // find the scales
                int k = srcpts.Count;
                double sx = 0, sy = 0, sz = 0;
                for (int i = 0; i < k; ++i)
                {
                    Vector3d p1 = srcpts[i] - t1;
                    Vector3d p2 = tarpts[i] - t2;
                    sx += p2.x / p1.x;
                    sy += p2.y / p1.y;
                    sz += p2.z / p1.z;
                }
                sx /= k;
                sy /= k;
                sz /= k;

                if (double.IsNaN(sx) || double.IsInfinity(sx))
                {
                    sx = 1.0;
                }
                if (double.IsNaN(sy) || double.IsInfinity(sy))
                {
                    sy = 1.0;
                }
                if (double.IsNaN(sz) || double.IsInfinity(sz))
                {
                    sz = 1.0;
                }

                // adjust scale 
                if (n > 3)
                {
                    //// find the points plane
                    //double[] points = new double[srcpts.Count * 3];
                    //for (int i = 0, jj = 0; i < srcpts.Count; ++i, jj += 3)
                    //{
                    //    points[jj] = srcpts[i].x;
                    //    points[jj + 1] = srcpts[i].y;
                    //    points[jj + 2] = srcpts[i].z;
                    //}
                    //double[] plane = new double[4];
                    //PlaneFitter.ZyyPlaneFitter _plfitter = new PlaneFitter.ZyyPlaneFitter();
                    //fixed (double* _pts = points, _pl = plane)
                    //    _plfitter.GetFittingPlane(_pts, srcpts.Count, _pl);
                    //Vector3d normal = new Vector3d(plane, 0);
                    //normal = normal.normalize();

                    // permutations
                    Vector3d[] vecs = new Vector3d[4];
                    vecs[0] = tarpts[0];
                    vecs[1] = tarpts[1];
                    vecs[2] = tarpts[2];
                    vecs[3] = tarpts[3];
                    Vector3d[] nn = new Vector3d[4] {
                        ((vecs[0] - vecs[1]).Cross(vecs[1] - vecs[2])).normalize(),
                        ((vecs[1] - vecs[2]).Cross(vecs[2] - vecs[3])).normalize(),
                        ((vecs[0] - vecs[2]).Cross(vecs[2] - vecs[3])).normalize(),
                        ((vecs[0] - vecs[1]).Cross(vecs[1] - vecs[3])).normalize()
                    };
                    Random rand = new Random();
                    int npnts = tarpts.Count;
                    while (this.hasInvalidVec(nn))
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            vecs[i] = tarpts[rand.Next(npnts)];
                        }
                        nn = new Vector3d[4] {
                        ((vecs[0] - vecs[1]).Cross(vecs[1] - vecs[2])).normalize(),
                        ((vecs[1] - vecs[2]).Cross(vecs[2] - vecs[3])).normalize(),
                        ((vecs[0] - vecs[2]).Cross(vecs[2] - vecs[3])).normalize(),
                        ((vecs[0] - vecs[1]).Cross(vecs[1] - vecs[3])).normalize()
                        };
                    }

                    Vector3d nor = new Vector3d();
                    for (int i = 0; i < 4; ++i)
                    {
                        if (!double.IsNaN(nn[i].x))
                        {
                            nor = nn[i];
                            break;
                        }
                    }
                    Vector3d normal = new Vector3d();
                    int count = 0;
                    for (int i = 0; i < 4; ++i)
                    {
                        if (!double.IsNaN(nn[i].x))
                        {
                            if (nn[i].Dot(nor) < 0)
                            {
                                nn[i] = new Vector3d() - nn[i];
                            }
                            normal += nn[i];
                            count++;
                        }
                    }
                    normal = normal.normalize();

                    if (Common.isValidNumber(normal.x) && Common.isValidNumber(normal.y) && Common.isValidNumber(normal.z))
                    {
                        if (Math.Abs(normal.x) > 0.5)
                        {
                            sx = 1.0;
                        }
                        else if (Math.Abs(normal.y) > 0.5)
                        {
                            sy = 1.0;
                        }
                        else if (Math.Abs(normal.z) > 0.5)
                        {
                            sz = 1.0;
                        }
                    }
                }

                Vector3d scale = new Vector3d(sx, sy, sz);
                //scale = adjustScale(scale);

                if (double.IsNaN(scale.x) || double.IsNaN(trans.x)) throw new Exception();

                S = Matrix4d.ScalingMatrix(scale.x, scale.y, scale.z);

                if (useScale && boxScale.isValidVector())
                {
                    S = Matrix4d.ScalingMatrix(boxScale);
                }

                Q = Matrix4d.TranslationMatrix(t2) * S * Matrix4d.TranslationMatrix(new Vector3d() - t1);
                if (isNaNMat(Q))
                {
                    Q = Matrix4d.IdentityMatrix();
                }
            }
        }// getTransformation

        private Vector3d adjustScale(Vector3d scale)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (scale[i] > Common._max_scale)
                {
                    scale[i] = Common._max_scale;
                }

                if (scale[i] < Common._min_scale)
                {
                    scale[i] = Common._min_scale;
                }
            }
            return scale;
        }// adjustScale

        private bool hasInvalidVec(Vector3d[] vecs)
        {
            foreach (Vector3d v in vecs)
            {
                if (v.isValidVector())
                {
                    return true;
                }
            }
            return false;
        }// hasInvalidVec

        /*****************end - Functionality-aware evolution*************************/


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
                case 9:
                    this.currUIMode = UIMode.Contact;
                    clearHighlights();
                    break;
                case 0:
                default:
                    this.currUIMode = UIMode.Viewing;
                    break;
            }
            if (i >= 6 && i <= 9)
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
                    this.isDrawBbox = !this.isDrawBbox;
                    break;
                case 5:
                    this.isDrawGraph = !this.isDrawGraph;
                    break;
                case 6:
                    this.isDrawFuncSpace = !this.isDrawFuncSpace;
                    break;
                case 3:
                default:
                    this.drawFace = !this.drawFace;
                    break;
            }
            this.Refresh();
        }//setRenderOption

        public void setShowHumanPoseOption(bool isTranlucent)
        {
            _isDrawTranslucentHumanPose = isTranlucent;
            this.Refresh();
        }

        public void setShowAxesOption(bool isShow)
        {
            this.isDrawAxes = isShow;
            this.Refresh();
        }

        public void setRandomColorToNodes()
        {
            if (_currModel != null && _currModel._GRAPH != null)
            {
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    node._PART.setRandomColorToNodes();
                }
            }
        }// setRandomColorToNodes

        private void calEditAxesLoc()
        {
            Vector3d center = new Vector3d();
            double ad = 0;
            if (_selectedParts.Count > 0)
            {
                foreach (Part p in _selectedParts)
                {
                    center += p._BOUNDINGBOX.CENTER;
                    double d = (p._BOUNDINGBOX.MaxCoord - p._BOUNDINGBOX.MinCoord).Length();
                    ad = ad > d ? ad : d;
                }
                center /= _selectedParts.Count;
            }
            else if (_currHumanPose != null)
            {
                center = _currHumanPose._ROOT._POS;
            }
            else if (_selectedEdge != null && _selectedContact != null)
            {
                center = _selectedContact._pos3d;
            }
            ad /= 2;
            if (ad == 0)
            {
                ad = 0.2;
            }
            double arrow_d = ad / 6;
            _editAxes = new Contact[18];
            for (int i = 0; i < _editAxes.Length; ++i)
            {
                _editAxes[i] = new Contact(new Vector3d());
            }
            _editAxes[0]._pos3d = center - ad * Vector3d.XCoord;
            _editAxes[1]._pos3d = center + ad * Vector3d.XCoord;
            _editAxes[2]._pos3d = _editAxes[1]._pos3d - arrow_d * Vector3d.XCoord + arrow_d * Vector3d.YCoord;
            _editAxes[3]._pos3d = new Vector3d(_editAxes[1]._pos3d);
            _editAxes[4]._pos3d = _editAxes[1]._pos3d - arrow_d * Vector3d.XCoord - arrow_d * Vector3d.YCoord;
            _editAxes[5]._pos3d = new Vector3d(_editAxes[1]._pos3d);

            _editAxes[6]._pos3d = center - ad * Vector3d.YCoord;
            _editAxes[7]._pos3d = center + ad * Vector3d.YCoord;
            _editAxes[8]._pos3d = _editAxes[7]._pos3d - arrow_d * Vector3d.YCoord + arrow_d * Vector3d.XCoord;
            _editAxes[9]._pos3d = new Vector3d(_editAxes[7]._pos3d);
            _editAxes[10]._pos3d = _editAxes[7]._pos3d - arrow_d * Vector3d.YCoord - arrow_d * Vector3d.XCoord;
            _editAxes[11]._pos3d = new Vector3d(_editAxes[7]._pos3d);

            _editAxes[12]._pos3d = center - ad * Vector3d.ZCoord;
            _editAxes[13]._pos3d = center + ad * Vector3d.ZCoord;
            _editAxes[14]._pos3d = _editAxes[13]._pos3d - arrow_d * Vector3d.ZCoord + arrow_d * Vector3d.XCoord;
            _editAxes[15]._pos3d = new Vector3d(_editAxes[13]._pos3d);
            _editAxes[16]._pos3d = _editAxes[13]._pos3d - arrow_d * Vector3d.ZCoord - arrow_d * Vector3d.XCoord;
            _editAxes[17]._pos3d = new Vector3d(_editAxes[13]._pos3d);
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
            double[] ballmat = m.Transpose().ToArray();	// matrix applied with arcball
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
            }
            else if (this.arcBall.motion == ArcBall.MotionType.Rotate)
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
        }// viewMouseUp

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.mouseDownPos = new Vector2d(e.X, e.Y);
            this.currMousePos = new Vector2d(e.X, e.Y);
            this.isMouseDown = true;
            this.highlightQuad = null;
            _isRightClick = e.Button == System.Windows.Forms.MouseButtons.Right;

            this.ContextMenuStrip = Program.GetFormMain().getRightButtonMenu();
            this.ContextMenuStrip = Program.GetFormMain().getRightButtonMenu();
            if (this.ContextMenuStrip != null)
            {
                this.ContextMenuStrip.Hide();
            }

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
                case UIMode.Contact:
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
                        if (_currModel != null)
                        {
                            if (this.isMouseDown)
                            {
                                this.transformSelections(this.currMousePos);
                            }
                            else
                            {
                                this.selectAxisWhileMouseMoving(this.currMousePos);
                            }
                            this.Refresh();
                        }
                    }
                    break;
                case UIMode.Contact:
                    {
                        if (_currModel != null && _currModel._GRAPH != null)
                        {
                            if (this.isMouseDown)
                            {
                                this.moveContactPoint(this.currMousePos);
                            }
                            else
                            {
                                this.selectContactPoint(currMousePos);
                                this.selectAxisWhileMouseMoving(this.currMousePos);
                            }
                            this.Refresh();
                        }
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
                case UIMode.Translate:
                case UIMode.Scale:
                case UIMode.Rotate:
                    {
                        this.editMouseUp();
                        this.Refresh();
                    }
                    break;
                case UIMode.Contact:
                    {
                        //_selectedEdge = null;
                        this.moveContactUp();
                        this.Refresh();
                    }
                    break;
                case UIMode.Viewing:
                default:
                    {
                        this.viewMouseUp();
                        this.refreshModelViewers();
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
                        Program.GetFormMain().updateStats();
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
                case Keys.C:
                    {
                        _showContactPoint = !_showContactPoint;
                        if (this._showContactPoint)
                        {
                            this.setUIMode(9); // contact
                        }
                        else
                        {
                            this.setUIMode(0);
                        }
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
                case Keys.Delete:
                    {
                        this.deleteParts();
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
            // cannot use GRAPH, as it maybe used for data preprocessing, i.e., grouping, i dont need graph here
            if (this._currModel == null || q == null) return;
            this.cal2D();
            _selectedNodes = new List<Node>();
            foreach (Part p in _currModel._PARTS)
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
                            _selectedParts.Add(p);
                        }
                        break;
                    }
                }
            }
            if (_currModel._GRAPH != null)
            {
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    if (_selectedParts.Contains(node._PART))
                    {
                        _selectedNodes.Add(node);
                    }
                }
            }
        }//selectBbox

        public void selectContactPoint(Vector2d mousePos)
        {
            if (this._currModel == null || _currModel._GRAPH == null) return;
            this.cal2D();
            double mind = double.MaxValue;
            _selectedEdge = null;
            _selectedContact = null;
            Edge nearestEdge = null;
            Contact nearestContact = null;
            foreach (Edge e in this._currModel._GRAPH._EDGES)
            {
                foreach (Contact pnt in e._contacts)
                {
                    Vector2d v2 = pnt._pos2d;
                    double dis = (v2 - mousePos).Length();
                    if (dis < mind)
                    {
                        mind = dis;
                        nearestEdge = e;
                        nearestContact = pnt;
                    }
                }
            }

            if (mind < Common._thresh2d)
            {
                _selectedEdge = nearestEdge;
                _selectedContact = nearestContact;
            }
            if (_selectedEdge != null)
            {
                this.calEditAxesLoc();
            }
        }//selectContactPoint

        public void setMeshColor(Color c)
        {
            foreach (Part p in _selectedParts)
            {
                p._COLOR = c;
            }
            this.Refresh();
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
            _currModel._GRAPH = null;
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
            ModelViewer mv = new ModelViewer(m, -1, this, 1);
            _partViewers.Add(mv);
            return mv;
        }// addSelectedPartsToBasket

        private void editMouseDown(int mode, Vector2d mousePos)
        {
            _editArcBall = new ArcBall(this.Width, this.Height);
            switch (mode)
            {
                case 1: // Translate
                    _editArcBall.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Pan);
                    break;
                case 2: // Scaling
                    _editArcBall.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Scale);
                    break;
                case 3: // Rotate
                    _editArcBall.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Rotate);
                    break;
            }
        }// editMouseDown

        private Matrix4d editMouseMove(int x, int y)
        {
            if (!this.isMouseDown) return Matrix4d.IdentityMatrix();
            _editArcBall.mouseMove(x, y);
            Matrix4d T = _editArcBall.getTransformMatrix(3);
            return T;
        }// editMouseMove

        private void editMouseUp()
        {
            _hightlightAxis = -1;
            foreach (Part p in _selectedParts)
            {
                p.updateOriginPos();
            }
            if (_currHumanPose != null)
            {
                _currHumanPose.updateOriginPos();
                this.updateBodyBones();
            }
            if (_editArcBall != null)
            {
                _editArcBall.mouseUp();
            }
            this.cal2D();
        }// editMouseUp

        private void transformSelections(Vector2d mousePos)
        {
            if (_selectedParts.Count == 0 && _currHumanPose == null)
            {
                return;
            }
            Matrix4d T = editMouseMove((int)mousePos.x, (int)mousePos.y);
            // use a fixed axis
            switch (this.currUIMode)
            {
                case UIMode.Translate:
                    {
                        if (_hightlightAxis == 2)
                        {
                            T[2, 3] = T[0, 3];
                        }
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
                        if (!_isRightClick) // right click == uniform scale
                        {
                            for (int i = 0; i < 3; ++i)
                            {
                                if (_hightlightAxis != -1 && i != _hightlightAxis)
                                {
                                    T[i, i] = 1;
                                }
                            }
                        }
                        break;
                    }
                case UIMode.Rotate:
                    {
                        if (_hightlightAxis != -1)
                        {
                            T = _editArcBall.getRotationMatrixAlongAxis(_hightlightAxis);
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
            Vector3d ori = new Vector3d();
            if (_selectedParts.Count > 0)
            {
                ori = getCenter(_selectedParts);
                foreach (Part p in _selectedParts)
                {
                    p.TransformFromOrigin(T);
                }
            }
            else if (_currHumanPose != null) // NOTE!! else relation
            {
                ori = _currHumanPose._ROOT._ORIGIN;
                _currHumanPose.TransformFromOrigin(T);
            }

            if (this.currUIMode != UIMode.Translate)
            {
                Vector3d after = new Vector3d();
                if (_selectedParts.Count > 0)
                {
                    after = getCenter(_selectedParts);
                    Matrix4d TtoCenter = Matrix4d.TranslationMatrix(ori - after);
                    foreach (Part p in _selectedParts)
                    {
                        p.Transform(TtoCenter);
                    }
                }
                else if (_currHumanPose != null)
                {
                    after = _currHumanPose._ROOT._POS;
                    Matrix4d TtoCenter = Matrix4d.TranslationMatrix(ori - after);
                    if (_currHumanPose != null)
                    {
                        foreach (BodyNode bn in _currHumanPose._bodyNodes)
                        {
                            bn.Transform(TtoCenter);
                        }
                    }
                }
            }
        }// transformSelections

        private void moveContactPoint(Vector2d mousePos)
        {
            if (_selectedEdge == null || _selectedContact == null)
            {
                return;
            }
            Matrix4d T = editMouseMove((int)mousePos.x, (int)mousePos.y);
            if (_hightlightAxis == 2)
            {
                T[2, 3] = T[0, 3];
            }
            for (int i = 0; i < 3; ++i)
            {
                if (_hightlightAxis != -1 && i != _hightlightAxis)
                {
                    T[i, 3] = 0;
                }
            }
            _selectedContact.TransformFromOrigin(T);
        }// moveContactPoint

        private void moveContactUp()
        {
            if (_currModel != null && _currModel._GRAPH != null)
            {
                foreach (Edge e in _currModel._GRAPH._EDGES)
                {
                    foreach (Contact c in e._contacts)
                    {
                        c.updateOrigin();
                    }
                }
            }
        }// moveContactUp

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
            this.updateCamera();
            this.cal2D();
            Vector2d s = _editAxes[0]._pos2d;
            Vector2d e = _editAxes[1]._pos2d;
            Line2d xline = new Line2d(s, e);
            double xd = Polygon2D.PointDistToLine(mousePos, xline);

            s = _editAxes[6]._pos2d;
            e = _editAxes[7]._pos2d;
            Line2d yline = new Line2d(s, e);
            double yd = Polygon2D.PointDistToLine(mousePos, yline);

            s = _editAxes[12]._pos2d;
            e = _editAxes[13]._pos2d;
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

        public void addAnEdge()
        {
            if (_currModel == null || _currModel._GRAPH == null || _selectedParts.Count != 2)
            {
                return;
            }
            int i = _currModel._PARTS.IndexOf(_selectedParts[0]);
            int j = _currModel._PARTS.IndexOf(_selectedParts[1]);
            if (i != -1 && j != -1)
            {
                Edge e = _currModel._GRAPH.isEdgeExist(_currModel._GRAPH._NODES[i], _currModel._GRAPH._NODES[j]);
                if (e == null)
                {
                    _currModel._GRAPH.addAnEdge(_currModel._GRAPH._NODES[i], _currModel._GRAPH._NODES[j]);
                }
                else
                {
                    int ncontact = e._contacts.Count;
                    string s = "Already has " + ncontact.ToString() + " contacts, add a contact anyway?";
                    if (MessageBox.Show(s, "Edit Edge", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Vector3d v = e._contacts[0]._pos3d + new Vector3d(0.05, 0, 0);
                        e._contacts.Add(new Contact(v));
                    }
                }
            }
        }// addAnEdge

        public void deleteAnEdge()
        {
            if (_selectedParts.Count != 2)
            {
                return;
            }
            int i = _currModel._PARTS.IndexOf(_selectedParts[0]);
            int j = _currModel._PARTS.IndexOf(_selectedParts[1]);
            if (i != -1 && j != -1)
            {
                _currModel._GRAPH.deleteAnEdge(_currModel._GRAPH._NODES[i], _currModel._GRAPH._NODES[j]);
            }
        }// deleteAnEdge

        public void loadHuamPose(string filename)
        {
            _currHumanPose = this.loadAHumanPose(filename);
            _humanposes = new List<HumanPose>();
            _humanposes.Add(_currHumanPose);
            // test
            //Matrix4d T = Matrix4d.TranslationMatrix(new Vector3d(0, -0.2, -0.4));
            //foreach (BodyNode bn in _currHumanPose._bodyNodes)
            //{
            //    bn.TransformOrigin(T);
            //    bn.Transform(T);
            //}
            //foreach (BodyBone bb in _currHumanPose._bodyBones)
            //{
            //    bb.updateEntity();
            //}
        }// loadHuamPose

        public void importHumanPose(string filename)
        {
            HumanPose hp = this.loadAHumanPose(filename);
            _humanposes.Add(hp);
            this.Refresh();
        }

        private HumanPose loadAHumanPose(string filename)
        {
            HumanPose hp = new HumanPose();
            hp.loadPose(filename);
            return hp;
        }

        public void saveHumanPose(string name)
        {
            if (_currHumanPose != null)
            {
                _currHumanPose.savePose(name);
            }
        }// saveHumanPose

        private void SelectBodyNode(Vector2d mousePos)
        {
            if (_currHumanPose == null)
            {
                return;
            }
            _selectedNode = null;
            foreach (BodyNode bn in _currHumanPose._bodyNodes)
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
            this.updateCamera();
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
            node.TransformFromOrigin(T);
        }// DeformBodyNode

        private void DeformBodyNodePropagation(BodyNode node, Matrix4d T)
        {
            if (node == null) return;
            List<BodyNode> children = node.getDescendents();
            foreach (BodyNode bn in children)
            {
                bn.TransformFromOrigin(T);
            }
        }// DeformBodyNodePropagation

        private void updateBodyBones()
        {
            if (_currHumanPose == null) return;
            foreach (BodyBone bn in _currHumanPose._bodyBones)
            {
                bn.updateEntity();
            }
        }// DeformBodyNodePropagation

        //######### end-Part-based #########//

        //######### Data & feature from ICON2 paper #########//
        public void loadPatchInfo(string foldername, bool isOriginal)
        {
            this.foldername = foldername;
            string modelFolder = foldername + "\\meshes\\";
            string sampleFolder = foldername + "\\samples\\";
            string weightFolder = foldername + "\\weights\\";
            string funcSpaceFolder = foldername + "\\funcSpace\\";

            if (!Directory.Exists(sampleFolder) || !Directory.Exists(weightFolder) ||
                !Directory.Exists(modelFolder) || !Directory.Exists(funcSpaceFolder))
            {
                MessageBox.Show("Lack data folder.");
                return;
            }

            string[] modelFiles = Directory.GetFiles(modelFolder, "*.obj");
            string[] sampleFiles = Directory.GetFiles(sampleFolder, "*.poisson");
            string[] weightFiles = Directory.GetFiles(weightFolder, "*.csv");
            string[] funspaceFiles = Directory.GetFiles(funcSpaceFolder, "*.fs");

            _models = new List<Model>();

            int nfile = 0;
            // re-normalize all meshes w.r.t func space
            // make sure meshes, sample points, func space in the same scale
            foreach (string modelstr in modelFiles)
            {
                // model
                string model_name = Path.GetFileName(modelstr);
                if (isOriginal)
                {
                    model_name = model_name.Substring(0, model_name.LastIndexOf('_'));
                }
                else
                {
                    model_name = model_name.Substring(0, model_name.LastIndexOf('.'));
                }
                Mesh mesh = new Mesh(modelstr, false);
                // category name
                string category = model_name.Substring(model_name.LastIndexOf('_') + 1);
                // sample points
                string sample_name = sampleFolder + model_name + ".poisson";
                SamplePoints sp = loadSamplePoints(sample_name, mesh.FaceCount);           
                // weights
                List<string> cur_wfiles = new List<string>();
                // in case the order of files are not the same in diff folders
                int fid = 0;
                string model_name_filter = model_name + "_";
                string model_wight_name_filter = model_name_filter + "predict_" + category + "_";
                while (fid < weightFiles.Length)
                {
                    string weight_name = Path.GetFileName(weightFiles[fid]);
                    if (weight_name.StartsWith(model_wight_name_filter))
                    {
                        // locate the weight files
                        while (weight_name.StartsWith(model_wight_name_filter))
                        {
                            cur_wfiles.Add(weightFiles[fid++]);
                            if (fid >= weightFiles.Length)
                            {
                                break;
                            }
                            weight_name = Path.GetFileName(weightFiles[fid]);
                        }
                        break;
                    }
                    ++fid;
                }
                // load patch weights
                int npatch = 0;
                int nFaceFromSP = sp._faceIdx.Length;
                // multiple weights file w.r.t. patches
                double[,] weights_patches = new double[cur_wfiles.Count, nFaceFromSP];
                Color[,] colors_patches = new Color[cur_wfiles.Count, nFaceFromSP];
                sp._blendColors = new Color[nFaceFromSP];
                for (int c = 0; c < nFaceFromSP; ++c)
                {
                    sp._blendColors[c] = Color.LightGray;
                }
                foreach (string wfile in cur_wfiles)
                {
                    double minw;
                    double maxw;
                    double[] weights = loadPatchWeight(wfile, out minw, out maxw);
                    if (weights == null || weights.Length != nFaceFromSP)
                    {
                        MessageBox.Show("Weight file does not match sample file: " + Path.GetFileName(wfile));
                        continue;
                    }
                    double wdiff = maxw - minw;
                    for (int i = 0; i < weights.Length; ++i)
                    {
                        weights_patches[npatch, i] = weights[i];
                        double ratio = (weights[i] - minw) / wdiff;
                        if (ratio < 0.4)
                        {
                            continue;
                        }
                        Color color = GLDrawer.getColorGradient(ratio, npatch);
                        //mesh.setVertexColor(GLDrawer.getColorArray(color), i);
                        byte[] color_array = GLDrawer.getColorArray(color);
                        mesh.setFaceColor(color_array, sp._faceIdx[i]);
                        colors_patches[npatch, i] = GLDrawer.getColorRGB(color_array);
                        sp._blendColors[i] = colors_patches[npatch, i];
                    }
                    ++npatch;
                }
                // weights & colors
                sp._weights = weights_patches;
                sp._colors = colors_patches;
                //// functional space
                //fid = 0;
                //List<string> fspaceFiles = new List<string>();
                //while (fid < funspaceFiles.Length)
                //{
                //    string func_name = Path.GetFileName(funspaceFiles[fid]);
                //    if (func_name.StartsWith(model_name_filter))
                //    {
                //        // locate the weight files
                //        while (func_name.StartsWith(model_name_filter))
                //        {
                //            fspaceFiles.Add(funspaceFiles[fid++]);
                //            if (fid >= funspaceFiles.Length)
                //            {
                //                break;
                //            }
                //            func_name = Path.GetFileName(funspaceFiles[fid]);
                //        }
                //        break;
                //    }
                //    ++fid;
                //}
                //if (fspaceFiles.Count != npatch)
                //{
                //    MessageBox.Show("#Functional space file does not match weight file.");
                //    return;
                //}
                //FuncSpace[] fss = new FuncSpace[npatch];
                //int nfs = 0;
                //foreach (String fsfile in fspaceFiles)
                //{
                //    FuncSpace fs = loadFunctionSpace(fsfile);
                //    if (fs == null)
                //    {
                //        MessageBox.Show("Functional space file error: " + Path.GetFileName(fsfile));
                //        return;
                //    }
                //    fss[nfs++] = fs;
                //}

                FuncSpace[] fss = null;
                Model model = new Model(mesh, sp, fss, isOriginal);
                model._model_name = model_name;
                _models.Add(model);

                ++nfile;
                //if (nfile > 2)
                //{
                //    // TEST
                //    break;
                //}
            }
            if (_models.Count > 0)
            {
                _currModel = _models[0];
            }
            this.Refresh();
        }// loadPatchInfo

        private FuncSpace loadFunctionSpace(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            // load mesh
            Mesh mesh = new Mesh(filename, false); // not normalize
            double[] weights = new double[mesh.FaceCount];
            // weights
            using (StreamReader sr = new StreamReader(filename))
            {
                // read only weights
                char[] separators = { ' ', '\\', '\t' };
                int faceid = 0;
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs[0] == "v")
                    {
                        continue;
                    }
                    if (strs.Length < 5)
                    {
                        MessageBox.Show("Functional space file error: " + Path.GetFileName(filename));
                        continue;
                    }
                    double w = double.Parse(strs[4]);
                    weights[faceid++] = w;
                }
                FuncSpace fs = new FuncSpace(mesh, weights);
                return fs;
            }
        }// loadFunctionSpace

        private SamplePoints loadSamplePoints(string filename, int totalNFaces)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            List<int> faceIndex = new List<int>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                List<Vector3d> points = new List<Vector3d>();
                List<Vector3d> normals = new List<Vector3d>();
                int nline = 0;
                while (sr.Peek() > -1)
                {
                    ++nline;
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs.Length < 7)
                    {
                        MessageBox.Show("Wrong format at line " + nline.ToString());
                        return null;
                    }
                    // pos
                    points.Add(new Vector3d(double.Parse(strs[0]),
                        double.Parse(strs[1]),
                        double.Parse(strs[2])));
                    // normal
                    normals.Add(new Vector3d(double.Parse(strs[3]),
                        double.Parse(strs[4]),
                        double.Parse(strs[5])));
                    int fidx = int.Parse(strs[6]);
                    faceIndex.Add(fidx);
                }
                // colors
                string colorName = filename.Substring(0, filename.LastIndexOf("."));
                colorName += ".color";
                Color[] colors = loadSamplePointsColors(colorName);
                SamplePoints sp = new SamplePoints(points.ToArray(), normals.ToArray(),
                    faceIndex.ToArray(), colors, totalNFaces);
                return sp;
            }
        }// loadSamplePoints

        private double[] loadPatchWeight(string filename, out double minw, out double maxw)
        {
            minw = double.MaxValue;
            maxw = double.MinValue;
            if (!File.Exists(filename))
            {
                return null;
            }
            List<double> weights = new List<double>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                int nline = 0;
                while (sr.Peek() > -1)
                {
                    ++nline;
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs.Length == 0)
                    {
                        MessageBox.Show("Wrong format at line " + nline.ToString());
                        return null;
                    }
                    double w = double.Parse(strs[0]);
                    weights.Add(w);
                    minw = minw < w ? minw : w;
                    maxw = maxw > w ? maxw : w;
                }
            }
            return weights.ToArray();
        }// loadPatchWeight

        private Color[] loadSamplePointsColors(string filename)
        {
            if (!File.Exists(filename))
            {
                //MessageBox.Show("Sample point color file does not exist.");
                return null;
            }
            List<Color> colors = new List<Color>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs.Length < 3)
                    {
                        MessageBox.Show("Wrong color file");
                        return null;
                    }
                    Color c = Color.FromArgb(byte.Parse(strs[0]), byte.Parse(strs[1]), byte.Parse(strs[2]));
                    if (c.R == 255 && c.G == 255 && c.B == 255)
                    {
                        c = Color.LightGray; // GLDrawer.MeshColor;
                    }
                    colors.Add(c);
                }
                return colors.ToArray();
            }
        }// loadSamplePointsColors

        public void loadPointWeight_0(string filename)
        {
            if (_currModel == null)
            {
                MessageBox.Show("Please load a model first.");
                return;
            }
            // load the point cloud with weights indicating the functionality patch
            // since it is not a segmented model, and most of the models are hard to segment
            // there is no need to take it as an original input and perform segmentation.
            // Unify the mesh, and compare the vertex to vertex distances between the point cloud with a segmented version
            List<Vector3d> points = new List<Vector3d>();
            List<double> weights = new List<double>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                string s = sr.ReadLine();
                int nline = 0;
                while (sr.Peek() > -1)
                {
                    ++nline;
                    string[] strs = s.Split(separators);
                    if (strs.Length < 4)
                    {
                        MessageBox.Show("Wrong format at line " + nline.ToString());
                        return;
                    }
                    Vector3d v = new Vector3d();
                    for (int i = 0; i < 3; ++i)
                    {
                        v[i] = double.Parse(strs[i]);
                    }
                    points.Add(v);
                    weights.Add(double.Parse(strs[3]));
                }
                convertFunctionalityDescription(points.ToArray(), weights.ToArray());
            }
        }// loadPointWeight

        public void loadFunctionalityModelsFromIcon(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            char[] separators = { ' ', '\\', '\t' };
            _functionalityModels = new List<FunctionalityModel>();
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    string name = strs[0]; // category name
                    double[] fs = new double[strs.Length - 1];
                    for (int i = 1; i < strs.Length; ++i)
                    {
                        fs[i - 1] = double.Parse(strs[i]);
                    }
                    FunctionalityModel fm = new FunctionalityModel(fs, name);
                    if (fm != null)
                    {
                        _functionalityModels.Add(fm);
                    }
                }
            }
        }// loadFunctionalityModelsFromIcon

        public void loadFunctionalityModelsFromIcon2(string foldername)
        {
            string[] filenames = Directory.GetFiles(foldername);
            _functionalityModels = new List<FunctionalityModel>();
            foreach (string filename in filenames)
            {
                FunctionalityModel fm = loadOneFunctionalityModel(filename);
                if (fm != null)
                {
                    _functionalityModels.Add(fm);
                }
            }
        }// loadFunctionalityModelsFromIcon2

        private FunctionalityModel loadOneFunctionalityModel(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                string nameAndExt = Path.GetFileName(filename);
                string name = nameAndExt.Substring(0, nameAndExt.LastIndexOf('.'));
                char[] separators = { ' ', '\\', '\t' };
                string s = sr.ReadLine();
                string[] strs = s.Split(separators);
                double[] fs = new double[strs.Length];
                for (int i = 0; i < strs.Length; ++i)
                {
                    fs[i] = double.Parse(strs[i]);
                }
                FunctionalityModel fm = new FunctionalityModel(fs, name);
                return fm;
            }
        }// loadOneFunctionalityModel

        //######### END - Data & feature from ICON2 paper #########//

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

            foreach (ModelViewer mv in _ancesterModelViewers)
            {
                mv.setModelViewMatrix(m);
            }
            foreach (ModelViewer mv in _partViewers)
            {
                mv.setModelViewMatrix(m);
            }
            foreach (ModelViewer mv in _currGenModelViewers)
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
                GLDrawer.drawQuadTranslucent2d(this.highlightQuad, GLDrawer.SelectionColor);
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

            if (this.isDrawGround)
            {
                drawGround();
            }

            //Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);

            if (this.enableDepthTest)
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
            }

            // Draw all meshes
            if (_currModel != null && _meshClasses.Count == 0)
            {
                this.drawModel();
                if (_currModel._GRAPH != null && this.isDrawGraph)
                {
                    this.drawGraph(_currModel._GRAPH);
                }
            }

            this.drawCurrentMesh();

            this.drawImportMeshes();

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

        private void drawGround()
        {
            //GLDrawer.drawPlane(_groundPlane, Color.LightGray);
            // draw grids
            if (_groundGrids != null)
            {
                for (int i = 0; i < _groundGrids.Length; i += 2)
                {
                    GLDrawer.drawLines3D(_groundGrids[i], _groundGrids[i + 1], Color.Gray, 2.0f);
                }
            }
        }// isDrawGround

        private void drawGraph(Graph g)
        {
            foreach (Node node in g._NODES)
            {
                GLDrawer.drawSphere(node._pos, 0.05, node._PART._COLOR);
            }
            foreach (Edge e in g._EDGES)
            {
                GLDrawer.drawLines3D(e._start._pos, e._end._pos, Color.Gray, 2.0f);
            }
        }// drawGraph

        private void drawCurrentMesh()
        {
            //if (this._meshClasses.Count > 0)
            //{
            //    this.drawAllMeshes();
            //    return;
            //}
            if (this.currMeshClass == null || _currModel != null)
            {
                return;
            }
            if (this.currMeshClass != null)
            {
                if (this.drawFace)
                {
                    GLDrawer.drawMeshFace(currMeshClass.Mesh, Color.White, false);
                    //GLDrawer.drawMeshFace(currMeshClass.Mesh, GLDrawer.MeshColor, false);
                    //GLDrawer.drawMeshFace(currMeshClass.Mesh);
                }
                if (this.drawEdge)
                {
                    currMeshClass.renderWireFrame();
                }
                if (this.drawVertex)
                {
                    if (currMeshClass.Mesh.VertexColor != null && currMeshClass.Mesh.VertexColor.Length > 0)
                    {
                        currMeshClass.renderVertices_color();
                    }
                    else
                    {
                        currMeshClass.renderVertices();
                    }
                }
                currMeshClass.drawSamplePoints();
                currMeshClass.drawSelectedVertex();
                currMeshClass.drawSelectedEdges();
                currMeshClass.drawSelectedFaces();
            }
        }// drawCurrentMesh

        private void drawAllMeshes()
        {
            if (_currModel != null)
            {
                return;
            }
            foreach (MeshClass mc in this._meshClasses)
            {
                if (this.drawFace)
                {
                    GLDrawer.drawMeshFace(mc.Mesh, GLDrawer.MeshColor, false);
                }
                if (this.drawEdge)
                {
                    mc.renderWireFrame();
                }
                if (this.drawVertex)
                {
                    if (mc.Mesh.VertexColor != null && mc.Mesh.VertexColor.Length > 0)
                    {
                        mc.renderVertices_color();
                    }
                    else
                    {
                        mc.renderVertices();
                    }
                }
                mc.drawSelectedVertex();
                mc.drawSelectedEdges();
                mc.drawSelectedFaces();
            }
        }// drawAllMeshes

        private void drawImportMeshes()
        {
            int i = 0;
            foreach (MeshClass mc in this._meshClasses)
            {
                Color ic = GLDrawer.ColorSet[i];
                Color c = ic;
                if (i > 0)
                {
                    c = Color.FromArgb(50, 0, 0, 255);
                    GLDrawer.drawMeshFace(mc.Mesh, c);
                }
                else
                {
                    GLDrawer.drawMeshFace(mc.Mesh, c, false);
                }
                //GLDrawer.drawMeshFace(mc.Mesh, c, false);
                ++i;
            }
        }

        private void drawModel()
        {
            if (_currModel == null)
            {
                return;
            }
            // draw mesh
            if (_currModel._MESH != null && _currModel._NPARTS == 0)
            {
                GLDrawer.drawMeshFace(_currModel._MESH, GLDrawer.MeshColor, false);
            }
            // draw functional space
            if (this.isDrawFuncSpace && _currModel._SP != null && _currModel._funcSpaces != null)
            {
                int nfs = 1;// _currModel._SP.funcSpaces.Length;
                for (int i = 0; i < nfs; ++i )
                {
                    FuncSpace fs = _currModel._funcSpaces[i];
                    GLDrawer.drawMeshFace(fs._mesh, GLDrawer.FunctionalSpaceColor);
                }
            }
            // draw parts
            if (_currModel._PARTS != null)
            {
                drawParts(_currModel._PARTS);
            }
            //if (this.isDrawSamplePoints && _currModel._SP != null && _currModel._SP._points != null)
            //{
            //    GLDrawer.drawPoints(_currModel._SP._points, _currModel._SP._blendColors);
            //}
        }// drawModel

        private void drawParts(List<Part> parts)
        {
            foreach (Part part in parts)
            {
                if (_selectedParts.Contains(part))
                {
                    continue;
                }
                bool isSelected = _selectedEdge != null && (_selectedEdge._start._PART == part || _selectedEdge._end._PART == part);
                if (this.drawFace)
                {
                    if (isSelected)
                    {
                        GLDrawer.drawMeshFace(part._MESH, GLDrawer.HighlightBboxColor, false);
                    }
                    else if (this.isDrawSamplePoints)
                    {
                        GLDrawer.drawMeshFace(part._MESH, GLDrawer.MeshColor, false);
                    } 
                    else {
                        GLDrawer.drawMeshFace(part._MESH, part._COLOR, false);
                    }
                    //GLDrawer.drawMeshFace(part._MESH, GLDrawer.MeshColor, false);
                }
                if (this.drawEdge)
                {
                    GLDrawer.drawMeshEdge(part._MESH);
                }
                if (this.drawVertex)
                {
                    GLDrawer.drawMeshVertices(part._MESH);
                    //if (part._MESH.VertexColor != null && part._MESH.VertexColor.Length > 0)
                    //{
                    //    GLDrawer.drawMeshVertices_color(part._MESH);
                    //}
                    //else
                    //{
                    //    GLDrawer.drawMeshVertices(part._MESH);
                    //}
                }
                if (this.isDrawBbox)
                {
                    if (isSelected)
                    {
                        GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, GLDrawer.HighlightBboxColor);
                    }
                    else
                    {
                        GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, part._COLOR);
                    }
                    if (part._BOUNDINGBOX.type == Common.PrimType.Cuboid)
                    {
                        GLDrawer.drawBoundingboxEdges(part._BOUNDINGBOX, part._COLOR);
                    }
                }
                
            }
            if (this.isDrawSamplePoints)
            {
                foreach (Part part in parts)
                {
                    if (part._partSP != null && part._partSP._points != null)
                    {
                        GLDrawer.drawPoints(part._partSP._points, part._partSP._blendColors, 12.0f);
                        //for (int i = 0; i < part._partSP._points.Length; ++i)
                        //{
                        //    Vector3d v = part._partSP._points[i];
                        //    GLDrawer.drawSphere(v, 0.005, part._partSP._blendColors[i]);
                        //}
                    }
                }
            }
        }//drawParts

        private void drawHumanPose()
        {
            if (_humanposes.Count == 0)
            {
                return;
            }
            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);
            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);

            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            if (iNumSamples == 0 && _isDrawTranslucentHumanPose)
            {
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
            foreach (HumanPose hp in _humanposes)
            {
                foreach (BodyBone bb in hp._bodyBones)
                {
                    if (_isDrawTranslucentHumanPose)
                    {
                        GLDrawer.drawCylinderTranslucent(bb._SRC._POS, bb._DST._POS, Common._bodyNodeRadius / 2, GLDrawer.BodeyBoneColor);
                        for (int i = 0; i < bb._FACEVERTICES.Length; i += 4)
                        {
                            GLDrawer.drawQuadTranslucent3d(bb._FACEVERTICES[i], bb._FACEVERTICES[i + 1],
                                bb._FACEVERTICES[i + 2], bb._FACEVERTICES[i + 3], GLDrawer.TranslucentBodyColor);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bb._FACEVERTICES.Length; i += 4)
                        {
                            GLDrawer.drawQuadSolid3d(bb._FACEVERTICES[i], bb._FACEVERTICES[i + 1],
                                bb._FACEVERTICES[i + 2], bb._FACEVERTICES[i + 3], GLDrawer.BodyColor);
                        }
                    }
                }
                foreach (BodyNode bn in hp._bodyNodes)
                {
                    if (bn != _selectedNode)
                    {
                        GLDrawer.drawSphere(bn._POS, bn._RADIUS, GLDrawer.BodyNodeColor);
                    }
                }
            }
            if (iNumSamples == 0 && _isDrawTranslucentHumanPose)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                Gl.glDepthMask(Gl.GL_TRUE);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
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

            if (_showContactPoint)
            {
                if (_currModel != null && _currModel._GRAPH != null)
                {
                    foreach (Edge e in _currModel._GRAPH._EDGES)
                    {
                        foreach (Contact c in e._contacts)
                        {
                            if (e == _selectedEdge && c == _selectedContact)
                            {
                                GLDrawer.drawSphere(c._pos3d, Common._hightlightContactPointsize, GLDrawer.HightLightContactColor);
                            }
                            else
                            {
                                GLDrawer.drawSphere(c._pos3d, Common._contactPointsize, GLDrawer.ContactColor);
                            }
                        }
                    }
                }
            }

            if (_selectedEdge != null)
            {
                // hightlight the nodes
                GLDrawer.drawBoundingboxPlanes(_selectedEdge._start._PART._BOUNDINGBOX, GLDrawer.HighlightBboxColor);
                GLDrawer.drawBoundingboxPlanes(_selectedEdge._end._PART._BOUNDINGBOX, GLDrawer.HighlightBboxColor);
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

        private void drawAxes(Contact[] axes, float wid)
        {
            // draw axes with arrows
            for (int i = 0; i < 6; i += 2)
            {
                GLDrawer.drawLines3D(axes[i]._pos3d, axes[i + 1]._pos3d, _hightlightAxis == 0 ? Color.Yellow : Color.Red, wid);
            }

            for (int i = 6; i < 12; i += 2)
            {
                GLDrawer.drawLines3D(axes[i]._pos3d, axes[i + 1]._pos3d, _hightlightAxis == 1 ? Color.Yellow : Color.Green, wid);
            }

            for (int i = 12; i < 18; i += 2)
            {
                GLDrawer.drawLines3D(axes[i]._pos3d, axes[i + 1]._pos3d, _hightlightAxis == 2 ? Color.Yellow : Color.Blue, wid);
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
