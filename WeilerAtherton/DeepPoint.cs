using System;
using System.Collections.Generic;
using System.Drawing;

namespace WeilerAtherton
{
    public class DeepPoint
    {
        public PointF p;
        public PointType type;
        public PointStatus status;

        public List<DeepPoint> intersections;

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

        public DeepPoint() { }

        public DeepPoint(PointF point, PointType pType, PointStatus pStatus)
        {
            p = point;

            type = pType;
            status = pStatus;

            intersections = new List<DeepPoint>();
        }

        public void SortIntersections()
        {
            if (intersections.Count <= 1) return;

            throw new NotImplementedException();
            //sort
        }
    }
}
