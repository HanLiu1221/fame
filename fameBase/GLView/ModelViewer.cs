using System;
using System.Drawing;

using Tao.OpenGl;
using Tao.Platform.Windows;

using Component;
using Geometry;

namespace FameBase
{
    class ModelViewer : SimpleOpenGlControl
    {
        public ModelViewer(Model m) 
        {
            this.InitializeComponent();
            this.InitializeContexts();
            _model = m;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Name = "ModelViewer";

            this.ResumeLayout(false);
        }

        // data read from GLViewer
        Model _model;
        Matrix4d _modelView;
        Vector3d _eye = new Vector3d(0, 0, 1.5);

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            this.MakeCurrent();
            draw();
            this.SwapBuffers();
        }// onPaint

        public void setModelViewMatrix(Matrix4d m)
        {
            _modelView = m;
            this.Refresh();
        }

        private void draw()
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

            Glu.gluLookAt(_eye.x, _eye.y, _eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);

            Gl.glPushMatrix();
            Gl.glMultMatrixd(_modelView.Transpose().ToArray());

            drawParts();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
        }

        private void drawParts()
        {
            if (_model == null || _model._PARTS == null)
            {
                return;
            }

            foreach (Part part in _model._PARTS)
            {
                this.drawMeshFace(part._MESH, part._COLOR);
                this.drawBoundingbox(part._BOUNDINGBOX, part._COLOR);
            }
        }//drawParts

        private void drawMeshFace(Mesh m, Color c)
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
            float[] ks = { 0, 0, 0, 0 };
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

            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDepthMask(Gl.GL_TRUE);

            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_LIGHT0);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
        }

        private void drawBoundingbox(Prim box, Color c)
        {
            if (box == null || box._PLANES == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                this.drawQuad3d(box._PLANES[i], c);
            }
        }// drawBoundingbox

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
    }// ModelViewer
}
