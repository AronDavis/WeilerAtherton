﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeilerAtherton
{
    static class Line
    {
        public static bool HasIntersection(PointF start1, PointF end1, PointF start2, PointF end2)
        {
            return false;
        }

        public static bool HasIntersection(DeepPoint start1, DeepPoint end1, DeepPoint start2, DeepPoint end2)
        {
            return false;
        }

        public static PointF Intersection(PointF start1, PointF end1, PointF start2, PointF end2)
        {
            return new PointF();
        }

        public static DeepPoint Intersection(DeepPoint start1, DeepPoint end1, DeepPoint start2, DeepPoint end2)
        {
            return new DeepPoint();
        }
    }
}
