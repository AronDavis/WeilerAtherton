using System;
using System.Collections.Generic;
using System.Drawing;

namespace WeilerAtherton
{
    public static class Extensions
    {
        public static DeepPoint.PointStatus InOrOut(this PointF point, PointF[] shape)
        {
            //a point that we can guarantee is outside of our shape
            PointF outside = new PointF(float.MaxValue, float.MaxValue);

            //find the leftmost and topmost bounds
            for (int i = 0; i < shape.Length; i++)
            {
                PointF p = shape[i];

                if (p.X < outside.X) outside.X = p.X;
                if (p.Y < outside.Y) outside.Y = p.Y;
            }

            outside.X--;
            outside.Y--;

            int intersections = 0;
            for (int i = 0; i < shape.Length; i++)
            {                

                PointF c1 = shape[i];
                PointF c2 = shape[(i + 1) % shape.Length];

                float det;
                float A1, B1, C1;
                float A2, B2, C2;

                do
                {
                    outside.X--;
                    //https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/#line_line_intersection
                    A1 = c2.Y - c1.Y;
                    B1 = c1.X - c2.X;
                    C1 = A1 * c1.X + B1 * c1.Y;

                    A2 = outside.Y - point.Y;
                    B2 = point.X - outside.X;
                    C2 = A2 * point.X + B2 * point.Y;

                    det = A1 * B2 - A2 * B1;
                } 
                while (det == 0); //keep adjusting until are lines aren't parallel
              
                PointF intersect = new PointF(B2 * C1 - B1 * C2, A1 * C2 - A2 * C1); //would normally be /det
                intersect.X /= det;
                intersect.Y /= det;

                //using multiplication instead since it's less likely to have precision errors
                float xMin = Math.Min(c1.X, c2.X);
                float xMax = Math.Max(c1.X, c2.X);

                float yMin = Math.Min(c1.Y, c2.Y);
                float yMax = Math.Max(c1.Y, c2.Y);

                float xMin2 = Math.Min(point.X, outside.X);
                float xMax2 = Math.Max(point.X, outside.X);

                float yMin2 = Math.Min(point.Y, outside.Y);
                float yMax2 = Math.Max(point.Y, outside.Y);

                if (xMin <= intersect.X && intersect.X <= xMax 
                    && yMin <= intersect.Y && intersect.Y <= yMax
                    && xMin2 <= intersect.X && intersect.X <= xMax2
                    && yMin2 <= intersect.Y && intersect.Y <= yMax2)
                    intersections++;
            }

            //covers 0 + evens
            if (intersections % 2 == 0) return DeepPoint.PointStatus.Out;
            else return DeepPoint.PointStatus.In; //odds > 0
        }
    }
}
