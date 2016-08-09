using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.IO;
using Tao.OpenGl;
using Tao.Platform.Windows;
//using OpenTK.Graphics.OpenGL;
//using OpenTK.Platform.Windows;
using System.Drawing;

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
            this.initializeColors();
            this.camera = new Camera();

            axes = new Vector3d[18] { new Vector3d(-1.2, 0, 0), new Vector3d(1.2, 0, 0),
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
        }

        private void initializeColors()
        {
            ColorSet = new Color[20];

            ColorSet[0] = Color.FromArgb(203, 213, 232);
            ColorSet[1] = Color.FromArgb(252, 141, 98);
            ColorSet[2] = Color.FromArgb(102, 194, 165);
            ColorSet[3] = Color.FromArgb(231, 138, 195);
            ColorSet[4] = Color.FromArgb(166, 216, 84);
            ColorSet[5] = Color.FromArgb(251, 180, 174);
            ColorSet[6] = Color.FromArgb(204, 235, 197);
            ColorSet[7] = Color.FromArgb(222, 203, 228);
            ColorSet[8] = Color.FromArgb(31, 120, 180);
            ColorSet[9] = Color.FromArgb(251, 154, 153);
            ColorSet[10] = Color.FromArgb(227, 26, 28);
            ColorSet[11] = Color.FromArgb(252, 141, 98);
            ColorSet[12] = Color.FromArgb(166, 216, 84);
            ColorSet[13] = Color.FromArgb(231, 138, 195);
            ColorSet[14] = Color.FromArgb(141, 211, 199);
            ColorSet[15] = Color.FromArgb(255, 255, 179);
            ColorSet[16] = Color.FromArgb(251, 128, 114);
            ColorSet[17] = Color.FromArgb(179, 222, 105);
            ColorSet[18] = Color.FromArgb(188, 128, 189);
            ColorSet[19] = Color.FromArgb(217, 217, 217);

            ModelColor = Color.FromArgb(254,224,139);//(166, 189, 219);
            GuideLineColor = Color.FromArgb(116, 169, 207); //Color.FromArgb(4, 90, 141);// Color.FromArgb(0, 15, 85); // pen ink blue
        }

        // modes
        public enum UIMode 
        {
            // !Do not change the order of the modes --- used in the current program to retrieve the index (Integer)
            Viewing, VertexSelection, EdgeSelection, FaceSelection, BoxSelection, NONE
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
        public bool enableDepthTest = false;
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

        private bool inGuideMode = false;
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
        private Vector3d[] axes;
        private Quad2d highlightQuad;
        private int[] meshStats;
        private int[] segStats;
        private Camera camera;
        private Shader shader;
        public static uint pencilTextureId, crayonTextureId, inkTextureId, waterColorTextureId, charcoalTextureId,
            brushTextureId;
        
        private bool drawShadedOrTexturedStroke = true;
        public string foldername;
        public static Random rand = new Random();
        private Vector3d objectCenter = new Vector3d();
        private enum Depthtype
        {
            opacity, hidden, OpenGLDepthTest, none, rayTracing // test 
        }
        private Depthtype depthType = Depthtype.opacity;
        private int vanishinglineDrawType = 0;

        public bool showVanishingRay1 = true;
        public bool showVanishingRay2 = true;
        public bool showVanishingPoints = true;
        public bool showBoxVanishingLine = true;
        public bool showGuideLineVanishingLine = true;
        private List<int> boxShowSequence = new List<int>();

        public bool zoonIn = false;

        //########## sketch vars ##########//
        private List<Vector2d> currSketchPoints = new List<Vector2d>();
        private double strokeLength = 0;
        //private List<DrawStroke2d> drawStrokes = new List<DrawStroke2d>();

        //########## static vars ##########//
        public static Color[] ColorSet;
        static public Color ModelColor;
        public static Color GuideLineColor;

        /******************** Vars ********************/
        Model _currModel;
        List<Model> _models;
        List<Part> _selectedParts = new List<Part>();
        List<ModelViewer> _modelViewers = new List<ModelViewer>();
        public static Color MeshColor = Color.FromArgb(173, 210, 222);

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
            this.meshClasses.Clear();
        }

        public void loadMesh(string filename)
        {
            this.clearContext();

            Mesh m = new Mesh(filename, true);
            MeshClass mc = new MeshClass(m);
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;
            this._currModel = new Model(m);

            meshStats = new int[3];
            meshStats[0] = m.VertexCount;
            meshStats[1] = m.Edges.Length;
            meshStats[2] = m.FaceCount;
        }

        public void importMesh(string filename)
        {
            Mesh m = new Mesh(filename, true);
            MeshClass mc = new MeshClass(m);
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;

            meshStats = new int[3];
            meshStats[0] = m.VertexCount;
            meshStats[1] = m.Edges.Length;
            meshStats[2] = m.FaceCount;
        }

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
            }
        }// loadAPartBasedModel

        public void loadPartBasedModels(string segfolder, int x, int y)
        {
            if (!Directory.Exists(segfolder))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            this.clearContext();
            _models = new List<Model>();
            _modelViewers = new List<ModelViewer>();
            string[] files = Directory.GetFiles(segfolder);
            int w = 100;
            int h = 100;
            int i = 0;
            foreach (string file in files)
            {
                loadAPartBasedModel(file);
                _models.Add(_currModel);
                ModelViewer modelViewer = new ModelViewer(_currModel);
                modelViewer.SetBounds(x + w * (i++), y, w, h);
                _modelViewers.Add(modelViewer);
            }
        }// loadPartBasedModels
     
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
        }

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
                case 0:
                default:
                    this.currUIMode = UIMode.Viewing;
                    break;
            }
        }

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

        public void displayAxes()
        {
            this.isDrawAxes = !this.isDrawAxes;
        }

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
                        }
                        break;
                    }
                case UIMode.Viewing:
                    //default:
                    {
                        if (!this.lockView)
                        {
                            this.viewMouseMove(e.X, e.Y);
                            //this.Refresh();
                        }
                        break;
                    }
            }

            this.Refresh();
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
                case Keys.D:
                    {
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
            //base.OnPaint(e);
            this.MakeCurrent();
            this.clearScene();
            this.Draw3D();
            //this.Draw2D();

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

        //######### end-Part-based #########//

        private void setViewMatrix()
        {

            int w = this.Width;
            int h = this.Height;
            if (h == 0)
            {
                h = 1;
            }
            //this.MakeCurrent();

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

            if (_modelViewers != null)
            {
                foreach (ModelViewer mv in _modelViewers)
                {
                    mv.setModelViewMatrix(m);
                }
            }

            Gl.glMatrixMode(Gl.GL_MODELVIEW);

            Gl.glPushMatrix();
            Gl.glMultMatrixd(m.Transpose().ToArray());
        }

        private int startWid = 0, startHeig = 0;

        private void Draw2D()
        {
            int w = this.Width, h = this.Height;

            //Gl.glPushMatrix();

            Gl.glViewport(0, 0, w, h);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();

            //double aspect = (double)w / h;
            //Glu.gluPerspective(90, aspect, 0.1, 1000);
            //Glu.gluLookAt(0, 0, 1.5, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Glu.gluOrtho2D(0, w, 0, h);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            Gl.glPushMatrix();

            //Gl.glScaled((double)this.Width / this.startWid, (double)this.Height / this.startHeig, 1.0);

            this.DrawHighlight2D();  
         
            /*****TEST*****/
            //this.drawTest2D();

            //this.DrawLight();            

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            //Gl.glPopMatrix();
        }

        private void Draw3D()
        {

            this.setViewMatrix();

			SetDefaultLight();

            /***** Draw *****/
            //clearScene();

            if (this.isDrawAxes)
            {
                this.drawAxes();
            }

            // for visibility rendering, the order is computed from
            // setHiddenLines()

            Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);

            if (this.enableDepthTest)
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
            }
            
            // Draw all meshes
            if (_currModel != null)
            {
                this.drawParts();
            }
            else
            {
                this.drawAllMeshes();
            }       

            this.DrawHighlight3D();

            if (this.enableDepthTest)
            {
                Gl.glDisable(Gl.GL_DEPTH_TEST);
            }

            Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();

            if (this.isDrawQuad && this.highlightQuad != null)
            {
                this.drawQuad2d(this.highlightQuad, ColorSet[3]);
            }

            //this.SwapBuffers();
        }// Draw3D   

        private void drawAllMeshes()
        {
            if (this.meshClasses == null)
            {
                return;
            }
            foreach (MeshClass meshclass in this.meshClasses)
            {
                if (this.drawFace)
                {
                    this.drawMeshFace(meshclass.Mesh, MeshColor, false);
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
                if (this.drawFace)
                {
                    this.drawMeshFace(part._MESH, part._COLOR, false);
                }
                if (this.drawEdge)
                {
                    this.drawMeshEdge(part._MESH);
                }
                if (this.drawBbox)
                {
                    this.drawBoundingbox(part._BOUNDINGBOX, part._COLOR);
                }
            }
        }//drawParts

        private void drawTriangle(Triangle3D t)
        {
            Gl.glVertex3dv(t.u.ToArray());
            Gl.glVertex3dv(t.v.ToArray());
            Gl.glVertex3dv(t.v.ToArray());
            Gl.glVertex3dv(t.w.ToArray());
            Gl.glVertex3dv(t.w.ToArray());
            Gl.glVertex3dv(t.u.ToArray());
        }

        private void drawCircle3D(Circle3D e, Color c)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glLineWidth(1.0f);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points3d.Length; ++i)
            {
                Gl.glVertex3dv(e.points3d[i].ToArray());
                Gl.glVertex3dv(e.points3d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawcircle3D

        private void drawCircle2D(Circle3D e, Color c)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glLineWidth(1.0f);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points2d.Length; ++i)
            {
                Gl.glVertex3dv(e.points2d[i].ToArray());
                Gl.glVertex3dv(e.points2d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawcircle2D

        private void drawEllipseCurve3D(Ellipse3D e)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points3d.Length; ++i)
            {
                Gl.glVertex3dv(e.points3d[i].ToArray());
                Gl.glVertex3dv(e.points3d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawEllipseCurve3D

        private void drawEllipse3D(Ellipse3D e)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points3d.Length; ++i)
            {
                Gl.glVertex3dv(e.points3d[i].ToArray());
                Gl.glVertex3dv(e.points3d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawEllipse3D

        private void DrawHighlight2D()
        {
        }

        private void DrawHighlight3D()
        {
            if (this._selectedParts != null)
            {
                foreach (Part part in _selectedParts)
                {
                    this.drawBoundingboxEdges(part._BOUNDINGBOX, ColorSet[4]);
                }
            }

            if (this.enableDepthTest)
            {
                Gl.glDisable(Gl.GL_DEPTH_TEST);
            }
        }// DrawHighlight3D

        private void drawPoints2d(Vector2d[] points3d, Color c, float pointSize)
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPointSize(pointSize);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (Vector2d v in points3d)
            {
                Gl.glVertex2dv(v.ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
        }

        private void drawPoints3d(Vector3d[] points3d, Color c, float pointSize)
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPointSize(pointSize);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (Vector3d v in points3d)
            {
                Gl.glVertex3dv(v.ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_POINT_SMOOTH);
        }

        private void drawPlane2D(Plane3D plane)
        {
            if (plane.points2d == null) return;
            Gl.glColor3ub(0, 0, 255);
            Gl.glPointSize(4.0f);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (Vector2d p in plane.points2d)
            {
                Gl.glVertex2dv(p.ToArray());
            }
            Gl.glEnd();
        }

        private void drawLines2D(List<Vector2d> points3d, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glColor3ub(c.R, c.G, c.B);
            for (int i = 0; i < points3d.Count - 1;++i )
            {
                Gl.glVertex2dv(points3d[i].ToArray());
                Gl.glVertex2dv(points3d[i+1].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        private void drawLines2D(Vector2d v1, Vector2d v2, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glVertex2dv(v1.ToArray());
            Gl.glVertex2dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        private void drawDashedLines2D(Vector2d v1, Vector2d v2, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glLineStipple(1, 0x00FF);
            Gl.glEnable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_LINE_STIPPLE);

            Gl.glLineWidth(1.0f);
        }

        private void drawLines3D(List<Vector3d> points3d, Color c, float linewidth)
        {

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            foreach (Vector3d p in points3d)
            {
                Gl.glVertex3dv(p.ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        private void drawLines3D(Vector3d v1, Vector3d v2, Color c, float linewidth)
        {

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        private void drawDashedLines3D(Vector3d v1, Vector3d v2, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glLineStipple(1, 0x00FF);
            Gl.glEnable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_LINE_STIPPLE);

            Gl.glLineWidth(1.0f);
        }

        private void drawAxes()
        {
            // draw axes with arrows
            for (int i = 0; i < 6; i += 2)
            {
                this.drawLines3D(axes[i], axes[i + 1], Color.Red, 2.0f);
            }

            for (int i = 6; i < 12; i += 2)
            {
                this.drawLines3D(axes[i], axes[i + 1], Color.Green, 2.0f);
            }

            for (int i = 12; i < 18; i += 2)
            {
                this.drawLines3D(axes[i], axes[i + 1], Color.Blue, 2.0f);
            }
            Gl.glEnd();
        }

        private void clearScene()
        {
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }

        private void drawQuad2d(Quad2d q, Color c)
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            //Glu.gluOrtho2D(0, this.Width, 0, this.Height);
            Glu.gluOrtho2D(0, this.Width, this.Height, 0);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex2dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glLineWidth(2.0f);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex2dv(q.points3d[i].ToArray());
                Gl.glVertex2dv(q.points3d[(i + 1) % 4].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_LINE_SMOOTH);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }

        private void drawQuad3d(Plane3D q, Color c)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            // face
            Gl.glColor4ub(c.R, c.G, c.B, c.A);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex3dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
        }

        private void drawQuadTransparent3d(Plane3D q, Color c)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            // face
            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex3dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
        }

        private void drawQuadEdge3d(Plane3D q, Color c)
        {
            for (int i = 0; i < 4; ++i)
            {
                this.drawLines3D(q.points3d[i], q.points3d[(i + 1) % q.points3d.Length], c, 1.5f);
            }
        }

        public static float[] matAmbient = { 0.1f, 0.1f, 0.1f, 1.0f };
        public static float[] matDiffuse = { 0.4f, 0.4f, 0.4f, 1.0f };
        public static float[] matSpecular = { 0.5f, 0.5f, 0.5f, 1.0f };
        public static float[] shine = { 7.0f };

        private static void SetDefaultLight()
        {

            float[] col1 = new float[4]  { 0.7f, 0.7f, 0.7f, 1.0f };
            float[] col2 = new float[4] { 0.8f, 0.7f, 0.7f, 1.0f };
            float[] col3 = new float[4] { 0, 0, 0, 1 };//{ 1.0f, 1.0f, 1.0f, 1.0f };

            float[] pos_1 = {10, 0,0};// { 0, -5, 10.0f };
            float[] pos_2 = {0, 10, 0};// { 0, 5, -10.0f };
            float[] pos_3 = {0,0,10};//{ -5, 5, -10.0f };
            float[] pos_4 = { -10, 0, 0 };// { 0, -5, 10.0f };
            float[] pos_5 = { 0, -10, 0 };// { 0, 5, -10.0f };
            float[] pos_6 = { 0, 0, -10 };//{ -5, 5, -10.0f };

            float[] intensity = {0.5f, 0.5f, 0.5f};
            //Gl.glLightModeli(Gl.GL_LIGHT_MODEL_TWO_SIDE, Gl.GL_TRUE);
            Gl.glEnable(Gl.GL_LIGHT0);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, pos_1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_INTENSITY, intensity);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, col1);

            Gl.glEnable(Gl.GL_LIGHT1);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, pos_2);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_INTENSITY, intensity);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_SPECULAR, col1);

            Gl.glEnable(Gl.GL_LIGHT2);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_POSITION, pos_3);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_SPECULAR, col1);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_INTENSITY, intensity);

            //Gl.glEnable(Gl.GL_LIGHT3);
            //Gl.glLightfv(Gl.GL_LIGHT3, Gl.GL_POSITION, pos_4);
            //Gl.glLightfv(Gl.GL_LIGHT3, Gl.GL_DIFFUSE, col1);
            //Gl.glLightfv(Gl.GL_LIGHT3, Gl.GL_SPECULAR, col1);
            //Gl.glLightfv(Gl.GL_LIGHT3, Gl.GL_INTENSITY, intensity);


            Gl.glEnable(Gl.GL_LIGHT4);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_POSITION, pos_5);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_SPECULAR, col1);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_INTENSITY, intensity);

            Gl.glEnable(Gl.GL_LIGHT5);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_POSITION, pos_6);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_SPECULAR, col1);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_INTENSITY, intensity);

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
        public static void DrawCircle2(Vector2d p, Color c, float radius)
        {
            Gl.glEnable(Gl.GL_BLEND);
            //	Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glColor4ub(c.R, c.G, c.B, 50);

            int nsample = 50;
            double delta = Math.PI * 2 / nsample;

            Gl.glLineWidth(1.0f);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < nsample; ++i)
            {
                double theta1 = i * delta;
                double x1 = p.x + radius * Math.Cos(theta1), y1 = p.y + radius * Math.Sin(theta1);
                double theta2 = (i + 1) * delta;
                double x2 = p.x + radius * Math.Cos(theta2), y2 = p.y + radius * Math.Sin(theta2);
                Gl.glVertex2d(x1, y1);
                Gl.glVertex2d(x2, y2);
            }
            Gl.glEnd();
            Gl.glLineWidth(1.0f);

            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < nsample; ++i)
            {
                double theta1 = i * delta;
                double x1 = p.x + radius * Math.Cos(theta1), y1 = p.y + radius * Math.Sin(theta1);
                Gl.glVertex2d(x1, y1);
            }
            Gl.glEnd();

            //	Gl.glDisable(Gl.GL_BLEND);
        }

        private void DrawLight()
        {
            for (int i = 0; i < lightPositions.Count; ++i)
            {
                Vector3d pos3 = new Vector3d(lightPositions[i][0],
                    lightPositions[i][1],
                    lightPositions[i][2]);
                Vector3d pos2 = this.camera.Project(pos3.x, pos3.y, pos3.z);
                DrawCircle2(new Vector2d(pos2.x, pos2.y), Color.Yellow, 0.2f);
            }
        }

        // draw mesh
        public void drawMeshFace(Mesh m, Color c, bool useMeshColor)
        {
            if (m == null) return;


            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glShadeModel(Gl.GL_SMOOTH);

			float[] mat_a = new float[4] { c.R / 255.0f, c.G / 255.0f, c.B / 255.0f, 1.0f };

			float[] ka = { 0.1f, 0.05f, 0.0f, 1.0f };
			float[] kd = { .9f, .6f, .2f, 1.0f };
			float[] ks = { 0, 0, 0, 0 };//{ .2f, .2f, .2f, 1.0f };
			float[] shine = { 1.0f };
			Gl.glColorMaterial(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE);
			Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, mat_a);
			Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, mat_a);
			Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, ks);
			Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shine);            

            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

            Gl.glEnable(Gl.GL_DEPTH_TEST);
			Gl.glEnable(Gl.GL_LIGHTING);
			Gl.glEnable(Gl.GL_NORMALIZE);

            
            if (useMeshColor)
            {
                Gl.glColor3ub(GLViewer.ModelColor.R, GLViewer.ModelColor.G, GLViewer.ModelColor.B);
                for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
                {
                    int vidx1 = m.FaceVertexIndex[j];
                    int vidx2 = m.FaceVertexIndex[j + 1];
                    int vidx3 = m.FaceVertexIndex[j + 2];
                    Vector3d v1 = new Vector3d(
                        m.VertexPos[vidx1 * 3], m.VertexPos[vidx1 * 3 + 1], m.VertexPos[vidx1 * 3 + 2]);
                    Vector3d v2 = new Vector3d(
                        m.VertexPos[vidx2 * 3], m.VertexPos[vidx2 * 3 + 1], m.VertexPos[vidx2 * 3 + 2]);
                    Vector3d v3 = new Vector3d(
                        m.VertexPos[vidx3 * 3], m.VertexPos[vidx3 * 3 + 1], m.VertexPos[vidx3 * 3 + 2]);
                    Color fc = Color.FromArgb(m.FaceColor[i * 4 + 3], m.FaceColor[i * 4], m.FaceColor[i * 4 + 1], m.FaceColor[i * 4 + 2]);
                    Gl.glColor4ub(fc.R, fc.G, fc.B, fc.A);
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    Vector3d centroid = (v1 + v2 + v3) / 3;
                    Vector3d normal = new Vector3d(m.FaceNormal[i * 3], m.FaceNormal[i * 3 + 1], m.FaceNormal[i * 3 + 2]);
                    //if ((centroid - newEye).Dot(normal) > 0)
                    //{
                    //    normal *= -1.0;
                    //}
                    //normal *= -1;
                    //Gl.glNormal3dv(normal.ToArray());
                    Vector3d n1 = new Vector3d(m.VertexNormal[vidx1 * 3], m.VertexNormal[vidx1 * 3 + 1], m.VertexNormal[vidx1 * 3 + 2]);
                    Gl.glNormal3dv(n1.ToArray());
                    Gl.glVertex3d(v1.x, v1.y, v1.z);
                    Vector3d n2 = new Vector3d(m.VertexNormal[vidx2 * 3], m.VertexNormal[vidx2 * 3 + 1], m.VertexNormal[vidx2 * 3 + 2]);
                    Gl.glNormal3dv(n2.ToArray());
                    Gl.glVertex3d(v2.x, v2.y, v2.z);
                    Vector3d n3 = new Vector3d(m.VertexNormal[vidx3 * 3], m.VertexNormal[vidx3 * 3 + 1], m.VertexNormal[vidx3 * 3 + 2]);
                    Gl.glNormal3dv(n3.ToArray());
                    Gl.glVertex3d(v3.x, v3.y, v3.z);
                    Gl.glEnd();
                }
            }
            else
            {
                Gl.glColor3ub(c.R, c.G, c.B);
                Gl.glBegin(Gl.GL_TRIANGLES);
                for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
                {
                    int vidx1 = m.FaceVertexIndex[j];
                    int vidx2 = m.FaceVertexIndex[j + 1];
                    int vidx3 = m.FaceVertexIndex[j + 2];
                    Vector3d v1 = new Vector3d(
                        m.VertexPos[vidx1 * 3], m.VertexPos[vidx1 * 3 + 1], m.VertexPos[vidx1 * 3 + 2]);
                    Vector3d v2 = new Vector3d(
                        m.VertexPos[vidx2 * 3], m.VertexPos[vidx2 * 3 + 1], m.VertexPos[vidx2 * 3 + 2]);
                    Vector3d v3 = new Vector3d(
                        m.VertexPos[vidx3 * 3], m.VertexPos[vidx3 * 3 + 1], m.VertexPos[vidx3 * 3 + 2]);
                    Vector3d n1 = new Vector3d(m.VertexNormal[vidx1 * 3], m.VertexNormal[vidx1 * 3 + 1], m.VertexNormal[vidx1 * 3 + 2]);
					Gl.glNormal3dv(n1.ToArray());
					Gl.glVertex3d(v1.x, v1.y, v1.z);
					Vector3d n2 = new Vector3d(m.VertexNormal[vidx2 * 3], m.VertexNormal[vidx2 * 3 + 1], m.VertexNormal[vidx2 * 3 + 2]);
					Gl.glNormal3dv(n2.ToArray());
					Gl.glVertex3d(v2.x, v2.y, v2.z);
					Vector3d n3 = new Vector3d(m.VertexNormal[vidx3 * 3], m.VertexNormal[vidx3 * 3 + 1], m.VertexNormal[vidx3 * 3 + 2]);
					Gl.glNormal3dv(n3.ToArray());
					Gl.glVertex3d(v3.x, v3.y, v3.z);
                }
                Gl.glEnd();
            }

            //Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDepthMask(Gl.GL_TRUE);

            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_LIGHT0);
            //Gl.glDisable(Gl.GL_LIGHT1);
            //Gl.glDisable(Gl.GL_LIGHT2);
            //Gl.glDisable(Gl.GL_LIGHT3);
            //Gl.glDisable(Gl.GL_LIGHT4);
            //Gl.glDisable(Gl.GL_LIGHT5);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
        }

        private void drawMeshEdge(Mesh m)
        {
            if (m == null) return;
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glColor3ub(GLViewer.ColorSet[1].R, GLViewer.ColorSet[1].G, GLViewer.ColorSet[1].B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < m.Edges.Length; ++i)
            {
                int fromIdx = m.Edges[i].FromIndex;
                int toIdx = m.Edges[i].ToIndex;
                Gl.glVertex3d(m.VertexPos[fromIdx * 3],
                    m.VertexPos[fromIdx * 3 + 1],
                    m.VertexPos[fromIdx * 3 + 2]);
                Gl.glVertex3d(m.VertexPos[toIdx * 3],
                    m.VertexPos[toIdx * 3 + 1],
                    m.VertexPos[toIdx * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            //Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void drawBoundingboxWithEdges(Prim box, Color planeColor, Color lineColor)
        {
            if (box == null) return;
            if (box._PLANES != null)
            {
                for (int i = 0; i < box._PLANES.Length; ++i)
                {
                    this.drawQuad3d(box._PLANES[i], planeColor);
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        this.drawLines3D(box._PLANES[i].points3d[j], box._PLANES[i].points3d[(j + 1) % 4], lineColor, 2.0f);
                    }
                }
            }
        }// drawBoundingboxWithEdges

        private void drawBoundingbox(Prim box, Color c)
        {
            if (box == null || box._PLANES == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                this.drawQuad3d(box._PLANES[i], c);
            }
        }// drawBoundingbox

        public void drawBoundingboxEdges(Prim box, Color c)
        {
            if (box == null) return;
            if (box._PLANES != null)
            {
                for (int i = 0; i < box._PLANES.Length; ++i)
                {
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        this.drawLines3D(box._PLANES[i].points3d[j], box._PLANES[i].points3d[(j + 1) % 4], c, 2.0f);
                    }
                }
            }
        }// drawBoundingboxWithEdges

        public void drawBoundingboxWithoutBlend(Prim box, Color c)
        {
            if (box == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                // face
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glColor4ub(c.R, c.G, c.B, c.A);
                Gl.glBegin(Gl.GL_POLYGON);
                for (int j = 0; j < 4; ++j)
                {
                    Gl.glVertex3dv(box._PLANES[i].points3d[j].ToArray());
                }
                Gl.glEnd();
            }
        }// drawBoundingbox
    }// GLViewer
}// namespace
