using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.UI;

using Geometry;
using System.Drawing;

namespace IBrushes
{
	class Stroke {

		public Stroke() { }
		~Stroke() { }

		public bool addPoint(Vector2d p)
		{
			if (points.Count == 0)
			{
				points.Add(p);
				tangentDir.Add(new Vector2d(0.0f, 0.0f));
				curvature.Add(0.0f);
				speeds.Add(0);
				times.Add(0);
				startTime = DateTime.Now.Ticks;
				return true;
			}
			Vector2d lastPoint = points[points.Count-1];
			double edgeLen = (lastPoint - p).Length();
			points.Add(p);
			tangentDir.Add(new Vector2d());
			curvature.Add(0.0f);
			endTime = startTime = DateTime.Now.Ticks;
			times.Add(endTime - startTime);
			speeds.Add(edgeLen / (times[times.Count-1] - times[times.Count - 2] + 1) / halfStrokeWidth);
			return true;
		}
		public double lenPercent(int id)
		{
			double subLen = 0;
			for (int i = 1; i <= id; i++)
			{
				subLen += (points[i] - points[i - 1]).Length();
			}
			return subLen / length;
		}
		public void obtainTangentDirection()
		{
			if (points.Count() < 2) return;
			Vector2d e0 = points[1] - points[0];
			Vector2d en = points[points.Count - 1] - points[points.Count - 2];
			tangentDir[0] = e0.Normalize();
			tangentDir[points.Count - 1] = en.Normalize();
			for (int i = 1; i < points.Count - 1; i++)
			{
				Vector2d e1 = points[i] - points[i - 1];
				Vector2d e2 = points[i + 1] - points[i];
				double len1 = e1.Length() + EPS;
				double len2 = e2.Length() + EPS;
				tangentDir[i] = (e1 / len1) + (e2 / len2);
				tangentDir[i] = tangentDir[i].Normalize();
			}
		}
		public void obtainLaplacian() // used in drawing
		{
			laplacian = new Vector2d[points.Count];
			for (int i = 0; i < points.Count - 1; i++)
			{
				laplacian[i] += points[i + 1];
			}
			for (int i = 1; i < points.Count; i++)
			{
				laplacian[i] += points[i - 1];
			}
			for (int i = 1; i < points.Count - 1; i++)
			{
				laplacian[i] /= 2.0f;
			}
			for (int i = 0; i < points.Count; i++)
			{
				laplacian[i] = points[i] - laplacian[i];
			}
		}
		public void obtainCurvature() // used in drawing
		{

		}
		public void resetFlags(int value)
		{
			flags = new int[points.Count];
			for (int i = 0; i < points.Count; ++i) flags[i] = value;
		}
		public void updateLength()
		{
			length = 0;
			for (int i = 1; i < points.Count; i++)
			{
				length += (points[i] - points[i - 1]).Length();
			}
		}
		public void Clear()
		{
			isstylistic = false;
			points.Clear();
			finalPositions.Clear();
			tangentDir.Clear();
			curvature.Clear();
			speeds.Clear();
			times.Clear();
			candidatePoints.Clear();
			candidatePointWeights.Clear();
			candidateCosts.Clear();

			smoothCandPoints.Clear();
			smoothPoints.Clear();
			smoothFinalPoints.Clear();
		}
		public void smoothStrokes() // used for drawing
		{
			smoothPoints = Utils.SmoothCurve(points);
			smoothFinalPoints = Utils.SmoothCurve(finalPositions);
			smoothCandPoints.Clear();
			for (int i = 0; i < candidatePoints.Count; i++) {
				smoothCandPoints.Add(Utils.SmoothCurve(candidatePoints[i]));
			}
		}
		
		public int size()
		{
			return points.Count;
		}
		public void setStylistic(bool _stylistic) // stylistic stroke has same position with original one.
		{
			isstylistic = _stylistic;
		}
		public bool isStylistic()
		{
			return isstylistic;
		}
		public bool isStatic()
		{
			return candidatePoints.Count == 1;
		}
		public int containSimilarCandidate(List<Vector2d> targets)
		{
			int foundPos = -1;
	
			double avgThreshold = Math.Max(halfStrokeWidth / 3.0f, 1.0f);

			for(int i = 0; i < candidatePoints.Count; i++){
				double avgDist = 0;
				double avgLen = 0;
				for(int j = 0; j < targets.Count; j++){
					double minD = double.MaxValue;
					int minId = Math.Max(0, j - 3);
					int maxId = Math.Min(j + 3, (int)targets.Count - 1);
					for(int k = minId; k <= maxId; k++){
						Vector2d delta = targets[j] - candidatePoints[i][k];
						Vector2d delta2 = targets[k] - candidatePoints[i][j];
						minD = Math.Min(minD, Math.Min(delta.Length(), delta2.Length()));
					}
					avgDist += minD;
				}
				avgDist /= targets.Count;
				avgLen /= targets.Count;

				if(avgDist <= avgThreshold){
					foundPos = i;
					break;
				}
			}
			return foundPos;
		}


		private const double EPS = 1e-20f;
		public double length;
		public double halfStrokeWidth;
		public int bestCandidateId = -1;
		public List<Vector2d> points = new List<Vector2d>();
		public List<Vector2d> finalPositions = new List<Vector2d>();
		public List<List<Vector2d>> candidatePoints = new List<List<Vector2d>>();
		public List<List<double>> candidatePointWeights = new List<List<double>>();
		public List<double> candidateCosts = new List<double>();
		public List<double> speeds = new List<double>();
		public List<long> times = new List<long>();
		public List<Vector2d> tangentDir = new List<Vector2d>();
		public List<double> curvature = new List<double>();
		public Vector2d[] laplacian;
		public int[] flags;
		public bool isstylistic;
		public bool isambiguious;

		public List<Vector2d> smoothPoints = new List<Vector2d>();
		public List<List<Vector2d>> smoothCandPoints = new List<List<Vector2d>>();	// the candidate strokes_
		public List<Vector2d> smoothFinalPoints = new List<Vector2d>();

		public List<Vector2d> GetBestCandidateStroke()
		{
			if (this.smoothCandPoints.Count == 0 || this.bestCandidateId < 0) return null;
			return this.smoothCandPoints[this.bestCandidateId];
		}

		private long startTime, endTime;

	}
	class FDogFilter
	{
		CLDiMatrix img;
		double tau;
		CLDVec gau1, gau2, gau3;

		public FDogFilter(Image<Gray, byte> image)
		{
			tau = 0.99f;
			gau1 = Utils.MakeGaussianVector(1);
			gau2 = Utils.MakeGaussianVector(1.6);
			gau1.resize(gau2.dim(), 0);
			gau3 = Utils.MakeGaussianVector(3);

			this.setImage(image);
		}


		~FDogFilter()
		{
		}

		private void setImage(Image<Gray, byte> grayImage)
		{
			this.img = Utils.GrayImage2imatrix(grayImage);
		}

		private void setImage(CLDiMatrix grayImage)
		{
			img = grayImage;
		}

		public double getFilterValue(Vector2d direction, Vector2d pos)
		{
			double flowValue = getFlowValue(direction, pos);
			if (flowValue > 0)
				return 0.0f;
			else
				return -Math.Tanh(flowValue);
		}

		double getDirectionalValue(Vector2d direction, Vector2d pos)
		{
			double sum1 = 0, sum2 = 0;
			double weightSum1 = 0, weightSum2 = 0;

			for (int i = 0; i < gau1.dim(); i++)
			{
				Vector2d p = pos + ((double)i * direction);
				if (p[0] >= 0 && p[0] < img.getRows() && p[1] >= 0 && p[1] < img.getCols())
				{
					int pixel = img[(int)p[0]][(int)p[1]];
					sum1 += gau1[i] * pixel;//grayImg->at<uchar>(p[0], p[1]);
					sum2 += gau2[i] * pixel;
					weightSum1 += gau1[i];
					weightSum2 += gau2[i];
				}
				p = pos - ((double)i * direction);
				if (p[0] >= 0 && p[0] < img.getRows() && p[1] >= 0 && p[1] < img.getCols())
				{
					int pixel = img[(int)p[0]][(int)p[1]];
					sum1 += gau1[i] * pixel;//grayImg->at<uchar>(p[0], p[1]);
					sum2 += gau2[i] * pixel;
					weightSum1 += gau1[i];
					weightSum2 += gau2[i];
				}
			}
			sum1 /= weightSum1;
			sum2 /= weightSum2;
			return sum1 - tau * sum2;
		}

		double getFlowValue(Vector2d direction, Vector2d pos)
		{
			double flowValue = 0;
			double weightSum = 0;
			Vector2d orthorginalDirection = new Vector2d(direction[1], -direction[0]);
			for (int i = 0; i < gau3.dim(); i++)
			{
				Vector2d p;
				p = pos + ((double)i * direction);
				if (p[0] >= 0 && p[0] < img.getCols() && p[1] >= 0 && p[1] < img.getCols())
				{
					flowValue += gau3[i] * getDirectionalValue(orthorginalDirection, p);
					weightSum += gau3[i];
				}

				p = pos - ((double)i * direction);
				if (p[0] >= 0 && p[0] < img.getRows() && p[1] >= 0 && p[1] < img.getCols())
				{
					flowValue += gau3[i] * getDirectionalValue(orthorginalDirection, p);
					weightSum += gau3[i];
				}
			}
			return flowValue / weightSum;
		}
	}
	class EZSketching
	{
		/* 
		 * This calss implements (partially) the following SIGGRAPH paper:
		 *
			 EZ-Sketching:
			 Three-Level Optimization for Error-Tolerant Image Tracing	(Patent Pending)

			 ACM Transaction on Graphics (Proceedings of ACM SIGGRAPH 2014)

			 Qingkun Su   Wing Ho Andy Li    Jue Wang    Hongbo Fu
		 * 
		 */

		class GroupNode{
			
			public List<Vector2d> position = new List<Vector2d>();
			public List<int> parentId = new List<int>();
			public List<double> distance = new List<double>();
			public List<bool> isUsed = new List<bool>();
			public List<double> normalizedGradient = new List<double>();
			public List<List<double>> shapeCost = new List<List<double>>();
			public List<List<double>> gradientCost = new List<List<double>>();
			public List<List<double>> totalCost = new List<List<double>>();
			public List<List<bool>> isValid = new List<List<bool>>();

			public int size(){
				return position.Count();
			}
		}

        private class EZnode : PriorityQueueElement
        {
            public double distance_;
            public Vector2d pos_;
            public EZnode parent_;
            public List<EZnode> adjNodes_ = new List<EZnode>();
            public List<EZEdge> adjEdges_ = new List<EZEdge>();
            public EZnode(Vector2d pos)
            {
                this.pos_ = pos;
            }
            public void AddAdjNode(EZnode node)
            {
                if (!this.adjNodes_.Contains(node))
                    this.adjNodes_.Add(node);
            }
            public void AddEdge(EZEdge e)
            {
                if (!this.adjEdges_.Contains(e))
                    this.adjEdges_.Add(e);
            }
			public void RemoveEdge(EZEdge e)
			{
				this.adjEdges_.Remove(e);
			}
			public void Isolate()
			{
				// get isolated from the connectity (graph)
				this.distance_ = double.MaxValue;
				this.parent_ = null;
				foreach (EZEdge e in this.adjEdges_)
				{
					EZnode t = e.node1_ == this ? e.node2_ : e.node1_;
					t.RemoveEdge(e);
				}
				this.adjEdges_.Clear();
			}
            private int pqIndex = -1;
			public int PQIndex {
				get { return pqIndex; }
				set { pqIndex = value; }
			}
			public int CompareTo(PriorityQueueElement another)
            {
				EZnode right = another as EZnode;
                double d = this.distance_ - right.distance_;
                return d > 0 ? 1 : d == 0 ? 0 : -1;
            }
        }
        private class EZEdge
        {
            public double weight;
            public EZnode node1_, node2_;
            public EZEdge(EZnode node1, EZnode node2)
            {
                this.node1_ = node1;
                this.node2_ = node2;
            }
        }

		private int MAXNUMCANDIDATES = 4;
		private const int SAMPLE_RADIUS = 6;
		private const double GRADIENTTHRESHOLD = 0.05;
		private const double SHAPETHRESHOLD = 0.02;
		private const double LOCALTHRESHOLD = (GRADIENTTHRESHOLD + SHAPETHRESHOLD);
		private const double BIGCONSTANT = 1e6;
		private const double localAlpha = 0.1;
		private Image<Gray, byte> image_;
		private double radius_ = 7.5;
		private const double EPS = 1e-20f;
		private double[,] gradient_;
		private bool[,] isLocalMax_;
		private bool[,] isLabeled_;
		private bool usingSemiOpt_ = true;
		private FDogFilter fdogFilter_;
		private Random rand = new Random();
		private Stroke currentStroke_ = new Stroke();
		private List<Stroke> strokes_ = new List<Stroke>();
		private List<GroupNode> strokeGraph_ = new List<GroupNode>();
		private List<Vector2d> initialStroke_ = new List<Vector2d>();
		private List<Vector2d> optimizedStroke_ = new List<Vector2d>();
		private List<List<Vector2d>> candidatePositions_;
		
		public EZSketching(Image<Bgr, byte> image)
		{
			this.image_ = image.Convert<Gray,byte>();
			this.ComputeImageGradient();

			this.fdogFilter_ = new FDogFilter(this.image_);

			this.isLabeled_ = new bool[this.image_.Width, this.image_.Height];

		//	ImageViewer iv = new ImageViewer(this.image_);
		//	iv.Show();

		}
		public EZSketching(Image<Gray,byte> image)
		{
			this.image_ = image;
			this.ComputeImageGradient();

			this.fdogFilter_ = new FDogFilter(this.image_);

			this.isLabeled_ = new bool[this.image_.Width, this.image_.Height];
		}
		public void AddStrokePoint(Vector2d p, double radius)
		{
			this.radius_ = radius;
			this.currentStroke_.halfStrokeWidth = radius;

			int x = (int)Math.Round(p[0]);
			int y = (int)Math.Round(p[1]);
			if (!(x >= 0 && x < this.image_.Width && y >= 0 && y < this.image_.Height
				&& isLabeled_[x, y] == false))
			{
				return;
			}

			if (currentStroke_.points.Count == 0 || (currentStroke_.points[currentStroke_.points.Count-1] - p).Length() > Math.Max(1.0f, this.radius_ / 4))
			{
				currentStroke_.addPoint(p);
				isLabeled_[x, y] = true;
				addOneGroupNode();
			}
		}
		public List<Vector2d> Refine(List<Vector2d> stroke, double size)
		{
			this.radius_ = size;

			this.initialStroke_ = new List<Vector2d>();
			this.initialStroke_.AddRange(stroke);

			// 1. get sample points within each stroke point circle

			int step = (int)Math.Max(2.0f, (double)Math.Round(this.radius_ / SAMPLE_RADIUS));
			int thickness = (int)this.radius_;
			int rangeLen = 2 * thickness + 1;
			List<List<Vector2d>> candidatePositions = new List<List<Vector2d>>();
			foreach (Vector2d point in stroke)
			{
				List<Vector2d> candidates = new List<Vector2d>();
				int xx = (int)point.x, yy = (int)point.y;

				Vector2d xInterval = getInterval(xx, thickness, new Vector2d(0, this.image_.Width - 1));
				Vector2d yInterval = getInterval(yy, thickness, new Vector2d(0, this.image_.Height - 1));

				int sampleNum = (int)((xInterval[1] - xInterval[0] + 1) *
					(yInterval[1] - yInterval[0] + 1) / step / step);
				if (sampleNum < 0) throw new ArgumentException("sampleNum < 0!");

				int temp = 0;
				for (int x = (int)xInterval[0]; x <= (int)xInterval[1]; x += step, temp++)
				{
					for (int y = (int)yInterval[0] + (temp % 2) * step / 2; y <= (int)yInterval[1]; y += step)
					{
						Vector2d pos = new Vector2d(x, y);
						if ((pos - point).Length() > thickness)
						{
							continue;
						}
						candidates.Add(pos);
					}
				}

				if (candidates.Count < MAXNUMCANDIDATES) MAXNUMCANDIDATES = candidates.Count;

				candidatePositions.Add(candidates);

			}


			//
			this.candidatePositions_ = candidatePositions;


			// 2. build the graph
			List<List<EZnode>> eznodes = new List<List<EZnode>>();
			foreach (List<Vector2d> vlist in candidatePositions)
			{
				List<EZnode> nodeList = new List<EZnode>();
				foreach (Vector2d v in vlist)
				{
					nodeList.Add(new EZnode(v));
				}
				eznodes.Add(nodeList);
			}
			int N = eznodes.Count;
			for (int i = 0; i < N - 1; ++i)
			{
				List<EZnode> nodeList1 = eznodes[i];
				List<EZnode> nodeList2 = eznodes[i + 1];
				Vector2d pi = stroke[i], pj = stroke[i + 1];
				foreach (EZnode node1 in nodeList1)
				{
					foreach (EZnode node2 in nodeList2)
					{
						EZEdge e = new EZEdge(node1, node2);
						e.weight = this.ComputeEdgeWeight2(pi, pj, node1.pos_, node2.pos_);
						if (e.weight < 1e3)
						{
							node1.AddEdge(e);
							node2.AddEdge(e);
						}
					}
				}
			}

			// add virtual source and sink node
			EZnode virtualSource = new EZnode(new Vector2d());
			EZnode virtualTarget = new EZnode(new Vector2d());
			foreach (EZnode node in eznodes[0])
			{
				EZEdge e = new EZEdge(virtualSource, node);
				e.weight = 0.1;
				node.AddEdge(e);
				virtualSource.AddEdge(e);
			}
			foreach (EZnode node in eznodes[N - 1])
			{
				EZEdge e = new EZEdge(node, virtualTarget);
				e.weight = 0.1;
				node.AddEdge(e);
				virtualTarget.AddEdge(e);
			}
			List<EZnode> src = new List<EZnode>();
			List<EZnode> snk = new List<EZnode>();
			src.Add(virtualSource);
			snk.Add(virtualTarget);
			eznodes.Add(src);
			eznodes.Add(snk);

			// find the M shortest paths from source to sink
			List<Vector2d> candidate = new List<Vector2d>();
			for (int i = 0; i < MAXNUMCANDIDATES; ++i)
			{

				// find shortest path
				this.FindDistanceFrom(virtualSource, eznodes);
				List<EZnode> path = this.GetShortestPath(virtualSource, virtualTarget);
				foreach (EZnode node in path)
					candidate.Add(node.pos_);

				// isolate the nodes on the path from the graph (break connectivity)
				foreach (EZnode node in path)
				{
					node.Isolate();
				}

				break;	// to remove...

			}

			this.optimizedStroke_ = new List<Vector2d>();
			this.optimizedStroke_.AddRange(candidate);

			return candidate;
		}
		public void LocalOptimization(bool isLongEnough)
		{
			// too few points
			if (currentStroke_.size() <= 2)
			{
				ClearCurrent();
				return;
			}

			if (!isLongEnough)
			{ // stroke is not long enough
				currentStroke_.candidatePoints.Add(currentStroke_.points);
				List<double> pweights = new List<double>();
				for (int i = 0; i < currentStroke_.size(); ++i)
					pweights.Add(1.0);
				currentStroke_.candidatePointWeights.Add(pweights);
				currentStroke_.candidateCosts.Add(1.0f);
				currentStroke_.setStylistic(true);
				currentStroke_.smoothStrokes();
				strokes_.Add(currentStroke_);
				ClearCurrent();
				return;
			}

			Console.WriteLine("-------obtainLaplacian");
			currentStroke_.obtainLaplacian();

			Console.WriteLine("-------obtainTangentDirection");
			currentStroke_.obtainTangentDirection();

			Console.WriteLine("-------obtainCurrentStrokeCandidates");
			obtainCandidatesOfCurrentStroke();

			strokeGraph_.Clear();
			currentStroke_.smoothStrokes();
			strokes_.Add(currentStroke_);

		}
		public List<Vector2d> GetOptimizedStroke()
		{
			return this.currentStroke_.GetBestCandidateStroke();
		}
		public void ClearCurrent()
		{
			currentStroke_.Clear();
			strokeGraph_.Clear();
		}

		// add one stroke group node, a group node contains a set of sampled graph nodes
		// where each graph node is connecting to all nodes in the previous group node
		private void addOneGroupNode()
		{
			if(currentStroke_.size() == 0) return;

			int step = (int)Math.Max(2.0f, (double) Math.Round(this.radius_ / SAMPLE_RADIUS));
			int thickness = (int)this.radius_;
			int rangeLen = 2 * thickness + 1;
	
			GroupNode currNode = new GroupNode();
			int currentI = currentStroke_.size() - 1;
			Vector2d currentStrokePoint = currentStroke_.points[currentI];

			Vector2d xInterval = getInterval((int)currentStrokePoint.x, thickness, new Vector2d(0, this.image_.Width - 1));
			Vector2d yInterval = getInterval((int)currentStrokePoint.y, thickness, new Vector2d(0, this.image_.Height - 1));

			int sampleNum = (int)((xInterval[1] - xInterval[0] + 1) * (yInterval[1] - yInterval[0] + 1) / step / step);
			if (sampleNum < 0) throw new ArgumentException("sampleNum < 0!");

			currNode.distance = new List<double>(sampleNum);
			currNode.parentId = new List<int>(sampleNum);
			currNode.position = new List<Vector2d>(sampleNum);
			currNode.shapeCost = new List<List<double>>(sampleNum);
			currNode.gradientCost = new List<List<double>>(sampleNum);
			currNode.totalCost = new List<List<double>>(sampleNum);
			currNode.isUsed = new List<bool>(sampleNum);

			Console.WriteLine("sampleNum = " + sampleNum + ", radius = " + currentStroke_.halfStrokeWidth);

			int nodeCount = 0;
			if(currentI == 0){
				int temp = 0;
				for(int x = (int)xInterval[0]; x <= xInterval[1]; x += step, temp++){
					for(int y = (int)yInterval[0] + (temp % 2) * step / 2; y <= yInterval[1]; y += step){
						Vector2d pos = new Vector2d(x,y);
						if((pos-currentStrokePoint).Length() > thickness){
							continue;
						}

						double gradientCost		= 0;
						double shapeCost		= 0;

						List<double> shape_cost = new List<double>(); shape_cost.Add(shapeCost);
						currNode.shapeCost.Add(shape_cost);

						List<double> gradient_cost = new List<double>(); gradient_cost.Add(gradientCost);
						currNode.gradientCost.Add(gradient_cost);
						
						List<double> total_cost = new List<double>(); total_cost.Add(gradientCost+shapeCost);
						currNode.totalCost.Add(total_cost);
						
						List<bool> valids = new List<bool>(); valids.Add(true);
						currNode.isValid.Add(valids);
						
						currNode.isUsed.Add(false);
						currNode.distance.Add(0);
						currNode.parentId.Add(0);
						currNode.position.Add(pos);
					}
				}
			}else{
				int temp = 0, validCount = 0, invalidCount = 0;
				Vector2d prevStrokePoint = currentStroke_.points[currentI - 1];
				Vector2d midSeg = (currentStrokePoint + prevStrokePoint) / 2.0;

				for(int x = (int)xInterval[0]; x <= xInterval[1]; x += step, temp++){
					for(int y = (int)yInterval[0] + (temp % 2) * step / 2; y <= yInterval[1]; y += step){
						
						Vector2d pos = new Vector2d(x,y);
						if((pos-currentStrokePoint).Length() > thickness){
							continue;
						}
						
						// allocate sizes
						GroupNode prevNode = strokeGraph_[currentI - 1];
						int prevNodesCount = prevNode.size();
						List<double> shape_cost = new List<double>();
						List<double> gradient_cost = new List<double>();
						List<double> total_cost = new List<double>();
						List<bool> is_valid = new List<bool>();
						for (int i = 0; i < prevNodesCount;++i) {
							shape_cost.Add(0);
							gradient_cost.Add(0);
							total_cost.Add(0);
							is_valid.Add(true);
						}
						currNode.shapeCost.Add(shape_cost);
						currNode.gradientCost.Add(gradient_cost);
						currNode.totalCost.Add(total_cost);
						currNode.isValid.Add(is_valid);

						// add edges and compute distances
						Vector2d strokeEdge = currentStroke_.points[currentI] - currentStroke_.points[currentI - 1];
						strokeEdge = strokeEdge.Normalize();
						for(int k = 0; k < prevNodesCount; k++){
							Vector2d previousPos = prevNode.position[k];
							Vector2d midPos = (pos + previousPos) / 2.0f;
							Vector2d tiltVec = midPos - midSeg;
							tiltVec /= this.radius_;
							double shapeWeight    = this.getShapeCost(currentI, pos, previousPos);
							double gradientWeight;
							if((pos - previousPos).Dot(strokeEdge) < 0.0f){
								gradientWeight = localAlpha;
								currNode.isValid[currNode.isValid.Count-1][k] = false;
								invalidCount ++;
							}else{
								gradientWeight = this.getGradientCost(currentI, midPos);
								currNode.isValid[currNode.isValid.Count-1][k] = true;
								validCount ++;
							}

							double sumWeight										= shapeWeight + gradientWeight;
							currNode.shapeCost[currNode.shapeCost.Count-1][k]       = shapeWeight;
							currNode.gradientCost[currNode.gradientCost.Count-1][k] = gradientWeight;
							currNode.totalCost[currNode.totalCost.Count-1][k]       = sumWeight;
						}
						currNode.position.Add(pos);
						currNode.distance.Add(0.0f);
						currNode.parentId.Add(0);
						currNode.isUsed.Add(false);
					}
				}

				Console.WriteLine("valid and invalid: " + validCount + " " + invalidCount);

			}
			strokeGraph_.Add(currNode);
			nodeCount += currNode.size();
		}
		// update shortest path
		private void updateGraphShortestPath() {
			for(int nodeId = 0; nodeId < strokeGraph_[0].size(); nodeId++){
				strokeGraph_[0].distance[nodeId] = strokeGraph_[0].totalCost[nodeId][0];
				strokeGraph_[0].parentId[nodeId] = 0;
			}
			for(int groupNodeId = 1; groupNodeId < strokeGraph_.Count(); groupNodeId++){
				GroupNode gn = strokeGraph_[groupNodeId];
				for(int nodeId = 0; nodeId < strokeGraph_[groupNodeId].size(); nodeId++){
					if(gn.isUsed[nodeId]){
						strokeGraph_[groupNodeId].distance[nodeId] = double.MaxValue;
						strokeGraph_[groupNodeId].parentId[nodeId] = -1;
						continue;
					}
					int bestId = -1;
					double shortestD = double.MaxValue;
					for(int edgeId = 0; edgeId < strokeGraph_[groupNodeId].totalCost[nodeId].Count(); edgeId++){
						if(strokeGraph_[groupNodeId - 1].isUsed[edgeId]) continue;
						if(!strokeGraph_[groupNodeId].isValid[nodeId][edgeId]) continue;
						double temp = strokeGraph_[groupNodeId].totalCost[nodeId][edgeId] + strokeGraph_[groupNodeId - 1].distance[edgeId];
						if(shortestD > temp){
							shortestD = temp;
							bestId = edgeId;
						}
					}
					strokeGraph_[groupNodeId].distance[nodeId] = shortestD;
					strokeGraph_[groupNodeId].parentId[nodeId] = bestId;
					if(shortestD == double.MaxValue) return;
				}
			}
		}
		// shape cost
		// directional gradient cost
		private double getShapeCost(int currentI, Vector2d currentPos, Vector2d previousPos){
			Vector2d strokeEdge = currentStroke_.points[currentI] - currentStroke_.points[currentI - 1];
			Vector2d nodeEdge	= currentPos - previousPos;
			double deltaE = (strokeEdge - nodeEdge).Length() / this.radius_;
			return deltaE * deltaE;
		}
		private double getGradientCost(int currentI, Vector2d currentMidPos){

			Vector2d seg = currentStroke_.points[currentI] - currentStroke_.points[currentI - 1];
			Vector2d segV = seg / (seg.Length() + EPS);

			double fdogValue  = this.fdogFilter_.getFilterValue(segV, currentMidPos);
			
			double v = localAlpha * (1 - fdogValue);
			return v;
		}
		private double ComputeEdgeWeight(Vector2d pi, Vector2d pj, Vector2d qi, Vector2d qj)
		{
			// pi pj are the stroke points, qi, qj are sampled points
			int X = 4, Y = 7;
			double rho = 0.99;
			double sigma_c = 1.0;
			double sigma_s = 1.6;
			double sigma_m = 3.0;
			Vector2d m = (qi + qj) / 2;
			Vector2d v = (qj - qi).Normalize();
			Vector2d u = new Vector2d(v.y, -v.x).Normalize();
			double Hmv = 0;
			for (int y = -Y; y <= Y; ++y)
			{
				double sum = 0;
				for (int x = -X; x <= X; ++x)
				{
					Vector2d lxy = m + x * u + y * v;
					int I = (int)lxy.x, J = (int)lxy.y;
					if (I >= this.image_.Width || I < 0 ||
						J >= this.image_.Height || J < 0)
						continue;
					double fx = Gaussian(x, sigma_c) - rho * Gaussian(x, sigma_s);
					Gray g = this.image_[J, I]; // (x,y);
					sum += g.Intensity * fx / 255.0;
				}
				Hmv += Gaussian(y, sigma_m) * sum;
			}
			if (Hmv < 0)
				Hmv = 1 + Math.Tanh(Hmv);
			else
				Hmv = 1;
		//	return Hmv;
			return ((pj - pi) - (qj - qi)).SQLength() / (this.radius_ * this.radius_) + 0.1 * Hmv;
		}
		private double ComputeEdgeWeight2(Vector2d pi, Vector2d pj, Vector2d qi, Vector2d qj)
		{
			// pi pj are the stroke points, qi, qj are sampled points
			if ((qj-qi).Dot(pj-pi) <= 0) return BIGCONSTANT;
			
			Vector2d m = (qi + qj) / 2;
			Vector2d v = (qj - qi).Normalize();
			double shapeWeight = ((pj - pi) - (qj - qi)).SQLength() / (this.radius_ * this.radius_);
			double gradientWeight = 1e5;	// a big constant
			double fdogVal = this.fdogFilter_.getFilterValue(v, m);
			gradientWeight = 1.0 - fdogVal;
			
			return shapeWeight + 0.5 * gradientWeight;

		}
		private double Gaussian(double x, double sigma)
		{
			return Math.Exp(-x * x / (2 * sigma * sigma)) / (Math.Sqrt(2*Math.PI)*sigma);
		}
		private Vector2d getInterval(int center, int width, Vector2d bounds)
		{
			Vector2d itv = new Vector2d();
			itv[0] = Math.Max(bounds[0], center - width);
			itv[1] = Math.Min(bounds[1], center + width);
			return itv;
		}
		private void obtainCandidatesOfCurrentStroke()	// get candidates shortest path
		{
			currentStroke_.candidatePoints.Clear();
			currentStroke_.candidatePointWeights.Clear();

			updateGraphShortestPath();
			Vector2d v = obtainOneCandidateOfCurrentStroke();

			if (usingSemiOpt_)
			{
				for (int i = 0; i < 10 && v[0] != double.MaxValue; i++)
				{
					updateGraphShortestPath();
					v = obtainOneCandidateOfCurrentStroke();
				}
			}
			else
			{
				for (int i = 0; i < 10 && v[0] != double.MaxValue && currentStroke_.candidatePoints.Count == 0; i++)
				{
					updateGraphShortestPath();
					v = obtainOneCandidateOfCurrentStroke();
				}
			}

			//int foundPos = currentStroke_.containSimilarCandidate(currentStroke_.points);
			if (currentStroke_.candidatePoints.Count == 0)
			{
				currentStroke_.candidatePoints.Add(currentStroke_.points);
				List<double> pweights = new List<double>();
				for (int i = 0; i < currentStroke_.size(); ++i)
					pweights.Add(1.0);
				currentStroke_.candidatePointWeights.Add(pweights);
				currentStroke_.candidateCosts.Add(1.0f);
				currentStroke_.setStylistic(true);
			}
			else
			{
				currentStroke_.setStylistic(false);
				currentStroke_.isambiguious = currentStroke_.candidatePoints.Count > 1;
			}

			currentStroke_.bestCandidateId = 0;

		}
		private Vector2d obtainOneCandidateOfCurrentStroke()
		{

			if (currentStroke_.size() < 1) return new Vector2d(double.MaxValue, double.MaxValue);	// empty

			Vector2d[] targets = new Vector2d[currentStroke_.size()];
			double[] targetGradWeights = new double[currentStroke_.size()];
			if (this.strokeGraph_.Count <= 1)
			{
				Console.WriteLine("size = 0");
				return new Vector2d(double.MaxValue, double.MaxValue);
			}

			int bestId = -1;
			double shortestD = double.MaxValue;
			GroupNode gn = strokeGraph_[strokeGraph_.Count - 1];
			Console.WriteLine("strokeGraph_.size: " + strokeGraph_.Count);
			for (int i = 0; i < gn.size(); i++)
			{
				if (!gn.isUsed[i] && shortestD > gn.distance[i])
				{
					shortestD = gn.distance[i];
					bestId = i;
				}
			}
			if (shortestD == double.MaxValue)
			{
				return new Vector2d(double.MaxValue, double.MaxValue);
			}
			int currentId = bestId;
			double shapeWeightSum = 0, gradientWeightSum = 0;
			for (int i = strokeGraph_.Count - 1; i >= 0; i--)
			{
				strokeGraph_[i].isUsed[currentId] = true;
				int pid = strokeGraph_[i].parentId[currentId];
				Vector2d pos = strokeGraph_[i].position[currentId];
				shapeWeightSum += strokeGraph_[i].shapeCost[currentId][pid];
				gradientWeightSum += strokeGraph_[i].gradientCost[currentId][pid];
				targetGradWeights[i] = 2 + 0.1 - strokeGraph_[i].gradientCost[currentId][pid] / localAlpha;
				targets[i] = pos;
				currentId = pid;
			}
			double avgShape = shapeWeightSum / (strokeGraph_.Count - 1);
			double avgGradient = gradientWeightSum / (strokeGraph_.Count - 1);
			double avgSum = avgShape + avgGradient;

			double gradientTh = GRADIENTTHRESHOLD, shapeTh = SHAPETHRESHOLD;

			if (avgGradient < gradientTh && avgShape < shapeTh)
			{

				List<Vector2d> targetsPoints = targets.ToList();
				if (currentStroke_.containSimilarCandidate(targetsPoints) == -1)
				{
					currentStroke_.candidatePoints.Add(targetsPoints);
					currentStroke_.candidatePointWeights.Add(targetGradWeights.ToList());
					double avgSC = avgShape / SHAPETHRESHOLD;
					double avgGC = avgGradient / GRADIENTTHRESHOLD;
					currentStroke_.candidateCosts.Add((avgSC + avgGC) / 2.0f);

					Console.WriteLine("√");
					return new Vector2d(avgShape, avgGradient);
				}
				else
				{
					Console.WriteLine("×");
					return new Vector2d(-1, -1);
				}
			}
			else if (avgShape < 1.5f * shapeTh)
			{
				Console.WriteLine("×!");
				return new Vector2d(-1, -1);
			}
			else
			{
				Console.WriteLine("×!!end");
				return new Vector2d(double.MaxValue, double.MaxValue);
			}
		}
		private List<EZnode> GetShortestPath(EZnode source, EZnode target)
		{
			// get the path from source to target
			List<EZnode> path = new List<EZnode>();
			EZnode current = target;
			while (current != source)
			{
				path.Add(current);
				current = current.parent_;
			}
			// exclude the source and sink node from the path
			path.Remove(target);
			return path;
		}
		private void FindDistanceFrom(EZnode node, List<List<EZnode>> allnodes)
		{
			PriorityQueue Q = new PriorityQueue();
			foreach (List<EZnode> nodeList in allnodes)
			{
				foreach (EZnode nd in nodeList)
				{
					nd.distance_ = double.MaxValue;
					nd.parent_ = null;
					Q.Insert(nd);
				}
			}

			node.distance_ = 0;
			Q.Update(node);
			while (!Q.IsEmpty())
			{
				EZnode s = Q.DeleteMin() as EZnode;
				foreach (EZEdge e in s.adjEdges_)
				{
					EZnode t = e.node1_ == s ? e.node2_ : e.node1_;
					double dis = s.distance_ + e.weight;
					if (dis < t.distance_)
					{
						t.distance_ = dis;
						t.parent_ = s;
						Q.Update(t);
					}
				}
			}
		}
		private void ComputeImageGradient()
		{
			if (this.image_ == null) return;

			Image<Gray, float> dx = this.image_.Sobel(0, 1, 3);
			Image<Gray, float> dy = this.image_.Sobel(1, 0, 3);


			int i, j;
			int w = this.image_.Width, h = this.image_.Height;

			//Compute the gradient_ magnitude based on derivatives in x and y:
			this.gradient_ = new double[w, h];
			double max = double.MinValue;
			for (i = 0; i < w; i++)
			{
				for (j = 0; j < h; j++)
				{
					double dxij = dx[j, i].Intensity, dyij = dy[j, i].Intensity;
					double d = (double)Math.Sqrt((dxij * dxij) + (dyij * dyij));
					this.gradient_[i, j] = d;
					if (d > max)
					{
						max = d;
					}
				}
			}


			// max gray intensity value
			max = 255 * Math.Sqrt(2);


			// normalize to [0,1];
			for (i = 0; i < w; i++)
			{
				for (j = 0; j < h; j++)
				{
					this.gradient_[i, j] /= max;
				}
			}
			// get maxmiums as in 3x3 neighborhood
			this.isLocalMax_ = new bool[w, h];
			int L = 1;
			for (i = 0; i < w; i++)
			{
				for (j = 0; j < h; j++)
				{
					double d = this.gradient_[i, j];
					bool ismax = true;
					for (int k = -L; k <= L; ++k)
					{
						for (int l = -L; l <= L; ++l)
						{
							int I = i + k, J = j + l;
							if (I < 0 || I >= this.image_.Width ||
								J < 0 || J >= this.image_.Height)
								continue;
							if (this.gradient_[I, J] > d)
							{
								ismax = false;
								break;
							}
						}
						if (!ismax)
							break;
					}
					this.isLocalMax_[i, j] = ismax;
				}
			}
		}
		// drawing function
		public void Draw()
		{
			// highlight strokes_ (before/after optimization)
			if (this.initialStroke_ != null)
			{
				GLRenderer.DrawStroke2(this.initialStroke_, Color.Cyan, 4);
			}
			if (this.optimizedStroke_ != null)
			{
				GLRenderer.DrawStroke2(this.optimizedStroke_, Color.Lime, 4);
			}

			// 
			if (false && this.candidatePositions_ != null)
			{
				int index = 0;
				foreach (List<Vector2d> candidates in this.candidatePositions_)
				{
					GLRenderer.DrawPoints2(candidates, 4, Utils.ColorMall[index++ % Utils.ColorMall.Length]);
				}
			}
		}

	}
}
