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
            // glsl shaders
            this.shader = new Shader(
                @"shaders\vertexshader.glsl",
                @"shaders\fragmentshader.glsl");
            this.shader.Link();

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
            this.segmentClasses = new List<SegmentClass>();
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
            this.initializeInsetViewer();
        }

        public void initializeInsetViewer()
        {
            this.insetViewer = new InsetViewer();

            this.insetViewer.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            this.insetViewer.BorderStyle = BorderStyle.None;
            this.insetViewer.AutoSwapBuffers = true;
            this.insetViewer.BackColor = System.Drawing.Color.Transparent;
            this.insetViewer.accModelView(this._currModelTransformMatrix, this.eye);
			int h = this.Height / 3, w = h * 4 / 3;
			int off = 50;
			this.insetViewer.Location = new Point(this.Location.X + this.Width - w - off / 2,
				this.Location.Y + off);
			this.insetViewer.Size = new System.Drawing.Size(w, h);
			//this.insetViewer.BringToFront();

			this.insetViewer.Hide();
			
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
            Viewing, VertexSelection, EdgeSelection, FaceSelection, Guide, 
            Sketch, Eraser, MoveCamera, StrokeSelection, BoxSelection, NONE
        }

        private bool drawVertex = false;
        private bool drawEdge = false;
        private bool drawFace = true;
        private bool isDrawAxes = false;
        private bool isDrawQuad = false;

        public bool showBoundingbox = false;
        public bool showMesh = false;
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
        private List<SegmentClass> segmentClasses;
        private MeshClass currMeshClass;
        private SegmentClass currSegmentClass;
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
        public InsetViewer insetViewer = null;

        //########## sketch vars ##########//
        private List<Vector2d> currSketchPoints = new List<Vector2d>();
        private double strokeLength = 0;
        private Stroke currStroke = null;
        //private List<DrawStroke2d> drawStrokes = new List<DrawStroke2d>();

        //########## static vars ##########//
        public static Color[] ColorSet;
        static public Color ModelColor;
        public static Color GuideLineColor;
        private StrokePoint[] paperPos = null;
        private Vector2d[] paperPosLines = null;

        /******************** Vars ********************/
        List<Model> _models;

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
            this.currSegmentClass = null;
            this.meshClasses.Clear();
            this.showSegmentContourIndex = -1;
            //seg.sketch = new List<DrawStroke2d>();
            this.contourPoints.Clear();
            this.silhouettePoints.Clear();
            this.apparentRidgePoints.Clear();
            this.boundaryPoints.Clear();
        }

        public void loadMesh(string filename)
        {
            this.clearContext();

            Mesh m = new Mesh(filename, true);
            MeshClass mc = new MeshClass(m);
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;

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

            this.currSegmentClass = null;
            MeshClass mc = new MeshClass();
            this.contourPoints = mc.loadTriMesh(filename, this.eye);
            this.meshClasses.Add(mc);
            this.currMeshClass = mc;
            this.Refresh();
        }

        public void loadPartBasedModels(string segfolder)
        {
            // Data fromat:
            // n Parts
            // Part #i:
            // bbox vertices
            // mesh file loc
            this.clearContext();
            this.segmentClasses = new List<SegmentClass>();
            int idx = segfolder.LastIndexOf('\\');
            string bbofolder = segfolder.Substring(0, idx + 1);
            bbofolder += "bounding_boxes\\";
            SegmentClass sc = new SegmentClass();
            if (sc.ReadSegments(segfolder, bbofolder))
            {
                this.setRandomSegmentColor(sc);
                this.segmentClasses.Add(sc);
                this.currSegmentClass = sc;
                this.calculateSketchMesh2d();
                segStats = new int[2];
                segStats[0] = sc.segments.Count;
            }
        }// loadPartBasedModel
     
        private void cal2D()
        {
            // otherwise when glViewe is initialized, it will run this function from MouseUp()
            if (this.currSegmentClass == null) return;

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
            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);            



            this.calculatePoint2DInfo();

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();


            
        }//cal2D

        private Vector2d guidelineTextPos;
        private void calculatePoint2DInfo()
        {
            this.updateCamera();
            if (this.currSegmentClass == null) return;
            //Vector3d z = new Vector3d(0, 0, 1);
            //Matrix4d mvp = this.camera.GetProjMat().Inverse();
            //mvp = this._modelTransformMatrix.Inverse() * mvp;
            //mvp = this.camera.GetProjMat().Transpose() * mvp;
            //this.eye = (mvp * new Vector4d(z, 1)).ToHomogeneousVector();
            Vector2d max_coord = Vector2d.MinCoord();
            Vector2d min_coord = Vector2d.MaxCoord();
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                Primitive box = seg.boundingbox;
                box.points2d = new Vector2d[box.points3d.Length];
                for (int i = 0; i < box.points3d.Length; ++i)
                {
                    box.points2d[i] = this.camera.Project(box.points3d[i]).ToVector2d();
                    max_coord = Vector2d.Max(max_coord, box.points2d[i]);
                    min_coord = Vector2d.Min(min_coord, box.points2d[i]);
                }
                List<GuideLine> allLines = seg.boundingbox.getAllLines();
                foreach (GuideLine line in allLines)
                {
                    line.u2 = this.camera.Project(line.u).ToVector2d();
                    line.v2 = this.camera.Project(line.v).ToVector2d();
                    foreach (Stroke stroke in line.strokes)
                    {
                        foreach (StrokePoint sp in stroke.strokePoints)
                        {
                            Vector3d v3 = this.camera.Project(sp.pos3);
                            sp.pos2 = v3.ToVector2d();
                        }
						stroke.u2 = this.camera.Project(stroke.u3).ToVector2d();
						stroke.v2 = this.camera.Project(stroke.v3).ToVector2d();
                    }
                }
                if (seg.boundingbox.planes != null)
                {
                    foreach (Plane3D plane in seg.boundingbox.planes)
                    {
                        plane.points2d = new Vector2d[plane.points3d.Length];
                        for (int i = 0; i < plane.points3d.Length; ++i)
                        {
                            Vector3d v3 = this.camera.Project(plane.points3d[i]);
                            plane.points2d[i] = v3.ToVector2d();
                        }
                    }
                }
                foreach (Circle3D circle in seg.boundingbox.circles)
                {
                    circle.points2d = new Vector2d[circle.points3d.Length];
                    for (int i = 0; i < circle.points3d.Length; ++i)
                    {
                        Vector3d v3 = circle.points3d[i];
                        circle.points2d[i] = this.camera.Project(v3).ToVector2d();
                    }
                    circle.center2 = this.camera.Project(circle.center).ToVector2d();
                    circle.calAxes();
                }
            }
            foreach (GuideLine line in this.currSegmentClass.guideLines)
            {
                line.u2 = this.camera.Project(line.u).ToVector2d();
                line.v2 = this.camera.Project(line.v).ToVector2d();
                foreach (Stroke stroke in line.strokes)
                {
                    foreach (StrokePoint sp in stroke.strokePoints)
                    {
                        Vector3d v3 = this.camera.Project(sp.pos3);
                        sp.pos2 = v3.ToVector2d();
                    }
                    stroke.u2 = this.camera.Project(stroke.u3).ToVector2d();
                    stroke.v2 = this.camera.Project(stroke.v3).ToVector2d();
                }
            }
            this.guidelineTextPos = new Vector2d((max_coord.x + min_coord.x) / 2, this.Height - 50);
        }//calculatePoint2DInfo

        private void calSegmentsBounds(out Vector2d minCoord, out Vector2d maxCoord)
        {
            minCoord = Vector2d.MaxCoord();
            maxCoord = Vector2d.MinCoord();
            if (this.currSegmentClass == null) return;

            foreach (Segment seg in this.currSegmentClass.segments)
            {
                Primitive box = seg.boundingbox;
                if (seg.boundingbox.planes != null)
                {
                    foreach (Plane3D plane in seg.boundingbox.planes)
                    {
                        plane.points2d = new Vector2d[plane.points3d.Length];
                        for (int i = 0; i < plane.points3d.Length; ++i)
                        {
                            Vector3d v3 = this.camera.Project(plane.points3d[i]);
                            plane.points2d[i] = v3.ToVector2d();
                            minCoord = Vector2d.Min(minCoord, plane.points2d[i]);
                            maxCoord = Vector2d.Max(maxCoord, plane.points2d[i]);
                        }
                    }
                }
            }

            //foreach (DrawStroke2d stroke in seg.sketch)
            //{
            //    foreach (StrokePoint sp in stroke.strokes[0].strokePoints)
            //    {
            //        minCoord = Vector2d.Min(minCoord, sp.pos2);
            //        maxCoord = Vector2d.Max(maxCoord, sp.pos2);
            //    }
            //}
            Vector2d center = (maxCoord + minCoord) / 2;
        }//calSegmentsBounds

        public void setRandomSegmentColor(SegmentClass sc)
        {
            foreach (Segment seg in sc.segments)
            {
                int idx = GLViewer.rand.Next(0, GLViewer.ColorSet.Length - 1);
                Color c = GLViewer.ColorSet[idx];
                seg.color = Color.FromArgb(80, c);
            }
        }

        public void setStrokeSize(double size)
        {
            SegmentClass.StrokeSize = size;
            if (this.currSegmentClass == null) return;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                this.setStrokeSize(size, seg, Color.White, Color.White);
                foreach (DrawStroke2d drawStroke in seg.sketch)
                {
                    double dsize = size;
                    for (int i = 0; i < drawStroke.strokes.Count; ++i)
                    {
                        Stroke stroke = drawStroke.strokes[i];
                        if (i > 0)
                        {
                            dsize /= 2;
                        }
                        stroke.setStrokeSize(dsize);
                        stroke.changeStyle2d((int)SegmentClass.strokeStyle);
                    }
                }
            }

            
        }//setStrokeSize

        private void setStrokeSize(double size, Segment seg, Color c, Color gc)
        {
            Primitive box = seg.boundingbox;
            foreach (GuideLine edge in box.edges)
            {
                for (int i = 0; i < edge.strokes.Count; ++i)
                {
                    Stroke stroke = edge.strokes[i];
                    if (c != Color.White)
                    {
                        stroke.strokeColor = c;
                    }
                    if (i == 0)
                    {
                        stroke.setStrokeSize(size);
                    }
                    else
                    {
                        stroke.setStrokeSize((double)size * 0.7);
                    }
                    stroke.changeStyle((int)SegmentClass.strokeStyle);
                }
            }
            for (int g = 0; g < box.guideLines.Count; ++g)
            {
                foreach (GuideLine line in box.guideLines[g])
                {
                    for (int i = 0; i < line.strokes.Count; ++i )
                    {
                        Stroke stroke = line.strokes[i];
                        if (c != Color.White)
                        {
                            stroke.strokeColor = gc;
                        }
                        if (i == 0)
                        {
                            stroke.setStrokeSize(size);
                        }
                        else
                        {
                            stroke.setStrokeSize((double)size * 0.7);
                        }
                        stroke.changeStyle((int)SegmentClass.strokeStyle);
                    }
                }
            }
        }// setStrokeSize - per seg

        private void setStrokeStylePerLine(GuideLine line, double size, Color c)
        {
            for (int i = 0; i < line.strokes.Count; ++i)
            {
                Stroke stroke = line.strokes[i];
                if (c != Color.White)
                {
                    stroke.strokeColor = c;
                }
                if (i == 0)
                {
                    stroke.setStrokeSize(size);
                }
                else
                {
                    stroke.setStrokeSize((double)size * 0.7);
                    stroke.strokeColor = SegmentClass.sideStrokeColor;
                }

                stroke.changeStyle((int)SegmentClass.strokeStyle);
            }

        }// setStrokeStylePerLine - per seg

        public void setSegmentColor(Color c)
        {
            if (this.currSegmentClass == null) return;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                seg.color = c;
            }
        }

        private List<List<int>> curUserGuides = new List<List<int>>();
        private List<List<int>> curUserPrevGuides = new List<List<int>>();
        private int showSegmentContourIndex = -1;

        private void projectStrokePointsTo2d(Stroke stroke)
        {
            if (stroke.strokePoints == null || stroke.strokePoints.Count == 0)
                return;
            foreach(StrokePoint p in stroke.strokePoints)
            {
                Vector3d p3 = p.pos3;
                p.pos2 = this.camera.Project(p3.x, p3.y, p3.z).ToVector2d();
            }
        }

        public void calculateSketchMesh2d()
        {
            this.updateCamera();
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                List<GuideLine> allLines = seg.boundingbox.getAllLines();
                foreach (GuideLine edge in allLines)
                {
                    foreach (Stroke stroke in edge.strokes)
                    {
                        Vector3d u3 = stroke.u3;
                        Vector3d v3 = stroke.v3;
                        stroke.u2 = this.camera.Project(u3.x, u3.y, u3.z).ToVector2d();
                        stroke.v2 = this.camera.Project(v3.x, v3.y, v3.z).ToVector2d();
                        Vector2d dir = (stroke.v2 - stroke.u2).normalize();
                        Vector2d normal = new Vector2d(-dir.y, dir.x);
                        normal.normalize();
                        this.projectStrokePointsTo2d(stroke);
                        stroke.hostPlane = edge.hostPlane;
                    }
                }
            }

            foreach (GuideLine edge in this.currSegmentClass.guideLines)
            {
                foreach (Stroke stroke in edge.strokes)
                {
                    Vector3d u3 = stroke.u3;
                    Vector3d v3 = stroke.v3;
                    stroke.u2 = this.camera.Project(u3.x, u3.y, u3.z).ToVector2d();
                    stroke.v2 = this.camera.Project(v3.x, v3.y, v3.z).ToVector2d();
                    Vector2d dir = (stroke.v2 - stroke.u2).normalize();
                    Vector2d normal = new Vector2d(-dir.y, dir.x);
                    normal.normalize();
                    this.projectStrokePointsTo2d(stroke);
                    stroke.hostPlane = edge.hostPlane;
                }
            }

        }// calculateSketchMesh2d

        public Ellipse3D createEllipse(Segment seg)
        {
            if (seg == null) return null;
            Primitive box = seg.boundingbox;
            int[] ids = { 1, 2, 6, 5 };
            Vector3d u = (box.points3d[ids[2]] + box.points3d[ids[3]]) / 2 - (box.points3d[ids[0]] + box.points3d[ids[1]]) / 2;
            Vector3d v = (box.points3d[ids[0]] + box.points3d[ids[3]]) / 2 - (box.points3d[ids[1]] + box.points3d[ids[2]]) / 2;
            double a = (box.points3d[ids[0]] - box.points3d[ids[3]]).Length();
            double b = (box.points3d[ids[0]] - box.points3d[ids[1]]).Length();
            Vector3d c = new Vector3d();
            for (int i = 0; i < ids.Length; ++i)
            {
                c += box.points3d[ids[i]];
            }
            c /= ids.Length;
            Ellipse3D e = new Ellipse3D(c, u, v, a, b);
            return e;
        }
   
        public void outputBoxSequence()
        {
            if (this.currSegmentClass == null) return;
            string filename = foldername + "\\sequence.txt";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < this.currSegmentClass.segments.Count; ++i)
                {
                    sw.WriteLine("box:" + i.ToString());
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
			this.insetViewer.accModelView(this._currModelTransformMatrix, this.eye);
			this.insetViewer.Refresh();
        }

        public void captureScreen(int idx)
        {
            //Size newSize = new System.Drawing.Size(this.Width - (int)(this.paperPos[0].pos2.x + 100), //(Screen.PrimaryScreen.Bounds.Size.Width - 400,// - 360,// - 500, 
            //    this.Height); //Screen.PrimaryScreen.Bounds.Size.Height - 150);
            //var bmp = new Bitmap(newSize.Width, newSize.Height);
            //var gfx = Graphics.FromImage(bmp);
            //gfx.CopyFromScreen((int)(this.paperPos[0].pos2.x + this.Location.X - 50), Screen.PrimaryScreen.Bounds.Y + 100, 0, 0, newSize, CopyPixelOperation.SourceCopy);
            int insetPos = Program.GetFormMain().getScreenCaptureBoundaryX(this.Location.X);
            insetPos = insetPos == 0 ? this.Width - (int)(this.paperPos[0].pos2.x + 100):
                insetPos - 150;
            Size newSize = new System.Drawing.Size(insetPos, this.Height - 20); 
            var bmp = new Bitmap(newSize.Width, newSize.Height);
            var gfx = Graphics.FromImage(bmp);
            gfx.CopyFromScreen((int)(this.paperPos[0].pos2.x + this.Location.X - 60),
                Screen.PrimaryScreen.Bounds.Y + 110, 0, 0, newSize, CopyPixelOperation.SourceCopy);
            string imageFolder = foldername + "\\screenCapture";
            
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }
            string name = imageFolder + "\\seq_" + idx.ToString() + ".png";
            bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void computeContours()
        {
            //return;
			if (this.currSegmentClass == null && this.meshClasses == null) return;
            this.silhouettePoints = new List<Vector3d>();
            this.contourPoints = new List<Vector3d>();
            this.contourLines = new List<List<Vector3d>>();
            this.apparentRidgePoints = new List<Vector3d>();
            this.suggestiveContourPoints = new List<Vector3d>();
            this.boundaryPoints = new List<Vector3d>();
            this.sharpEdges = new List<Vector3d>();

			if (this.showSharpEdge)
			{
				//this.currSegmentClass.calculateContourPoint(this._currModelTransformMatrix, this.eye);
				if (this.currSegmentClass != null)
				{
					this.computeContourEdges();
				}
				else if (this.currMeshClass != null)
				{
					List<int> vidxs = this.computeMeshContour(this.currMeshClass.Mesh);
					//this.regionGrowingContours(this.currMeshClass.Mesh, vidxs);
					//this.computeContourByVisibility(this.currMeshClass.Mesh);
					this.computeMeshSharpEdge(this.currMeshClass.Mesh);
				}
				if (this.contourPoints.Count > 0)
				{
					this.drawEdge = false;
				}
			}
			else
			{
				if (this.currSegmentClass != null)
				{
					if (!this.isShowContour()) return;
					Matrix4d T = this._currModelTransformMatrix;
					Matrix4d Tv = T.Inverse();
					foreach (Segment seg in this.currSegmentClass.segments)
					{
						foreach (GuideLine line in seg.boundingbox.edges)
						{
							foreach (Stroke stroke in line.strokes)
							{
								stroke.strokeColor = SegmentClass.HiddenColor;
							}
						}

						if (!seg.active) continue;
						seg.updateVertex(T);
						if (this.showSegSilhouette)
						{
							seg.computeSihouette(Tv, this.eye);
						}
						if (this.showSegContour)
						{
							seg.computeContour(Tv, this.eye);
						}
						if (this.showSegSuggestiveContour)
						{
							seg.computeSuggestiveContour(Tv, this.eye);
						}
						if (this.showSegApparentRidge)
						{
							seg.computeApparentRidge(Tv, this.eye);
						}
						if (this.showSegBoundary)
						{
							seg.computeBoundary(Tv, this.eye);
						}
                        List<Vector3d> points3d = new List<Vector3d>();
                        if (seg.contourPoints.Count > 0)
                        {
                            points3d = seg.contourPoints;
                        }
                        else if (seg.silhouettePoints.Count > 0)
                        {
                            points3d = seg.silhouettePoints;
                        }
                        List<Vector2d> points2d = new List<Vector2d>();
                        foreach (Vector3d v3 in points3d)
                        {
                            Vector2d v2 = this.camera.Project(v3).ToVector2d();
                            points2d.Add(v2);
                        }
                        Stroke segStroke = new Stroke(points2d, SegmentClass.StrokeSize);
                        this.createDrawStroke(seg, segStroke);
					}
				}
				if (this.currMeshClass != null && this.currMeshClass.TriMesh != null)
				{
					if (!this.isShowContour()) return;

					Mesh mesh = this.currMeshClass.Mesh;
					double[] vertexPos = new double[mesh.VertexPos.Length];
					List<int> vidxs = new List<int>();
					for (int i = 0, j = 0; i < mesh.VertexCount; ++i, j += 3)
					{
						Vector3d v0 = new Vector3d(mesh.VertexPos[j],
							mesh.VertexPos[j + 1],
							mesh.VertexPos[j + 2]);
						Vector3d v1 = (this._currModelTransformMatrix * new Vector4d(v0, 1)).ToVector3D();
						vertexPos[j] = v1.x;
						vertexPos[j + 1] = v1.y;
						vertexPos[j + 2] = v1.z;
					}
					this.silhouettePoints.Clear();
					this.contourPoints.Clear();
					this.suggestiveContourPoints.Clear();
					this.apparentRidgePoints.Clear();

					if (this.showSegSilhouette)
					{
						this.silhouettePoints = this.currMeshClass.computeContour(vertexPos, this.eye, 1);
						for (int i = 0; i < this.silhouettePoints.Count; ++i)
						{
							Vector3d v = this.silhouettePoints[i];
							Vector3d vt = (this._currModelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
							this.silhouettePoints[i] = vt;
						}
					}
					if (this.showSegContour)
					{
						this.contourPoints = this.currMeshClass.computeContour(vertexPos, this.eye, 2);
						for (int i = 0; i < this.contourPoints.Count; ++i)
						{
							Vector3d v = this.contourPoints[i];
							Vector3d vt = (this._currModelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
							this.contourPoints[i] = vt;
						}
					}
					if (this.showSegSuggestiveContour)
					{
						this.suggestiveContourPoints = this.currMeshClass.computeContour(vertexPos, this.eye, 3);
						for (int i = 0; i < this.suggestiveContourPoints.Count; ++i)
						{
							Vector3d v = this.suggestiveContourPoints[i];
							Vector3d vt = (this._currModelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
							this.suggestiveContourPoints[i] = vt;
						}
					}
					if (this.showSegApparentRidge)
					{
						this.apparentRidgePoints = this.currMeshClass.computeContour(vertexPos, this.eye, 4);
						for (int i = 0; i < this.apparentRidgePoints.Count; ++i)
						{
							Vector3d v = this.apparentRidgePoints[i];
							Vector3d vt = (this._currModelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
							this.apparentRidgePoints[i] = vt;
						}
					}
					if (this.showSegBoundary)
					{
						this.boundaryPoints = this.currMeshClass.computeContour(vertexPos, this.eye, 5);
						for (int i = 0; i < this.boundaryPoints.Count; ++i)
						{
							Vector3d v = this.boundaryPoints[i];
							Vector3d vt = (this._currModelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
							this.boundaryPoints[i] = vt;
						}
					}

				}
			}

            #region
            //// my implementation
            if (this.currSegmentClass == null || (this.meshClasses == null || this.meshClasses.Count == 0)) return;
            this.contourPoints = new List<Vector3d>();
            this.sharpEdges = new List<Vector3d>();
            //this.currSegmentClass.calculateContourPoint(this._currModelTransformMatrix, this.eye);
            if (this.currSegmentClass != null)
            {
                this.computeContourEdges();
            }
            else if(this.currMeshClass != null)
            {
                List<int> vidxs = this.computeMeshContour(this.currMeshClass.Mesh);
                //this.regionGrowingContours(this.currMeshClass.Mesh, vidxs);
                //this.computeContourByVisibility(this.currMeshClass.Mesh);
                this.computeMeshSharpEdge(this.currMeshClass.Mesh);
            }
            if (this.contourPoints.Count > 0)
            {
                this.drawEdge = false;
            }
            #endregion
        }//computeContours

        private List<Vector3d> contourPoints = new List<Vector3d>(), 
            silhouettePoints = new List<Vector3d>(), 
            apparentRidgePoints = new List<Vector3d>(),
            suggestiveContourPoints = new List<Vector3d>(),
            boundaryPoints = new List<Vector3d>();
        private List<Vector3d> sharpEdges;
        private List<List<Vector3d>> contourLines;

        public void computeContourEdges()
        {
            // current pos, normal
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (seg.mesh == null) continue;
                List<int> vidxs = this.computeMeshContour(seg.mesh);
                //seg.regionGrowingContours(vidxs);
                this.computeMeshSharpEdge(seg.mesh);
            }// fore each segment
        }//computeContourEdges

        #region
        // contour tests
        public void regionGrowingContours(Mesh m, List<int> labeled)
        {
            int ndist = 10;
            this.contourLines = new List<List<Vector3d>>();
            while (labeled.Count > 0)
            {
                int i = labeled[0];
                labeled.RemoveAt(0);
                if (!m.Flags[i])
                {
                    continue;
                }
                m.Flags[i] = false;
                List<int> vids = new List<int>();
                List<int> queue = new List<int>();
                vids.Add(i);
                queue.Add(i);
                int s = 0;
                int d = 0;
                while (s < queue.Count && d < ndist)
                {
                    int j = queue[s];
                    for (int k = 0; k < m.VertexFaceIndex[j].Count; ++k)
                    {
                        int f = m.VertexFaceIndex[j][k];
                        if (f == -1) continue;
                        for (int fi = 0; fi < 3; ++fi)
                        {
                            int kv = m.FaceVertexIndex[f * 3 + fi];
                            if (m.Flags[kv])
                            {
                                vids.Add(kv);
                                m.Flags[kv] = false;
                                d = 0;
                            }
                            if (!queue.Contains(kv))
                            {
                                queue.Add(kv);
                            }
                        }
                        ++s;
                        ++d;
                    }
                }
                if (vids.Count > 4)
                {
                    List<Vector3d> c = new List<Vector3d>();
                    foreach (int v in vids)
                    {
                        c.Add(m.getVertexPos(v));
                    }
                    this.contourLines.Add(c);
                }
            }
        }//regionGrowingContours
       
        private void computeContourByVisibility(Mesh m)
        {
            this.clearScene();
            int n = m.FaceCount;
            int[] queryIDs = new int[n];
            Gl.glGenQueries(n, queryIDs);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            this.setViewMatrix();
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
                Gl.glBeginQuery(Gl.GL_SAMPLES_PASSED, queryIDs[i]);
                Gl.glColor3ub(0, 0, 0);
                Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glVertex3d(v1.x, v1.y, v1.z);
                Gl.glVertex3d(v2.x, v2.y, v2.z);
                Gl.glVertex3d(v3.x, v3.y, v3.z);
                Gl.glEnd();
                Gl.glEndQuery(Gl.GL_SAMPLES_PASSED);
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();

            // get # passed samples
            int[] faceVis = new int[n];
            int sum = 0;
            for (int i = 0; i < n; ++i)
            {
                int queryReady = Gl.GL_FALSE;
                int count = 1000;
                while (queryReady != Gl.GL_TRUE && count-- > 0)
                {
                    Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT_AVAILABLE, out queryReady);
                }
                if (queryReady == Gl.GL_FALSE)
                {
                    count = 1000;
                    while (queryReady != Gl.GL_TRUE && count-- > 0)
                    {
                        Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT_AVAILABLE, out queryReady);
                    }
                }
                Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT, out faceVis[i]);
                if (faceVis[i] > 2)
                {
                    sum += 0;
                }
                sum += faceVis[i];
            }
            Gl.glDeleteQueries(n, queryIDs);

            foreach (HalfEdge edge in m.HalfEdges)
            {
                if (edge.invHalfEdge == null) // boudnary
                {
                    //this.sharpEdges.Add(m.getVertexPos(edge.FromIndex));
                    //this.sharpEdges.Add(m.getVertexPos(edge.ToIndex));
                    continue;
                }
                if (edge.invHalfEdge.index < edge.index)
                {
                    continue; // checked
                }
                int fidx = edge.FaceIndex;
                int invfidx = edge.invHalfEdge.FaceIndex;
                if ((faceVis[fidx] < 3 && faceVis[invfidx] >= 3) ||
                    (faceVis[invfidx] < 3 && faceVis[fidx] >= 3))
                {
                    this.contourPoints.Add(m.getVertexPos(edge.FromIndex));
                    this.contourPoints.Add(m.getVertexPos(edge.ToIndex));
                }
            }

        }//computeContourByVisibility
        #endregion

        private List<int> computeMeshContour(Mesh mesh)
        {
            List<int> vidxs = new List<int>();
			if (mesh == null || mesh.VertexPos == null) return vidxs;
			double[] vertexPos = new double[mesh.VertexPos.Length];
			
            for (int i = 0, j = 0; i < mesh.VertexCount; ++i, j += 3)
            {
                Vector3d v0 = new Vector3d(mesh.VertexPos[j],
                    mesh.VertexPos[j + 1],
                    mesh.VertexPos[j + 2]);
                Vector3d v1 = (this._currModelTransformMatrix * new Vector4d(v0, 1)).ToVector3D();
                vertexPos[j] = v1.x;
                vertexPos[j + 1] = v1.y;
                vertexPos[j + 2] = v1.z;
            }

            // transformed mesh
            Mesh m = new Mesh(mesh, vertexPos);
            //for (int i = 0, j = 0; i < m.VertexCount; ++i, j += 3)
            //{
            //    Vector3d v0 = m.getVertexPos(i);
            //    Vector3d vn = new Vector3d(m.VertexNormal[j],
            //        m.VertexNormal[j + 1],
            //        m.VertexNormal[j + 2]).normalize();
            //    Vector3d v = (eye - v0).normalize();
            //    double cosv = v.Dot(vn);
            //    if (Math.Abs(cosv) < thresh)// && cosv > 0)
            //    {
            //        mesh.Flags[i] = true;
            //        vidxs.Add(i);
            //        this.contourPoints.Add(mesh.getVertexPos(i));
            //    }
            //}

            #region
            // check the sign change of each edge
            foreach (HalfEdge edge in m.HalfEdges)
            {
                if (edge.invHalfEdge == null) // boudnary
                {
                    //this.contourPoints.Add(mesh.getVertexPos(edge.FromIndex));
                    //this.contourPoints.Add(mesh.getVertexPos(edge.ToIndex));
                    continue;
                }
                if (edge.invHalfEdge.index < edge.index)
                {
                    continue; // checked
                }
                int fidx = edge.FaceIndex;
                int invfidx = edge.invHalfEdge.FaceIndex;
                Vector3d v1 = m.getFaceCenter(fidx);
                Vector3d v2 = m.getFaceCenter(invfidx);
                //Vector3d v1 = m.getVertexPos(edge.FromIndex);
                //Vector3d v2 = m.getVertexPos(edge.ToIndex);
                Vector3d e1 = (this.eye - v1).normalize();
                Vector3d e2 = (this.eye - v2).normalize();
                Vector3d n1 = m.getFaceNormal(fidx);
                Vector3d n2 = m.getFaceNormal(invfidx);
                double c1 = e1.Dot(n1);
                double c2 = e2.Dot(n2);
                //if (Math.Abs(c1) < thresh || Math.Abs(c2) < thresh)
                //{
                //    this.contourPoints.Add(mesh.getVertexPos(edge.FromIndex));
                //    this.contourPoints.Add(mesh.getVertexPos(edge.ToIndex));
                //}
                if (c1 * c2 <= 0)
                {
                    this.contourPoints.Add(mesh.getVertexPos(edge.FromIndex));
                    this.contourPoints.Add(mesh.getVertexPos(edge.ToIndex));
                    vidxs.Add(edge.FromIndex);
                    vidxs.Add(edge.ToIndex);
                    mesh.Flags[edge.FromIndex] = true;
                    mesh.Flags[edge.ToIndex] = true;
                }
            }

            #endregion

            return vidxs;
        }// computeMeshContour

        private void computeMeshSharpEdge(Mesh mesh)
        {
			if (mesh == null || mesh.VertexPos == null) return;
            double thresh = 0.3;
            double[] vertexPos = new double[mesh.VertexPos.Length];
            for (int i = 0, j = 0; i < mesh.VertexCount; ++i, j += 3)
            {
                Vector3d v0 = new Vector3d(mesh.VertexPos[j],
                    mesh.VertexPos[j + 1],
                    mesh.VertexPos[j + 2]);
                Vector3d v1 = (this._currModelTransformMatrix * new Vector4d(v0, 1)).ToVector3D();
                vertexPos[j] = v1.x;
                vertexPos[j + 1] = v1.y;
                vertexPos[j + 2] = v1.z;
            }

            // transformed mesh
            Mesh m = new Mesh(mesh, vertexPos);
            // check the sign change of each edge
            foreach (HalfEdge edge in m.HalfEdges)
            {
                if (edge.invHalfEdge == null) // boudnary
                {
                    this.sharpEdges.Add(mesh.getVertexPos(edge.FromIndex));
                    this.sharpEdges.Add(mesh.getVertexPos(edge.ToIndex));
                    continue;
                }
                if (edge.invHalfEdge.index < edge.index)
                {
                    continue; // checked
                }
                int fidx = edge.FaceIndex;
                int invfidx = edge.invHalfEdge.FaceIndex;
                Vector3d v1 = m.getFaceCenter(fidx);
                Vector3d v2 = m.getFaceCenter(invfidx);
                Vector3d n1 = m.getFaceNormal(fidx);
                Vector3d n2 = m.getFaceNormal(invfidx);
                double c = Math.Acos(n1.Dot(n2));
                if (Math.Abs(c) > thresh)
                {
                    this.sharpEdges.Add(mesh.getVertexPos(edge.FromIndex));
                    this.sharpEdges.Add(mesh.getVertexPos(edge.ToIndex));
                }
            }
        }

        private Vector3d getCurPos(Vector3d v)
        {
            return (this._currModelTransformMatrix * new Vector4d(v, 1)).ToVector3D();
        }

        private Vector3d getOriPos(Vector3d v)
        {
            return (this._modelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
        }

        private void calculateStrokePointDepthByCameraPos()
        {
            this.updateCamera();
            if (this.currSegmentClass == null)
            {
                return;
            }

            double minD = double.MaxValue, maxD = double.MinValue;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                //List<GuideLine> allLines = seg.boundingbox.getAllLines();
                foreach (GuideLine line in seg.boundingbox.edges)
                {
                    foreach (Stroke stroke in line.strokes)
                    {
                        foreach (StrokePoint sp in stroke.strokePoints)
                        {
                            Vector3d pos = this.getCurPos(sp.pos3);
                            sp.depth = (pos - eye).Length();
                            maxD = maxD > sp.depth ? maxD : sp.depth;
                            minD = minD < sp.depth ? minD : sp.depth;
                        }
                    }
                }
            }
            double scale = maxD - minD;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                //List<GuideLine> allLines = seg.boundingbox.getAllLines();
                foreach (GuideLine line in seg.boundingbox.edges)
                {
                    foreach (Stroke stroke in line.strokes)
                    {
                        foreach (StrokePoint sp in stroke.strokePoints)
                        {
                            sp.depth = 1 - (sp.depth - minD) / scale;
                        }
                    }
                }
            }
        }// calculateStrokePointDepthByCameraPos

        private void setHiddenLines(Segment seg)
        {
            if (this.depthType == Depthtype.hidden) return;

            this.clearScene();
            // draw the whole sceen from "Draw3D()" to get the visibility info depth value
            int n = 0;
            int nsample = 10; // sample 10 points3d on each line

            foreach (GuideLine edge in seg.boundingbox.edges)
            {
                ++n;
            }
            // draw and get visibility
            int[] queryIDs = new int[n];
            Gl.glGenQueries(n, queryIDs);

            Gl.glEnable(Gl.GL_DEPTH_TEST);
            int idx = 0;
            this.setViewMatrix();

            this.drawBoundingbox(seg.boundingbox, Color.White);

            foreach (GuideLine edge in seg.boundingbox.edges)
            {
                Vector3d dir = (edge.v - edge.u).normalize();
                double dist = (edge.v - edge.u).Length() / nsample;
                Gl.glBeginQuery(Gl.GL_SAMPLES_PASSED, queryIDs[idx++]);
                Gl.glColor3ub(0, 0, 0);
                Gl.glBegin(Gl.GL_LINES);
                for (int i = 0; i < nsample - 1; ++i)
                {
                    Vector3d v1 = edge.u + i * dist * dir;
                    Vector3d v2 = edge.u + (i + 1) * dist * dir;
                    Gl.glVertex3dv(v1.ToArray());
                    Gl.glVertex3dv(v2.ToArray());
                }
                Gl.glEnd();
                Gl.glEndQuery(Gl.GL_SAMPLES_PASSED);
            }

            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            //this.SwapBuffers();

            // get # passed samples
            int[] visPnts = new int[n];
            int maxv = 0;
            for (int i = 0; i < n; ++i)
            {
                int queryReady = Gl.GL_FALSE;
                int count = 1000;
                while (queryReady != Gl.GL_TRUE && count-- > 0)
                {
                    Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT_AVAILABLE, out queryReady);
                }
                if (queryReady == Gl.GL_FALSE)
                {
                    count = 1000;
                    while (queryReady != Gl.GL_TRUE && count-- > 0)
                    {
                        Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT_AVAILABLE, out queryReady);
                    }
                }
                Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT, out visPnts[i]);
                if (visPnts[i] > maxv)
                {
                    maxv = visPnts[i];
                }
            }
            Gl.glDeleteQueries(n, queryIDs);

            idx = 0;

            foreach (GuideLine edge in seg.boundingbox.edges)
            {
                if (visPnts[idx++] < 10)
                {
                    this.setStrokeStylePerLine(edge, (double)SegmentClass.StrokeSize / 4, SegmentClass.HiddenColor);
                    // the hidden color might get modified
                    foreach (Stroke stroke in edge.strokes)
                    {
                        stroke.strokeColor = SegmentClass.HiddenColor;
                    }
                    
                }
                //else
                //{
                //    this.setStrokeStylePerLine(edge, (double)SegmentClass.StrokeSize, SegmentClass.StrokeColor);
                //}
            }

        }// setHiddenLines

        private void setHiddenLines()
        {
            if (this.currSegmentClass == null || this.depthType == Depthtype.hidden || (this.inGuideMode )) 
                return;

            this.clearScene();
            // draw the whole sceen from "Draw3D()" to get the visibility info depth value
            int n = 0;
            int nsample = 10; // sample 10 points3d on each line
            // drawAllActiveBoxes() or drawSketchyEdges3D_hiddenLine()
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (!seg.active) continue;
                foreach (GuideLine edge in seg.boundingbox.edges)
                {
                    n += nsample;
                }
            }
            // draw and get visibility
            int[] queryIDs = new int[n];
            Gl.glGenQueries(n, queryIDs);
            //for (int i = 0; i < n; ++i)
            //{
            //    queryIDs[i] = i;
            //}
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            int idx = 0;
            this.setViewMatrix();
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (!seg.active) continue;
                this.drawBoundingbox(seg.boundingbox, Color.White);
            }
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (!seg.active) continue;
                foreach (GuideLine edge in seg.boundingbox.edges)
                {
                    Vector3d dir = (edge.v - edge.u).normalize();
                    double dist = (edge.v-edge.u).Length()/nsample;
                    Gl.glBeginQuery(Gl.GL_SAMPLES_PASSED, queryIDs[idx++]);
                    Gl.glColor3ub(0, 0, 0);
                    Gl.glBegin(Gl.GL_POINTS);
                    for (int i = 0; i < nsample; ++i)
                    {
                        Vector3d v = edge.u + i * dist * dir;
                        Gl.glVertex3dv(v.ToArray());
                    }
                    Gl.glEnd();
                    Gl.glEndQuery(Gl.GL_SAMPLES_PASSED);
                }
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            //this.SwapBuffers();

            // get # passed samples
            int[] visPnts = new int[n];
            int maxv = 0;
            for (int i = 0; i < n; ++i)
            {
                int queryReady = Gl.GL_FALSE;
                int count = 1000;
                while (queryReady != Gl.GL_TRUE && count-- > 0)
                {
                    Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT_AVAILABLE, out queryReady);
                }
                if (queryReady == Gl.GL_FALSE)
                {
                    count = 1000;
                    while (queryReady != Gl.GL_TRUE && count-- > 0)
                    {
                        Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT_AVAILABLE, out queryReady);
                    }
                }
                Gl.glGetQueryObjectiv(queryIDs[i], Gl.GL_QUERY_RESULT, out visPnts[i]);
                if (visPnts[i] > maxv)
                {
                    maxv = visPnts[i];
                }
            }
            Gl.glDeleteQueries(n, queryIDs);

            idx = 0;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (!seg.active) continue;
                foreach (GuideLine edge in seg.boundingbox.edges)
                {
                    if (visPnts[idx++] ==0)
                    {
                        this.setStrokeStylePerLine(edge, SegmentClass.StrokeSize / 4, SegmentClass.HiddenColor);
                        foreach (Stroke stroke in edge.strokes)
                        {
                            stroke.strokeColor = SegmentClass.HiddenColor;
                        }
                    }
                    else
                    {
                        this.setStrokeStylePerLine(edge, SegmentClass.StrokeSize, SegmentClass.StrokeColor);
                    }
                }
            }
            //this.Refresh();
        }// setHiddenLines

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

        public void exportContour(String filename)
        {
            if (this.currSegmentClass == null) return;
            using (StreamWriter sw = new StreamWriter(filename))
            {
                ContourJson contour = new ContourJson();
                contour.viewMatrix = this._currModelTransformMatrix.ToArray();
                contour.segmentContour = new List<SegmentJson>();
                for (int i = 0; i < this.currSegmentClass.segments.Count; ++i)
                {
                    SegmentJson segJson = new SegmentJson();
                    Segment seg = this.currSegmentClass.segments[i];
                    segJson.index = i;
                    List<double> points3d = new List<double>();
                    if (seg.contourPoints != null && seg.contourPoints.Count > 0)
                    {
                        foreach (Vector3d v in seg.contourPoints)
                        {
                            for (int j = 0; j < 3; ++j)
                            {
                                points3d.Add(v[j]);
                            }
                        }
                    }
                    segJson.contourPoints = points3d;
                    contour.segmentContour.Add(segJson);
                }
                string jsonFile = new JavaScriptSerializer().Serialize(contour);
                sw.Write(jsonFile);
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
                case 2:
                    this.currUIMode = UIMode.VertexSelection;
                    break;
                case 3:
                    this.currUIMode = UIMode.EdgeSelection;
                    break;
                case 4:
                    this.currUIMode = UIMode.FaceSelection;
                    break;
				case 5:
					{
						this.currUIMode = UIMode.Sketch;
						this.cal2D();
					}
					break;
                case 6:
                    this.currUIMode = UIMode.Eraser;
                    break;
                case 7:
                    this.currUIMode = UIMode.MoveCamera;
                    break;
                case 8:
                    this.currUIMode = UIMode.StrokeSelection;
                    break;
                case 9:
                    this.currUIMode = UIMode.BoxSelection;
                    break;
                case 1:
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
            if (this.enableHiddencheck || this.condition)
            {
                this.setHiddenLines();
            }
            this.computeContours();
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
			if (this.enableHiddencheck || this.condition)
            {
                this.setHiddenLines();
            }
            this.computeContours();
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
            if (this.enableHiddencheck || this.condition)
            {
                this.setHiddenLines();
            }
            this.computeContours();
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
			if (this.enableHiddencheck || this.condition)
            {
                this.setHiddenLines();
            }
            this.computeContours();
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
                case UIMode.StrokeSelection:
                case UIMode.BoxSelection:
                    {
                        if (this.currSegmentClass != null)
                        {
                            Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPushMatrix();
                            Gl.glMultMatrixd(m.Transpose().ToArray());

                            this.selectMouseDown((int)this.currUIMode,
                                Control.ModifierKeys == Keys.Shift,
                                Control.ModifierKeys == Keys.Control);

                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPopMatrix();

                           
                        }
                        break;
                    }
                case UIMode.Sketch:
                    {
                        this.SketchMouseDown(this.currMousePos);
                        break;
                    }
                case UIMode.Eraser:
                    {
                        this.EraserMouseDown(this.currMousePos, e);
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
                            //this.Refresh();
                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.StrokeSelection:
                case UIMode.BoxSelection:
                    {
                        if (this.currSegmentClass != null && this.currSegmentClass.segments != null && this.isMouseDown)
                        {
                            this.highlightQuad = new Quad2d(this.mouseDownPos, this.currMousePos);
                            this.selectMouseMove((int)this.currUIMode, this.highlightQuad,
                                Control.ModifierKeys == Keys.Control);
                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.Sketch:
                    {
                        this.SketchMouseMove(this.currMousePos);
                        break;
                    }
                case UIMode.Eraser:
                    {
                        this.EraserMouseMove(this.currMousePos, e);
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
                case UIMode.MoveCamera:
                    {
                        if (this.isMouseDown)
                        {
                            Vector2d trans = new Vector2d((this.currMousePos.x - this.mouseDownPos.x) / this.Width,
                                (-this.currMousePos.y + this.mouseDownPos.y) / this.Height);
                            if (Math.Abs(trans.y) > Math.Abs(trans.x))
                            {
                                this.eye.y += trans.y;
								this._currModelTransformMatrix[1,3] += trans.y;
                            }
							//else
							//{
							//	this.eye.x += trans.x;
							//}
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
                case UIMode.StrokeSelection:
                case UIMode.BoxSelection:
                    {
                        this.isDrawQuad = false;
                        this.selectMouseUp((int)this.currUIMode, this.highlightQuad);
                        break;
                    }
                case UIMode.Sketch:
                    {
                        this.SketchMouseUp();
                        break;
                    }
                case UIMode.Eraser:
                    {
                        this.EraserMouseUp();
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
            this.updateSketchStrokePositionToLocalCoord();
			//this.updateInsetViewSize();
            this.Refresh();
        }

        private List<Stroke> selectedStrokes;
        private Segment selectedSeg;
        public void selectMouseDown(int mode, bool isShift, bool isCtrl)
        {
            switch (mode)
            {
                case 8:
                    {
                        if (!isShift && !isCtrl)
                        {
                            this.selectedStrokes = new List<Stroke>();
                        }
                        break;
                    }
                case 9:
                    {
                        this.selectedSeg = null;
                        break;
                    }
                default:
                    break;
            }
        }

        public void selectMouseMove(int mode, Quad2d q, bool isCtrl)
        {
            switch (mode)
            {
                case 8:
                    {
                        this.selectStrokes(q, !isCtrl);
                        break;
                    }
                case 9:
                    {
                        break;
                    }
                default:
                    break;
            }
        }

        public void selectMouseUp(int mode, Quad2d q)
        {
            if (mode == 9 && q != null)
            {
                this.selectBox(q);
            }
            this.isDrawQuad = false;
        }

        public void selectStrokes(Quad2d q, bool select)
        {
            if (this.currSegmentClass == null || this.currSegmentClass.segments == null) 
                return;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                foreach (DrawStroke2d stroke in seg.sketch)
                {
                    foreach (StrokePoint sp in stroke.strokes[0].strokePoints)
                    {
                        if ((select && this.selectedStrokes.Contains(stroke.strokes[0])) ||
                            (!select && !this.selectedStrokes.Contains(stroke.strokes[0])))
                        {
                            continue;
                        }
                        Vector2d v = new Vector2d(sp.pos2);
                        v.y = this.Height - v.y;
                        if (Quad2d.isPointInQuad(v, q))
                        {
                            if (select)
                            {
                                if (!this.selectedStrokes.Contains(stroke.strokes[0]))
                                {
                                    this.selectedStrokes.Add(stroke.strokes[0]);
                                }
                                break;
                            }
                            else
                            {
                                this.selectedStrokes.Remove(stroke.strokes[0]);
                                break;
                            }
                        }
                    }
                }
            }
        }//selectStrokes

        public void selectBox(Quad2d q)
        {
            if (this.currSegmentClass == null || this.currSegmentClass.segments == null) return;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (seg.boundingbox == null) continue;
                foreach (Vector2d v in seg.boundingbox.points2d)
                {
                    if (this.selectedSeg == seg)
                    {
                        continue;
                    }
                    Vector2d v2 = new Vector2d(v);
                    v2.y = this.Height - v2.y;
                    if (Quad2d.isPointInQuad(v2, q))
                    {
                        this.selectedSeg = seg;
                        break;
                    }
                }
            }
        }//selectBox

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
                this.clearAllStrokes();
                this.Refresh();
                return;
            }
            if (e.Control == true && e.KeyCode == Keys.Z)
            {
                if (this.selectedSeg != null && this.selectedSeg.sketch.Count > 0)
                {
                    this.selectedSeg.sketch.RemoveAt(this.selectedSeg.sketch.Count - 1);
                    this.Refresh();
                    return;
                }
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
                case Keys.S:
                    {
                        this.setUIMode(5); // sketch
                        break;
                    }
                case Keys.E:
                    {
                        this.currUIMode = UIMode.Eraser;
                        break;
                    }
                case Keys.D:
                    {
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
            if (this.currUIMode == UIMode.Sketch || this.currUIMode == UIMode.Eraser)
            {
                double ratio = e.Delta / 100.0;
                if (ratio < 0)
                {
                    ratio = 1.0 / Math.Abs(ratio);
                }
                this.penRadius *= ratio;
                SegmentClass.PenSize = SegmentClass.PenSize * ratio;
                this.Refresh();
            }
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

        //######### Sketch #########//
        
        private void SketchMouseDown(Vector2d p)
        {
            if (this.selectedSeg == null)
            {
                MessageBox.Show("Please select a segment / box to draw your sketch inside of it.");
                this.isMouseDown = false;
                return;
            }
            Vector2d pos = new Vector2d(p.x, this.Height - p.y);
            this.currSketchPoints = new List<Vector2d>();
            this.currSketchPoints.Add(pos);
            this.strokeLength = 0;

            this.lockView = true;
        }

        public void SketchMouseMove(Vector2d p)
        {
            if (!this.isMouseDown) return;
            Vector2d prevpos = this.currSketchPoints[this.currSketchPoints.Count - 1];
            Vector2d pos = new Vector2d(p.x, this.Height - p.y);
            Vector2d off = pos - prevpos;
            double len = off.Length();
            Vector2d offnormal = off.normalize();
            this.strokeLength += len;

            double dis_thres = 6.0;
            if (len > dis_thres) 
            {
                // moving the pen
                int steps = (int)(len / dis_thres) + 1;
                steps = Math.Min(4, steps);
                double delta = len / steps;
                for (int i = 1; i <= steps; ++i)
                {
                    Vector2d v = prevpos + offnormal * (i * delta);
                    this.currSketchPoints.Add(v);
                }
            }
        }

        public void SketchMouseUp()
        {
            if (this.currSketchPoints.Count > 3) {
                bool isLongEnough = strokeLength >= 2;
                if (isLongEnough)
                {
                    //CubicSpline2 spline = new CubicSpline2(this.currSketchPoints.ToArray());
					this.currStroke = new Stroke(this.currSketchPoints, SegmentClass.PenSize);
					this.currStroke.smooth();
					//this.findClosestVertex(this.currSketchPoints);
					//this.currStroke = new Stroke(this.currSketchPoints, SegmentClass.PenSize);
					this.currStroke.TryRectifyToLine();
                    this.currStroke.strokeColor = SegmentClass.PenColor;
                    this.storeSketchStrokePositionToLocalCoord(this.currStroke);
                    this.addADrawStroke(this.currStroke);
                }
            }
            this.currStroke = new Stroke();
            this.currSketchPoints = new List<Vector2d>();
        }//SketchMouseUp

        private void createDrawStroke(Segment seg, Stroke stroke)
        {
            seg.sketch = new List<DrawStroke2d>();
            DrawStroke2d drawStroke = new DrawStroke2d(stroke);
            for (int i = 1; i < drawStroke.strokes.Count; ++i)
            {
                this.storeSketchStrokePositionToLocalCoord(drawStroke.strokes[i]);
            }
            seg.sketch.Add(drawStroke);
        }

        private void addADrawStroke(Stroke stroke)
        {
            DrawStroke2d drawStroke = new DrawStroke2d(stroke);
            for (int i = 1; i < drawStroke.strokes.Count; ++i)
            {
                this.storeSketchStrokePositionToLocalCoord(drawStroke.strokes[i]);
            }
            if (this.selectedSeg != null)
            {
                if (this.selectedSeg.sketch == null)
                {
                    this.selectedSeg.sketch = new List<DrawStroke2d>();
                }
                this.selectedSeg.sketch.Add(drawStroke);
            }
            if (this.selectedSeg.sketch.Count > 4)
            {
                this.saveSketches("");
            }
        }//addADrawStroke

        private void findClosestVertex(List<Vector2d> points3d)
        {
			if (this.contourPoints.Count == 0 && this.sharpEdges.Count == 0) return;
			double thresh = 2.0f;
			for (int i = 0; i < points3d.Count; ++i) 
			{
				Vector2d p = points3d[i];
				//double mind = double.MaxValue;
				//Vector3d closed = 
				foreach (Vector3d v in this.contourPoints)
				{
					Vector2d v2 = this.camera.Project(v).ToVector2d();
					if ((p - v2).Length() < thresh)
					{
						points3d[i] = new Vector2d(v2);
						break;
					}
				}
			}
			//if (this.currSegmentClass == null) return;
			//List<Vector2d> meshPoints2d = new List<Vector2d>();
			//foreach (Segment seg in this.currSegmentClass.segments)
			//{
			//	if (seg.mesh == null) continue;
			//	for (int i = 0, j = 0; i < seg.mesh.VertexCount; ++i, j += 3)
			//	{
			//		Vector3d v = new Vector3d(seg.mesh.VertexPos[j], seg.mesh.VertexPos[j + 1], seg.mesh.VertexPos[j + 2]);
                    
			//	}
			//}
        }

        private void storeSketchStrokePositionToLocalCoord(Stroke stroke)
        {
            Vector2d minCoord = Vector2d.MaxCoord(), maxCoord = Vector2d.MinCoord();
            this.calSegmentsBounds(out minCoord, out maxCoord);
            double wl = maxCoord.x - minCoord.x;
            double hl = maxCoord.y - minCoord.y;
            double wg = this.Width;
            double hg = this.Height;

            // relative _position [0, 1]
            //foreach (Stroke stroke in this.sketchStrokes)
            //{
                foreach (StrokePoint sp in stroke.strokePoints)
                {
                    sp.pos2_local = new Vector2d(
                        (sp.pos2.x - minCoord.x) / wl,
                        (sp.pos2.y - minCoord.y) / hl);
                }
            //}
        }//storeSketchStrokePositionToLocalCoord

        private void updateSketchStrokePositionToLocalCoord()
        {
            if(this.currSegmentClass == null) return;
            Vector2d minCoord = Vector2d.MaxCoord(), maxCoord = Vector2d.MinCoord();
            this.calSegmentsBounds(out minCoord, out maxCoord);
            double wl = maxCoord.x - minCoord.x;
            double hl = maxCoord.y - minCoord.y;
            double wg = this.Width;
            double hg = this.Height;
            // update pos2 origin in loca cooord

            //foreach (Stroke stroke in this.sketchStrokes)
            //{
            //    foreach (StrokePoint sp in stroke.strokePoints)
            //    {
            //        sp.pos2 = new Vector2d(
            //            sp.pos2_local.x * wl,
            //            sp.pos2_local.y * hl);
            //        sp.pos2 += minCoord;
            //    }
            //    stroke.changeStyle2d((int)SegmentClass.strokeStyle);
            //}
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (seg.sketch == null) continue;
                foreach (DrawStroke2d drawStroke in seg.sketch)
                {
                    foreach (Stroke stroke in drawStroke.strokes)
                    {
                        foreach (StrokePoint sp in stroke.strokePoints)
                        {
                            sp.pos2 = new Vector2d(
                                sp.pos2_local.x * wl,
                                sp.pos2_local.y * hl);
                            sp.pos2 += minCoord;
                        }
                        stroke.changeStyle2d((int)SegmentClass.strokeStyle);
                    }
                }
            }
        }//updateSketchStrokePositionToLocalCoord

        private void updateSketchStrokePositionToLocalCoord(Stroke stroke)
        {
            Vector2d minCoord = Vector2d.MaxCoord(), maxCoord = Vector2d.MinCoord();
            this.calSegmentsBounds(out minCoord, out maxCoord);
            double wl = maxCoord.x - minCoord.x;
            double hl = maxCoord.y - minCoord.y;
            double wg = this.Width;
            double hg = this.Height;

            foreach (StrokePoint sp in stroke.strokePoints)
            {
                sp.pos2 = new Vector2d(
                    sp.pos2_local.x * wl,
                    sp.pos2_local.y * hl);
                sp.pos2 += minCoord;
            }
            stroke.changeStyle2d((int)SegmentClass.strokeStyle);

        }

        private List<DrawStroke2d> currEraseStrokes;
        private double penRadius = 8.0f;
        private void selectEraseStroke(Vector2d p)
        {
            this.currEraseStrokes = new List<DrawStroke2d>();
            if (this.currSegmentClass == null) return;
            Vector2d pos = new Vector2d(p.x, this.Height - p.y);

            foreach (Segment seg in this.currSegmentClass.segments)
            {
                foreach (DrawStroke2d drawStroke in seg.sketch)
                {
                    Stroke stroke = drawStroke.strokes[0];
                    foreach (StrokePoint sp in stroke.strokePoints)
                    {
                        if ((sp.pos2 - pos).Length() < this.penRadius)
                        {
                            this.currEraseStrokes.Add(drawStroke);
                            if (!this.editedStrokes.Contains(drawStroke))
                            {
                                editedStrokes.Add(drawStroke);
                            }
                            this.selectedSeg = seg;
                            break;
                        }
                    }
                }
            }
        }//selectEraseStroke

        public void clearAllStrokes()
        {
            this.currStroke = null;
			this.editedStrokes = new List<DrawStroke2d>();
            if (this.currSegmentClass != null)
            {
                foreach (Segment seg in this.currSegmentClass.segments)
                {
                    seg.sketch = new List<DrawStroke2d>();
                }
            }
        }

        private void SplitStrokeByEraser(DrawStroke2d drawStroke, Vector2d pos)
		{
			// split the current stroke under erasing
			double radius = this.penRadius;
			int s = -1, e = -1;
            Stroke stroke = drawStroke.strokes[0];
			int n = stroke.strokePoints.Count;
			for (int i = 0; i < n; ++i )
			{
				StrokePoint p = stroke.strokePoints[i];
				if ((pos - p.pos2).Length() < radius)
				{
					if (s == -1)
						s = i;
				}
				else if (s != -1)
				{
					e = i;
					break;
				}
			}
			if (s == -1 || e == -1)
				return;
			List<StrokePoint> points_left = stroke.strokePoints.GetRange(0, s);
			List<StrokePoint> points_right = stroke.strokePoints.GetRange(e, n - e);
            List<Vector2d> left = new List<Vector2d>();
            List<Vector2d> right = new List<Vector2d>();
            double len_left = 0, len_right = 0;
            for(int i = 0; i < points_left.Count; ++i){
                StrokePoint sp = points_left[i];
                if(i + 1 < points_left.Count){
                len_left += (sp.pos2 - points_left[i+1].pos2).Length();
                }
                left.Add(sp.pos2);
            }
            for(int i = 0; i < points_right.Count; ++i){
                StrokePoint sp = points_right[i];
                if(i + 1 < points_right.Count){
                len_right += (sp.pos2 - points_right[i+1].pos2).Length();
                }
                right.Add(sp.pos2);
            }
			if(len_left > this.penRadius * 2){
                Stroke leftStroke = new Stroke(left, SegmentClass.PenSize);
                this.addADrawStroke(leftStroke);
                
            }
            if(len_right > this.penRadius * 2){
                Stroke rightStroke = new Stroke(right, SegmentClass.PenSize);
                this.addADrawStroke(rightStroke);
            }
			this.selectedSeg.sketch.Remove(drawStroke);
		}//SplitStrokeByEraser

        private void toneStroke(Vector2d pos)
        {
            if (this.currEraseStrokes.Count == 0 || this.selectedSeg == null || this.selectedSeg.sketch == null)
                return;
            double radius = this.penRadius;
            pos = new Vector2d(pos.x, this.Height - pos.y);
            for (int s = 0; s < this.currEraseStrokes.Count; ++s)
            {
                int idx = this.selectedSeg.sketch.IndexOf(this.currEraseStrokes[s]);
                if (idx == -1) continue;
                DrawStroke2d drawStroke = this.selectedSeg.sketch[idx];
				int ns = drawStroke.strokes.Count;
				for (int i = 1; i < ns; ++i)
				{
                    // remove sketchy strokes
                    drawStroke.strokes.RemoveAt(1);
				}
                Stroke stroke = drawStroke.strokes[0];
                // get the set of points3d that is on the radius
                List<int> point_in_radii = new List<int>();
                // endpoint
                double len_to_left = (pos - stroke.strokePoints[0].pos2).Length();
                double len_to_right = (pos - stroke.strokePoints[stroke.strokePoints.Count - 1].pos2).Length();

                bool left_end_in = len_to_left < radius;
                bool right_end_in = len_to_right < radius;

                //if (!left_end_in && !right_end_in)
                //	continue;
                if (!left_end_in && !right_end_in)
                {
                    this.SplitStrokeByEraser(drawStroke, pos);
                    this.currEraseStrokes.RemoveAt(s);
                    --s;
                    continue;
                }

                for (int i = 0; i < stroke.strokePoints.Count; ++i)
                {
                    double len = (pos - stroke.strokePoints[i].pos2).Length();
                    if (len < radius)
                    {
                        point_in_radii.Add(i);
                    }
                }
                // remove if not at endpoint
                if (left_end_in && right_end_in)
                {
                    // select on endpoint
                    int split = -1;
                    for (int i = 0; i < point_in_radii.Count - 1; ++i)
                    {
                        if (point_in_radii[i + 1] - point_in_radii[i] > 1)
                        {
                            split = i;
                        }
                    }
                    if (split >= 0)
                    {
                        if (split > point_in_radii.Count - split - 1)
                        {
                            int n = point_in_radii.Count - split - 1;
                            for (int i = 0; i < n; ++i)
                            {
                                point_in_radii.RemoveAt(point_in_radii.Count - 1);
                            }
                            right_end_in = false;
                        }
                        else
                        {
                            for (int i = 0; i <= split; ++i)
                            {
                                point_in_radii.RemoveAt(0);
                            }
                            left_end_in = false;
                        }
                    }
                }
                if (point_in_radii.Count == 0)
                    continue;
                //bool left_end_in = point_in_radii[0] == 0;
                //bool right_end_in = point_in_radii[point_in_radii.Count - 1] == stroke.strokePoints.Count - 1;

                int left = point_in_radii[0];
                int right = point_in_radii[point_in_radii.Count - 1];

                if (point_in_radii.Count == stroke.strokePoints.Count)
                    stroke.strokePoints.Clear();
                else
                {
                    StrokePoint I = stroke.strokePoints[0];
                    if (left_end_in)
                    {
                        for (int i = 0; i < stroke.strokePoints.Count; ++i)
                        {
                            if (!point_in_radii.Contains(i))
                            {
                                I = stroke.strokePoints[i - 1];
                                break;
                            }
                        }
                    }
                    StrokePoint J = stroke.strokePoints[stroke.strokePoints.Count - 1];
                    if (right_end_in)
                    {
                        for (int i = stroke.strokePoints.Count - 1; i >= 0; --i)
                        {
                            if (!point_in_radii.Contains(i))
                            {
                                J = stroke.strokePoints[i + 1];
                                break;
                            }
                        }
                    }
                    List<StrokePoint> new_stroke_points = new List<StrokePoint>();
                    int old_count = stroke.strokePoints.Count;
                    int new_count = old_count - point_in_radii.Count + 1;
                    for (int i = 0; i < stroke.strokePoints.Count; ++i)
                    {
                        if (!point_in_radii.Contains(i))
                        {
                            new_stroke_points.Add(stroke.strokePoints[i]);
                        }
                    }
                    if (left_end_in)
                    {
                        StrokePoint u = I, v = new_stroke_points[0];
                        Vector2d q = Polygon2D.FindLinesegmentCircleIntersection(u.pos2, v.pos2, pos, this.penRadius);
                        new_stroke_points.Insert(0, new StrokePoint(q));	// at head
						//stroke.strokePoints = new_stroke_points;

                    }
                    if (right_end_in)
                    {
                        StrokePoint u = J, v = new_stroke_points[new_stroke_points.Count - 1];
                        Vector2d q = Polygon2D.FindLinesegmentCircleIntersection(u.pos2, v.pos2, pos, this.penRadius);
                        new_stroke_points.Add(new StrokePoint(q));		// at tail
						//stroke.strokePoints = new_stroke_points;
                    }
                    if (stroke.get2DLength() < this.penRadius * 2)
                    {
                        this.currEraseStrokes.RemoveAt(s);
                        --s;
                        this.selectedSeg.sketch.Remove(drawStroke);
                    }
                    else
                    {
                        stroke.changeStyle2d((int)SegmentClass.strokeStyle);
						//drawStroke = new DrawStroke2d(stroke);
                    }
					
                }
            }// for
        }// tone stroke

		List<DrawStroke2d> editedStrokes = new List<DrawStroke2d>();
        private void EraserMouseDown(Vector2d p, MouseEventArgs e)
        {
			this.editedStrokes.Clear();
            this.selectEraseStroke(p);
            if (this.selectedSeg == null) return;
            if(e.Button == System.Windows.Forms.MouseButtons.Left){
                this.toneStroke(p);
            }else if(e.Button == System.Windows.Forms.MouseButtons.Right){
                foreach (DrawStroke2d stroke in this.currEraseStrokes)
                {
                    this.selectedSeg.sketch.Remove(stroke);
                }
            }
        }

        private void EraserMouseMove(Vector2d p, MouseEventArgs e)
        {
            if (!this.isMouseDown) return;
            this.selectEraseStroke(p);
            if (this.selectedSeg == null) return;
            if(e.Button == System.Windows.Forms.MouseButtons.Left){
                this.toneStroke(p);
            }else if(e.Button == System.Windows.Forms.MouseButtons.Right){
                foreach (DrawStroke2d stroke in this.currEraseStrokes)
                {
                    this.selectedSeg.sketch.Remove(stroke);
                }
            }
            this.currEraseStrokes = new List<DrawStroke2d>();
        }//EraserMouseMove

        private void EraserMouseUp()
        {
			if (!this.isMouseDown) return;
            this.currEraseStrokes = new List<DrawStroke2d>();
			//for(int i = 0; i < this.editedStrokes.Count; ++i) 
			//{
			//	DrawStroke2d drawStroke = this.editedStrokes[i];
			//	int idx = seg.sketch.IndexOf(drawStroke);
			//	if (idx != -1) {
			//		seg.sketch[idx] = new DrawStroke2d(seg.sketch[idx].strokes[0]);
			//	}
			//}
        }//EraserMouseUp

        //######### end- Sketch #########//

        //private void setViewMatrix()
        //{
        //    int w = this.Width;
        //    int h = this.Height;
        //    if (h == 0)
        //    {
        //        h = 1;
        //    }
        //    //this.MakeCurrent();

        //    if (this.zoonIn)
        //    {
        //        Gl.glViewport(0, 0, w * 2, h * 2);
        //    }
        //    else
        //    {
        //        Gl.glViewport(0, 0, w, h);
        //    }

        //    double aspect = (double)w / h;

        //    Gl.glMatrixMode(Gl.GL_PROJECTION);
        //    Gl.glLoadIdentity();
        //    //if (w >= h)
        //    //{
        //    //    Glu.gluOrtho2D(-1.0 * aspect, 1.0 * aspect, -1.0, 1.0);
        //    //}
        //    //else
        //    //{
        //    //    Glu.gluOrtho2D(-1.0, 1.0, -1.0 * aspect, 1.0 * aspect);
        //    //}
        //    Glu.gluPerspective(90, aspect, 0.1, 1000);
            
            
        //    Gl.glMatrixMode(Gl.GL_MODELVIEW);
        //    Gl.glPushMatrix();

        //    if (this.nPointPerspective == 2)
        //    {
        //        float q = 1.5f;
        //        float[] cam = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1,(float)( Math.Sin(q) / this.eye.z), 0.5f, (float)(Math.Cos(q) / this.eye.z), 1 };
        //        Gl.glLoadMatrixf(cam);
        //        //Gl.glLoadIdentity();
        //    }
        //    else
        //    {
        //        Gl.glLoadIdentity();
        //    }
        //    if (this.zoonIn)
        //    {
        //        Glu.gluLookAt(-0.5, -0.5, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);
        //    }
        //    else
        //    {
        //        Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);
        //    }
        //    //this.drawPoints3d(new Vector3d[1] { this.eye }, Color.DarkOrange, 10.0f);
        //    Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
        //    m = Matrix4d.TranslationMatrix(this.objectCenter) * m * Matrix4d.TranslationMatrix(
        //        new Vector3d() - this.objectCenter);
        //    //m[1, 0] = 0;
        //    //m[0, 1] = 0;
        //    //m[1, 1] = 1;
        //    //m[2, 1] = 0;
        //    //m[1, 2] = 0;
        //    //Vector3d v0 = new Vector3d(m[0, 0], m[0, 1], m[0, 2]);
        //    //v0.normalize();
        //    //Vector3d v2 = new Vector3d(m[2, 0], m[2, 1], m[2, 2]).normalize();
        //    //m[0, 0] = v0.x;
        //    //m[1, 0] = v0.y;
        //    //m[2, 0] = v0.z;

        //    //m[0, 2] = v2.x;
        //    //m[1, 2] = v2.y;
        //    //m[2, 2] = v2.z;

        //    Gl.glMatrixMode(Gl.GL_MODELVIEW);

        //    //Gl.glPushMatrix();
        //    Gl.glMultMatrixd(m.Transpose().ToArray());

        //    if (this.zoonIn)
        //    {
        //        Gl.glPushMatrix();
        //        Gl.glTranslated(-0.5, -0.5, 0);
        //    }
        //}

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

            if (this.paperPos != null)
            {
                this.drawPaperBoundary2d();
            }

	
      

            this.drawSketchPoints();

            this.drawSegmentContourSequences();

            this.DrawHighlight2D();

            this.drawSegmentContours();

            //this.drawSketchyLines2D();   
         
            /*****TEST*****/
            //this.drawTest2D();

            //this.DrawLight();            

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            //Gl.glPopMatrix();
        }

        private void drawSketchPoints()
        {
            if (this.currSketchPoints != null)
            {
                this.drawLines2D(this.currSketchPoints, SegmentClass.PenColor, (float)SegmentClass.PenSize * 2);
                //this.drawPoints2d(this.currSketchPoints.ToArray(), SegmentClass.PenColor, (float)SegmentClass.PenSize);
            }
            //foreach (List<Vector2d> points3d in this.sketchPoints)
            //{
            //    //this.drawLines2D(points3d, Color.Black, 4.0f);
            //    this.drawPoints2d(points3d.ToArray(), Color.Black, 4.0f);
            //}
        }

        private void drawSketchStrokes()
        {
            if (!this.showDrawnStroke) return;
            //foreach (Stroke stroke in this.sketchStrokes)
            //{
            //    if (this.drawShadedOrTexturedStroke)
            //    {
            //        this.drawTriMeshShaded2D(stroke, false, this.showOcclusion);
            //    }
            //    else
            //    {
            //        this.drawTriMeshTextured2D(stroke, this.showOcclusion);
            //    }
            //}

            if (this.currStroke != null)
            {
                if (this.drawShadedOrTexturedStroke)
                {
                    this.drawTriMeshShaded2D(this.currStroke, false, this.showOcclusion);
                }
                else
                {
                    this.drawTriMeshTextured2D(this.currStroke, this.showOcclusion);
                }
            }
            if (this.currSegmentClass == null) return;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (seg.sketch == null) continue;
                foreach (DrawStroke2d drawStroke in seg.sketch)
                {
                    ////this.drawTriMeshShaded2D(drawStroke.strokes[0], false, this.showOcclusion);
                    //for (int i = 1; i < drawStroke.strokes.Count; ++i)
                    //{
                    //    Stroke stroke = drawStroke.strokes[i];
                    //    if (this.drawShadedOrTexturedStroke)
                    //    {
                    //        this.drawTriMeshShaded2D(stroke, false, this.showOcclusion);
                    //    }
                    //    else
                    //    {
                    //        this.drawTriMeshTextured2D(stroke, this.showOcclusion);
                    //    }
                    //}
                    this.drawTriMeshShaded2D(drawStroke.strokes[0], false, this.showOcclusion);
                }
            }
        }// drawSketchStrokes

        private void drawSegmentContours()
        {
            if (this.currSegmentClass == null) return;
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (seg.contour == null) continue;
                foreach(Stroke stroke in seg.contour.miniStrokes){
                    if (this.drawShadedOrTexturedStroke)
                    {
                        this.drawTriMeshShaded2D(stroke, false, this.showOcclusion);
                    }
                    else
                    {
                        this.drawTriMeshTextured2D(stroke, this.showOcclusion);
                    }
                }
            }
        }// drawSketchStrokes

        public void saveSketches(string filename)
        {
            if (filename == "")
            {
                filename = this.foldername + "\\contour.sketch";
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int idx = 0;
                foreach (Segment seg in this.currSegmentClass.segments)
                {
                    sw.WriteLine("segment " + idx.ToString());
                    if (seg.sketch == null) continue;
                    int i = 0;
                    foreach (DrawStroke2d drawStroke in seg.sketch)
                    {
                        Stroke stroke = drawStroke.strokes[0];
                        sw.WriteLine("sketch " + i.ToString());
                        sw.WriteLine(stroke.strokePoints.Count().ToString());
                        foreach (StrokePoint sp in stroke.strokePoints)
                        {
                            //sw.WriteLine(sp.pos2.x.ToString() + " " + sp.pos2.y.ToString());
                            sw.WriteLine(sp.pos2_local.x.ToString() + " " + sp.pos2_local.y.ToString());
                        }
                        ++i;
                    }
                    ++idx;
                }
            }
        }//saveSketches

        public void loadSketches(string filename)
        {
            if (this.currSegmentClass == null) return;
            Random rand = new Random();
            if (filename == "")
            {
                filename = this.foldername + "\\contour.sketch";
            }
            if (!File.Exists(filename)) return;
            this.cal2D();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ' };
                Segment currSeg = null;
                foreach (Segment seg in this.currSegmentClass.segments)
                {
                    seg.sketch = new List<DrawStroke2d>();
                }
                Color color = SegmentClass.PenColor;
                while (sr.Peek() > -1)
                {
                    string line = sr.ReadLine();
                    string[] tokens = line.Split(separator);
                    int n = 0;
                    if (tokens.Length > 0 && tokens[0] == "segment")
                    {
                        int segId = Int32.Parse(tokens[1]);
                        currSeg = this.currSegmentClass.segments[segId];
                        currSeg.sketch = new List<DrawStroke2d>();
                        //color = Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255));
                        continue;
                    }
                    if (tokens.Length > 0 && tokens[0] == "sketch")
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(separator);
                        n = Int32.Parse(tokens[0]);
                    }

                    List<Vector2d> points3d = new List<Vector2d>();
                    for (int i = 0; i < n; ++i)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(separator);
                        double x = double.Parse(tokens[0]);
                        double y = double.Parse(tokens[1]);
                        Vector2d p = new Vector2d(x, y);
                        points3d.Add(p);
                    }
                    Stroke stroke = new Stroke(points3d, SegmentClass.PenSize);
                    //stroke.strokeColor = SegmentClass.PenColor;
                    stroke.strokeColor = color;
                    //stroke.changeStyle2d((int)SegmentClass.strokeStyle);
                    this.updateSketchStrokePositionToLocalCoord(stroke);
                    DrawStroke2d drawStroke = new DrawStroke2d(stroke);
                    for (int i = 1; i < drawStroke.strokes.Count; ++i)
                    {
                        this.storeSketchStrokePositionToLocalCoord(drawStroke.strokes[i]);
                    }
                    if (currSeg != null)
                    {
                        currSeg.sketch.Add(drawStroke);
                    }
                }
            }
        }

        private void drawPaperBoundary2d()
        {
            Color c = Color.FromArgb(120,198,121);
            float size = 8.0f;
            Gl.glColor3ub(c.R, c.G, c.B);

            Gl.glPointSize(size);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < this.paperPos.Length; ++i)
            {
                Gl.glVertex2dv(this.paperPos[i].pos2.ToArray());
            }
            Gl.glEnd();

            for (int i = 0; i < 4; ++i)
            {
                //if (i % 2 == 0)
                //{
                //    this.drawLines2D(this.paperPos[i].pos2 - new Vector2d(size / 4, 0), this.paperPosLines[2 * i], c, size);
                //}
                //else
                //{
                //    this.drawLines2D(this.paperPos[i].pos2 + new Vector2d(size / 4, 0), this.paperPosLines[2 * i], c, size);
                //}
                this.drawLines2D(this.paperPos[i].pos2, this.paperPosLines[2 * i], c, size);
                this.drawLines2D(this.paperPos[i].pos2, this.paperPosLines[2 * i + 1], c, size);
            }

        }//drawPaperBoundary2d

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
            this.drawAllMeshes();

            this.drawSegments();

            this.drawContours();

            //drawSegmentContourSequences();            

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
                    //meshclass.renderShaded();
                    if (this.isShowContour())
                    {
                        //this.drawMeshFace(meshclass.Mesh, Color.WhiteSmoke, false);
                        this.drawMeshFace(meshclass.Mesh, SegmentClass.MeshColor, false);
                    }
                    else
                    {
                        this.drawMeshFace(meshclass.Mesh, SegmentClass.MeshColor, true);
                    }
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

        private void drawSegmentContourSequences()
        {
            if (this.currSegmentClass == null || this.showSegmentContourIndex == -1 ||
                this.showSegmentContourIndex > this.currSegmentClass.segments.Count ||
                this.currSegmentClass.segments[this.showSegmentContourIndex].sketch == null)
            {
                return;
            }
            for (int i = 0; i <= this.showSegmentContourIndex; ++i)
            {
                foreach (DrawStroke2d drawStroke in this.currSegmentClass.segments[i].sketch)
                {
                    if (this.showSketchyContour)
                    {
                        foreach (Stroke stroke in drawStroke.strokes)
                        {
                            this.drawTriMeshShaded2D(
                                stroke,
                                i == this.showSegmentContourIndex ? true : false, 
                                this.showOcclusion);
                        }
                    }
                    else if (drawStroke.strokes.Count > 0)
                    {
                        this.drawTriMeshShaded2D(
                            drawStroke.strokes[0], 
                            i == this.showSegmentContourIndex ? true : false, 
                            this.showOcclusion);

                    }
                }
            }
        }

        private void drawHightlightCircles()
        {
            if (!this.showAllGuides) return;
            for (int i = 0; i <= this.showSegmentContourIndex; ++i)
            {
                if (this.currSegmentClass.segments[i].boundingbox.circles != null)
                {
                    foreach (Circle3D circle in this.currSegmentClass.segments[i].boundingbox.circles)
                    {
                        this.drawCircle3D(circle, SegmentClass.VanLineColor);
                    }
                }
            }
        }
   
        private void drawContours()
        {
            // for mesh
            //if (this.isShowContour())
            //{
            //    this.drawContourPoints();
            //}

            if (this.showSharpEdge)
            {
                this.drawSharpEdges();
            }
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthFunc(Gl.GL_ADD);

            if (this.currSegmentClass == null) return;
            // for segment
            float width = 6.0f;
            if (this.showSegSilhouette)
            {
				//this.drawSegmentSilhouette(Color.FromArgb(252, 141, 98), width);
				this.drawSegmentSilhouette(Color.Black, width);
            }

            if (this.showSegContour)
            {
                this.drawSegmentContour(Color.FromArgb(0, 15, 85), width);
            }

            if (this.showSegSuggestiveContour)
            {
                this.drawSegmentSuggestiveContour(Color.FromArgb(231, 138, 195), width);
            }

            if (this.showSegApparentRidge)
            {
                this.drawSegmentApparentRige(SegmentClass.StrokeColor, width);
            }

            if (this.showSegBoundary)
            {
				//this.drawSegmentBoundary(Color.FromArgb(251, 106, 74), width);
				this.drawSegmentBoundary(Color.Black, width);
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }//drawContours

        private bool isShowContour()
        {
            return this.showSegSilhouette || this.showSegContour || this.showSegSuggestiveContour 
                || this.showSegApparentRidge || this.showSegBoundary || this.showSharpEdge;
        }

        private void drawSegments()
        {
            if (this.currSegmentClass == null)
            {
                return;
            }
            
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                if (!seg.active) continue;
                if (this.showMesh)
                {
                    if (this.drawFace)
                    {
                        if (this.isShowContour() || this.showDrawnStroke)
                        {
                            //this.drawMeshFace(seg.mesh, Color.Blue, false);
                            this.drawMeshFace(seg.mesh, SegmentClass.MeshColor, false);
                        }
                        else
                        {
                            //this.drawMeshFace(seg.mesh, seg.color, false);
                            this.drawMeshFace(seg.mesh, SegmentClass.MeshColor, false);
                        }                        
                    }
                    if (this.drawEdge)
                    {
                        this.drawMeshEdge(seg.mesh);
                    }
                }
                if (this.showBoundingbox)
                {
                    this.drawBoundingboxWithEdges(seg.boundingbox, seg.color, Color.Black);
                }
            }
        }//drawSegments

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
            if (this.currSegmentClass == null) return;

            if (this.currUIMode == UIMode.Sketch ||this.currUIMode == UIMode.Eraser)
            {
                DrawCircle2(new Vector2d(this.currMousePos.x, this.Height-this.currMousePos.y), Color.Black, (float)this.penRadius);
            }

            if (this.selectedStrokes != null)
            {
                foreach (Stroke stroke in this.selectedStrokes)
                {
                    this.drawStrok2D(stroke, Color.Red);
                }
            } 
        }

        private void DrawHighlight3D()
        {
            if (this.selectedSeg != null)
            {
                this.drawBoundingboxEdges(this.selectedSeg.boundingbox, ColorSet[4]);
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

        public void drawStrokeMesh3d(Stroke stroke)
        {
            Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            Gl.glColorMaterial(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE);
            //Gl.glEnable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_LIGHT0);
            Gl.glDepthFunc(Gl.GL_LESS);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_NORMALIZE);

            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            Gl.glShadeModel(Gl.GL_SMOOTH);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

            Gl.glColor4ub(stroke.strokeColor.R, stroke.strokeColor.G, stroke.strokeColor.B, stroke.strokeColor.A);

            Gl.glBegin(Gl.GL_TRIANGLES);
            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int vidx1 = stroke.faceIndex[j];
                int vidx2 = stroke.faceIndex[j + 1];
                int vidx3 = stroke.faceIndex[j + 2];
                Gl.glVertex3dv(stroke.meshVertices3d[vidx1].ToArray());
                Gl.glVertex3dv(stroke.meshVertices3d[vidx2].ToArray());
                Gl.glVertex3dv(stroke.meshVertices3d[vidx3].ToArray());
            }
            Gl.glEnd();

            //Gl.glLineWidth(1.0f);
            //Gl.glBegin(Gl.GL_LINES);
            //for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            //{
            //    int vidx1 = stroke.faceIndex[j];
            //    int vidx2 = stroke.faceIndex[j + 1];
            //    int vidx3 = stroke.faceIndex[j + 2];
            //    Gl.glVertex3dv(stroke.meshVertices3d[vidx1].ToArray());
            //    Gl.glVertex3dv(stroke.meshVertices3d[vidx2].ToArray());
            //    Gl.glVertex3dv(stroke.meshVertices3d[vidx2].ToArray());
            //    Gl.glVertex3dv(stroke.meshVertices3d[vidx3].ToArray());
            //    Gl.glVertex3dv(stroke.meshVertices3d[vidx3].ToArray());
            //    Gl.glVertex3dv(stroke.meshVertices3d[vidx1].ToArray());
            //}
            //Gl.glEnd();

            //Gl.glColor3ub(0, 255, 0);
            //Gl.glPointSize(4.0f);
            //Gl.glBegin(Gl.GL_POINTS);
            //foreach (StrokePoint p in stroke.strokePoints)
            //{
            //    Gl.glVertex3dv(p.pos3.ToArray());
            //}
            //Gl.glEnd();

            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_LIGHT0);
            //Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
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

        private void drawStrok2D(Stroke stroke, Color c)
        {
            if (stroke.meshVertices2d == null || stroke.faceIndex == null)
            {
                return;
            }

            Gl.glColor3ub(c.R,c.G,c.B);
            Gl.glPointSize(4.0f);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (StrokePoint p in stroke.strokePoints)
            {
                Gl.glVertex2dv(p.pos2.ToArray());
            }
            Gl.glEnd();

            //Gl.glEnable(Gl.GL_CULL_FACE);
            //Gl.glEnable(Gl.GL_LIGHTING);

            //Gl.glColor3ub(c.R, c.G, c.B);

            Gl.glBegin(Gl.GL_TRIANGLES);
            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int vidx1 = stroke.faceIndex[j];
                int vidx2 = stroke.faceIndex[j + 1];
                int vidx3 = stroke.faceIndex[j + 2];
                Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
            }
            Gl.glEnd();

            Gl.glLineWidth(1.0f);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int vidx1 = stroke.faceIndex[j];
                int vidx2 = stroke.faceIndex[j + 1];
                int vidx3 = stroke.faceIndex[j + 2];
                Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
            }
            Gl.glEnd();

            //Gl.glDisable(Gl.GL_LIGHTING);
            //Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glLineWidth(2.0f);
        }

        private void drawSketchyBoxEdges(Stroke stroke)
        {
            if(stroke.meshVertices2d == null || stroke.faceIndex == null)
            {
                return;
            }

            Gl.glColor3ub(255, 0, 0);
            Gl.glPointSize(4.0f);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (StrokePoint p in stroke.strokePoints)
            {
                Gl.glVertex2dv(p.pos2.ToArray());
            }
            Gl.glEnd();

            //Gl.glEnable(Gl.GL_CULL_FACE);
            //Gl.glEnable(Gl.GL_LIGHTING);

            //Gl.glColor4ub(stroke.strokeColor.R, stroke.strokeColor.G, stroke.strokeColor.B, stroke.strokeColor.A);

            //Gl.glBegin(Gl.GL_TRIANGLES);
            //for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            //{
            //    int vidx1 = stroke.faceIndex[j];
            //    int vidx2 = stroke.faceIndex[j + 1];
            //    int vidx3 = stroke.faceIndex[j + 2];
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
            //}
            //Gl.glEnd();

            //Gl.glLineWidth(1.0f);
            //Gl.glBegin(Gl.GL_LINES);
            //for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            //{
            //    int vidx1 = stroke.faceIndex[j];
            //    int vidx2 = stroke.faceIndex[j + 1];
            //    int vidx3 = stroke.faceIndex[j + 2];
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
            //    Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
            //}
            //Gl.glEnd();

            //Gl.glDisable(Gl.GL_LIGHTING);
            //Gl.glDisable(Gl.GL_CULL_FACE);

            //Gl.glLineWidth(2.0f);
        }

        // vanishing lines associated with the the two vanishing points3d
        int[] vp1 = { 1, 5, 4, 0 };
        int[] vp2 = { 1, 2, 3, 0 };
        private void drawVanishingLines3d(Primitive box)
        {
            switch (box.type)
            {
                case "Plane3D":
                case "Line":
                    drawPlaneVanishingLine3d(box);
                    break;
                case "Box":
                default:
                    drawBoxVanishingLine3d(box);
                    break;
            }
        }

        private void drawBoxVanishingLine3d(Primitive box)
        {
            for (int i = 0; i < vp1.Length; ++i)
            {
                Line3d line = box.vanLines[0][vp1[i]];
                switch (this.vanishinglineDrawType)
                {
                    case 0:
                        this.drawLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    case 1:
                        this.drawDashedLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    default:
                        break;
                }
            }
            for (int i = 0; i < vp2.Length; ++i)
            {
                Line3d line = box.vanLines[1][vp2[i]];
                switch (this.vanishinglineDrawType)
                {
                    case 0:
                        this.drawLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    case 1:
                        this.drawDashedLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    default:
                        break;
                }
            }
        }

        private void drawPlaneVanishingLine3d(Primitive box)
        {
            for (int i = 0; i < box.vanLines[0].Length; ++i)
            {
                Line3d line = box.vanLines[0][i];
                switch (this.vanishinglineDrawType)
                {
                    case 0:
                        this.drawLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    case 1:
                        this.drawDashedLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    default:
                        break;
                }
            }
            for (int i = 0; i < box.vanLines[1].Length; ++i)
            {
                Line3d line = box.vanLines[1][i];
                switch (this.vanishinglineDrawType)
                {
                    case 0:
                        this.drawLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    case 1:
                        this.drawDashedLines3D(line.u3, line.v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    default:
                        break;
                }
            }
        }

        private void drawVanishingLines3d(GuideLine line)
        {
            switch (this.vanishinglineDrawType)
            {
                case 0:
                    {
                        this.drawLines3D(line.vanLines[0][0].u3, line.vanLines[0][0].v3, SegmentClass.VanLineColor, 1.0f);
                        this.drawLines3D(line.vanLines[0][1].u3, line.vanLines[0][1].v3, SegmentClass.VanLineColor, 1.0f);
                        this.drawLines3D(line.vanLines[1][0].u3, line.vanLines[1][0].v3, SegmentClass.VanLineColor, 1.0f);
                        this.drawLines3D(line.vanLines[1][1].u3, line.vanLines[1][1].v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    }
                case 1:
                    {
                        this.drawDashedLines3D(line.vanLines[0][0].u3, line.vanLines[0][0].v3, SegmentClass.VanLineColor, 1.0f);
                        this.drawDashedLines3D(line.vanLines[0][1].u3, line.vanLines[0][1].v3, SegmentClass.VanLineColor, 1.0f);
                        this.drawDashedLines3D(line.vanLines[1][0].u3, line.vanLines[1][0].v3, SegmentClass.VanLineColor, 1.0f);
                        this.drawDashedLines3D(line.vanLines[1][1].u3, line.vanLines[1][1].v3, SegmentClass.VanLineColor, 1.0f);
                        break;
                    }
                default:
                    break;
            }                         
        }

        private void drawVanishingLines2d(Segment seg, Color c, float width)
        {
            Primitive box = seg.boundingbox;
            switch (box.type)
            {
                case "Plane3D":
                case "Line":
                    drawPlaneLines2d(box, c, width);
                    break;
                case "Box":
                default:
                    drawBoxVanishingLines2d(box,c, width);
                    break;
            }
        }

        private void drawBoxVanishingLines2d(Primitive box, Color c, float width)
        {
            if (this.showVanishingRay1)
            {
                for (int i = 0; i < vp1.Length; ++i)
                {
                    Line3d line = box.vanLines[0][vp1[i]];
                    if (!line.active) continue; // if not aligned with one vanishing dir
                    switch (this.vanishinglineDrawType)
                    {
                        case 0:
                            this.drawLines2D(line.u2, line.v2, c, width);
                            break;
                        case 1:
                            this.drawDashedLines2D(line.u2, line.v2, c, width);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (this.showVanishingRay2)
            {
                for (int i = 0; i < vp2.Length; ++i)
                {
                    Line3d line = box.vanLines[1][vp2[i]];
                    if (!line.active) continue; // if not aligned with one vanishing dir
                    switch (this.vanishinglineDrawType)
                    {
                        case 0:
                            this.drawLines2D(line.u2, line.v2, c, width);
                            break;
                        case 1:
                            this.drawDashedLines2D(line.u2, line.v2, c, width);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void drawPlaneLines2d(Primitive box, Color c, float width)
        {
            if (this.showVanishingRay1)
            {
                for (int i = 0; i < box.vanLines[0].Length; ++i)
                {
                    Line3d line = box.vanLines[0][i];
                    if (!line.active) continue; // if not aligned with one vanishing dir
                    switch (this.vanishinglineDrawType)
                    {
                        case 0:
                            this.drawLines2D(line.u2, line.v2, c, width);
                            break;
                        case 1:
                            this.drawDashedLines2D(line.u2, line.v2, c, width);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (this.showVanishingRay2)
            {
                for (int i = 0; i < box.vanLines[1].Length; ++i)
                {
                    Line3d line = box.vanLines[1][i];
                    if (!line.active) continue; // if not aligned with one vanishing dir
                    switch (this.vanishinglineDrawType)
                    {
                        case 0:
                            this.drawLines2D(line.u2, line.v2, c, width);
                            break;
                        case 1:
                            this.drawDashedLines2D(line.u2, line.v2, c, width);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void drawVanishingLines2d(GuideLine line, Color c, float width)
        {
            switch (this.vanishinglineDrawType)
            {
                case 0:
                    {
                        if (this.showVanishingRay1)
                        {
                            if (line.vanLines[0][0].active)
                            {
                                this.drawLines2D(line.vanLines[0][0].u2, line.vanLines[0][0].v2, c, width);
                            }
                            if (line.vanLines[0][1].active)
                            {
                                this.drawLines2D(line.vanLines[0][1].u2, line.vanLines[0][1].v2, c, width);
                            }
                        }
                        if (this.showVanishingRay2)
                        {
                            if (line.vanLines[1][0].active)
                            {
                                this.drawLines2D(line.vanLines[1][0].u2, line.vanLines[1][0].v2, c, width);
								double acos = (line.u2 - line.v2).normalize().Dot((line.vanLines[1][0].u2- line.vanLines[1][0].v2).normalize());
                            }
                            if (line.vanLines[1][1].active)
                            {
                                this.drawLines2D(line.vanLines[1][1].u2, line.vanLines[1][1].v2, c, width);
								double acos = (line.u2 - line.v2).normalize().Dot((line.vanLines[1][0].u2- line.vanLines[1][0].v2).normalize());
                            }
                        }
                        break;
                    }
                case 1:
                    {
                        if (this.showVanishingRay1)
                        {
                            if (line.vanLines[0][0].active)
                            {
                                this.drawDashedLines2D(line.vanLines[0][0].u2, line.vanLines[0][0].v2, c, width);
                            }
                            if (line.vanLines[0][1].active)
                            {
                                this.drawDashedLines2D(line.vanLines[0][1].u2, line.vanLines[0][1].v2, c, width);
                            }
                        }
                        if (this.showVanishingRay2)
                        {
                            if (line.vanLines[1][0].active)
                            {
                                this.drawDashedLines2D(line.vanLines[1][0].u2, line.vanLines[1][0].v2, c, width);
                            }
                            if (line.vanLines[1][1].active)
                            {
                                this.drawDashedLines2D(line.vanLines[1][1].u2, line.vanLines[1][1].v2, c, width);
                            }
                        }
                        break;
                    }
                default:
                    break;
            }   
            
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
                this.drawLines3D(q.points3d[i], q.points3d[(i + 1) % q.points3d.Length], c, (float)SegmentClass.StrokeSize * 1.5f);
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

        public void drawBoundingboxWithEdges(Primitive box, Color planeColor, Color lineColor)
        {
            if (box == null) return;
            if (box.planes != null)
            {
                for (int i = 0; i < box.planes.Length; ++i)
                {
                    this.drawQuad3d(box.planes[i], planeColor);
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        this.drawLines3D(box.planes[i].points3d[j], box.planes[i].points3d[(j + 1) % 4], lineColor, 2.0f);
                    }
                }
            }
            if (box.type == "Line")
            {
                for (int j = 0; j < box.points3d.Length; j += 2)
                {
                    this.drawLines3D(box.points3d[j], box.points3d[j + 1], lineColor, 2.0f);
                }
            }
        }// drawBoundingboxWithEdges

        public void drawBoundingbox(Primitive box, Color c)
        {
            if (box == null || box.planes == null) return;
            for (int i = 0; i < box.planes.Length; ++i)
            {
                this.drawQuad3d(box.planes[i], c);
            }
        }// drawBoundingbox

        public void drawBoundingboxEdges(Primitive box, Color c)
        {
            if (box == null) return;
            if (box.planes != null)
            {
                for (int i = 0; i < box.planes.Length; ++i)
                {
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        this.drawLines3D(box.planes[i].points3d[j], box.planes[i].points3d[(j + 1) % 4], c, 2.0f);
                    }
                }
            }
            if (box.type == "Line")
            {
                for (int j = 0; j < box.points3d.Length; j += 2)
                {
                    this.drawLines3D(box.points3d[j], box.points3d[j+1], c, 2.0f);
                }
            }
        }// drawBoundingboxWithEdges

        public void drawBoundingboxWithoutBlend(Primitive box, Color c)
        {
            if (box == null) return;
            for (int i = 0; i < box.planes.Length; ++i)
            {
                // face
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glColor4ub(c.R, c.G, c.B, c.A);
                Gl.glBegin(Gl.GL_POLYGON);
                for (int j = 0; j < 4; ++j)
                {
                    Gl.glVertex3dv(box.planes[i].points3d[j].ToArray());
                }
                Gl.glEnd();
            }
        }// drawBoundingbox

        

        // draw

        public void drawTriMeshShaded2D(Stroke stroke, bool highlight, bool useOcclusion)
        {
            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);

            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);
            if (iNumSamples == 0)
            {
                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

                Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
                Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_LINE_SMOOTH);
                Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_SMOOTH);
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }
            
            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int vidx1 = stroke.faceIndex[j];
                int vidx2 = stroke.faceIndex[j + 1];
                int vidx3 = stroke.faceIndex[j + 2];

                int ipt = (i + stroke.ncapoints) / 2;
                if (ipt < 0) ipt = 0;
                if (ipt >= stroke.strokePoints.Count) ipt = stroke.strokePoints.Count - 1;
                StrokePoint pt = stroke.strokePoints[ipt];

                byte opa = (byte)(pt.depth * 255);

                if (useOcclusion)
                {
                    opa = opa >= 0 ? opa : pt.opacity;
                    opa = opa < 255 ? opa : pt.opacity;
                }
                else
                    opa = pt.opacity;
                if (!highlight)
                {
                    Gl.glColor4ub(Color.Gray.R, Color.Gray.G, Color.Gray.B, (byte)opa);
                    //Gl.glColor4ub(stroke.strokeColor.R, stroke.strokeColor.G, stroke.strokeColor.B, (byte)opa);
                }
                else
                {
                    Gl.glColor4ub(stroke.strokeColor.R, stroke.strokeColor.G, stroke.strokeColor.B, (byte)opa);
                    //Gl.glColor4ub(GuideLineColor.R, GuideLineColor.G, GuideLineColor.B, (byte)opa);
                }
                Gl.glBegin(Gl.GL_POLYGON);
                Gl.glVertex2dv(stroke.meshVertices2d[vidx1].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx2].ToArray());
                Gl.glVertex2dv(stroke.meshVertices2d[vidx3].ToArray());
                Gl.glEnd();
            }
            
            if (iNumSamples == 0)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }

            Gl.glPopAttrib();
        } // drawTriMeshShaded2D

        public void drawTriMeshTextured2D(Stroke stroke, bool useOcclusion)
        {
            uint tex_id = GLViewer.pencilTextureId;
            switch ((int)SegmentClass.strokeStyle)
            {
                case 3:
                    tex_id = GLViewer.crayonTextureId;
                    break;
                case 4:
                    tex_id = GLViewer.inkTextureId;
                    break;
                case 0:
                default:
                    tex_id = GLViewer.pencilTextureId;
                    break;
            }

            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);

            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);
            if (iNumSamples == 0)
            {
                Gl.glEnable(Gl.GL_POINT_SMOOTH);
                Gl.glEnable(Gl.GL_LINE_SMOOTH);
                Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

                Gl.glDisable(Gl.GL_CULL_FACE);
                Gl.glDisable(Gl.GL_LIGHTING);
                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
                Gl.glShadeModel(Gl.GL_SMOOTH);
                Gl.glDisable(Gl.GL_DEPTH_TEST);

                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendEquation(Gl.GL_ADD);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);

                Gl.glDepthMask(Gl.GL_FALSE);

                Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }

            Gl.glEnable(Gl.GL_TEXTURE_2D);
            //Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, tex_id);

            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int j1 = stroke.faceIndex[j];
                int j2 = stroke.faceIndex[j + 1];
                int j3 = stroke.faceIndex[j + 2];
                Vector2d pos1 = stroke.meshVertices2d[j1];
                Vector2d pos2 = stroke.meshVertices2d[j2];
                Vector2d pos3 = stroke.meshVertices2d[j3];

                int ipt = (i + stroke.ncapoints) / 2;
                if (ipt < 0) ipt = 0;
                if (ipt >= stroke.strokePoints.Count) ipt = stroke.strokePoints.Count - 1;
                StrokePoint pt = stroke.strokePoints[ipt];

                byte opa = (byte)(pt.depth * 255);

                if (useOcclusion)
                {
                    opa = opa >= 0 ? opa : pt.opacity;
                    opa = opa < 255 ? opa : pt.opacity;
                }
                else
                    opa = pt.opacity;

                Gl.glColor4ub(255, 255, 255, (byte)opa);

                Gl.glBegin(Gl.GL_POLYGON);

                if (i % 2 == 1)
                {
                    Gl.glTexCoord2d(0, 0);
                    Gl.glVertex2dv(pos1.ToArray());
                    Gl.glVertex2d(0, 1);
                    Gl.glVertex2dv(pos2.ToArray());
                    Gl.glTexCoord2d(1, 1);
                    Gl.glVertex2dv(pos3.ToArray());
                }
                else
                {
                    Gl.glTexCoord2d(0, 0);
                    Gl.glVertex2dv(pos1.ToArray());
                    Gl.glTexCoord2d(1, 1);
                    Gl.glVertex2dv(pos3.ToArray());
                    Gl.glTexCoord2d(1, 0);
                    Gl.glVertex2dv(pos2.ToArray());
                }

                Gl.glEnd();
            }

            Gl.glDisable(Gl.GL_TEXTURE_2D);

            if (iNumSamples == 0)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                Gl.glDisable(Gl.GL_POINT_SMOOTH);
                Gl.glDisable(Gl.GL_LINE_SMOOTH);
                Gl.glDepthMask(Gl.GL_TRUE);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }
            Gl.glPopAttrib();
        }

        public void drawTriMeshShaded3D(Stroke stroke, bool highlight, bool useOcclusion)
        {
            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);

            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);
            if (iNumSamples == 0)
            {
                //Gl.glEnable(Gl.GL_DEPTH_TEST);
                //Gl.glDepthMask(Gl.GL_FALSE);
                //Gl.glEnable(Gl.GL_CULL_FACE);

                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

                Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
                Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_LINE_SMOOTH);
                Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);


                Gl.glEnable(Gl.GL_BLEND);
                //Gl.glBlendEquation(Gl.GL_ADD);
                //Gl.glBlendFunc(Gl.GL_SRC_ALPHA_SATURATE, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_SMOOTH);          
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }

            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int vidx1 = stroke.faceIndex[j];
                int vidx2 = stroke.faceIndex[j + 1];
                int vidx3 = stroke.faceIndex[j + 2];

                int ipt = (i + stroke.ncapoints) / 2;
                if (ipt < 0) ipt = 0;
                if (ipt >= stroke.strokePoints.Count) ipt = stroke.strokePoints.Count - 1;
                StrokePoint pt = stroke.strokePoints[ipt];

                byte opa = (byte)(pt.depth * 255);

                if (useOcclusion)
                {
                    opa = opa >= 0 ? opa : pt.opacity;
                    opa = opa < 255 ? opa : pt.opacity;
                }
                else
                    opa = pt.opacity;
                if (!highlight)
                {
                    Gl.glColor4ub(stroke.strokeColor.R, stroke.strokeColor.G, stroke.strokeColor.B, (byte)opa);
                }
                else
                {
                    Gl.glColor4ub(GuideLineColor.R, GuideLineColor.G, GuideLineColor.B, (byte)opa);
                }
                //Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glBegin(Gl.GL_POLYGON);
                //Gl.glNormal3dv(stroke.hostPlane.normal.ToArray());
                Gl.glVertex3dv(stroke.meshVertices3d[vidx1].ToArray());
                //Gl.glNormal3dv(stroke.hostPlane.normal.ToArray());
                Gl.glVertex3dv(stroke.meshVertices3d[vidx2].ToArray());
                //Gl.glNormal3dv(stroke.hostPlane.normal.ToArray());
                Gl.glVertex3dv(stroke.meshVertices3d[vidx3].ToArray());
                Gl.glEnd();
            }          


            if (iNumSamples == 0)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                //Gl.glDisable(Gl.GL_CULL_FACE);
                //Gl.glDepthMask(Gl.GL_TRUE);
                //Gl.glDisable(Gl.GL_DEPTH_TEST);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }

            Gl.glPopAttrib();
        }

        public void drawTriMeshTextured3D(Stroke stroke, bool useOcclusion)
        {
            uint tex_id = GLViewer.pencilTextureId;
            switch ((int)SegmentClass.strokeStyle)
            {
                case 3:
                    tex_id = GLViewer.crayonTextureId;
                    break;
                case 4:
                    tex_id = GLViewer.inkTextureId;
                    break;
                case 0:
                default:
                    tex_id = GLViewer.pencilTextureId;
                    break;
            }

            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);

            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);
            if (iNumSamples == 0)
            {
                Gl.glEnable(Gl.GL_POINT_SMOOTH);
                Gl.glEnable(Gl.GL_LINE_SMOOTH);
                Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

                Gl.glDisable(Gl.GL_CULL_FACE);
                Gl.glDisable(Gl.GL_LIGHTING);
                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
                Gl.glShadeModel(Gl.GL_SMOOTH);
                Gl.glDisable(Gl.GL_DEPTH_TEST);

                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendEquation(Gl.GL_ADD);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);

                Gl.glDepthMask(Gl.GL_FALSE);

                Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);
                Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }

            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, tex_id);

            for (int i = 0, j = 0; i < stroke.FaceCount; ++i, j += 3)
            {
                int j1 = stroke.faceIndex[j];
                int j2 = stroke.faceIndex[j + 1];
                int j3 = stroke.faceIndex[j + 2];

                Vector3d pos1 = stroke.meshVertices3d[j1];
                Vector3d pos2 = stroke.meshVertices3d[j2];
                Vector3d pos3 = stroke.meshVertices3d[j3];

                int ipt = (i + stroke.ncapoints) / 2;
                if (ipt < 0) ipt = 0;
                if (ipt >= stroke.strokePoints.Count) ipt = stroke.strokePoints.Count - 1;
                StrokePoint pt = stroke.strokePoints[ipt];

                byte opa = (byte)(pt.depth * 255);

                if (useOcclusion)
                {
                    opa = opa >= 0 ? opa : pt.opacity;
                    opa = opa < 255 ? opa : pt.opacity;
                }
                else
                    opa = pt.opacity;

                Gl.glColor4ub(255, 255, 255, (byte)opa);

                Gl.glBegin(Gl.GL_POLYGON);

                if (i % 2 == 1)
                {
                    Gl.glTexCoord2d(0, 0);
                    Gl.glVertex3d(pos1.x, pos1.y, pos1.z);
                    Gl.glTexCoord2d(0, 1);
                    Gl.glVertex3d(pos2.x, pos2.y, pos2.z);
                    Gl.glTexCoord2d(1, 1);
                    Gl.glVertex3d(pos3.x, pos3.y, pos3.z);
                }
                else
                {
                    Gl.glTexCoord2d(0, 0);
                    Gl.glVertex3d(pos1.x, pos1.y, pos1.z);
                    Gl.glTexCoord2d(1, 1);
                    Gl.glVertex3d(pos3.x, pos3.y, pos3.z);
                    Gl.glTexCoord2d(1, 0);
                    Gl.glVertex3d(pos2.x, pos2.y, pos2.z);
                }

                Gl.glEnd();
            }

            Gl.glDisable(Gl.GL_TEXTURE_2D);

            if (iNumSamples == 0)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                Gl.glDisable(Gl.GL_POINT_SMOOTH);
                Gl.glDisable(Gl.GL_LINE_SMOOTH);
                Gl.glDepthMask(Gl.GL_TRUE);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }
            Gl.glPopAttrib();
        }

        public void drawSegmentContour(Color c, float width)
        {
            //if (this.currSegmentClass == null) return;

            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.contourPoints == null) continue;
            //    for (int i = 0; i < seg.contourPoints.Count - 2; i += 2)
            //    {
            //        Vector3d p1 = seg.contourPoints[i];
            //        Vector3d p2 = seg.contourPoints[i + 1];
            //        this.drawLines3D(p1, p2, c, width);
            //    }
            //}
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.contourPoints == null) continue;
            //    this.drawPoints3d(seg.contourPoints.ToArray(), c, width);
            //}

            foreach (Segment seg in this.currSegmentClass.segments)
            {
                this.drawContours(seg.contourPoints, width, c);
            }

        }//drawSegmentContour

        public void drawSegmentSilhouette(Color c, float width)
        {
            if (this.currSegmentClass == null) return;
            
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.silhouettePoints == null) continue;
            //    for (int i = 0; i < seg.silhouettePoints.Count - 2; i += 2)
            //    {
            //        Vector3d p1 = seg.silhouettePoints[i];
            //        Vector3d p2 = seg.silhouettePoints[i + 1];
            //        this.drawLines3D(p1, p2, c, width);
            //    }
            //}

            //Gl.glBegin(Gl.GL_POINTS);
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.silhouettePoints == null) continue;
            //    this.drawPoints3d(seg.silhouettePoints.ToArray(), c, width);
            //}
            //Gl.glEnd();

            foreach (Segment seg in this.currSegmentClass.segments)
            {
                this.drawContours(seg.silhouettePoints, width, c);
            }

        }//drawSegmentSilhouette

        public void drawSegmentSuggestiveContour(Color c, float width)
        {
            if (this.currSegmentClass == null) return;
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.suggestiveContourPoints == null) continue;
            //    for (int i = 0; i < seg.suggestiveContourPoints.Count - 2; i += 2)
            //    {
            //        Vector3d p1 = seg.suggestiveContourPoints[i];
            //        Vector3d p2 = seg.suggestiveContourPoints[i + 1];
            //        this.drawLines3D(p1, p2, c, width);
            //    }
            //}

            //Gl.glBegin(Gl.GL_POINTS);
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.suggestiveContourPoints == null) continue;
            //    this.drawPoints3d(seg.suggestiveContourPoints.ToArray(), c, width);
            //}
            //Gl.glEnd();

            foreach (Segment seg in this.currSegmentClass.segments)
            {
                this.drawContours(seg.suggestiveContourPoints, width, c);
            }
        }//drawSegmentSuggestiveContour

        public void drawSegmentApparentRige(Color c, float width)
        {
            if (this.currSegmentClass == null) return;

            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.ridgePoints == null) continue;
            //    for (int i = 0; i < seg.ridgePoints.Count - 2; i += 2)
            //    {
            //        Vector3d p1 = seg.ridgePoints[i];
            //        Vector3d p2 = seg.ridgePoints[i + 1];
            //        this.drawLines3D(p1, p2, c, width);
            //    }
            //}
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.ridgePoints == null) continue;
            //    this.drawPoints3d(seg.ridgePoints.ToArray(), c, width);
            //}
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                this.drawContours(seg.ridgePoints, width, c);
            }
        }//drawSegmentApparentRige

        public void drawSegmentBoundary(Color c, float width)
        {
            if (this.currSegmentClass == null) return;

            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.boundaryPoints == null) continue;
            //    for (int i = 0; i < seg.boundaryPoints.Count - 2; i += 2)
            //    {
            //        Vector3d p1 = seg.boundaryPoints[i];
            //        Vector3d p2 = seg.boundaryPoints[i + 1];
            //        this.drawLines3D(p1, p2, c, width);
            //    }
            //}
            //foreach (Segment seg in this.currSegmentClass.segments)
            //{
            //    if (!seg.active || seg.boundaryPoints == null) continue;
            //    this.drawPoints3d(seg.boundaryPoints.ToArray(), c, width);
            //}
            foreach (Segment seg in this.currSegmentClass.segments)
            {
                this.drawContours(seg.boundaryPoints, width, c);
            }
        }//drawSegmentBoundary

        public void drawContours(List<Vector3d> points3d, float width, Color c)
        {
            if (points3d == null) return;

            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthMask(Gl.GL_FALSE);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(width);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);

            for (int i = 0; i < points3d.Count; i++)
            {
                Vector3d p1 = points3d[i];
                Gl.glVertex3dv(p1.ToArray());
            }

            Gl.glEnd();

            Gl.glPointSize(width * 0.5f);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < points3d.Count; i++)
            {
                Vector3d p1 = points3d[i];
                Gl.glVertex3dv(p1.ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDepthMask(Gl.GL_TRUE);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }

        public void drawContourPoints()
        {
             Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthMask(Gl.GL_FALSE);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(5.0f);
			//Gl.glColor3ub(253, 141, 60);
			Gl.glColor3ub(0,0,0);

            for (int i = 0; i < this.contourPoints.Count; i += 2)
            {
                Vector3d p1 = this.contourPoints[i];
                Vector3d p2 = this.contourPoints[i + 1];
                Gl.glBegin(Gl.GL_LINES);
                Gl.glVertex3dv(p1.ToArray());
                Gl.glVertex3dv(p2.ToArray());
                Gl.glEnd();
            }

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < this.suggestiveContourPoints.Count; i++)
            {
                Vector3d p1 = this.suggestiveContourPoints[i];
                Gl.glVertex3dv(p1.ToArray());
            }
            Gl.glEnd();

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < this.silhouettePoints.Count; i++)
            {
                Vector3d p1 = this.silhouettePoints[i];
                Gl.glVertex3dv(p1.ToArray());
            }
            Gl.glEnd();

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < this.apparentRidgePoints.Count; i++)
            {
                Vector3d p1 = this.apparentRidgePoints[i];
                Gl.glVertex3dv(p1.ToArray());
            }
            Gl.glEnd();

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < this.boundaryPoints.Count; i++)
            {
                Vector3d p1 = this.boundaryPoints[i];
                Gl.glVertex3dv(p1.ToArray());
            }

            Gl.glEnd();

            //Gl.glPointSize(6.0f);
            //Gl.glBegin(Gl.GL_POINTS);
            //for (int i = 0; i < this.contourPoints.Count; i++)
            //{
            //    Vector3d p1 = this.contourPoints[i];
            //    Gl.glVertex3dv(p1.ToArray());
            //}


            //for (int i = 0; i < this.suggestiveContourPoints.Count; i++)
            //{
            //    Vector3d p1 = this.suggestiveContourPoints[i];
            //    Gl.glVertex3dv(p1.ToArray());
            //}

            //for (int i = 0; i < this.silhouettePoints.Count; i++)
            //{
            //    Vector3d p1 = this.silhouettePoints[i];
            //    Gl.glVertex3dv(p1.ToArray());
            //}

            //for (int i = 0; i < this.apparentRidgePoints.Count; i++)
            //{
            //    Vector3d p1 = this.apparentRidgePoints[i];
            //    Gl.glVertex3dv(p1.ToArray());
            //}

            //for (int i = 0; i < this.boundaryPoints.Count; i++)
            //{
            //    Vector3d p1 = this.boundaryPoints[i];
            //    Gl.glVertex3dv(p1.ToArray());
            //}

            //Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDepthMask(Gl.GL_TRUE);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }

        public void drawContourLine()
        {
            Gl.glEnable(Gl.GL_DEPTH_TEST);

            if (this.currSegmentClass != null)
            {
                foreach (Segment seg in this.currSegmentClass.segments)
                {
                    if (seg.contours == null) continue;
                    foreach (List<int> vids in seg.contours)
                    {
                        for (int i = 0; i < vids.Count - 1; ++i)
                        {
                            Vector3d p1 = seg.mesh.getVertexPos(vids[i]);
                            Vector3d p2 = seg.mesh.getVertexPos(vids[i + 1]);
                            this.drawLines3D(p1, p2, SegmentClass.StrokeColor, 2.0f);
                        }
                    }
                }
            }
            if (this.currMeshClass != null && this.contourLines != null)
            {
                foreach (List<Vector3d> points3d in this.contourLines)
                {
                    for (int i = 0; i < points3d.Count - 1; ++i)
                    {
                        this.drawLines3D(points3d[i], points3d[i + 1], Color.Blue, 2.0f);
                    }
                }
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }//drawContourLine

        public void drawSharpEdges()
        {
            if (this.sharpEdges == null || this.contourPoints == null)
            {
                return;
            }
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(6.0f);
            Gl.glColor3ub(0, 0, 0);

            for (int i = 0; i < this.contourPoints.Count; i += 2)
            {
                Vector3d p1 = this.contourPoints[i];
                Vector3d p2 = this.contourPoints[i + 1];
                Gl.glBegin(Gl.GL_LINES);
                Gl.glVertex3dv(p1.ToArray());
                Gl.glVertex3dv(p2.ToArray());
                Gl.glEnd();
            }

            Gl.glEnable(Gl.GL_DEPTH_TEST);
            for (int i = 0; i < this.sharpEdges.Count; i += 2)
            {
                Vector3d p1 = this.sharpEdges[i];
                Vector3d p2 = this.sharpEdges[i + 1];
                //this.drawLines3D(p1, p2, Color.Black, 5.0f);
                Gl.glBegin(Gl.GL_LINES);
                Gl.glVertex3dv(p1.ToArray());
                Gl.glVertex3dv(p2.ToArray());
                Gl.glEnd();
            }

            //this.drawPoints3d(this.sharpEdges.ToArray(), Color.Black,  8.0f);

            //Gl.glDisable(Gl.GL_DEPTH_TEST);
        }//drawContourLine


    }// GLViewer
}// namespace
