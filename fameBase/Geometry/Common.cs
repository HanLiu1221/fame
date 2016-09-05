﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geometry
{
    // Common methods, variables that can be used across methods. 
    public class Common
    {
        public static int _nPrimPoint = 8;
        public static double _thresh = 1e-6;
        public static double _thresh2d = 20;
        public static double _bodyNodeRadius = 0.06;
        public static Random rand = new Random();
        public enum PrimType { Cuboid, Cylinder };
        public enum NodeRelationType { Orthogonal, Parallel, None };
        public Common() { }
    }

    public class Pos
    {
        Vector3d _originPos3d;
        Vector2d _originPos2d;
        public Vector3d _pos3d;
        public Vector2d _pos2d;

        public Pos(Vector3d v)
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

        public void setPos2d(Vector2d v2)
        {
            _originPos2d = new Vector2d(v2);
            _pos2d = new Vector2d(v2);
        }

        public void updatePos2d(Vector2d v2)
        {
            _pos2d = new Vector2d(v2);
        }
    }// Pos

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
    }// CoordinateSystem
}// namespace
