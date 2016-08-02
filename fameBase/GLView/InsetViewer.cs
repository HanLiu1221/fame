using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Tao.OpenGl;
using Tao.Platform.Windows;

//using OpenTK.Graphics.OpenGL;
//using OpenTK.Platform.Windows;

using Component;
using Geometry;

namespace SketchPlatform
{
    public class InsetViewer : SimpleOpenGlControl
    {
        public InsetViewer() 
        {
            InitializeComponent();
            this.AutoMakeCurrent = true;
            this.InitializeContexts();

        }


        private void InitializeComponent() 
        {
            this.SuspendLayout();
            this.Name = "InsetViewer";
            this.ResumeLayout(false);
        }

        /******************** Variables ********************/

        private Primitive activeBox = null;
        private Primitive guideBox = null; // previous
		private List<Primitive> boxes = null;
		private List<Primitive> drawnBoxes = null;
        private Matrix4d modelViewMat = Matrix4d.IdentityMatrix();
        private Vector3d eye = new Vector3d(0,0,1.5);

        public void accModelView(Matrix4d mat, Vector3d eye)
        {
            this.modelViewMat = new Matrix4d(mat);
			this.eye = new Vector3d(eye);
        }

        public void accData(Primitive _box, Primitive guide_box, List<Primitive> drawnBoxes)
        {
            this.activeBox = _box;
            this.guideBox = guide_box;
			this.boxes = new List<Primitive>();
			this.drawnBoxes = drawnBoxes;
        }

		public void accBoxes(List<Primitive> boxes)
		{
			this.activeBox = null;
			this.guideBox = null;
			this.boxes = boxes;
		}

		public void clear()
		{
			this.activeBox = null;
			this.guideBox = null;
			this.boxes = null;
			this.drawnBoxes = null;
		}

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            //base.OnPaint(e);

            this.MakeCurrent();
            this.clearScene();

            this.Draw3D();
			this.Draw2D();

            this.SwapBuffers();
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

        public void Draw3D()
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
            Glu.gluPerspective(75, aspect, 0.1, 1000);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);


            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            //Gl.glPushMatrix();
            Gl.glMultMatrixd(this.modelViewMat.Transpose().ToArray());

			if (this.boxes != null && this.boxes.Count > 0)
			{
				Gl.glEnable(Gl.GL_DEPTH_TEST);
				drawAllBoxes();
			}
			this.drawDrawnBoxes();
			this.drawInsetBox();
			if (this.boxes != null)
			{
				Gl.glDisable(Gl.GL_DEPTH_TEST);
			}
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
        }

        public void Draw2D()
        {
            int w = this.Width, h = this.Height;

            Gl.glViewport(0, 0, w, h);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();

            Glu.gluOrtho2D(0, w, 0, h);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            Gl.glPushMatrix();

			//this.drawVanishingGuide2d();
			// draw rectangle
			this.drawFrame();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
        }

		private void drawAllBoxes()
		{
			if (this.boxes == null) return;

			foreach (Primitive box in this.boxes)
			{
				this.drawBoundingbox(box, Color.White);
				foreach (GuideLine edge in box.edges)
				{
					foreach (Stroke stroke in edge.strokes)
					{
						this.drawLines3D(stroke.u3, stroke.v3, SegmentClass.HiddenColor, (float)stroke.weight);
					}
				}
			}

		}

		private void drawDrawnBoxes()
		{
			if (this.drawnBoxes == null) return;
			foreach (Primitive box in this.drawnBoxes)
			{
				foreach (GuideLine edge in box.edges)
				{
					Stroke stroke = edge.strokes[0];
					this.drawLines3D(stroke.u3, stroke.v3, SegmentClass.HiddenColor, 1f);
				}
			}
		}

		private void drawBoundingbox(Primitive box, Color c)
		{
			if (box == null) return;
			for (int i = 0; i < box.planes.Length; ++i)
			{
				this.drawQuad3d(box.planes[i], c);
			}
		}// drawBoundingbox

        private void drawInsetBox()
        {
            if (this.activeBox == null) return;
            // draw lines here

            foreach (GuideLine edge in this.activeBox.edges)
            {
                foreach (Stroke stroke in edge.strokes)
                {
                    this.drawLines3D(stroke.u3, stroke.v3, SegmentClass.HiddenColor, (float)stroke.weight * 0.6f);
                }
            }

			if (this.activeBox.highlightFaceIndex != -1 && this.activeBox.facesToHighlight != null)
			{
				this.drawQuad3d(this.activeBox.facesToHighlight[this.activeBox.highlightFaceIndex], Color.FromArgb(255,255,191));
				this.drawQuadEdge3d(this.activeBox.facesToHighlight[this.activeBox.highlightFaceIndex], SegmentClass.StrokeColor);
			}

            for (int g = 0; g < this.activeBox.guideLines.Count; ++g)
            {
                foreach (GuideLine line in this.activeBox.guideLines[g])
                {
                    if (!line.active) continue;
                    Color c = GLViewer.GuideLineColor;
                    if (line.isGuide)
                    {
                        c = SegmentClass.GuideLineWithTypeColor;
                        foreach (Stroke stroke in line.strokes)
                        {
                            this.drawLines3D(stroke.u3, stroke.v3, c, (float)SegmentClass.StrokeSize);
                        }
                    }
                    else
                    {
                        c = GLViewer.GuideLineColor;
                        foreach (Stroke stroke in line.strokes)
                        {
                            this.drawLines3D(stroke.u3, stroke.v3, c, (float)SegmentClass.StrokeSize/2);
                        }
                    }
                    
                }
            }

            if (this.guideBox != null)
            {
                foreach (List<GuideLine> lines in this.guideBox.guideLines)
                {
                    foreach (GuideLine line in lines)
                    {
                        if (line.active)
                        {
                            foreach (Stroke stroke in line.strokes)
                            {
                                this.drawLines3D(stroke.u3, stroke.v3, SegmentClass.GuideLineWithTypeColor, (float)stroke.weight);
                            }
                        }
                    }
                }
            }

            
        }//drawInsetBox

        private void drawVanishingGuide2d()
        {
            if (this.activeBox == null) return;
            Color c = SegmentClass.VanLineColor;
            float width = 2.0f;
            for (int g = 0; g < this.activeBox.guideLines.Count; ++g)
            {
                foreach (GuideLine line in this.activeBox.guideLines[g])
                {
                    if (!line.active) continue;
                    this.drawLines2D(line.vanLines[0][0].u2, line.vanLines[0][0].v2, c, width);
                    this.drawLines2D(line.vanLines[0][1].u2, line.vanLines[0][1].v2, c, width);
                    this.drawLines2D(line.vanLines[1][0].u2, line.vanLines[1][0].v2, c, width);
                    this.drawLines2D(line.vanLines[1][1].u2, line.vanLines[1][1].v2, c, width);
                }
            }
        }//drawVanishingGuide2d

        private void drawQuad3d(Plane q, Color c)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            // face
            Gl.glColor4ub(c.R, c.G, c.B, c.A);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex3dv(q.points[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
        }

        private void drawQuadEdge3d(Plane q, Color c)
        {
            for (int i = 0; i < 4; ++i)
            {
                this.drawLines3D(q.points[i], q.points[(i + 1) % q.points.Length], c, (float)SegmentClass.StrokeSize * 1.5f);
            }
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

		private void drawFrame()
		{
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

			Color c = Color.CornflowerBlue;
			float lineWidth = 4.0f;
			Vector2d[] vs = new Vector2d[4];
			vs[0] = new Vector2d();
			vs[1] = vs[0] + new Vector2d(this.Width, 0);
			vs[2] = vs[1] + new Vector2d(0, this.Height);
			vs[3] = vs[0] + new Vector2d(0, this.Height);

			Gl.glLineWidth(lineWidth);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			Gl.glColor3ub(c.R, c.G, c.B);
			for (int i = 0; i < 4; ++i)
			{
				Gl.glVertex2dv(vs[i].ToArray());
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

        
    }// InsetViewer
}
