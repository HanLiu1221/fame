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
        public Common() { }
    }
}
