﻿using System;
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

        private bool _showContactPoint = false;

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
        private List<MeshClass> meshClasses;
        private MeshClass currMeshClass;
        private Quad2d highlightQuad;
        private Camera camera;
        private Shader shader;
        public static uint pencilTextureId, crayonTextureId, inkTextureId, waterColorTextureId, charcoalTextureId,
            brushTextureId;

        private List<Model> _crossOverBasket = new List<Model>();
        private int _selectedModelIndex = -1;
        
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
        List<Node> _selectedNodes = new List<Node>();
        List<ModelViewer> _modelViewers = new List<ModelViewer>();
        List<ModelViewer> _partViewers = new List<ModelViewer>();
        List<ModelViewer> _resViewers = new List<ModelViewer>();
        HumanPose _currHumanPose;
        List<HumanPose> _humanposes = new List<HumanPose>();
        BodyNode _selectedNode;
        public bool _unitifyMesh = true;
        bool _showEditAxes = false;
        public bool showGround = false;
        private Vector3d[] _axes;
        private Contact[] _editAxes;
        private Polygon3D _groundPlane;
        int _hightlightAxis = -1;
        ArcBall _editArcBall;
        ArcBall _bodyArcBall;
        bool _isRightClick = false;
        bool _isDrawTranslucentHumanPose = true;

        private Vector3d[] _groundGrids;
        Edge _selectedEdge = null;
        Contact _selectedContact = null;
        private ReplaceablePair[,] _replaceablePairs;

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
            _currModel = null;
            _selectedParts = new List<Part>();
            _models.Clear();
            _modelViewers = new List<ModelViewer>();
            _partViewers = new List<ModelViewer>();
            _currHumanPose = null;
            this.meshClasses.Clear();
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
        }// saveModelObj

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

        public void switchXYZ(int mode)
        {
            foreach (Part p in _selectedParts)
            {
                Mesh pm = p._MESH;
                for (int i = 0, j = 0; i < pm.VertexCount; i++, j += 3)
                {
                    double x = pm.VertexPos[j];
                    double y = pm.VertexPos[j + 1];
                    double z = pm.VertexPos[j + 2];
                    if (mode == 1)
                    {
                        pm.setVertextPos(i, new Vector3d(-y, x, z));
                    }
                    else if (mode == 2)
                    {
                        pm.setVertextPos(i, new Vector3d(-z, y, x));
                    }
                    else
                    {
                        pm.setVertextPos(i, new Vector3d(x, -z, y));
                    }
                }
                pm.afterUpdatePos();
                p.fitProxy();
                p.updateOriginPos();
            }

            //foreach (Model md in _models)
            //{
            //    Mesh m = md._MESH;
            //    if (m != null)
            //    {
            //        for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
            //        {
            //            double x = m.VertexPos[j];
            //            double y = m.VertexPos[j + 1];
            //            double z = m.VertexPos[j + 2];
            //            if (mode == 1)
            //            {
            //                m.setVertextPos(i, new Vector3d(-y, x, z));
            //            }
            //            else if (mode == 2)
            //            {
            //                m.setVertextPos(i, new Vector3d(-z, y, x));
            //            }
            //            else
            //            {
            //                m.setVertextPos(i, new Vector3d(x, -z, y));
            //            }
            //        }
            //        m.afterUpdatePos();
            //        md.setMesh(m);
            //    }
            //    foreach (Part p in md._PARTS)
            //    {
            //        Mesh pm = p._MESH;
            //        for (int i = 0, j = 0; i < pm.VertexCount; i++, j += 3)
            //        {
            //            double x = pm.VertexPos[j];
            //            double y = pm.VertexPos[j + 1];
            //            double z = pm.VertexPos[j + 2];
            //            if (mode == 1)
            //            {
            //                pm.setVertextPos(i, new Vector3d(-y, x, z));
            //            }
            //            else if (mode == 2)
            //            {
            //                pm.setVertextPos(i, new Vector3d(-z, y, x));
            //            }
            //            else
            //            {
            //                pm.setVertextPos(i, new Vector3d(x, -z, y));
            //            }
            //        }
            //        pm.afterUpdatePos();
            //        p.calculateBbox();
            //    }// each part
            //}// each model
            this.Refresh();
        }// switchXYZ

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
            // m edges (for graph)
            // id1, id2
            // ..., ...
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
                        sw.Write(vector3dToString(v, true));
                    }
                    sw.WriteLine();
                    // principal axes
                    sw.Write(vector3dToString(ipart._BOUNDINGBOX.coordSys.x, true));
                    sw.Write(vector3dToString(ipart._BOUNDINGBOX.coordSys.y, true));
                    sw.WriteLine(vector3dToString(ipart._BOUNDINGBOX.coordSys.z, false));
                    // save mesh
                    string meshName = "part_" + i.ToString() + ".obj";
                    this.saveObj(ipart._MESH, meshDir + meshName, ipart._COLOR);
                    sw.WriteLine(meshFolder + "\\" + meshName);
                }
                if (_currModel._GRAPH != null)
                {
                    string graphName = filename.Substring(0, filename.LastIndexOf('.')) + ".graph";
                    saveAGrapph(graphName);
                }
            }
        }// saveAPartBasedModel

        private string vector3dToString(Vector3d v, bool tailSpace)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0:0.###}", v.x) + " " +
                            string.Format("{0:0.###}", v.y) + " " +
                            string.Format("{0:0.###}", v.z));
            if (tailSpace)
            {
                sb.Append(" ");
            }
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
                    Part part = new Part(mesh);
                    //Part part = hasPrism ? new Part(mesh, prim) : new Part(mesh);
                    parts.Add(part);
                }
                Model model = new Model(parts);
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
            string graphName = filename.Substring(0, filename.LastIndexOf('.')) + ".graph";
            if (!File.Exists(graphName))
            {
                _currModel.initialize();
            }
            else
            {
                loadAGrapph(_currModel, graphName);
            }
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
            string[] files = Directory.GetFiles(segfolder);
            int idx = 0;
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
                    loadAGrapph(m, graphName);
                    hasInValidContact(m._GRAPH);
                    ModelViewer modelViewer = new ModelViewer(m, idx++, this);
                    _modelViewers.Add(modelViewer);
                }
            }
            if (_modelViewers.Count > 0)
            {
                this.setCurrentModel(_modelViewers[_modelViewers.Count - 1]._MODEL, _modelViewers.Count - 1);
            }
            this.readModelModelViewMatrix(foldername + "\\view.mat");

            // try to load replaceable pairs
            tryLoadReplaceablePairs();

            return _modelViewers;
        }// loadPartBasedModels

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
            if (_modelViewers.Count == 0)
            {
                return;
            }
            int n = _modelViewers.Count;
            _replaceablePairs = new ReplaceablePair[n, n];
            for (int i = 0; i < n - 1; ++i)
            {
                Model model_i = _modelViewers[i]._MODEL;
                Graph graph_i = _modelViewers[i]._GRAPH;
                for (int j = i + 1; j < n; ++j)
                {
                    Model model_j = _modelViewers[j]._MODEL;
                    Graph graph_j = _modelViewers[j]._GRAPH;
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

        public void loadAGrapph(Model m, string filename)
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
                        int isFunc = int.Parse(strs[6]);
                        node._isFunction = isFunc == 1 ? true : false;
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
                g.analyzeScale();
                m.setGraph(g);
            }
        }// loadAGrapph

        public void saveAGrapph(string filename)
        {
            if (_currModel._GRAPH == null)
            {
                return;
            }
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
                    // function
                    int isFunc = 0;
                    if (iNode._isFunction) {
                        isFunc = 1;
                    }
                    sw.WriteLine(" " + isFunc.ToString());
                }
                sw.WriteLine(_currModel._GRAPH._NEdges.ToString() + " edges.");
                foreach(Edge e in _currModel._GRAPH._EDGES)
                {
                    sw.Write(e._start._INDEX.ToString() + " " + e._end._INDEX.ToString() + " ");
                    foreach (Contact pnt in e._contacts)
                    {
                        sw.Write(this.vector3dToString(pnt._pos3d, true));
                    }
                    sw.WriteLine();
                }
            }
        }// saveAGrapph

        public void refreshModelViewers()
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
            foreach (ModelViewer mv in _resViewers)
            {
                mv.Refresh();
            }
        }// refreshModelViewers

        public void setCurrentModel(Model m, int idx)
        {
            _currModel = m;
            _selectedParts.Clear();
            _models.Clear();
            _models.Add(m);
            _selectedModelIndex = idx;

            _crossOverBasket.Remove(m);
            m._GRAPH.selectedNodePairs.Clear();

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

        public void switchParts(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2)
        {
            List<Edge> edgesToConnect_1 = g1.collectOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.collectOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_1);
            List<Vector3d> targets = collectPoints(edgesToConnect_2);

            if (sources.Count == targets.Count && sources.Count == 2)
            {
                
            }
        }// switchParts

        List<List<Model>> _mutateGenerations = new List<List<Model>>();
        List<List<Model>> _crossoverGenerations = new List<List<Model>>();

        public List<ModelViewer> autoGenerate()
        {
            _resViewers = new List<ModelViewer>();

            this.isDrawBbox = false;
            this.isDrawGraph = false;
            Program.GetFormMain().setCheckBox_drawBbox(this.isDrawBbox);
            Program.GetFormMain().setCheckBox_drawGraph(this.isDrawGraph);

            this.reloadView();

            List<Model> firstGen = new List<Model>();
            foreach (ModelViewer mv in _modelViewers)
            {
                firstGen.Add(mv._MODEL);
            }
            _mutateGenerations.Clear();
            _crossoverGenerations.Clear();

            _mutateGenerations.Add(firstGen);
            _crossoverGenerations.Add(firstGen);
            string mutateFolder = this.foldername.Clone() as string;
            string crossoverFolder = this.foldername.Clone() as string;
            string imageFolder_m = mutateFolder + "\\screenCapture\\mutate\\";
            string imageFolder_c = crossoverFolder + "\\screenCapture\\crossover\\";
            if (!Directory.Exists(imageFolder_m))
            {
                Directory.CreateDirectory(imageFolder_m);
            }
            if (!Directory.Exists(imageFolder_c))
            {
                Directory.CreateDirectory(imageFolder_c);
            }
            // mutate
            List<Model> secondGen = new List<Model>();
            for (int i = 0; i < firstGen.Count; ++i)
            {
                Model m = firstGen[i];
                this.setCurrentModel(m, i);
                List<Model> res = this.mutate();
                foreach (Model model in res)
                {
                    //if (model._GRAPH.isGeometryViolated())
                    //{
                    //    continue;
                    //}
                    //secondGen.Add(model);
                    // screenshot
                    this.setCurrentModel(model, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(imageFolder_m + model._model_name + ".png");
                    saveAPartBasedModel(model._path + model._model_name + ".pam");
                }
            }
            //_mutateGenerations.Add(secondGen);
            // crossover
            // only for the same contact points, for rebuilding the graph
            int maxIter = 1;
            int iter = 0;
            int index = 0;
            while (iter < maxIter)
            {
                List<Model> cross_results = this.crossOver(_crossoverGenerations[iter]);
                List<Model> gen = new List<Model>();
                foreach (Model model in cross_results)
                {
                    if (model._GRAPH.isGeometryViolated())
                    {
                        //continue;
                        _resViewers.Add(new ModelViewer(model, index++, this));
                        Program.writeToConsole(model._model_name);
                    }
                    //gen.Add(model);
                    // screenshot
                    this.setCurrentModel(model, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(imageFolder_c + model._model_name + ".png");
                    saveAPartBasedModel(model._path + model._model_name + ".pam");
                }
                ++iter;
                _crossoverGenerations.Add(gen);
            }
            // cossover + mutate
            return _resViewers;
        }// autoGenerate

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
                outEdgeNum1.Add(g1.collectOutgoingEdges(nodes).Count);
            }
            foreach (List<Node> nodes in symPairs2)
            {
                outEdgeNum2.Add(g2.collectOutgoingEdges(nodes).Count);
            }
            for (int i = 0; i < symPairs1.Count; ++i)
            {
                bool isGround1 = symPairs1[i][0]._isGroundTouching;
                for (int j = 0; j < symPairs2.Count; ++j)
                {
                    bool isGround2 = symPairs2[j][0]._isGroundTouching;
                    if ( (isGround1 && isGround2) || (!isGround1 && !isGround2 && outEdgeNum1[i] == outEdgeNum2[j]))
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

        public void markSymmetry()
        {
            if (_selectedNodes.Count != 2)
            {
                return;
            }
            _currModel._GRAPH.markSymmtry(_selectedNodes[0], _selectedNodes[1]);
        }// markSymmetry

        public void markFunctionPart()
        {
            foreach (Node node in _selectedNodes)
            {
                node._isFunction = true;
            }
        }// markFunctionPart

        public List<ModelViewer> getMutateViewers()
        {
            List<Model> models = mutate();
            _resViewers = new List<ModelViewer>();
            int i = 0;
            foreach (Model m in models)
            {
                _resViewers.Add(new ModelViewer(m, i++, this));
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
                e.TransformContact(T);
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
            _resViewers = new List<ModelViewer>();
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
                    _resViewers.Add(new ModelViewer(newM1, k++, this));
                    newM2.replaceNodes(nodes2, updatedNodes1);
                    _resViewers.Add(new ModelViewer(newM2, k++, this));
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

                    if (_replaceablePairs != null)
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
                        List<Model> crossed = this.crossOverOp(m1, m2, t);
                        crossedModels.AddRange(crossed);
                    }
                }
            }
            return crossedModels;
        }// crossover

        public List<Model> crossOverOp(Model m1, Model m2, int idx)
        {
            if (m1 == null || m2 == null)
            {
                return null;
            }
            List<Model> crossModels = new List<Model>();

            List<Node> nodes1 = new List<Node>();
            List<Node> nodes2 = new List<Node>();
            //findOneToOneMatchingNodes(g1, g2, out nodes1, out nodes2);

            Model newM1 = m1.Clone() as Model;
            Model newM2 = m2.Clone() as Model;
            newM1._path = m1._path + "crossOver_models\\";
            newM2._path = m2._path + "crossOver_models\\";
            if (!Directory.Exists(newM1._path))
            {
                Directory.CreateDirectory(newM1._path);
            }
            if (!Directory.Exists(newM2._path))
            {
                Directory.CreateDirectory(newM2._path);
            }
            newM1._model_name = m1._model_name + "_cross_" + m2._model_name + "_" + idx.ToString();
            newM2._model_name = m2._model_name + "_cross_" + m1._model_name + "_" + idx.ToString();

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
            crossModels.Add(newM1);

            newM2.replaceNodes(nodes2, updatedNodes1);
            crossModels.Add(newM2);

            return crossModels;
        }// crossover

        private void findOneToOneMatchingNodes(Graph g1, Graph g2, out List<Node> nodes1, out List<Node> nodes2)
        {
            nodes1 = new List<Node>();
            nodes2 = new List<Node>();
            List<Node> matched = new List<Node>();
            foreach (Node n1 in g1._NODES)
            {
                foreach (Node n2 in g2._NODES)
                {
                    if ((n1._isGroundTouching && n2._isGroundTouching) ||
                        (n1._edges.Count == n2._edges.Count))
                    {
                        nodes1.Add(n1);
                        nodes2.Add(n2);
                    }
                }
            }
        }// findOneToOneMatchingNodes

        private void switchNodes(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2, 
            out List<Node> updateNodes1, out List<Node> updateNodes2)
        {
            List<Edge> edgesToConnect_1 = g1.collectOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.collectOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_1);
            List<Vector3d> targets = collectPoints(edgesToConnect_2);

            updateNodes1 = new List<Node>();
            updateNodes2 = new List<Node>();
            Vector3d center1 = new Vector3d();
            Vector3d maxv_s = Vector3d.MinCoord;
            Vector3d minv_s = Vector3d.MaxCoord;
            Vector3d maxv_t = Vector3d.MinCoord;
            Vector3d minv_t = Vector3d.MaxCoord;
            
            foreach (Node node in nodes1)
            {
                Node cloned = node.Clone() as Node;
                updateNodes1.Add(cloned);
                center1 += node._PART._BOUNDINGBOX.CENTER;
                maxv_s = Vector3d.Max(maxv_s, node._PART._BOUNDINGBOX.MaxCoord);
                minv_s = Vector3d.Min(minv_s, node._PART._BOUNDINGBOX.MinCoord);
            }
            center1 /= nodes1.Count;
            Vector3d center2 = new Vector3d();
            foreach (Node node in nodes2)
            {
                Node cloned = node.Clone() as Node;
                updateNodes2.Add(cloned);
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

            {
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
            }

            if (sources.Count == 1)
            {
                sources.Add(center1);
                targets.Add(center2);
            }

            Node ground1 = hasGroundTouchingNode(nodes1);
            Node ground2 = hasGroundTouchingNode(nodes2);
            bool isGround = false;
            if (ground1 != null && ground2 != null && sources.Count > 0)
            {
                //sources.Add(new Vector3d(center1.x, 0, center1.z));
                //targets.Add(new Vector3d(center2.x, 0, center2.z));
                sources.Add(new Vector3d(sources[0].x, 0, sources[0].z));
                targets.Add(new Vector3d(targets[0].x, 0, targets[0].z));
                isGround = true;
            }
            getTransformation(sources, targets, out S, out T, out Q, boxScale_1, true);
            foreach (Node node in updateNodes1)
            {
                node.Transform(Q);
            }
            if (isGround)
            {
                adjustGroundTouching(updateNodes1);
            }

            getTransformation(targets, sources, out S, out T, out Q, boxScale_2, true);
            foreach (Node node in updateNodes2)
            {
                node.Transform(Q);
            }
            if (isGround)
            {
                adjustGroundTouching(updateNodes2);
            }
        }// switchOneNode

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
            foreach (Node node in nodes)
            {
                node.Transform(T);
            }
        }// adjustGroundTouching

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
            } else if (n == 2)
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

                    // 4 permutations, 012, 123, 023, 013
                    Vector3d aa = tarpts[0];
                    Vector3d bb = tarpts[1];
                    Vector3d cc = tarpts[2];
                    Vector3d dd = tarpts[3];
                    //Vector3d aa = tarpts[0];
                    //Vector3d bb = tarpts[1];
                    //Vector3d cc = tarpts[tarpts.Count - 2];
                    //Vector3d dd = tarpts[tarpts.Count - 1];
                    Vector3d[] nn = new Vector3d[4] {
                        ((aa - bb).Cross(bb - cc)).normalize(),
                        ((bb - cc).Cross(cc - dd)).normalize(),
                        ((aa - cc).Cross(cc - dd)).normalize(),
                        ((aa - bb).Cross(bb - dd)).normalize()
                    };
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

                    if (double.IsNaN(normal.x)) throw new Exception();
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
                            sz = 1.0;
                    }
                }

                Vector3d scale = new Vector3d(sx, sy, sz);

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

        
        public void setRandomColor()
        {
            if (_currModel != null && _currModel._GRAPH != null)
            {
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    node._PART.setRandomColor();
                }
            }
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
                case 9:
                    this.currUIMode = UIMode.Contact;
                    this._showContactPoint = true;
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
            for (int i = 0; i < _editAxes.Length; ++i )
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
            this.ContextMenuStrip.Hide();

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
                    break;
                case UIMode.Contact:
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
                        this.setUIMode(9); // contact
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
            if (this._currModel == null || this._currModel._GRAPH == null || q == null) return;
            this.cal2D();
            _selectedNodes = new List<Node>();
            foreach(Node node in _currModel._GRAPH._NODES)
            {
                Part p = node._PART;
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
                            _selectedNodes.Remove(node);
                            break;
                        }
                        else
                        {
                            _selectedParts.Add(p);
                            _selectedNodes.Add(node);
                        }
                        break;
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
            ModelViewer mv = new ModelViewer(m, -1, this);
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
            } else if (_currHumanPose != null) // NOTE!! else relation
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
                    if (MessageBox.Show("Add a contact to an existing EDGE?") == DialogResult.Cancel)
                    { }
                    else
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
            foreach (ModelViewer mv in _resViewers)
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

            if (this.showGround)
            {
                drawGround();
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
                if (_currModel._GRAPH != null && this.isDrawGraph)
                {
                    this.drawGraph(_currModel._GRAPH);
                }
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
        }// showGround

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
                if (this.isDrawBbox)
                {
                    GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, part._COLOR);
                    if (part._BOUNDINGBOX.type == Common.PrimType.Cuboid)
                    {
                        GLDrawer.drawBoundingboxEdges(part._BOUNDINGBOX, part._COLOR);
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
