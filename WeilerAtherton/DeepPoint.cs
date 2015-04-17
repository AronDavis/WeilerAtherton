using System;
using System.Collections.Generic;
using System.Drawing;

namespace WeilerAtherton
{
    struct DeepPoint
    {
        public PointF p;
        public PointType type;
        public PointStatus status;
        public enum PointType
        {
            Normal,
            Intersection
        }

        public enum PointStatus
        {
            In,
            Out,
            Undetermined
        }

        public DeepPoint(PointF point, PointType pType, PointStatus pStatus)
        {
            p = point;

            type = pType;
            status = pStatus;
        }
    }
}
