using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Geometry;

namespace Component
{
    public class Contour
    {
        public List<Stroke> strokes;
        public List<Stroke> miniStrokes;
        Random rand = new Random();

        public Contour(List<Stroke> strokes)
        {
            this.strokes = strokes;
            this.miniStrokes = new List<Stroke>();
            foreach (Stroke stroke in strokes)
            {
                List<Stroke> res = this.splitStroke(stroke);
                this.miniStrokes.AddRange(res);
            }
        }

        private Vector2d[] computePCA(Stroke stroke)
        {
            int n = stroke.strokePoints.Count;
            if (n == 0) return null;

            double[,] x = new double[n,2];
            Vector2d[] axes = null;
            int info;
            double[] s2;
            double[,] v;
            for(int i = 0; i < n;++i){
                x[i, 0] = stroke.strokePoints[i].pos2.x;
                x[i, 1] = stroke.strokePoints[i].pos2.y;
            }
            alglib.pcabuildbasis(x, n, 2, out info, out s2, out v);
            if (info == 1)
            {
                axes = new Vector2d[2];
                axes[0] = new Vector2d(v[0, 0], v[0, 1]);
                axes[1] = new Vector2d(v[1, 0], v[1, 1]);
            }
            return axes;
        }// compute PCA

        private List<Vector2d> normalize(Stroke stroke)
        {
            Vector2d[] axes = this.computePCA(stroke);
            Vector2d c = new Vector2d();
            List<Vector2d> res = new List<Vector2d>();
            int n = stroke.strokePoints.Count;
            for (int i = 0; i < n; ++i)
            {
                c += stroke.strokePoints[i].pos2;
            }
            c /= n;
            for (int i = 0; i < n; ++i)
            {
                Vector2d vec = stroke.strokePoints[i].pos2 - c;
                Vector2d nv = new Vector2d();
                nv[0] = vec.Dot(axes[0]);
                nv[1] = vec.Dot(axes[1]);
                res.Add(nv);
            }
            return res;
        }

        public List<Stroke> splitStroke(Stroke stroke)
        {
            if (stroke.strokePoints == null || stroke.strokePoints.Count < 2) return null;
            List<Vector2d> vecs = this.normalize(stroke);
            List<Stroke> res = new List<Stroke>();
            int n = vecs.Count;
            Vector2d[] tangs = new Vector2d[n];
            double[] curvs = new double[n];
            double len = 0;
            int nthr = 10;
            int gap = 2;
            for (int i = 0; i < n; ++i)
            {
                if (i < gap)
                {
                    tangs[i] = (vecs[i + gap] - vecs[i]).normalize();
                }
                else if (i >= n - gap)
                {
                    tangs[i] = (vecs[i] - vecs[i - gap]).normalize();
                }
                else
                {
                    tangs[i] = (vecs[i + gap] - vecs[i - gap]).normalize();
                }
            }
            for (int i = gap; i < n - gap; ++i)
            {
                double arclen = 0;
                for (int j = i - gap; j < i + gap; ++j)
                {
                    arclen += (vecs[j + 1] - vecs[j]).Length();
                }
                if (arclen == 0)
                {
                    arclen = (vecs[i + 1] - vecs[i]).Length();
                }
                double cosv = tangs[i].Dot(tangs[i - 1]);
                if (cosv > 1) cosv = 1;
                if (cosv < -1) cosv = -1;
                double angle = Math.Acos(cosv);
                curvs[i] = angle / arclen;
                len += curvs[i];
            }
            for (int i = 0; i < gap; ++i)
            {
                curvs[i] = curvs[gap];
                curvs[n - 1 - i] = curvs[n - gap - 1];
            }
            int t = 1;
            double thr = len / n * 2.5;
            thr = Math.Max(thr, 1e-6);
            List<Vector2d> sec = new List<Vector2d>();
            sec.Add(stroke.strokePoints[0].pos2);
            while (t < n)
            {
                if (curvs[t] * curvs[t - 1] < 0 || Math.Abs(curvs[t] - curvs[t-1]) > thr)
                {
                    if (sec.Count > nthr)
                    {
                        Stroke minStroke = new Stroke(sec, SegmentClass.StrokeSize);
                        minStroke.strokeColor = Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255));
                        res.Add(minStroke);
                        sec = new List<Vector2d>();
                    }
                }
                sec.Add(stroke.strokePoints[t].pos2);
                ++t;
            }
            if (sec.Count > 0 )
            {
                Stroke minStroke = new Stroke(sec, SegmentClass.StrokeSize);
                minStroke.strokeColor = Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255));
                res.Add(minStroke);
            }
            return res;
        }
    } // Contour
}
