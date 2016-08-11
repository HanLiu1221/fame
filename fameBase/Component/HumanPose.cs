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
        string _name;
        Vector3d _originPos;
        Vector3d _pos;
        bool _isRoot = false; // move all nodes together
        List<BodyNode> _adjNodes;
        List<BodyNode> _childrenNodes;

        public BodyNode(string name, Vector3d v)
        {
            _name = name;
            _pos = new Vector3d(v);
            _originPos = new Vector3d(v);
        }
    }// BodyNode

    class BodyBone
    {
        BodyNode src;
        BodyNode dst;
        double radius; // of the cyclinder
    }// BodyBone
}
