using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Geometry;

namespace Geometry
{
    public class Polygon
    {
        public static double thresh = 1e-6;

        public Polygon()
        { }

        static public bool isPointInPolygon(Vector2d v, Vector2d[] points)
        {
            bool odd = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++ )
            {
                if ((points[i].y < v.y && points[j].y >= v.y) ||
                    (points[j].y < v.y && points[i].y >= v.y))
                {
                    if (points[i].x + (v.y - points[i].y) / (points[j].y - points[i].y) * (points[j].x - points[i].x) < v.x)
                    {
                        odd = !odd;
                    }
                }
            }
            return odd;
        }

        static public double getRandomDoubleInRange(Random rand, double s, double e)
        {
            return s + (e - s) * rand.NextDouble();
        }

        public static bool PointInPoly(Vector2d p, Vector2d[] points)
        {
            bool c = false;
            int n = points.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((points[i].y > p.y) != (points[j].y > p.y)) &&
                    (p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x))
                    c = !c;
            }
            return c;
        }

        public static bool LinePlaneIntersection(Line3d line, Vector3d planeCenter, Vector3d planeNormal, out Vector3d v)
        {
            Vector3d dir = (line.v3 - line.u3).normalize();
            v = new Vector3d();
            if (Math.Abs(dir.Dot(planeNormal)) < thresh)
            {
                return false; // parallel
            }
            Vector3d v0 = planeCenter;
            Vector3d n = planeNormal;
            Vector3d w = line.u3 - v0;
            double s = n.Dot(w) / (n.Dot(dir));
            s = -s;
            v = line.u3 + s * dir;
            return true;
        }

        public static Vector3d LineIntersectionPoint(Line3d l1, Line3d l2)
        {
            double x1 = l1.u3.x, y1 = l1.u3.y, z1 = l1.u3.z;
            double x2 = l2.u3.x, y2 = l2.u3.y, z3 = l2.u3.z;
            Vector3d d1 = (l1.v3 - l1.u3).normalize();
            Vector3d d2 = (l2.v3 - l2.u3).normalize();
            double dx = x2 - x1, dy = y2 - y1;
            double a1 = d1.x, a2 = d1.y, b1 = d2.x, b2 = d2.y;
            double t2 = (dx * a2 - dy * a1) / (a1 * b2 - b1 * a2);
            double t1 = t1 = (dx + b2 * t2) / a1;

            Vector3d l1_0 = l1.u3 + d1 * t1;
            Vector3d l2_0 = l2.u3 + d2 * t2;

            return l1_0;
        }

        public static Vector2d LineIntersectionPoint2d(Line2d l1, Line2d l2)
        {
            double x1 = l1.u.x, y1 = l1.u.y;
            double x2 = l2.u.x, y2 = l2.u.y;
            Vector2d d1 = (l1.v - l1.u).normalize();
            Vector2d d2 = (l2.v - l2.u).normalize();
            double dx = x2 - x1, dy = y2 - y1;
            double a1 = d1.x, a2 = d1.y, b1 = d2.x, b2 = d2.y;
            double t2 = (dx * a2 - dy * a1) / (a1 * b2 - b1 * a2);
            double t1 = 0;
            if (a1 != 0)
            {
                t1 = (dx + b1 * t2) / a1;
            }
            else
            {
                t1 = (dy + b2 * t2) / a2;
            }
            Vector2d vec1 = l1.u + d1 * t1;
            Vector2d vec2 = l2.u + d2 * t2;
            return vec1;
        }

        public static Vector2d FindPointTolineFootPrint(Vector2d pt, Vector2d u, Vector2d v)
        {
            Vector2d uv = (v - u).normalize();
            if (double.IsNaN(uv.x)) return pt;
            return u + (pt - u).Dot(uv) * uv;
        }

        public static double PointDistToPlane(Vector3d pos, Vector3d center, Vector3d normal)
        {
            double d = (pos - center).Dot(normal) / normal.Length();
            return Math.Abs(d);
        }
        public static bool IsLineSegmentIntersectWithCircle(Vector2d u, Vector2d v, Vector2d c, double radius)
        {
            if ((u - c).Length() < radius || (v - c).Length() < radius) return true;
            Vector2d uv = v - u;
            double r = (c - v).Dot(uv) / (u - v).Dot(uv);
            if (r < 0 || r > 1)
                return false;
            Vector2d p = u * r + (1 - r) * v;
            return (p - c).Length() < radius;
        }

        public static Vector2d FindLinesegmentCircleIntersection(Vector2d u, Vector2d v, Vector2d c, double radii)
        {
            if (!IsLineSegmentIntersectWithCircle(u, v, c, radii))
            {
                return u;
            }
            Vector2d v1 = new Vector2d();
            Vector2d v2 = new Vector2d();
            Vector2d fp = u;
            if ((v - c).Length() > (u - c).Length())
            {
                fp = v;
            }
            // line 
            if (Math.Abs(u.x - v.x) < thresh)
            {
                double x = (u.x + v.x) / 2;
                double y = Math.Sqrt(radii * radii - Math.Pow(x - c.x, 2)) + c.y;
                double ny = -Math.Sqrt(radii * radii - Math.Pow(x - c.x, 2)) + c.y;
                v1 = new Vector2d(x, y);
                v2 = new Vector2d(x, ny);
            }
            else if (Math.Abs(u.y - v.y) < thresh)
            {
                double y = (u.x + v.y) / 2;
                double x = Math.Sqrt(radii * radii - Math.Pow(y - c.y, 2)) + c.x;
                double nx = -Math.Sqrt(radii * radii - Math.Pow(y - c.y, 2)) + c.x;
                v1 = new Vector2d(x, y);
                v2 = new Vector2d(nx, y);
            }
            else
            {
                double k = (u.y - v.y) / (u.x - v.x);
                double b = u.y - k * u.x;
                double constant = c.x * c.x + b * b + c.y * c.y - 2 * b * c.y;
                constant = radii * radii - constant;
                double coef1 = 1 + k * k;
                double coef2 = 2 * k * b - 2 * c.x - 2 * k * c.y;
                coef2 /= coef1;
                constant /= coef1;
                constant += coef2 * coef2 / 4;
                double x = Math.Sqrt(constant) - coef2 / 2;
                double nx = -Math.Sqrt(constant) - coef2 / 2;
                v1 = new Vector2d(x, k * x + b);
                v2 = new Vector2d(nx, k * nx + b);
            }
            if ((v1 - fp).Length() < (v2 - fp).Length())
            {
                return v1;
            }
            else
            {
                return v2;
            }
        }//FindLinesegmentCircleIntersection


    }// Polygon

    public class Quad2d : Polygon
    {
        public Vector2d[] points = new Vector2d[4];
        public Quad2d(Vector2d v1, Vector2d v2)
        {
            points[0] = new Vector2d(v1);
            points[1] = new Vector2d(v2.x, v1.y);
            points[2] = new Vector2d(v2);
            points[3] = new Vector2d(v1.x, v2.y);
        }

        static public bool isPointInQuad(Vector2d v, Quad2d q)
        {
            Vector2d minv = Vector2d.MaxCoord();
            Vector2d maxv = Vector2d.MinCoord();
            for (int i = 0; i < q.points.Length; ++i)
            {
                minv = Vector2d.Min(minv, q.points[i]);
                maxv = Vector2d.Max(maxv, q.points[i]);
            }
            return v.x <= maxv.x && v.x >= minv.x && v.y <= maxv.y && v.y >= minv.y;
        }
    }// Quad2d

    public class Plane
    {
        public Vector3d[] points = null;
        public Vector2d[] points2 = null;
        public Vector3d normal;
        public Vector3d center;
        private int Npoints = 0;
        public double depth = 0;

        public Plane() { }

        public Plane(Vector3d[] vs)
        {
            if (vs == null) return;
            // newly created
            this.Npoints = vs.Length;
            this.points = new Vector3d[Npoints];
            for (int i = 0; i < Npoints; ++i)
            {
                this.points[i] = new Vector3d(vs[i]);
            }
            this.calclulateCenterNormal();
        }

        public Plane(List<Vector3d> vs)
        {
            // copy points
            this.points = vs.ToArray();
            this.Npoints = this.points.Length;
            this.calclulateCenterNormal();
        }

        public Plane(Vector3d center, Vector3d normal)
        {
            this.center = center;
            this.normal = normal;
        }

        private void calclulateCenterNormal()
        {
            if (this.Npoints == 0) return;
            this.center = new Vector3d();
            for (int i = 0; i < this.Npoints; ++i)
            {
                this.center += this.points[i];
            }
            this.center /= this.Npoints;
            Vector3d v1 = (this.points[1] - this.points[0]).normalize();
            Vector3d v2 = (this.points[this.Npoints - 1] - this.points[0]).normalize();
            this.normal = (v1.Cross(v2)).normalize();
        }

        public void Transform(Matrix4d T)
        {
            for (int i = 0; i < this.Npoints; ++i )
            {
                Vector3d v = this.points[i];
                Vector4d v4 = (T * new Vector4d(v, 1));
                double t = T[0, 3];
                this.points[i] = (T * new Vector4d(v, 1)).ToVector3D();
            }
            if (this.Npoints > 0)
            {
                this.calclulateCenterNormal();
            }
            else
            {
                this.center = (T * new Vector4d(this.center, 1)).ToVector3D();
                this.normal = (T * new Vector4d(this.normal, 1)).ToVector3D();
                this.normal.normalize();
            }
        }

        public Object clone()
        {
            Plane cloned = new Plane(this.points);
            return cloned;
        }
    }

    public class Line2d
    {
        public Vector2d u;
        public Vector2d v;

        public Line2d(Vector2d v1, Vector2d v2)
        {
            this.u = v1;
            this.v = v2;
        }
    }//Line2d

    public class Line3d
    {
        public Vector3d u3;
        public Vector3d v3;
        public Vector2d u2;
        public Vector2d v2;
        public bool active = true;

        public Line3d(Vector3d v1, Vector3d v2)
        {
            this.u3 = v1;
            this.v3 = v2;
        }

        public Line3d(Vector2d v1, Vector2d v2)
        {
            this.u2 = v1;
            this.v2 = v2;
        }
    }//Line3d

    public class Triangle
    {
        public Vector3d u;
        public Vector3d v;
        public Vector3d w;

        public Triangle(Vector3d p1, Vector3d p2, Vector3d p3)
        {
            this.u = new Vector3d(p1);
            this.v = new Vector3d(p2);
            this.w = new Vector3d(p3);
        }
    }

    public class Arrow3D
    {
        public Vector3d u;
        public Vector3d v;
        public Vector3d[] points; // curved arrow
        public Triangle cap;
        private double min_dcap = 0.04;
        private double max_dcap = 0.06;
        public bool active = false;
        public Arrow3D(Vector3d p1, Vector3d p2, Vector3d normal)
        {
            this.u = new Vector3d(p1);
            this.v = new Vector3d(p2);
            // triangle
            double d = (p2 - p1).Length() / 10;
            d = Math.Max(d, min_dcap);
            d = Math.Min(d, max_dcap);
            Vector3d lineDir = (p2-p1).normalize();
            Vector3d c = p2 - lineDir * d;
            Vector3d dir = normal.Cross(lineDir).normalize();
			dir = normal;
			if (double.IsNaN(dir.x) || double.IsNaN(dir.y) || double.IsNaN(dir.z))
			{
				dir = normal.Cross(lineDir).normalize();
			}
            double d2 = d * 0.6;
            Vector3d v1 = c + dir * d2;
            Vector3d v2 = c - dir * d2;
            this.cap = new Triangle(v1, p2, v2);
        }
    }

    public class Ellipse3D
    {
        public Vector3d[] points = null;
        public int npoints = 20;

        public Ellipse3D(Vector3d[] pts)
        {
            this.points = pts;
        }

        public Ellipse3D(Vector3d c, Vector3d u, Vector3d v, double a, double b)
        {
            points = new Vector3d[this.npoints];
            double alpha = Math.PI * 2 / this.npoints;
            for (int i = 0; i < this.npoints; ++i)
            {
                double angle = alpha * i;
                Vector3d p = c + a * Math.Cos(angle) * u + b * Math.Sin(angle) * v;
                points[i] = p;
            }
        }

    }// Ellipse3D

    public class Circle3D
    {
        public Vector3d[] points = null;
        public Vector2d[] points2 = null;
        public int npoints = 60;
        public Vector3d center;
        public Vector2d center2;
        public Vector3d normal;
        public double radius;
        public Vector2d[] a; // major axes
        public Vector2d[] b; // minor axes
        public Line3d[] guideLines;
        public Line3d[] angleLines;

        public Circle3D(Vector3d center, Vector3d normal, double radius)
        {
            this.center = center;
            this.normal = normal.normalize();
            this.radius = radius;
            this.samplePoints();
        }

        private void samplePoints()
        {
            Vector3d n = normal;
            Vector3d c = center;
            // calculate a second point
            Vector3d p = new Vector3d();
            if (n.z != 0)
            {
                p = new Vector3d(0, 0, (n.x * c.x + n.y * c.y) / n.z + c.z);
            }
            else if (n.y != 0)
            {
                p = new Vector3d(0, (n.x * c.x + n.z * c.z) / n.y + c.y, 0);
            }
            else
            {
                p = new Vector3d((n.y * c.y + n.z * c.z) / n.x + c.x, 0, 0);
            }
            Vector3d a = (p - center).normalize();
            Vector3d b = (a.Cross(n)).normalize();
            this.points = new Vector3d[npoints];
            for (int i = 0; i < npoints; ++i)
            {
                double angle = i * 2 * Math.PI / npoints;
                this.points[i] = c + (a * Math.Cos(angle) + b * Math.Sin(angle)) * radius;
            }
        }

        public void calAxes()
        {
            double max_dist = double.MinValue;
            double min_dist = double.MaxValue;
            b = new Vector2d[2];
            a = new Vector2d[2];
            foreach (Vector2d v2 in this.points2)
            {
                double dist = (center2 - v2).Length();
                if (dist > max_dist)
                {
                    max_dist = dist;
                    b[0] = v2;
                }
                if (dist < min_dist)
                {
                    min_dist = dist;
                    a[0] = v2;
                }
            }
            // using the vector to find a[1] b[1]
            Vector2d va = (center2 - a[0]).normalize();
            a[1] = center2 + (a[0] - center2).Length() * va;
            // perpendicular b
            Vector2d vb = new Vector2d(-va.y, va.x);
            double blen = (center2 - b[0]).Length();
            b[0] = center2 + blen * vb;
            b[1] = center2 - blen * vb;
            //b[1] = center2 + (b[0] - center2).Length() * (center2 - b[0]).normalize();
        }// calAxes

        public void calGuideLines(Vector2d van1, Vector2d van2, int mode)
        {
            if (mode == 0)
            {
                this.calGuideLines_vanishing(van1, van2);
            }
            else
            {
                this.calGuideLines_perpendicular(van1, van2);
            }
        }
        public void calGuideLines_vanishing(Vector2d van1, Vector2d van2)
        {
            Vector2d vdir1 = (this.center2 - van1).normalize();
            Vector2d vdir2 = (this.center2 - van2).normalize();
            Vector2d circle_van_1 = new Vector2d();
            Vector2d circle_van_2 = new Vector2d(); 
            double min_angle_1 = double.MaxValue;
            double min_angle_2 = double.MaxValue;
            double max_dist = double.MinValue;
            double min_dist = double.MaxValue;
            foreach (Vector2d v2 in this.points2)
            {
                Vector2d v2_d1 = (v2 - van1).normalize();
                Vector2d v2_d2 = (v2 - van2).normalize();
                double ang1 = Math.Abs(Math.Acos(vdir1.Dot(v2_d1)));
                double ang2 = Math.Abs(Math.Acos(vdir2.Dot(v2_d2)));
                if (ang1 < min_angle_1)
                {
                    min_angle_1 = ang1;
                    circle_van_1 = new Vector2d(v2);
                }
                if (ang2 < min_angle_2)
                {
                    min_angle_2 = ang2;
                    circle_van_2 = new Vector2d(v2);
                }
                double dist = (center2 - v2).Length();
                max_dist = max_dist > dist ? max_dist : dist;
                min_dist = min_dist < dist ? min_dist : dist;
            }
            double dist_1 = (this.center2 - circle_van_1).Length();
            double dist_2 = (this.center2 - circle_van_2).Length();
            dist_1 = dist_1 + dist_1 * 0.3;
            dist_2 = dist_2 + dist_2 * 0.3;
            this.guideLines = new Line3d[2];
            if ((this.center2 - van1).Length() < (circle_van_1 - van1).Length())
            {
                this.guideLines[0] = new Line3d(van1, circle_van_1);
            }
            else
            {
                this.guideLines[0] = new Line3d(van1, this.center2 + dist_1 * vdir1);
            }
            if ((this.center2 - van2).Length() < (circle_van_2 - van2).Length())
            {
                this.guideLines[1] = new Line3d(van2, circle_van_2);
            }
            else
            {
                this.guideLines[1] = new Line3d(van2, this.center2 + dist_2 * vdir2);
            }
            this.angleLines = new Line3d[2];
            this.angleLines[0] = null;
            this.angleLines[1] = null;
        }

        public void calGuideLines_perpendicular(Vector2d van1, Vector2d van2)
        {
            Vector2d vdir1 = (this.center2 - van1).normalize();
            Vector2d vdir2 = (this.center2 - van2).normalize();
            Vector2d circle_van_1 = new Vector2d();
            Vector2d circle_van_2 = new Vector2d();
            double min_angle_1 = double.MaxValue;
            double min_angle_2 = double.MaxValue;
            foreach (Vector2d v2 in this.points2)
            {
                Vector2d v2_d1 = (v2 - van1).normalize();
                Vector2d v2_d2 = (v2 - van2).normalize();
                double ang1 = Math.Abs(Math.Acos(vdir1.Dot(v2_d1)));
                double ang2 = Math.Abs(Math.Acos(vdir2.Dot(v2_d2)));
                if (ang1 < min_angle_1)
                {
                    min_angle_1 = ang1;
                    circle_van_1 = new Vector2d(v2);
                }
                if (ang2 < min_angle_2)
                {
                    min_angle_2 = ang2;
                    circle_van_2 = new Vector2d(v2);
                }
            }
            double dist_1 = (this.center2 - circle_van_1).Length();
            double dist_2 = (this.center2 - circle_van_2).Length();
            //dist_1 = dist_1 + dist_1 * 0.1;
            //dist_2 = dist_2 + dist_2 * 0.1;
            Vector2d va = new Vector2d();
            Vector2d vb = new Vector2d();
            this.guideLines = new Line3d[2];
            if (dist_1 < dist_2)
            {
                va = vdir1; // (this.center2 - circle_van_1).normalize();
                this.guideLines[0] = new Line3d(van1, this.center2 + dist_1 * va);
                vb = new Vector2d(-va.y, va.x);
                this.guideLines[1] = new Line3d(center2 + vb * dist_2, center2 - vb * dist_2);
            }
            else
            {
                va = vdir2; // (this.center2 - circle_van_2).normalize();
                this.guideLines[0] = new Line3d(van2, this.center2 + dist_2 * va);
                vb = new Vector2d(-va.y, va.x);
                this.guideLines[1] = new Line3d(center2 + vb * dist_1, center2 - vb * dist_1);
            }
            Vector2d far1 = this.guideLines[1].u2;
            Vector2d far2 = this.guideLines[1].v2;
            double max_dist = double.MinValue;
            for (int i = 0; i < this.points2.Length - 1; ++i)
            {
                for (int j = i + 1; j < this.points2.Length; ++j)
                {
                    double dist = (this.points2[i]-this.points2[j]).Length();
                    if (dist > max_dist)
                    {
                        max_dist = dist;
                        far1 = this.points2[i];
                        far2 = this.points2[j];
                    }
                }
            }
            this.guideLines[1] = new Line3d(far1, far2);

            va = (this.guideLines[0].u2 - this.guideLines[0].v2).normalize();
            vb = (this.guideLines[1].u2 - this.guideLines[1].v2).normalize();

            this.angleLines = new Line3d[2];
            double dis = dist_1 > dist_2 ? dist_1 / 5 : dist_2 / 5;
            Vector2d a1 = center2 + va * dis;
            Vector2d a2 = a1 + vb * dis;
            Vector2d b1 = center2 + vb * dis;
            this.angleLines[0] = new Line3d(a1, a2);
            this.angleLines[1] = new Line3d(b1, a2);
        }
        //public void calGuideLines(Vector2d van1, Vector2d van2)
        //{
        //    // major axes
        //    // minor axes alwaays recede the vanishing point
        //    this.guideLines = new Line3d[2];
        //    Vector2d v1 = (this.a[0] - van1).normalize();
        //    Vector2d v2 = (this.a[1] - van1).normalize();
        //    Vector2d v3 = (this.a[0] - van2).normalize();
        //    Vector2d v4 = (this.a[1] - van2).normalize();

        //    double ang1 = Math.Abs(Math.Acos(v1.Dot(v2)));
        //    double ang2 = Math.Abs(Math.Acos(v3.Dot(v4)));
        //    double ra = (this.a[0] - this.a[1]).Length() / 2;
        //    double rb = (this.b[0] - this.b[1]).Length() / 2;
        //    if (ang1 < ang2)
        //    {
        //        Vector2d va = (this.center2 - van1).normalize();
        //        this.guideLines[0] = new Line3d(van1, this.center2 + ra * va);
        //        Vector2d vb = new Vector2d(-va.y, va.x);
        //        this.guideLines[1] = new Line3d(center2 + vb * rb, center2 - vb * rb);
        //    }
        //    else
        //    {
        //        Vector2d va = (this.center2 - van2).normalize();
        //        this.guideLines[0] = new Line3d(van2, this.center2 + ra * va);
        //        Vector2d vb = new Vector2d(-va.y, va.x);
        //        this.guideLines[1] = new Line3d(center2 + vb * rb, center2 - vb * rb);
        //    }
        //    this.angleLines = new Line3d[2];
        //    double dis = (this.a[0] - this.a[1]).Length() / 5;
        //    Vector2d c = (this.a[0] + this.a[1]) / 2;
        //    Vector2d diry = this.a[0].y > this.a[1].y ?
        //        (this.a[0] - this.a[1]).normalize() :
        //        (this.a[1] - this.a[0]).normalize();
        //    Vector2d dirx = this.b[0].x > this.b[1].x ?
        //        (this.b[0] - this.b[1]).normalize() :
        //        (this.b[1] - this.b[0]).normalize();
        //    Vector2d a1 = c + diry * dis;
        //    Vector2d a2 = a1 + dirx * dis;
        //    Vector2d b1 = c + dirx * dis;
        //    this.angleLines[0] = new Line3d(a1, a2);
        //    this.angleLines[1] = new Line3d(b1, a2);
        //}
    }
}
