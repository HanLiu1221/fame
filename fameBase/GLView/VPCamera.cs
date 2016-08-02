using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Geometry;
using System.IO;

using Emgu.CV;
using Emgu.CV.Structure;

namespace SketchPlatform
{
    public class VPCamera
    {
        private double xlen, ylen, zlen;
        private Matrix3d camera_K = null;
        private Matrix3d camera_R = null;
        private Vector3d camera_t;
        private Matrix3d KR = null;
        private Vector3d Kt;
        private Vector2d focalPoint;
        private int iSInteriorStructure = 1;		// 1 => interior, -1 => outterior

        public static double zNear = 0.1;	// default z-near value
        public static double zFar = 20;	// default z-far value
        public Vector2d h, vx, vy, vz;		// vanishing points
        public Vector2d o, x, y, z;			// first 4 corners
        public Vector2d w, u, v, r;
        public List<Line2d> lineSegments;
        public List<Line2d> xycalibrators;
        public bool calibrated;
        public Vector3d EyePos;				// vpCamera pos
        public Vector3d target;				// look at pos
        public Vector3d upvector = new Vector3d(0, 1, 0);
        public void SwitchCorner()
        {
            this.iSInteriorStructure = this.iSInteriorStructure == 1 ? -1 : 1;
            this.UpdateXYZ();
            this.UpdateWUVRPos();
        }

        private Vector2d world_origin_imgpos;
        private double xProjScale, yProjScale, zProjScale;
        private int wndViewHeight;

        // interface
        public List<Vector2d> adjustablePoints;

        // gl matrices
        public double[] glprojMatrix;
        public double[] glmodelViewMatrix;
        public double[] ballMatrix;
        public double[] currObjTransMatrix;
        public int[] viewport;
        private Matrix4d objectSpaceTransform = Matrix4d.IdentityMatrix();

        public VPCamera()
        {
            this.CreateCalibratingElements();
        }

        public void SetBallMatrix(double[] ballMat)
        {
            this.ballMatrix = ballMat;
        }
        public void SetObjectSpaceTransform(Matrix4d T)
        {
            this.objectSpaceTransform = T;
        }
        public Matrix4d GetObjectSpaceTransform()
        {
            return this.objectSpaceTransform;
        }

        public bool IsPointInViewport(Vector2d point)
        {
            return this.viewport[0] < point.x && point.x < this.viewport[2] &&
                this.viewport[1] < point.y && point.y < this.viewport[3];
        }

        public void Init(Vector2d vx, Vector2d vy, int w, int h)
        {
            this.vx = vx;
            this.vy = vy;
            this.h = new Vector2d(w / 2, h / 2);
            this.vz = ComputeTriangleV3(this.h, vx, vy);
            this.o = new Vector2d(w / 2, h / 2);
            this.xlen = 100;
            this.ylen = 100;
            this.zlen = 100;
            this.UpdateXYZ();
            this.UpdateWUVRPos();

            Random rand = new Random();
            Vector2d u0 = new Vector2d(rand.Next(0, w), rand.Next(0, h));
            this.xycalibrators[0].u = u0;
            this.xycalibrators[0].v = u0 + (this.vx - u0).normalize() * this.xlen;
            Vector2d u1 = new Vector2d(rand.Next(0, w), rand.Next(0, h));
            this.xycalibrators[1].u = u1;
            this.xycalibrators[1].v = u1 + (this.vx - u1).normalize() * this.xlen;
            Vector2d v0 = new Vector2d(rand.Next(0, w), rand.Next(0, h));
            this.xycalibrators[2].u = v0;
            this.xycalibrators[2].v = v0 + (this.vy - v0).normalize() * this.ylen;
            Vector2d v1 = new Vector2d(rand.Next(0, w), rand.Next(0, h));
            this.xycalibrators[3].u = v1;
            this.xycalibrators[3].v = v1 + (this.vy - v1).normalize() * this.ylen;
        }

        public void UpdateFocalPoint(Vector2d fp)
        {
            this.h = fp;
        }

        public void UpdateXYZLengths()
        {
            this.xlen = (this.x - this.o).Length();
            this.ylen = (this.y - this.o).Length();
            this.zlen = (this.z - this.o).Length();
        }
        public void UpdateVp()
        {
            this.vx = ComputeLineIntersectPoint(this.xycalibrators[0].u, this.xycalibrators[0].v,
                this.xycalibrators[1].u, this.xycalibrators[1].v);
            this.vy = ComputeLineIntersectPoint(this.xycalibrators[2].u, this.xycalibrators[2].v,
                this.xycalibrators[3].u, this.xycalibrators[3].v);
            this.vz = ComputeTriangleV3(this.h, this.vx, this.vy);
            this.UpdateXYZ();
            this.UpdateWUVRPos();

            //Console.WriteLine("vpCamera vp: vx(" + 
            //    this.vx.x + ", " + this.vx.y + "), vy(" +
            //    this.vy.x + ", " + this.vy.y + "), vz(" +
            //    this.vz.x + ", " + this.vz.y + ")"
            //    ); 
        }

        public void UpdateWUVRPos()
        {
            // compute the positions of w, u, v, r based on 
            // o, x, y, z, vx, vy, vz
            u = ComputeLineIntersectPoint(vx, z, vz, x);
            v = ComputeLineIntersectPoint(vy, z, vz, y);
            r = ComputeLineIntersectPoint(vy, x, vx, y);
            w = ComputeLineIntersectPoint(vy, u, vx, v);
        }

        public bool Calibrate(int[] viewport, int wndHeight, double camera_height)
        {
            this.viewport = viewport;
            this.wndViewHeight = wndHeight;

            double w = this.viewport[2], h = this.viewport[3];
            double threshold = 50 * Math.Max(w, h);

            // Check if Vanishing Points lie at infinity
            bool[] infchk = new bool[3];
            infchk[0] = (Math.Abs(vx.x) > threshold || Math.Abs(vx.y) > threshold);
            infchk[1] = (Math.Abs(vy.x) > threshold || Math.Abs(vy.y) > threshold);
            infchk[2] = (Math.Abs(vz.x) > threshold || Math.Abs(vz.y) > threshold);

            int chkcount = 0;
            for (int i = 0; i < 3; ++i)
            {
                if (infchk[i]) chkcount++;
            }

            Console.WriteLine("calibrating with " + (3 - chkcount) + " vanishing points ...");

            double f = 0, u0 = 0, v0 = 0; // focal length, principal point
            //None
            if (chkcount == 0)
            {
                double Mats_11 = vy.x + vx.x;
                double Mats_12 = vx.y + vy.y;
                double Mats_13 = vy.x * vx.x + vx.y * vy.y;
                double Mats_21 = vy.x + vz.x;
                double Mats_22 = vy.y + vz.y;
                double Mats_23 = vy.x * vz.x + vy.y * vz.y;
                double Mats_31 = vz.x + vx.x;
                double Mats_32 = vz.y + vx.y;
                double Mats_33 = vz.x * vx.x + vz.y * vx.y;

                double A_11 = Mats_11 - Mats_21; double A_12 = Mats_12 - Mats_22;
                double A_21 = Mats_11 - Mats_31; double A_22 = Mats_12 - Mats_32;
                double b_1 = Mats_13 - Mats_23; double b_2 = Mats_13 - Mats_33;
                double detA = A_11 * A_22 - A_12 * A_21;
                u0 = (A_22 * b_1 - A_12 * b_2) / detA;
                v0 = (A_11 * b_2 - A_21 * b_1) / detA;

                double temp = Mats_11 * u0 + Mats_12 * v0 - Mats_13 - u0 * u0 - v0 * v0;
                if (temp < 0)
                {
                    Console.WriteLine("Calibration failed: focal length negative!!");
                    return false;
                }
                f = Math.Sqrt(temp);

                // the geometric way
                Vector2d cp = ComputeTriangleOrthoCenter(vx, vy, vz);
                double ff = Math.Sqrt(-(vz - cp).Dot(vx - cp));
                Matrix3d K = new Matrix3d();
                K[0, 0] = K[1, 1] = ff;
                K[2, 2] = 1;
                K[0, 2] = cp.x;
                K[1, 2] = cp.y;
                this.focalPoint = cp;

                double S = (new Vector3d(vx - vz, 0).Cross(new Vector3d(vy - vz, 0))).Length();
                double sx = (new Vector3d(vy - cp, 0).Cross(new Vector3d(vz - cp, 0))).Length();
                double sy = (new Vector3d(vx - cp, 0).Cross(new Vector3d(vz - cp, 0))).Length();
                double sz = (new Vector3d(vx - cp, 0).Cross(new Vector3d(vy - cp, 0))).Length();
                double r1 = sx / S;
                double r2 = sy / S;
                double r3 = sz / S;
                Vector3d q1 = new Vector3d((vx - cp) / f, 1) * r1;
                Vector3d q2 = new Vector3d((vy - cp) / f, 1) * r2;
                Vector3d q3 = new Vector3d((vz - cp) / f, 1) * r3;
                q1 = q1.normalize();
                q2 = q2.normalize();
                q3 = q3.normalize();

                Vector3d q33 = q1.Cross(q2).normalize();
                if (q33.Dot(q3) < 0)
                    q3 = (new Vector3d() - q33).normalize();
                else
                    q3 = q33;
                Vector3d q23 = q3.Cross(q1).normalize();
                if (q23.Dot(q2) < 0)
                    q2 = (new Vector3d() - q23).normalize();
                else
                    q2 = q23;


                double e1 = q1.Dot(q2);
                double e2 = q1.Dot(q3);
                double e3 = q2.Dot(q3);
                double ee = e1 + e2 + e3;
                Console.WriteLine("rotational " + ee);

                Matrix3d R = new Matrix3d(q1, q2, q3);

                this.camera_K = K;
                this.camera_R = R;
            }
            else if (chkcount == 1)
            {
                Vector2d v1 = vx, v2 = vy;
                if (infchk[0] == true)
                {
                    v1 = vy;
                    v2 = vz;
                }
                else if (infchk[1] == true)
                {
                    v1 = vx;
                    v2 = vz;
                }
                double r = ((w / 2 - v1.x) * (v2.x - v1.x) + (h / 2 - v1.y) * (v2.y - v1.y)) / (Math.Pow(v2.x - v1.x, 2) + Math.Pow(v2.y - v1.y, 2));
                u0 = v1.x + r * (v2.x - v1.x);
                v0 = v1.y + r * (v2.y - v1.y);
                double temp = u0 * (v1.x + v2.x) + v0 * (v2.y + v1.y) - (v1.x * v2.x + v2.y * v1.y + u0 * u0 + v0 * v0);
                if (temp < 0)
                {
                    Console.WriteLine("Calibration failed: focal length negative!!");
                    return false;
                }
                f = Math.Sqrt(temp);
            }
            if (chkcount == 1)
            {
                Matrix3d K = new Matrix3d();
                K[0, 0] = K[1, 1] = f;
                K[2, 2] = 1;
                K[0, 2] = u0;
                K[1, 2] = v0;
                Matrix3d Q = K.Inverse();
                Vector3d vecx = Q * new Vector3d(vx, 1);
                vecx = vecx.normalize();
                //	if (vx.x < u0)
                //		vecx = new Vector3d()-vecx;
                Vector3d vecz = Q * new Vector3d(vz, 1);
                vecz = vecz.normalize();
                Vector3d vecy = vecz.Cross(vecx).normalize();
                this.camera_R = new Matrix3d(vecx, vecy, vecz);
                this.camera_K = K;
            }

            if (chkcount == 2)
            {
                u0 = w / 2; v0 = h / 2;
                Vector3d vecx = new Vector3d();
                Vector3d vecy = new Vector3d();
                Vector3d vecz = new Vector3d();
                if (infchk[0])
                {
                    vecx = new Vector3d(vx, 0).normalize();
                }
                if (infchk[1])
                {
                    vecy = new Vector3d(vy, 0).normalize();
                    vecy = vecy * -Math.Sign(vecy.y);
                }
                if (infchk[2])
                {
                    vecz = new Vector3d(vz, 0).normalize();
                }

                if (infchk[0] && infchk[1])
                {
                    u0 = vz.x;
                    v0 = vz.y;
                    vecz = vecx.Cross(vecy);
                }
                else if (infchk[1] && infchk[2])
                {
                    u0 = vx.x;
                    v0 = vx.y;
                    vecx = vecy.Cross(vecz);
                }
                else
                {
                    u0 = vy.x;
                    v0 = vy.y;
                    vecy = vecz.Cross(vecx);
                }

                this.camera_R = new Matrix3d(new Vector3d() - vecx, vecy, vecz);
                f = 500;
                Matrix3d K = new Matrix3d();
                K[0, 0] = K[1, 1] = f;
                K[2, 2] = 1;
                K[0, 2] = u0;
                K[1, 2] = v0;
                this.camera_K = K;
            }

            Console.WriteLine("vanishing z = (" + vz.x + " " + vz.y + ")");
            Console.WriteLine("focal point = (" + camera_K[0, 2] + "," + camera_K[1, 2] + ")");

            this.ComputeCameraT(camera_height, this.o);

            this.calibrated = true;

            return true;
        }

        public bool CubeCalibrate(int[] viewport, int wndViewHeight, out Cube cube) // two cases
        {
            cube = null;
            this.viewport = viewport;
            this.wndViewHeight = wndViewHeight;
            double w = viewport[2], h = viewport[3];

            // find the projection matrix
            // (u,x,r,y,v,z)
            Vector2d[] imgpts = new Vector2d[6] {
	            this.u,
	            this.x,
	            this.r,
				this.y,
				this.v,
				this.z,
	        };
            Vector3d[] spacepoints = null;
            spacepoints = new Vector3d[6] { // case 1
				new Vector3d(1,-1,1),
				new Vector3d(1,-1,0),
				new Vector3d(1,1,0),
				new Vector3d(-1,1,0),
				new Vector3d(-1,1,1),
				new Vector3d(-1,-1,1)
			};

            if (spacepoints == null) return false;
            double[,] mat = ComputeProjectionMatrixCV(imgpts, spacepoints);

            // get initial guess from mat M$
            Vector3d m1 = new Vector3d(mat[0, 0], mat[1, 0], mat[2, 0]); // first column
            Vector3d m2 = new Vector3d(mat[0, 1], mat[1, 1], mat[2, 1]); // second column
            Vector3d m3 = new Vector3d(mat[0, 2], mat[1, 2], mat[2, 2]); // third column

            // solve directly
            double u = w / 2, v = h / 2;
            double a1 = m1[0], b1 = m1[1], c1 = m1[2];
            double a2 = m2[0], b2 = m2[1], c2 = m2[2];
            double a3 = m3[0], b3 = m3[1], c3 = m3[2];
            Vector3d b = new Vector3d(-(a1 * a2 + b1 * b2), -(a1 * a3 + b1 * b3), -(a3 * a2 + b3 * b2));
            Matrix3d Q = new Matrix3d(
                new Vector3d(c1 * c2, c1 * c3, c3 * c2),
                new Vector3d(c1 * a2 + a1 * c2, c1 * a3 + a1 * c3, c3 * a2 + a3 * c2),
                new Vector3d(c1 * b2 + b1 * c2, c1 * b3 + b1 * c3, c3 * b2 + b3 * c2)
            );
            Vector3d output = Q.Inverse() * b;
            u = -output[1];
            v = -output[2];
            double f2 = output[0] - u * u - v * v;
            if (f2 < 0)
            {
                Console.WriteLine("focal length^2 < 0!");
                //		return false;
            }
            double f = Math.Sqrt(Math.Abs(f2));
            // output error
            double aa = a1 * a2 + b1 * b2 + c1 * c2 * (u * u + v * v + f * f) + (c1 * a2 + a1 * c2) * (-u) + (c1 * b2 + b1 * c2) * (-v);
            double bb = a1 * a3 + b1 * b3 + c1 * c3 * (u * u + v * v + f * f) + (c1 * a3 + a1 * c3) * (-u) + (c1 * b3 + b1 * c3) * (-v);
            double cc = a3 * a2 + b3 * b2 + c3 * c2 * (u * u + v * v + f * f) + (c3 * a2 + a3 * c2) * (-u) + (c3 * b2 + b3 * c2) * (-v);
            double ee = aa * aa + bb * bb + cc * cc;
            Console.WriteLine("- direct solver error: " + ee + "   - R(", false);

            // compute W
            Matrix3d W = Matrix3d.IdentityMatrix();
            W[2, 2] = u * u + v * v;
            W[0, 2] = W[2, 0] = -u;
            W[1, 2] = W[2, 1] = -v;
            W[2, 2] += f * f;
            W *= (1 / f / f);
            double lambda = Math.Sqrt(m3.Dot(W * m3));
            double l1 = Math.Sqrt(m1.Dot(W * m1)) / lambda;
            double l2 = Math.Sqrt(m2.Dot(W * m2)) / lambda;

            Matrix3d InvK = new Matrix3d();
            InvK[0, 0] = InvK[1, 1] = 1.0 / f;
            InvK[0, 2] = -u / f;
            InvK[1, 2] = -v / f;
            InvK[2, 2] = 1.0;
            Matrix3d M = new Matrix3d(mat);
            Matrix3d F = InvK * M;
            Vector3d r1 = new Vector3d(F[0, 0], F[1, 0], F[2, 0]).normalize();
            Vector3d r2 = new Vector3d(F[0, 1], F[1, 1], F[2, 1]).normalize();
            Vector3d r3 = new Vector3d(F[0, 2], F[1, 2], F[2, 2]).normalize();
            Vector3d t = new Vector3d(F[0, 3], F[1, 3], F[2, 3]) * (1.0 / lambda);

            double e1 = r1.Dot(r2);
            double e2 = r1.Dot(r3);
            double e3 = r2.Dot(r3);

            Console.WriteLine(e1.ToString() + " " + e2.ToString() + " " + e3.ToString() + ")", true);
            Matrix3d K = new Matrix3d();
            K[0, 0] = K[1, 1] = f;
            K[0, 2] = u;
            K[1, 2] = v;
            K[2, 2] = 1.0;
            Matrix3d R = new Matrix3d(r1, r2, r3);

            this.camera_K = K;
            this.camera_R = R;
            this.camera_t = t;
            this.KR = this.camera_K * this.camera_R;
            this.Kt = this.camera_K * this.camera_t;
            this.EyePos = this.GetEyePosition();

            // get the box
            Matrix4d S = Matrix4d.ScalingMatrix(l1, l2, 1);
            Vector3d[] pts3 = new Vector3d[8];
            for (int i = 0; i < 6; ++i)
            {
                Vector3d pos = (S * new Vector4d(spacepoints[i], 0)).ToVector3D();
                pts3[i] = new Vector3d(pos);
            }

            Vector3d x3 = (S * new Vector4d(spacepoints[1], 0)).ToVector3D();
            Vector3d g3 = (S * new Vector4d(spacepoints[2], 0)).ToVector3D();
            Vector3d y3 = (S * new Vector4d(spacepoints[3], 0)).ToVector3D();
            Vector3d z3 = (S * new Vector4d(spacepoints[5], 0)).ToVector3D();
            Vector3d o3 = x3 + y3 - g3;
            Vector3d h3 = z3 - o3;
            Vector3d u3 = x3 + h3;
            Vector3d v3 = y3 + h3;
            Vector3d w3 = g3 + h3;

            Vector3d[] boxpionts = new Vector3d[8]
			{
				new Vector3d(o3), new Vector3d(x3), new Vector3d(g3), new Vector3d(y3),
				new Vector3d(z3), new Vector3d(u3), new Vector3d(w3), new Vector3d(v3)
			};

            cube = new Cube(boxpionts);

            this.calibrated = true;
            return true;
        }
        public void GetGLMatrices(out double[] glprojmatrix, out double[] glmodelviewmatrix, double w, double h, double znear, double zfar)
        {
            double[,] mat = new double[3, 4];
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    mat[i, j] = this.KR[i, j];
                }
            }
            mat[0, 3] = this.Kt[0];
            mat[1, 3] = this.Kt[1];
            mat[2, 3] = this.Kt[2];

            Matrix3d LHC = new Matrix3d();
            LHC[0, 0] = LHC[1, 1] = LHC[2, 2] = -1;
            LHC[0, 0] = 1;
            double[,] icpara = new double[3, 4];
            double[,] trans = new double[3, 4];
            if (arParamDecompMat(mat, icpara, trans) < 0)
            {
                throw new Exception();
            }
            Matrix3d R = new Matrix3d();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    R[i, j] = trans[i, j];
                }
            }
            Matrix3d LHCR = LHC * R;
            Matrix4d modelViewMatrix = new Matrix4d(LHCR);
            modelViewMatrix[0, 3] = trans[0, 3];
            modelViewMatrix[1, 3] = trans[1, 3];
            modelViewMatrix[2, 3] = trans[2, 3];
            modelViewMatrix[1, 3] = modelViewMatrix[1, 3] * (-1);
            modelViewMatrix[2, 3] = modelViewMatrix[2, 3] * (-1);
            modelViewMatrix[3, 3] = 1.0;
            glmodelviewmatrix = modelViewMatrix.Transpose().ToArray();

            Matrix4d H_inv = new Matrix4d();
            H_inv[0, 0] = 2.0 / w;
            H_inv[0, 2] = -1;
            H_inv[1, 1] = -2.0 / h;
            H_inv[1, 2] = 1.0;
            H_inv[3, 2] = 1.0;
            Matrix3d K = new Matrix3d();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    K[i, j] = icpara[i, j] / icpara[2, 2];
                }
            }
            Matrix3d y = K * LHC;
            Matrix4d y_ = new Matrix4d(y);
            Matrix4d result = H_inv * (y_);
            double C_ = -(zfar + znear) / (zfar - znear);
            double D_ = -(2 * zfar * znear) / (zfar - znear);
            result[2, 2] = C_;
            result[2, 3] = D_;
            glprojmatrix = result.Transpose().ToArray();

            this.glmodelViewMatrix = glmodelviewmatrix;
            this.glprojMatrix = glprojmatrix;

        }
        public void GetGLMatrices(double w, double h, double znear, double zfar)
        {
            double[,] mat = new double[3, 4];
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    mat[i, j] = this.KR[i, j];
                }
            }
            mat[0, 3] = this.Kt[0];
            mat[1, 3] = this.Kt[1];
            mat[2, 3] = this.Kt[2];

            Matrix3d LHC = new Matrix3d();
            LHC[0, 0] = LHC[1, 1] = LHC[2, 2] = -1;
            LHC[0, 0] = 1;
            double[,] icpara = new double[3, 4];
            double[,] trans = new double[3, 4];
            if (arParamDecompMat(mat, icpara, trans) < 0)
            {
                throw new Exception();
            }
            Matrix3d R = new Matrix3d();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    R[i, j] = trans[i, j];
                }
            }
            Matrix3d LHCR = LHC * R;
            Matrix4d modelViewMatrix = new Matrix4d(LHCR);
            modelViewMatrix[0, 3] = trans[0, 3];
            modelViewMatrix[1, 3] = trans[1, 3];
            modelViewMatrix[2, 3] = trans[2, 3];
            modelViewMatrix[1, 3] = modelViewMatrix[1, 3] * (-1);
            modelViewMatrix[2, 3] = modelViewMatrix[2, 3] * (-1);
            modelViewMatrix[3, 3] = 1.0;
            this.glmodelViewMatrix = modelViewMatrix.Transpose().ToArray();

            Matrix4d H_inv = new Matrix4d();
            H_inv[0, 0] = 2.0 / w;
            H_inv[0, 2] = -1;
            H_inv[1, 1] = -2.0 / h;
            H_inv[1, 2] = 1.0;
            H_inv[3, 2] = 1.0;
            Matrix3d K = new Matrix3d();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    K[i, j] = icpara[i, j] / icpara[2, 2];
                }
            }
            Matrix3d y = K * LHC;
            Matrix4d y_ = new Matrix4d(y);
            Matrix4d result = H_inv * (y_);
            double C_ = -(zfar + znear) / (zfar - znear);
            double D_ = -(2 * zfar * znear) / (zfar - znear);
            result[2, 2] = C_;
            result[2, 3] = D_;
            this.glprojMatrix = result.Transpose().ToArray();

            // get glu look at parameters
            double[] vmat = this.glmodelViewMatrix;
            Vector3d forward = new Vector3d(vmat[2], vmat[6], vmat[10]);
            Vector3d up = new Vector3d(vmat[1], vmat[5], vmat[9]);
            this.target = this.EyePos - forward.normalize();
            this.upvector = up;

        }

        // get vpCamera position and diretion
        public Vector3d GetEyePosition()
        {
            return new Vector3d() - this.camera_R.Transpose() * this.camera_t;
        }
        public Vector3d GetDirection()
        {
            // the extrinsic parameters tells how to convert world coordinates into vpCamera coordinates
            // http://ksimek.github.io/2012/08/22/extrinsic/
            // the original viewing direction in world coordinate is (0,0,-1)
            // e.g. R*C+t = 0  is converting vpCamera world coordinates to vpCamera coordinates
            // so to convert a vpCamera coordinate p back to world, one can simply apply
            // R^-1*(p-t), as for vectors R^-1*v
            Vector3d camera_world_viewing = new Vector3d(0, 0, -1);
            return this.camera_R.Transpose() * camera_world_viewing;
        }

        // determine a line w.r.t. the vanishing points
        public int FindLineVanishingPoint(Vector2d u, Vector2d v)
        {
            Vector2d[] vanishing_pts = new Vector2d[3] { this.vx, this.vy, this.vz };
            int t = -1; double min_angle = double.MaxValue;
            for (int i = 0; i < 3; ++i)
            {
                Vector2d o = vanishing_pts[i];
                double angle = this.ComputeTriAngle(u, o, v);
                if (angle < min_angle)
                {
                    min_angle = angle;
                    t = i;
                }
            }
            return t;
        }
        public double ComputeTriAngle(Vector2d u, Vector2d o, Vector2d v)
        {
            Vector2d uo = (u - o).normalize();
            Vector2d vo = (v - o).normalize();
            double cos = uo.Dot(vo);
            double angle = Math.Acos(cos);
            if (double.IsNaN(angle)) return 0;
            return angle;
        }
        public int FindLineVanishingDir(Line2d line)
        {
            // line (a,b) -- y = ax + b
            Vector2d line_dir = (line.v - line.u).normalize();
            Vector2d line_ctr = (line.v + line.u) / 2;
            Vector2d[] vanishing_dirs = new Vector2d[3] { 
				(this.vx - line_ctr).normalize(), 
				(this.vy - line_ctr).normalize(), 
				(this.vz - line_ctr).normalize() 
			};
            int t = 0; double min_angle = double.MaxValue;
            for (int i = 0; i < 3; ++i)
            {
                Vector2d o = vanishing_dirs[i];
                double angle = Math.Acos(Math.Abs(o.Dot(line_dir)));
                if (angle < min_angle)
                {
                    min_angle = angle;
                    t = i;
                }
            }
            return t;
        }

        // interfaces
        public Cube ComputeRoomBox3DCoord()
        {
            Vector3d x3 = this.Compute3DCoordinate(this.x, 0);
            Vector3d y3 = this.Compute3DCoordinate(this.y, 0);
            Vector3d r3 = this.Compute3DCoordinate(this.r, 0);
            Vector3d o3 = new Vector3d();

            Vector2d oo = this.Compute2D(o3);
            double error = (oo - this.o).Length();
            Console.WriteLine("origin error = " + error);

            Vector2d xx = this.Compute2D(x3);
            error = (xx - this.x).Length();
            Console.WriteLine("x error = " + error);

            Vector2d yy = this.Compute2D(y3);
            error = (yy - this.y).Length();
            Console.WriteLine("y error = " + error);

            Vector2d rr = this.Compute2D(r3);
            error = (rr - this.r).Length();
            Console.WriteLine("r error = " + error);

            Vector3d z3 = this.Compute3DCoordinate(this.z, this.o);
            Vector3d h3 = z3 - o3;
            Vector3d u3 = x3 + h3;
            Vector3d v3 = y3 + h3;
            Vector3d w3 = r3 + h3;

            Vector3d[] boxpionts = new Vector3d[8]
			{
				new Vector3d(o3), new Vector3d(x3), new Vector3d(r3), new Vector3d(y3),
				new Vector3d(z3), new Vector3d(u3), new Vector3d(w3), new Vector3d(v3)
			};

            return new Cube(boxpionts);

        }
        public void Read(string file)
        {
            StreamReader sr = new StreamReader(file);
            char[] delimiters = { ' ', '\t' };
            string s = "";

            while (sr.Peek() > -1)
            {
                s = sr.ReadLine();
                string[] tokens = s.Split(delimiters);

                int j = 0;
                this.o.x = double.Parse(tokens[j++]); this.o.y = double.Parse(tokens[j++]);
                this.x.x = double.Parse(tokens[j++]); this.x.y = double.Parse(tokens[j++]);
                this.y.x = double.Parse(tokens[j++]); this.y.y = double.Parse(tokens[j++]);
                this.z.x = double.Parse(tokens[j++]); this.z.y = double.Parse(tokens[j++]);
                this.w.x = double.Parse(tokens[j++]); this.w.y = double.Parse(tokens[j++]);
                this.u.x = double.Parse(tokens[j++]); this.u.y = double.Parse(tokens[j++]);
                this.v.x = double.Parse(tokens[j++]); this.v.y = double.Parse(tokens[j++]);
                this.r.x = double.Parse(tokens[j++]); this.r.y = double.Parse(tokens[j++]);

                this.xycalibrators[0].u.x = double.Parse(tokens[j++]);
                this.xycalibrators[0].u.y = double.Parse(tokens[j++]);
                this.xycalibrators[0].v.x = double.Parse(tokens[j++]);
                this.xycalibrators[0].v.y = double.Parse(tokens[j++]);
                this.xycalibrators[1].u.x = double.Parse(tokens[j++]);
                this.xycalibrators[1].u.y = double.Parse(tokens[j++]);
                this.xycalibrators[1].v.x = double.Parse(tokens[j++]);
                this.xycalibrators[1].v.y = double.Parse(tokens[j++]);
                this.xycalibrators[2].u.x = double.Parse(tokens[j++]);
                this.xycalibrators[2].u.y = double.Parse(tokens[j++]);
                this.xycalibrators[2].v.x = double.Parse(tokens[j++]);
                this.xycalibrators[2].v.y = double.Parse(tokens[j++]);
                this.xycalibrators[3].u.x = double.Parse(tokens[j++]);
                this.xycalibrators[3].u.y = double.Parse(tokens[j++]);
                this.xycalibrators[3].v.x = double.Parse(tokens[j++]);
                this.xycalibrators[3].v.y = double.Parse(tokens[j++]);

            }
            sr.Close();

            this.UpdateXYZLengths();
            this.UpdateVp();
            this.UpdateXYZLengths();

        }
        public void Save(string file)
        {
            StreamWriter sw = new StreamWriter(file);

            sw.Write(this.o.x + " " + this.o.y + " ");
            sw.Write(this.x.x + " " + this.x.y + " ");
            sw.Write(this.y.x + " " + this.y.y + " ");
            sw.Write(this.z.x + " " + this.z.y + " ");
            sw.Write(this.w.x + " " + this.w.y + " ");
            sw.Write(this.u.x + " " + this.u.y + " ");
            sw.Write(this.v.x + " " + this.v.y + " ");
            sw.Write(this.r.x + " " + this.r.y + " ");

            sw.Write(this.xycalibrators[0].u.x + " " + this.xycalibrators[0].u.y + " ");
            sw.Write(this.xycalibrators[0].v.x + " " + this.xycalibrators[0].v.y + " ");
            sw.Write(this.xycalibrators[1].u.x + " " + this.xycalibrators[1].u.y + " ");
            sw.Write(this.xycalibrators[1].v.x + " " + this.xycalibrators[1].v.y + " ");
            sw.Write(this.xycalibrators[2].u.x + " " + this.xycalibrators[2].u.y + " ");
            sw.Write(this.xycalibrators[2].v.x + " " + this.xycalibrators[2].v.y + " ");
            sw.Write(this.xycalibrators[3].u.x + " " + this.xycalibrators[3].u.y + " ");
            sw.Write(this.xycalibrators[3].v.x + " " + this.xycalibrators[3].v.y + " ");

            sw.Close();
        }

        public void Serialize(StreamWriter sw)
        {
            sw.WriteLine("vpCamera");
            double[] camera_k = this.camera_K.ToArray();
            for (int i = 0; i < 9; ++i)
            {
                sw.Write(camera_k[i] + " ");
            }
            sw.WriteLine();
            double[] camera_r = this.camera_R.ToArray();
            for (int i = 0; i < 9; ++i)
            {
                sw.Write(camera_r[i] + " ");
            }
            sw.WriteLine();
            double[] camera_t = this.camera_t.ToArray();
            for (int i = 0; i < 3; ++i)
            {
                sw.Write(camera_t[i] + " ");
            }
            sw.WriteLine();
        }

        public void DeSerialize(Matrix3d K, Matrix3d R, Vector3d t, int[] viewport, int wndHeight)
        {
            this.camera_K = K;
            this.camera_R = R;
            this.camera_t = t;
            this.KR = this.camera_K * this.camera_R;
            this.Kt = this.camera_K * this.camera_t;
            this.viewport = viewport;
            this.wndViewHeight = wndHeight;
            this.EyePos = new Vector3d() - this.camera_R.Transpose() * this.camera_t;
            this.GetGLMatrices(viewport[2], viewport[3], VPCamera.zNear, VPCamera.zFar);
            this.calibrated = true;
        }

        public Vector3d[] Compute3DCoordinate(List<Vector2d> impos, double height)
        {
            Vector2d[] pt2 = new Vector2d[4] {
				new Vector2d(0,0),
				new Vector2d(1,0),
				new Vector2d(1,1),
				new Vector2d(0,1)
			};
            Vector2d[] impt2 = new Vector2d[4] {
				this.Compute2D(new Vector3d(pt2[0], height)),
				this.Compute2D(new Vector3d(pt2[1], height)),
				this.Compute2D(new Vector3d(pt2[2], height)),
				this.Compute2D(new Vector3d(pt2[3], height))
			};
            Matrix3d H = this.ComputeHomographyMatrix(impt2, pt2);
            Vector3d[] outpt3 = new Vector3d[impos.Count];
            int index = 0;
            foreach (Vector2d p in impos)
            {
                Vector3d point = H * new Vector3d(p, 1);
                point.HomogeneousNormalize();
                point.z = height;
                outpt3[index++] = point;
            }
            return outpt3;
        }
        public Vector3d Compute3DCoordinate(Vector2d impos, double height)
        {
            Vector2d[] pt2 = new Vector2d[4] {
				new Vector2d(0,0),
				new Vector2d(1,0),
				new Vector2d(1,1),
				new Vector2d(0,1)
			};
            Vector2d[] impt2 = new Vector2d[4] {
				this.Compute2D(new Vector3d(pt2[0],height)),
				this.Compute2D(new Vector3d(pt2[1],height)),
				this.Compute2D(new Vector3d(pt2[2],height)),
				this.Compute2D(new Vector3d(pt2[3],height))
			};
            Matrix3d H = this.ComputeHomographyMatrix(impt2, pt2);
            Vector3d outpt3 = H * new Vector3d(impos, 1);
            outpt3.HomogeneousNormalize();
            outpt3.z = height;
            return outpt3;
        }
        public Vector3d Compute3DCoordinate(Vector2d impos, Vector2d groundproj)
        {
            Vector3d vanishingline = new Vector3d(this.vx, 1).Cross(new Vector3d(this.vy, 1));
            Vector3d pointT = new Vector3d(impos, 1);
            Vector3d pointB = new Vector3d(groundproj, 1);
            Vector3d pointQ = new Vector3d(groundproj, 1);
            Vector3d pointV = new Vector3d(this.vz, 1);

            Vector3d KR_1 = new Vector3d(this.KR[0, 0], this.KR[1, 0], this.KR[2, 0]);
            Vector3d KR_2 = new Vector3d(this.KR[0, 1], this.KR[1, 1], this.KR[2, 1]);
            Matrix3d homographymatrix = new Matrix3d(KR_1, KR_2, Kt).Inverse();
            Vector3d pointS = homographymatrix * pointQ;
            pointS.HomogeneousNormalize();
            Vector3d BT = pointB.Cross(pointT);
            Vector3d VT = pointV.Cross(pointT);

            Vector3d origin = new Vector3d(this.world_origin_imgpos, 1);
            int sign = BT.Dot(VT) < 0 ? -1 : 1;
            pointS.z = -sign * origin.Dot(vanishingline) * Math.Abs(BT.Length()) /
                    (this.zProjScale * pointB.Dot(vanishingline) * Math.Abs(VT.Length()));

            return pointS;
        }
        public Vector2d Compute2D(Vector3d p)
        {
            // in case if p is view-transformed
            p = (this.objectSpaceTransform * new Vector4d(p, 1)).ToVector3D();

            // the vpCamera projection matrix
            Vector3d q = this.KR * p + this.Kt;
            q.HomogeneousNormalize();
            return q.ToVector2d();
        }
        
        public void ProjectPointToPlane(Vector2d screenpt, Vector3d planecenter, Vector3d planenormal,
            out Vector3d p3, out double r)
        {
            // the out parameter r measures how close the intersecting point is to the near plane'
            // 1. get transformed plane normal and center (due to view point change)
            Vector3d c = this.GetObjSpaceTransformedPoint(planecenter);
            Vector3d nor = this.GetObjSpaceTransformedVector(planenormal);
            double[] ss = new double[3];
            double[] tt = new double[3];
            if (this.UnProject(screenpt.x, screenpt.y, -1, ss) == 0 ||
                this.UnProject(screenpt.x, screenpt.y, 1, tt) == 0)
                p3 = new Vector3d(0, 0, 0);
            Vector3d s = new Vector3d(ss);
            Vector3d t = new Vector3d(tt);
            r = (c - t).Dot(nor) / ((s - t).Dot(nor));
            p3 = r * s + (1 - r) * t;
        }
        public Vector3d ProjectPointToPlane(Vector2d screenpt, Vector3d planecenter, Vector3d planenormal)
        {
            // the out parameter r measures how close the intersecting point is to the near plane'
            // 1. get transformed plane normal and center (due to view point change)
            Vector3d c = this.GetObjSpaceTransformedPoint(planecenter);
            Vector3d nor = this.GetObjSpaceTransformedVector(planenormal);
            double[] ss = new double[3];
            double[] tt = new double[3];
            if (this.UnProject(screenpt.x, screenpt.y, -1, ss) == 0 ||
                this.UnProject(screenpt.x, screenpt.y, 1, tt) == 0)
                return new Vector3d(0, 0, 0);
            Vector3d s = new Vector3d(ss);
            Vector3d t = new Vector3d(tt);
            double r = (c - t).Dot(nor) / ((s - t).Dot(nor));
            return r * s + (1 - r) * t;
        }
        private Vector3d GetObjSpaceTransformedPoint(Vector3d point)
        {
            return (this.objectSpaceTransform * new Vector4d(point, 1)).ToVector3D();
        }
        private Vector3d GetObjSpaceTransformedVector(Vector3d vector)
        {
            return (this.objectSpaceTransform * new Vector4d(vector, 0)).ToVector3D();
        }
        private void CreateCalibratingElements()
        {
            this.o = new Vector2d(); this.u = new Vector2d();
            this.x = new Vector2d(); this.v = new Vector2d();
            this.y = new Vector2d(); this.w = new Vector2d();
            this.z = new Vector2d(); this.r = new Vector2d();

            Line2d ox = new Line2d(o, x); 
            Line2d oy = new Line2d(o, y); 
            Line2d oz = new Line2d(o, z); 
            Line2d zu = new Line2d(z, u); 
            Line2d zv = new Line2d(z, v); 
            Line2d rx = new Line2d(r, x); 
            Line2d ry = new Line2d(r, y);
            Line2d rw = new Line2d(r, w); 
            Line2d wu = new Line2d(w, u); 
            Line2d wv = new Line2d(w, v); 
            Line2d ux = new Line2d(u, x); 
            Line2d uy = new Line2d(v, y); 

            this.lineSegments = new List<Line2d>();
            this.lineSegments.Add(ox);
            this.lineSegments.Add(oy);
            this.lineSegments.Add(oz);
            this.lineSegments.Add(zu);
            this.lineSegments.Add(zv);
            this.lineSegments.Add(rx);
            this.lineSegments.Add(ry);
            this.lineSegments.Add(rw);
            this.lineSegments.Add(wu);
            this.lineSegments.Add(wv);
            this.lineSegments.Add(ux);
            this.lineSegments.Add(uy);

            this.xycalibrators = new List<Line2d>();
            this.xycalibrators.Add(new Line2d(new Vector2d(), new Vector2d()));
            this.xycalibrators.Add(new Line2d(new Vector2d(), new Vector2d()));
            this.xycalibrators.Add(new Line2d(new Vector2d(), new Vector2d()));
            this.xycalibrators.Add(new Line2d(new Vector2d(), new Vector2d()));


            this.adjustablePoints = new List<Vector2d>();
            this.adjustablePoints.Add(this.o);
            this.adjustablePoints.Add(this.x);
            this.adjustablePoints.Add(this.y);
            this.adjustablePoints.Add(this.z);

            this.ballMatrix = Matrix4d.IdentityMatrix().ToArray();
            this.currObjTransMatrix = Matrix4d.IdentityMatrix().ToArray();

        }
        private void UpdateXYZ()
        {
            // compute x y z position based on position o and vx, vy, vz
            this.x = this.o - this.iSInteriorStructure * (this.vx - this.o).normalize() * this.xlen;
            this.y = this.o - this.iSInteriorStructure * (this.vy - this.o).normalize() * this.ylen;

            int sign = Math.Sign((this.vz - this.o).y);
            this.z = this.o - sign * (this.vz - this.o).normalize() * this.zlen;
        }
        private void ComputeCameraT(double camera_height, Vector2d world_origin_imgpos)
        {
            // C = -R'*T (a*o=KT) => C = -R'*aK^-1*o, with C.z = cameraheight;
            Matrix3d RT = this.camera_R.Transpose() * this.camera_R;
            double err = Math.Abs(RT.Determinant() - 1);
            Console.WriteLine("--- vpCamera R accuracy = " + err);
            //	if (err > 1e-5) {
            //		throw new Exception();
            //	}

            Vector3d o = new Vector3d(world_origin_imgpos, 1);
            Matrix3d K_inv = this.camera_K.Inverse();
            Vector3d q = (this.camera_R.Transpose() * K_inv) * o;
            double alpha = -camera_height / q.z;
            this.camera_t = K_inv * o * alpha;

            this.KR = this.camera_K * this.camera_R;
            this.Kt = this.camera_K * this.camera_t;

            Matrix3d X = new Matrix3d(new Vector3d(this.vx, 1), new Vector3d(this.vy, 1), new Vector3d(this.vz, 1));
            Matrix3d I = X.Inverse() * this.KR;
            this.xProjScale = I[0, 0] / alpha;
            this.yProjScale = I[1, 1] / alpha;
            this.zProjScale = I[2, 2] / alpha;

            this.world_origin_imgpos = world_origin_imgpos;
            this.EyePos = new Vector3d() - this.camera_R.Transpose() * this.camera_t;
        }

        public Vector3d Project(double objx, double objy, double objz)
        {
            double[] modelview = this.glmodelViewMatrix;
            double[] projection = this.glprojMatrix;
            double[] ballmat = this.ballMatrix;

            //Transformation vectors
            double[] tmpb = new double[4];
            //Arcball transform
            tmpb[0] = ballmat[0] * objx + ballmat[4] * objy + ballmat[8] * objz + ballmat[12];  //w is always 1
            tmpb[1] = ballmat[1] * objx + ballmat[5] * objy + ballmat[9] * objz + ballmat[13];
            tmpb[2] = ballmat[2] * objx + ballmat[6] * objy + ballmat[10] * objz + ballmat[14];
            tmpb[3] = ballmat[3] * objx + ballmat[7] * objy + ballmat[11] * objz + ballmat[15];

            double[] fTempo = new double[8];
            //Modelview transform
            fTempo[0] = modelview[0] * tmpb[0] + modelview[4] * tmpb[1] + modelview[8] * tmpb[2] + modelview[12] * tmpb[3];  //w is always 1
            fTempo[1] = modelview[1] * tmpb[0] + modelview[5] * tmpb[1] + modelview[9] * tmpb[2] + modelview[13] * tmpb[3];
            fTempo[2] = modelview[2] * tmpb[0] + modelview[6] * tmpb[1] + modelview[10] * tmpb[2] + modelview[14] * tmpb[3];
            fTempo[3] = modelview[3] * tmpb[0] + modelview[7] * tmpb[1] + modelview[11] * tmpb[2] + modelview[15] * tmpb[3];

            //Projection transform, the final row of projection matrix is always [0 0 -1 0]
            //so we optimize for that.
            fTempo[4] = projection[0] * fTempo[0] + projection[4] * fTempo[1] + projection[8] * fTempo[2] + projection[12] * fTempo[3];
            fTempo[5] = projection[1] * fTempo[0] + projection[5] * fTempo[1] + projection[9] * fTempo[2] + projection[13] * fTempo[3];
            fTempo[6] = projection[2] * fTempo[0] + projection[6] * fTempo[1] + projection[10] * fTempo[2] + projection[14] * fTempo[3];
            fTempo[7] = -fTempo[2];
            //The result normalizes between -1 and 1
            if (fTempo[7] == 0.0)        //The w value
                return new Vector3d();
            fTempo[7] = 1.0 / fTempo[7];
            //Perspective division
            fTempo[4] *= fTempo[7];
            fTempo[5] *= fTempo[7];
            fTempo[6] *= fTempo[7];
            //Window coordinates
            //Map x, y to range 0-1
            Vector3d windowCoordinate = new Vector3d();
            windowCoordinate[0] = (fTempo[4] * 0.5 + 0.5) * viewport[2] + viewport[0];
            windowCoordinate[1] = (fTempo[5] * 0.5 + 0.5) * viewport[3] + viewport[1];
            //This is only correct when glDepthRange(0.0, 1.0)
            windowCoordinate[2] = (1.0 + fTempo[6]) * 0.5;  //Between 0 and 1

            // convert from gl 2d coords to windows coordinates
            windowCoordinate.y = this.wndViewHeight - windowCoordinate.y;

            return windowCoordinate;
        }

        public int UnProject_mat(double winx, double winy, double winz, double[] objectCoordinate)
        {
            //Transformation matrices
            Matrix4d A = new Matrix4d(this.glprojMatrix).Transpose() * (new Matrix4d(this.glmodelViewMatrix).Transpose()
                * new Matrix4d(this.ballMatrix).Transpose());
            Matrix4d M = A.Inverse();

            //Transformation of normalized coordinates between -1 and 1
            double[] data_in = new double[4];
            data_in[0] = (winx - (double)this.viewport[0]) / (double)this.viewport[2] * 2.0 - 1.0;
            data_in[1] = (winy - (double)this.viewport[1]) / (double)this.viewport[3] * 2.0 - 1.0;
            data_in[2] = 2.0 * winz - 1.0;
            data_in[3] = 1.0;

            //Objects coordinates
            Vector4d data_out = M * new Vector4d(data_in);
            if (data_out.w == 0.0)
                return 0;

            double w = 1.0 / data_out.w;
            objectCoordinate[0] = data_out.x * w;
            objectCoordinate[1] = data_out.y * w;
            objectCoordinate[2] = data_out.z * w;

            return 1;
        }
        public int UnProject(double winx, double winy, double winz, double[] objectCoordinate)
        {
            // convert from windows coordinate to opengl coordinate
            winy = this.wndViewHeight - winy;

            //Transformation matrices
            double[] m = new double[16];
            double[] A = new double[16];
            double[] tmpA = new double[16];
            double[] data_in = new double[4];
            double[] data_out = new double[4];
            //Calculation for inverting a matrix, compute projection x modelview
            //and store in A[16]
            MultiplyMatrices4by4OpenGL_FLOAT(tmpA, this.glmodelViewMatrix, this.ballMatrix);
            MultiplyMatrices4by4OpenGL_FLOAT(A, this.glprojMatrix, tmpA);
            //Now compute the inverse of matrix A
            if (glhInvertMatrixf2(A, m) == 0)
                return 0;
            //Transformation of normalized coordinates between -1 and 1
            data_in[0] = (winx - (double)this.viewport[0]) / (double)this.viewport[2] * 2.0 - 1.0;
            data_in[1] = (winy - (double)this.viewport[1]) / (double)this.viewport[3] * 2.0 - 1.0;
            data_in[2] = 2.0 * winz - 1.0;
            data_in[3] = 1.0;
            //Objects coordinates
            MultiplyMatrixByVector4by4OpenGL_FLOAT(data_out, m, data_in);
            if (data_out[3] == 0.0)
                return 0;
            data_out[3] = 1.0 / data_out[3];
            objectCoordinate[0] = data_out[0] * data_out[3];
            objectCoordinate[1] = data_out[1] * data_out[3];
            objectCoordinate[2] = data_out[2] * data_out[3];
            return 1;
        }
        private static void MultiplyMatrices4by4OpenGL_FLOAT(double[] result, double[] matrix1, double[] matrix2)
        {
            result[0] = matrix1[0] * matrix2[0] +
                matrix1[4] * matrix2[1] +
                matrix1[8] * matrix2[2] +
                matrix1[12] * matrix2[3];
            result[4] = matrix1[0] * matrix2[4] +
                matrix1[4] * matrix2[5] +
                matrix1[8] * matrix2[6] +
                matrix1[12] * matrix2[7];
            result[8] = matrix1[0] * matrix2[8] +
                matrix1[4] * matrix2[9] +
                matrix1[8] * matrix2[10] +
                matrix1[12] * matrix2[11];
            result[12] = matrix1[0] * matrix2[12] +
                matrix1[4] * matrix2[13] +
                matrix1[8] * matrix2[14] +
                matrix1[12] * matrix2[15];
            result[1] = matrix1[1] * matrix2[0] +
                matrix1[5] * matrix2[1] +
                matrix1[9] * matrix2[2] +
                matrix1[13] * matrix2[3];
            result[5] = matrix1[1] * matrix2[4] +
                matrix1[5] * matrix2[5] +
                matrix1[9] * matrix2[6] +
                matrix1[13] * matrix2[7];
            result[9] = matrix1[1] * matrix2[8] +
                matrix1[5] * matrix2[9] +
                matrix1[9] * matrix2[10] +
                matrix1[13] * matrix2[11];
            result[13] = matrix1[1] * matrix2[12] +
                matrix1[5] * matrix2[13] +
                matrix1[9] * matrix2[14] +
                matrix1[13] * matrix2[15];
            result[2] = matrix1[2] * matrix2[0] +
                matrix1[6] * matrix2[1] +
                matrix1[10] * matrix2[2] +
                matrix1[14] * matrix2[3];
            result[6] = matrix1[2] * matrix2[4] +
                matrix1[6] * matrix2[5] +
                matrix1[10] * matrix2[6] +
                matrix1[14] * matrix2[7];
            result[10] = matrix1[2] * matrix2[8] +
                matrix1[6] * matrix2[9] +
                matrix1[10] * matrix2[10] +
                matrix1[14] * matrix2[11];
            result[14] = matrix1[2] * matrix2[12] +
                matrix1[6] * matrix2[13] +
                matrix1[10] * matrix2[14] +
                matrix1[14] * matrix2[15];
            result[3] = matrix1[3] * matrix2[0] +
                matrix1[7] * matrix2[1] +
                matrix1[11] * matrix2[2] +
                matrix1[15] * matrix2[3];
            result[7] = matrix1[3] * matrix2[4] +
                matrix1[7] * matrix2[5] +
                matrix1[11] * matrix2[6] +
                matrix1[15] * matrix2[7];
            result[11] = matrix1[3] * matrix2[8] +
                matrix1[7] * matrix2[9] +
                matrix1[11] * matrix2[10] +
                matrix1[15] * matrix2[11];
            result[15] = matrix1[3] * matrix2[12] +
                matrix1[7] * matrix2[13] +
                matrix1[11] * matrix2[14] +
                matrix1[15] * matrix2[15];
        }
        private static void MultiplyMatrixByVector4by4OpenGL_FLOAT(double[] resultvector, double[] matrix, double[] pvector)
        {
            resultvector[0] = matrix[0] * pvector[0] + matrix[4] * pvector[1] + matrix[8] * pvector[2] + matrix[12] * pvector[3];
            resultvector[1] = matrix[1] * pvector[0] + matrix[5] * pvector[1] + matrix[9] * pvector[2] + matrix[13] * pvector[3];
            resultvector[2] = matrix[2] * pvector[0] + matrix[6] * pvector[1] + matrix[10] * pvector[2] + matrix[14] * pvector[3];
            resultvector[3] = matrix[3] * pvector[0] + matrix[7] * pvector[1] + matrix[11] * pvector[2] + matrix[15] * pvector[3];
        }
        private static void SWAP_ROWS_FLOAT(double[] a, double[] b)
        {
            double[] _tmp = a; (a) = (b); (b) = _tmp;
        }
        private static double MAT(double[] m, int r, int c)
        {
            return m[(c) * 4 + (r)];
        }

        //This code comes directly from GLU except that it is for float
        private static int glhInvertMatrixf2(double[] m, double[] out_mat)
        {
            double[][] wtmp = new double[4][];
            for (int i = 0; i < 4; ++i) wtmp[i] = new double[8];

            double m0, m1, m2, m3, s;
            double[] r0 = wtmp[0];
            double[] r1 = wtmp[1];
            double[] r2 = wtmp[2];
            double[] r3 = wtmp[3];
            r0[0] = MAT(m, 0, 0); r0[1] = MAT(m, 0, 1);
            r0[2] = MAT(m, 0, 2); r0[3] = MAT(m, 0, 3);
            r0[4] = 1.0; r0[5] = r0[6] = r0[7] = 0.0;
            r1[0] = MAT(m, 1, 0); r1[1] = MAT(m, 1, 1);
            r1[2] = MAT(m, 1, 2); r1[3] = MAT(m, 1, 3);
            r1[5] = 1.0; r1[4] = r1[6] = r1[7] = 0.0;
            r2[0] = MAT(m, 2, 0); r2[1] = MAT(m, 2, 1);
            r2[2] = MAT(m, 2, 2); r2[3] = MAT(m, 2, 3);
            r2[6] = 1.0; r2[4] = r2[5] = r2[7] = 0.0;
            r3[0] = MAT(m, 3, 0); r3[1] = MAT(m, 3, 1);
            r3[2] = MAT(m, 3, 2); r3[3] = MAT(m, 3, 3);
            r3[7] = 1.0; r3[4] = r3[5] = r3[6] = 0.0;
            /* choose pivot - or die */
            if (Math.Abs(r3[0]) > Math.Abs(r2[0]))
                SWAP_ROWS_FLOAT(r3, r2);
            if (Math.Abs(r2[0]) > Math.Abs(r1[0]))
                SWAP_ROWS_FLOAT(r2, r1);
            if (Math.Abs(r1[0]) > Math.Abs(r0[0]))
                SWAP_ROWS_FLOAT(r1, r0);
            if (0.0 == r0[0])
                return 0;
            /* eliminate first variable     */
            m1 = r1[0] / r0[0];
            m2 = r2[0] / r0[0];
            m3 = r3[0] / r0[0];
            s = r0[1];
            r1[1] -= m1 * s;
            r2[1] -= m2 * s;
            r3[1] -= m3 * s;
            s = r0[2];
            r1[2] -= m1 * s;
            r2[2] -= m2 * s;
            r3[2] -= m3 * s;
            s = r0[3];
            r1[3] -= m1 * s;
            r2[3] -= m2 * s;
            r3[3] -= m3 * s;
            s = r0[4];
            if (s != 0.0)
            {
                r1[4] -= m1 * s;
                r2[4] -= m2 * s;
                r3[4] -= m3 * s;
            }
            s = r0[5];
            if (s != 0.0)
            {
                r1[5] -= m1 * s;
                r2[5] -= m2 * s;
                r3[5] -= m3 * s;
            }
            s = r0[6];
            if (s != 0.0)
            {
                r1[6] -= m1 * s;
                r2[6] -= m2 * s;
                r3[6] -= m3 * s;
            }
            s = r0[7];
            if (s != 0.0)
            {
                r1[7] -= m1 * s;
                r2[7] -= m2 * s;
                r3[7] -= m3 * s;
            }
            /* choose pivot - or die */
            if (Math.Abs(r3[1]) > Math.Abs(r2[1]))
                SWAP_ROWS_FLOAT(r3, r2);
            if (Math.Abs(r2[1]) > Math.Abs(r1[1]))
                SWAP_ROWS_FLOAT(r2, r1);
            if (0.0 == r1[1])
                return 0;
            /* eliminate second variable */
            m2 = r2[1] / r1[1];
            m3 = r3[1] / r1[1];
            r2[2] -= m2 * r1[2];
            r3[2] -= m3 * r1[2];
            r2[3] -= m2 * r1[3];
            r3[3] -= m3 * r1[3];
            s = r1[4];
            if (0.0 != s)
            {
                r2[4] -= m2 * s;
                r3[4] -= m3 * s;
            }
            s = r1[5];
            if (0.0 != s)
            {
                r2[5] -= m2 * s;
                r3[5] -= m3 * s;
            }
            s = r1[6];
            if (0.0 != s)
            {
                r2[6] -= m2 * s;
                r3[6] -= m3 * s;
            }
            s = r1[7];
            if (0.0 != s)
            {
                r2[7] -= m2 * s;
                r3[7] -= m3 * s;
            }
            /* choose pivot - or die */
            if (Math.Abs(r3[2]) > Math.Abs(r2[2]))
                SWAP_ROWS_FLOAT(r3, r2);
            if (0.0 == r2[2])
                return 0;
            /* eliminate third variable */
            m3 = r3[2] / r2[2];
            r3[3] -= m3 * r2[3]; r3[4] -= m3 * r2[4];
            r3[5] -= m3 * r2[5]; r3[6] -= m3 * r2[6]; r3[7] -= m3 * r2[7];
            /* last check */
            if (0.0 == r3[3])
                return 0;
            s = 1.0 / r3[3];             /* now back substitute row 3 */
            r3[4] *= s;
            r3[5] *= s;
            r3[6] *= s;
            r3[7] *= s;
            m2 = r2[3];                  /* now back substitute row 2 */
            s = 1.0 / r2[2];
            r2[4] = s * (r2[4] - r3[4] * m2); r2[5] = s * (r2[5] - r3[5] * m2);
            r2[6] = s * (r2[6] - r3[6] * m2); r2[7] = s * (r2[7] - r3[7] * m2);
            m1 = r1[3];
            r1[4] -= r3[4] * m1; r1[5] -= r3[5] * m1;
            r1[6] -= r3[6] * m1; r1[7] -= r3[7] * m1;
            m0 = r0[3];
            r0[4] -= r3[4] * m0; r0[5] -= r3[5] * m0;
            r0[6] -= r3[6] * m0; r0[7] -= r3[7] * m0;
            m1 = r1[2];                  /* now back substitute row 1 */
            s = 1.0 / r1[1];
            r1[4] = s * (r1[4] - r2[4] * m1); r1[5] = s * (r1[5] - r2[5] * m1);
            r1[6] = s * (r1[6] - r2[6] * m1); r1[7] = s * (r1[7] - r2[7] * m1);
            m0 = r0[2];
            r0[4] -= r2[4] * m0; r0[5] -= r2[5] * m0;
            r0[6] -= r2[6] * m0; r0[7] -= r2[7] * m0;
            m0 = r0[1];                  /* now back substitute row 0 */
            s = 1.0 / r0[0];
            r0[4] = s * (r0[4] - r1[4] * m0); r0[5] = s * (r0[5] - r1[5] * m0);
            r0[6] = s * (r0[6] - r1[6] * m0); r0[7] = s * (r0[7] - r1[7] * m0);

            out_mat[0] = r0[4]; out_mat[4] = r0[5]; out_mat[8] = r0[6]; out_mat[12] = r0[7];
            out_mat[1] = r1[4]; out_mat[5] = r1[5]; out_mat[9] = r1[6]; out_mat[13] = r1[7];
            out_mat[2] = r2[4]; out_mat[6] = r2[5]; out_mat[10] = r2[6]; out_mat[14] = r2[7];
            out_mat[3] = r3[4]; out_mat[7] = r3[5]; out_mat[11] = r3[6]; out_mat[15] = r3[7];

            return 1;
        }
        private static int arParamDecompMat(double[,] source, double[,] cpara, double[,] trans)
        {
            int r, c;
            double[,] Cpara = new double[3, 4];
            double rem1, rem2, rem3;

            if (source[2, 3] >= 0)
            {
                for (r = 0; r < 3; r++)
                {
                    for (c = 0; c < 4; c++)
                    {
                        Cpara[r, c] = source[r, c];
                    }
                }
            }
            else
            {
                for (r = 0; r < 3; r++)
                {
                    for (c = 0; c < 4; c++)
                    {
                        Cpara[r, c] = -(source[r, c]);
                    }
                }
            }

            for (r = 0; r < 3; r++)
            {
                for (c = 0; c < 4; c++)
                {
                    cpara[r, c] = 0.0;
                }
            }
            cpara[2, 2] = norm(Cpara[2, 0], Cpara[2, 1], Cpara[2, 2]);
            trans[2, 0] = Cpara[2, 0] / cpara[2, 2];
            trans[2, 1] = Cpara[2, 1] / cpara[2, 2];
            trans[2, 2] = Cpara[2, 2] / cpara[2, 2];
            trans[2, 3] = Cpara[2, 3] / cpara[2, 2];

            cpara[1, 2] = dot(trans[2, 0], trans[2, 1], trans[2, 2],
                               Cpara[1, 0], Cpara[1, 1], Cpara[1, 2]);
            rem1 = Cpara[1, 0] - cpara[1, 2] * trans[2, 0];
            rem2 = Cpara[1, 1] - cpara[1, 2] * trans[2, 1];
            rem3 = Cpara[1, 2] - cpara[1, 2] * trans[2, 2];
            cpara[1, 1] = norm(rem1, rem2, rem3);
            trans[1, 0] = rem1 / cpara[1, 1];
            trans[1, 1] = rem2 / cpara[1, 1];
            trans[1, 2] = rem3 / cpara[1, 1];

            cpara[0, 2] = dot(trans[2, 0], trans[2, 1], trans[2, 2],
                               Cpara[0, 0], Cpara[0, 1], Cpara[0, 2]);
            cpara[0, 1] = dot(trans[1, 0], trans[1, 1], trans[1, 2],
                               Cpara[0, 0], Cpara[0, 1], Cpara[0, 2]);
            rem1 = Cpara[0, 0] - cpara[0, 1] * trans[1, 0] - cpara[0, 2] * trans[2, 0];
            rem2 = Cpara[0, 1] - cpara[0, 1] * trans[1, 1] - cpara[0, 2] * trans[2, 1];
            rem3 = Cpara[0, 2] - cpara[0, 1] * trans[1, 2] - cpara[0, 2] * trans[2, 2];
            cpara[0, 0] = norm(rem1, rem2, rem3);
            trans[0, 0] = rem1 / cpara[0, 0];
            trans[0, 1] = rem2 / cpara[0, 0];
            trans[0, 2] = rem3 / cpara[0, 0];

            trans[1, 3] = (Cpara[1, 3] - cpara[1, 2] * trans[2, 3]) / cpara[1, 1];
            trans[0, 3] = (Cpara[0, 3] - cpara[0, 1] * trans[1, 3]
                                       - cpara[0, 2] * trans[2, 3]) / cpara[0, 0];

            for (r = 0; r < 3; r++)
            {
                for (c = 0; c < 3; c++)
                {
                    cpara[r, c] /= cpara[2, 2];
                }
            }

            return 0;
        }
        private static double norm(double a, double b, double c)
        {
            return (Math.Sqrt(a * a + b * b + c * c));
        }
        private static double dot(double a1, double a2, double a3, double b1, double b2, double b3)
        {
            return (a1 * b1 + a2 * b2 + a3 * b3);
        }
        private static Vector2d ComputeTriangleV3(Vector2d h, Vector2d v1, Vector2d v2)
        {
            // this function compute the last point of a triangle, given two points
            // and the ortho center h. The algorithm uses the orthogonalty to
            // solve for the unknow X(x,y) by two linear euqations.
            // (X-H).(V2-V1) = 0 && (X-V1).(V2-H) = 0;
            Vector2d v2H = v2 - h;
            Vector2d v21 = v2 - v1;
            Matrix2d A = new Matrix2d(v2H, v21).Transpose();
            Vector2d b = new Vector2d(v2H.Dot(v1), v21.Dot(h));
            Vector2d x = A.Inverse() * b;
            return x;
        }
        private static Vector2d ComputeTriangleOrthoCenter(Vector2d v1, Vector2d v2, Vector2d v3)
        {
            // this function compute the pendicular foot of an triangle, given three points
            // v1, v2 and v3. The algorithm uses the orthogonalty to
            // solve for the unknow H(x,y) by two linear euqations.
            // (H-V1).(V3-V2) = 0 && (H-V3).(V2-V1) = 0;
            Vector2d v21 = v2 - v1;
            Vector2d v32 = v3 - v2;
            Matrix2d A = new Matrix2d(v21, v32).Transpose();
            Vector2d b = new Vector2d(v21.Dot(v3), v32.Dot(v1));
            Vector2d h = A.Inverse() * b;
            return h;
        }
        private static Vector2d ComputeLineIntersectPoint(Vector2d u, Vector2d v, Vector2d p, Vector2d q)
        {
            // compute the intersecting point of two lines: (u,v) and (p,q);
            Vector3d uu = new Vector3d(u, 1);
            Vector3d vv = new Vector3d(v, 1);
            Vector3d pp = new Vector3d(p, 1);
            Vector3d qq = new Vector3d(q, 1);

            Vector3d it = uu.Cross(vv).Cross(pp.Cross(qq));
            it.HomogeneousNormalize();

            return it.ToVector2d();
        }

        public static double[,] ComputeProjectionMatrixCV(Vector2d[] imgpts, Vector3d[] spacepts)
        {
            // this function computes the projection matrix given image-to-space points correspondence
            // using DLT algorithm for approximation
            // require: cv eigendecompose, num of points >= 6
            int n = imgpts.Length;
            double[,] mat = new double[n * 2, 12];
            for (int i = 0, j = 0; i < n; ++i, j += 2)
            {
                double x = imgpts[i].x, y = imgpts[i].y;
                int jj = j + 1;
                mat[j, 4] = spacepts[i].x;
                mat[j, 5] = spacepts[i].y;
                mat[j, 6] = spacepts[i].z;
                mat[j, 7] = 1.0;
                mat[j, 8] = -y * spacepts[i].x;
                mat[j, 9] = -y * spacepts[i].y;
                mat[j, 10] = -y * spacepts[i].z;
                mat[j, 11] = -y;
                mat[jj, 0] = spacepts[i].x;
                mat[jj, 1] = spacepts[i].y;
                mat[jj, 2] = spacepts[i].z;
                mat[jj, 3] = 1.0;
                mat[jj, 8] = -x * spacepts[i].x;
                mat[jj, 9] = -x * spacepts[i].y;
                mat[jj, 10] = -x * spacepts[i].z;
                mat[jj, 11] = -x;
            }
            // perform eigen decomposition
            // if n > 6, decompose ATA, else directly decompose A
            Matrix<double> eigvec = new Matrix<double>(12, 12);
            Matrix<double> eigval = new Matrix<double>(12, 1);
            Matrix<double> cvmat = new Matrix<double>(mat);
            double[,] p = new double[3, 4];
            //	if (n > 6)
            {
                cvmat = cvmat.Transpose().Mul(cvmat);
            }
            CvInvoke.cvEigenVV(cvmat.Ptr, eigvec.Ptr, eigval.Ptr, 1e-30, 0, 0);
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    p[i, j] = eigvec[11, i * 4 + j];
                }
            }
            // ||p|| = 1 from cv
            return p;
        }

        public Matrix3d ComputeHomographyMatrix(Vector2d[] imgpts, Vector2d[] refpts)
        {
            // compute mapping from imgpts to refpts
            if (refpts.Length != imgpts.Length || imgpts.Length < 4) return null;

            PointF[] source = new PointF[imgpts.Length];
            PointF[] target = new PointF[imgpts.Length];
            for (int i = 0; i < imgpts.Length; ++i)
            {
                source[i] = new PointF((float)imgpts[i].x, (float)imgpts[i].y);
                target[i] = new PointF((float)refpts[i].x, (float)refpts[i].y);
            }

            Matrix<double> mat = CameraCalibration.FindHomography(source, target,
                Emgu.CV.CvEnum.HOMOGRAPHY_METHOD.RANSAC, 1e-4);
            Matrix3d H = new Matrix3d();
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    H[i, j] = mat[i, j];
                }
            }
            return H;
        }//ComputeHomographyMatrix
    }
}
