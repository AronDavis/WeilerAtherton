using System;
using System.Collections.Generic;
using System.Drawing;

namespace WeilerAtherton
{
    public static class Extensions
    {
        public static DeepPoint.PointStatus InOrOut(this PointF point, PointF[] shape)
        {
            return DeepPoint.PointStatus.Undetermined;
        }
    }
}
