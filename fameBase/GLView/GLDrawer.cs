﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using Tao.OpenGl;
using Geometry;

namespace FameBase
{
    class GLDrawer
    {
        public static Color[] ColorSet = { Color.FromArgb(203, 213, 232), Color.FromArgb(252, 141, 98),
                                         Color.FromArgb(102, 194, 165), Color.FromArgb(231, 138, 195),
                                         Color.FromArgb(166, 216, 84), Color.FromArgb(251, 180, 174),
                                         Color.FromArgb(204, 235, 197), Color.FromArgb(222, 203, 228),
                                         Color.FromArgb(31, 120, 180), Color.FromArgb(251, 154, 153),
                                         Color.FromArgb(227, 26, 28), Color.FromArgb(252, 141, 98),
                                         Color.FromArgb(166, 216, 84), Color.FromArgb(231, 138, 195),
                                         Color.FromArgb(141, 211, 199), Color.FromArgb(255, 255, 179),
                                         Color.FromArgb(251, 128, 114), Color.FromArgb(179, 222, 105),
                                         Color.FromArgb(188, 128, 189), Color.FromArgb(217, 217, 217)};
        public static Color ModelColor = Color.FromArgb(254, 224, 139);
        public static Color GuideLineColor = Color.FromArgb(116, 169, 207);
        public static Color MeshColor = Color.FromArgb(173, 210, 222);
        public static Color BodyNodeColor = Color.FromArgb(230, 97, 1);
        public static Color SelectedBodyNodeColor = Color.FromArgb(215, 25, 28);
        public static Color BodeyBoneColor = Color.FromArgb(64, 64, 64);//Color.FromArgb(244, 165, 130);
        public static Color BodyColor = Color.FromArgb(60, 171, 217, 233);
        public static Color SelectionColor = Color.FromArgb(231, 138, 195);

        public static int _NSlices = 40;

        public static void drawTriangle(Triangle3D t)
        {
            Gl.glVertex3dv(t.u.ToArray());
            Gl.glVertex3dv(t.v.ToArray());
            Gl.glVertex3dv(t.v.ToArray());
            Gl.glVertex3dv(t.w.ToArray());
            Gl.glVertex3dv(t.w.ToArray());
            Gl.glVertex3dv(t.u.ToArray());
        }

        public static void drawCircle3D(Circle3D e, Color c)
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

        public static void drawCircle2D(Circle3D e, Color c)
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

        public static void drawCylinder(Vector3d u, Vector3d v, double r, Color c)
        {
            Glu.GLUquadric quad = Glu.gluNewQuadric();
            double height = (u - v).Length();
            Vector3d dir = (v - u).normalize();
            double angle = Math.Acos(dir.z / dir.Length()) * 180 / Math.PI;

            drawSphere(u, r, c);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPushMatrix();
            Gl.glTranslated(u.x, u.y, u.z);
            Gl.glRotated(angle, -dir.y, dir.x, 0);
            Glu.gluCylinder(quad, r, r, height, _NSlices, _NSlices);
            Gl.glPopMatrix();

            drawSphere(v, r, c);

            Glu.gluDeleteQuadric(quad); 
        }// drawCylinder

        public static void drawCylinderTransparent(Vector3d u, Vector3d v, double r, Color c)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glDisable(Gl.GL_CULL_FACE);

            Glu.GLUquadric quad = Glu.gluNewQuadric();
            double height = (u - v).Length();
            Vector3d dir = (v - u).normalize();
            double angle = Math.Acos(dir.z / dir.Length()) * 180 / Math.PI;

            drawSphere(u, r, c);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPushMatrix();
            Gl.glTranslated(u.x, u.y, u.z);
            Gl.glRotated(angle, -dir.y, dir.x, 0);
            Glu.gluCylinder(quad, r, r, height, _NSlices, _NSlices);
            Gl.glPopMatrix();

            drawSphere(v, r, c);

            Glu.gluDeleteQuadric(quad);

            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_CULL_FACE);
        }// drawCylinderTransparent

        public static void drawSphere(Vector3d o, double r, Color c)
        {
            Gl.glShadeModel(Gl.GL_SMOOTH);
            Glu.GLUquadric quad = Glu.gluNewQuadric();
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPushMatrix();
            Gl.glTranslated(o.x, o.y, o.z);
            Glu.gluSphere(quad, r / 2, _NSlices, _NSlices);
            Gl.glPopMatrix();
            Glu.gluDeleteQuadric(quad);
        }// drawSphere

        public static void drawEllipseCurve3D(Ellipse3D e)
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

        public static void drawEllipse3D(Ellipse3D e)
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

        public static void drawEllipsoidTransparent(Ellipsoid e, Color c)
        {
            

            Vector3d[] points = e.getFaceVertices();
            Vector3d[] quad = new Vector3d[4];
            for (int i = 0; i < points.Length; i += 4)
            {
                for (int j = 0; j < 4; ++j)
                {
                    quad[j] = points[i + j];
                }
                drawQuad3d(quad, c);
            }
        }// drawEllipsoidTransparent

        public static void drawEllipsoidSolid(Ellipsoid e, Color c)
        {
            Vector3d[] points = e.getFaceVertices();
            Vector3d[] quad = new Vector3d[4];
            for (int i = 0; i < points.Length; i += 4)
            {
                for (int j = 0; j < 4; ++j)
                {
                    quad[j] = points[i + j];
                }
                drawQuad3d(quad, c);
            }
        }// drawEllipsoidSolid

        public static void drawPoints2d(Vector2d[] points3d, Color c, float pointSize)
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

        public static void drawPoints3d(Vector3d[] points3d, Color c, float pointSize)
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

        public static void drawPlane2D(Plane3D plane)
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

        public static void drawLines2D(List<Vector2d> points3d, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glColor3ub(c.R, c.G, c.B);
            for (int i = 0; i < points3d.Count - 1; ++i)
            {
                Gl.glVertex2dv(points3d[i].ToArray());
                Gl.glVertex2dv(points3d[i + 1].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        public static void drawLines2D(Vector2d v1, Vector2d v2, Color c, float linewidth)
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

        public static void drawDashedLines2D(Vector2d v1, Vector2d v2, Color c, float linewidth)
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

        public static void drawLines3D(List<Vector3d> points3d, Color c, float linewidth)
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

        public static void drawLines3D(Vector3d v1, Vector3d v2, Color c, float linewidth)
        {
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glLineWidth(linewidth);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();
        }

        public static void drawDashedLines3D(Vector3d v1, Vector3d v2, Color c, float linewidth)
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

        public static void drawQuadTransparent2d(Quad2d q, Color c)
        {
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex2dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glLineWidth(2.0f);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex2dv(q.points3d[i].ToArray());
                Gl.glVertex2dv(q.points3d[(i + 1) % 4].ToArray());
            }
            Gl.glEnd();
            Gl.glEnable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
        }

        public static void drawQuad3d(Plane3D q, Color c)
        {
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

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
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
        }

        public static void drawQuad3d(Vector3d[] pos, Color c)
        {
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glColor4ub(c.R, c.G, c.B, c.A);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < pos.Length; ++i)
            {
                Gl.glVertex3dv(pos[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
        }

        public static void drawQuadTransparent3d(Vector3d v1, Vector3d v2, Vector3d v3, Vector3d v4, Color c)
        {
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_TRIANGLES);

            Gl.glVertex3d(v1.x, v1.y, v1.z);
            Gl.glVertex3d(v2.x, v2.y, v2.z);
            Gl.glVertex3d(v3.x, v3.y, v3.z);

            Gl.glVertex3d(v3.x, v3.y, v3.z);
            Gl.glVertex3d(v1.x, v1.y, v1.z);
            Gl.glVertex3d(v4.x, v4.y, v4.z);

            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }// drawQuadTransparent3d

        public static void drawQuadTransparent3d(Plane3D q, Color c)
        {
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glDisable(Gl.GL_CULL_FACE);
            // face
            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex3dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }

        public static void drawQuadEdge3d(Plane3D q, Color c)
        {
            for (int i = 0; i < 4; ++i)
            {
                drawLines3D(q.points3d[i], q.points3d[(i + 1) % q.points3d.Length], c, 1.5f);
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

        // draw mesh
        public static void drawMeshFace(Mesh m, Color c, bool useMeshColor)
        {
            if (m == null) return;


            //Gl.glEnable(Gl.GL_POINT_SMOOTH);
            //Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);
            //Gl.glEnable(Gl.GL_LINE_SMOOTH);
            //Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            //Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            //Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
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

            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_NORMALIZE);


            if (useMeshColor)
            {
                Gl.glColor3ub(ModelColor.R, ModelColor.G, ModelColor.B);
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

            //Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            //Gl.glDisable(Gl.GL_LINE_SMOOTH);
            //Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDepthMask(Gl.GL_TRUE);

            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_LIGHT0);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
        }

        public static void drawMeshEdge(Mesh m)
        {
            if (m == null) return;
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glColor3ub(ColorSet[1].R, ColorSet[1].G, ColorSet[1].B);
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

        public static void drawBoundingboxWithEdges(Prim box, Color planeColor, Color lineColor)
        {
            if (box == null) return;
            if (box._PLANES != null)
            {
                for (int i = 0; i < box._PLANES.Length; ++i)
                {
                    drawQuad3d(box._PLANES[i], planeColor);
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        drawLines3D(box._PLANES[i].points3d[j], box._PLANES[i].points3d[(j + 1) % 4], lineColor, 2.0f);
                    }
                }
            }
        }// drawBoundingboxWithEdges

        public static void drawBoundingboxPlanes(Prim box, Color c)
        {
            if (box == null || box._PLANES == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                drawQuadTransparent3d(box._PLANES[i], c);
            }
        }// drawBoundingboxPlanes

        public static void drawBoundingboxEdges(Prim box, Color c)
        {
            if (box == null) return;
            if (box._PLANES != null)
            {
                for (int i = 0; i < box._PLANES.Length; ++i)
                {
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        drawLines3D(box._PLANES[i].points3d[j], box._PLANES[i].points3d[(j + 1) % 4], c, 2.0f);
                    }
                }
            }
        }// drawBoundingboxWithEdges

        public static void drawBoundingboxWithoutBlend(Prim box, Color c)
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
        }// drawBoundingboxPlanes
    }// GLDrawer
}// namespace