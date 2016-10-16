using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geometry
{
    // Common methods, variables that can be used across methods. 
    public class Common
    {
        static public Vector3d uprightVec = new Vector3d(0, 1.0, 0);
        public static int _nCuboidPoint = 8;
        public static double _thresh = 1e-6;
        public static double _minus_thresh = -0.01;
        public static double _thresh2d = 20;
        public static double _deform_thresh_min = 0.1;
        public static double _deform_thresh_max = 4.0;
        public static double _bodyNodeRadius = 0.06;
        public static double _contactPointsize = 0.03;
        public static double _hightlightContactPointsize = 0.04;
        public static double _min_scale = 0.01;
        public static double _max_scale = 3.0;
        public static int _max_edge_contacts = 2; // max number of contacts between two nodes

        public static int _POINT_FEAT_DIM = 3;
        public static int _CURV_FEAT_DIM = 1;
        public static int _PCA_FEAT_DIM = 4;
        public static int _RAY_FEAT_DIM = 2;
        public static int _CONVEXHULL_FEAT_DIM = 2;

        public static Random rand = new Random();
        public enum PrimType { Cuboid, Cylinder };
        public enum NodeRelationType { Orthogonal, Parallel, None };

        public enum Functionality { GROUND_TOUCHING, HUMAN_BACK, HUMAN_HIP, HAND_HOLD, HAND_PLACE, SUPPORT };

        public Common() { }

        public static Vector3d getMaxCoord(List<Vector3d> vecs)
        {
            Vector3d maxv = Vector3d.MinCoord;
            foreach (Vector3d v in vecs)
            {
                maxv = Vector3d.Max(maxv, v);
            }
            return maxv;
        }// getMaxCoord

        public static Vector3d getMinCoord(List<Vector3d> vecs)
        {
            Vector3d minv = Vector3d.MaxCoord;
            foreach (Vector3d v in vecs)
            {
                minv = Vector3d.Min(minv, v);
            }
            return minv;
        }// getMinCoord

        public static bool isValidNumber(double x)
        {
            return (!double.IsNaN(x) && !double.IsInfinity(x));
        }// isValidNumber

        public static double PointDistToPlane(Vector3d pos, Vector3d center, Vector3d normal)
        {
            double d = (pos - center).Dot(normal) / normal.Length();
            return Math.Abs(d);
        }

        public static void switchXYZ_mesh(Mesh mesh, int mode)
        {
            for (int i = 0, j = 0; i < mesh.VertexCount; i++, j += 3)
            {
                double x = mesh.VertexPos[j];
                double y = mesh.VertexPos[j + 1];
                double z = mesh.VertexPos[j + 2];
                if (mode == 1)
                {
                    mesh.setVertextPos(i, new Vector3d(-y, x, z));
                }
                else if (mode == 2)
                {
                    mesh.setVertextPos(i, new Vector3d(-z, y, x));
                }
                else
                {
                    mesh.setVertextPos(i, new Vector3d(x, -z, y));
                }
            }
            mesh.afterUpdatePos();
        }// switchXYZ

        public static bool isRayIntersectTriangle(Vector3d origin, Vector3d ray, Vector3d v1, Vector3d v2, Vector3d v3, out double hitDist)
        {
            bool isHit = false;
            hitDist = 0;
            Vector3d edge1 = v2 - v1;
            Vector3d edge2 = v3 - v1;

            // determinant
            Vector3d directionCrossEdge2 = ray.Cross(edge2);
            double determinant = directionCrossEdge2.Dot(edge1);

            // If the ray is parallel to the triangle plane, there is no collision.
            if (Math.Abs(determinant) < Common._thresh)
            {
                return false;
            }
            double inverseDeterminant = 1.0 / determinant;

            // Calculate the U parameter of the intersection point.
            Vector3d distVector = origin - v1;
            double triangleU = distVector.Dot(directionCrossEdge2);
            triangleU *= inverseDeterminant;

            // Make sure it is inside the triangle.
            if (triangleU < 0 - Common._thresh || triangleU > 1 + Common._thresh)
                return false;

            // Calculate the V parameter of the intersection point.
            Vector3d distanceCrossEdge1 = distVector.Cross(edge1);
            double triangleV = ray.Dot(distanceCrossEdge1);
            triangleV *= inverseDeterminant;

            // Make sure it is inside the triangle.
            if (triangleV < 0 - Common._thresh || triangleU + triangleV > 1 + Common._thresh)
                return false;

            // Compute the distance along the ray to the triangle.
            double rayDistance = edge2.Dot(distanceCrossEdge1);
            rayDistance *= inverseDeterminant;

            // Is the triangle behind the ray origin?
            if (rayDistance < 0)
                return false;

            isHit = true;
            hitDist = rayDistance;

            return isHit;
        }// isRayIntersectTriangle

        public static double[] vectorArrayToDoubleArray(Vector3d[] vecs)
        {
            if (vecs == null)
            {
                return null;
            }
            int n = vecs.Length;
            double[] res = new double[n * 3];
            for (int i = 0; i < n; ++i)
            {
                res[i * 3] = vecs[i].x;
                res[i * 3 + 1] = vecs[i].y;
                res[i * 3 + 2] = vecs[i].z;
            }
            return res;
        }// vectorArrayToDoubleArray

        public static double cutoff(double val, double lower, double upper)
        {
            if (val < lower)
            {
                return lower;
            }
            if (val > upper)
            {
                return upper;
            }
            return val;
        }// cutoff
    }// Common

    public class Contact
    {
        public Vector3d _originPos3d;
        public Vector2d _originPos2d;
        public Vector3d _pos3d;
        public Vector2d _pos2d;

        public Contact(Vector3d v)
        {
            _originPos3d = new Vector3d(v);
            _pos3d = new Vector3d(v);
        }

        public void restoreOriginPos()
        {
            _pos3d = new Vector3d(_originPos3d);
        }

        public void Transform(Matrix4d m)
        {
            _pos3d = (m * new Vector4d(_pos3d, 1)).ToVector3D();
        }

        public void TransformFromOrigin(Matrix4d m)
        {
            _pos3d = (m * new Vector4d(_originPos3d, 1)).ToVector3D();
        }

        public void TransformOrigin(Matrix4d m)
        {
            _originPos3d = (m * new Vector4d(_originPos3d, 1)).ToVector3D();
        }

        public void updateOrigin()
        {
            _originPos3d = new Vector3d(_pos3d);
        }

        public void setPos2d(Vector2d v2)
        {
            _originPos2d = new Vector2d(v2);
            _pos2d = new Vector2d(v2);
        }

        public void updatePos2d(Vector2d v2)
        {
            _pos2d = new Vector2d(v2);
        }
    }// Contact

    public class CoordinateSystem
    {
        public Vector3d origin_o;
        public Vector3d origin_x;
        public Vector3d origin_y;
        public Vector3d origin_z;
        public Vector3d o, x, y, z;
        public Matrix3d XYZFrame = null;
        public Matrix3d originXYZFrame = null;

        public CoordinateSystem(Vector3d o, Vector3d x, Vector3d y, Vector3d z)
        {
            this.origin_o = this.o = o;
            this.origin_x = this.x = x;
            this.origin_y = this.y = y;
            this.origin_z = this.z = z;
            this.originXYZFrame = new Matrix3d(x, y, z);
            this.XYZFrame = new Matrix3d(x, y, z);
        }

        public Vector3d this[int index]
        {
            get
            {
                if (index == 0) return x;
                if (index == 1) return y;
                if (index == 2) return z;
                throw new ArgumentException();
            }
            set
            {
                if (index == 0) x = value;
                if (index == 1) y = value;
                if (index == 2) z = value;
            }
        }

        public Vector3d GetPointLocalCoord(Vector3d p)
        {
            Vector3d po = (p - this.o);
            return new Vector3d(po.Dot(this.x), po.Dot(this.y), po.Dot(this.z));
        }

        public Vector3d GetPointSpaceCoord(Vector3d p)
        {
            return this.XYZFrame * p + this.o;
        }

        public Vector3d PointAtCoord(Vector2d coord)
        {
            return this.XYZFrame * new Vector3d(coord, 0) + this.o;
        }

        public void TransformOld(Matrix4d T)
        {
            this.o = (T * new Vector4d(this.origin_o, 1)).XYZ();
            this.x = (T * new Vector4d(this.origin_x, 0)).XYZ().normalize();
            this.y = (T * new Vector4d(this.origin_y, 0)).XYZ().normalize();
            this.z = (T * new Vector4d(this.origin_z, 0)).XYZ().normalize();
            this.XYZFrame = new Matrix3d(this.x, this.y, this.z);
        }

        public void UpdateOriginFrame()
        {
            this.origin_o = this.o;
            this.origin_x = this.x;
            this.origin_y = this.y;
            this.origin_z = this.z;
            this.originXYZFrame = new Matrix3d(this.XYZFrame);
        }

        public Object Clone()
        {
            CoordinateSystem cs = new CoordinateSystem(new Vector3d(o), new Vector3d(x), new Vector3d(y), new Vector3d(z));
            return cs;
        }
    }// CoordinateSystem
}// namespace
