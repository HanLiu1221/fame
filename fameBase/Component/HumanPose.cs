using System;
using System.Collections.Generic;

using Geometry;

namespace Component
{
    public class HumanPose
    {

    }// HumanPose

    class BodyNode
    {
        // represent by a solid sphere
        string _name;
        Vector3d _originPos;
        Vector3d _pos;
        bool _isRoot = false; // move all nodes together
        List<BodyNode> _adjNodes = new List<BodyNode>();
        List<BodyNode> _childrenNodes = new List<BodyNode>();
        List<BodyBone> _adjBones = new List<BodyBone>();

        public BodyNode(string name, Vector3d v)
        {
            _name = name;
            _pos = new Vector3d(v);
            _originPos = new Vector3d(v);
        }

        public Vector3d _POS
        {
            get
            {
                return _pos;
            }
        }

        public void setAsRoot()
        {
            _isRoot = true;
        }

        public bool isRoot()
        {
            return _isRoot;
        }

        public void addAdjNode(BodyNode node)
        {
            _adjNodes.Add(node);
        }

        public void addChildNode(BodyNode node)
        {
            _childrenNodes.Add(node);
        }

        public void addAdjBone(BodyBone bone)
        {
            _adjBones.Add(bone);
        }

        public List<BodyNode> getAdjNodes()
        {
            return _adjNodes;
        }

        public List<BodyNode> getChildrenNodes()
        {
            return _childrenNodes;
        }

        public void Transform(Matrix4d T)
        {
            _pos = (T * new Vector4d(_pos, 1)).ToVector3D();
        }

        public void TransformFromOrigin(Matrix4d T)
        {
            _pos = (T * new Vector4d(_originPos, 1)).ToVector3D();
        }
    }// BodyNode

    class BodyBone
    {
        // represent by a cylinder + ellipsoid
        BodyNode _src;
        BodyNode _dst;
        double _radius = 0.01; // of the cyclinder
        Ellipsoid _entity;
        double _len = 0.002;
        double _wid = 0.002;
        double _thickness = 0.002;

        public BodyBone(BodyNode s, BodyNode d)
        {
            _src = s;
            _dst = d;
            _src.addAdjBone(this);
            _dst.addAdjBone(this);
            _entity = new Ellipsoid(_len, _wid, _thickness, 20);
            updateEntity();
        }

        public void updateEntity()
        {
            // body nodes have been updated
            _entity.create(_src._POS, _dst._POS);
        }
    }// BodyBone
}
