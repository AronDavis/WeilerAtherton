using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeilerAtherton
{
    public partial class Main : Form
    {
        System.Drawing.Graphics g;
        Pen pen;
        public Main()
        {
            InitializeComponent();
            g = this.CreateGraphics();
            pen = new Pen(Color.Red);
            txtInput.Text = "0,0|100,0|100,100|0,100||0,0|200,200|0,200";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] lists = txtInput.Text.Split(new string[] { "||" }, StringSplitOptions.None);

            string[] first = lists[0].Split('|');
            string[] second = lists[1].Split('|');

            PointF[] clip = convert(first);
            PointF[] shape = convert(second);

            if(clip == null || shape == null) return;

            DrawLines(clip, Color.Red);
            DrawLines(shape, Color.Blue);

            doClip(clip, shape);
        }

        private void DrawLines(PointF[] points, Color color)
        {
            pen.Color = color;
            for (int i = 0; i < points.Length; i++)
            {
                PointF p1 = points[i];
                PointF p2 = points[(i + 1) % points.Length];
                g.DrawLine(pen, p1, p2);
            }
        }

        private PointF[] convert(string[] text)
        {
            PointF[] points = new PointF[text.Length];

            for(int i = 0; i < text.Length; i++)
            {
                string[] split = text[i].Split(',');
                float x;
                float y;
                if(!float.TryParse(split[0], out x)) return null;
                if (!float.TryParse(split[1], out y)) return null;

                points[i] = new PointF(x, y);
            }

            return points;
        }

        private void doClip(PointF[] clip, PointF[] shape)
        {
            List<DeepPoint> deepShape = new List<DeepPoint>(Array.ConvertAll(shape, p => new DeepPoint(p, DeepPoint.PointType.Normal, p.InOrOut(clip))));
            List<DeepPoint> deepClip = new List<DeepPoint>(Array.ConvertAll(shape, p => new DeepPoint(p, DeepPoint.PointType.Normal, p.InOrOut(shape))));

            //TODO: make a dictionary to get an intersection (DeepPoint) from two lines (4 DeepPoints) to ensure we don't get precision errors

            for (int i = 0; i < deepShape.Count; i++)
            {
                DeepPoint p1 = deepShape[i];
                DeepPoint p2 = deepShape[(i + 1) % deepShape.Count];

                //check for intersections
                for (int j = 0; j < deepClip.Count; j++)
                {
                    DeepPoint c1 = deepClip[j];
                    DeepPoint c2 = deepClip[(j + 1) % deepClip.Count];

                    if (Line.HasIntersection(p1, p2, c1, c2))
                    {
                        PointF intersection = Line.Intersection(p1, p2, c1, c2);
                        p1.AddIntersection(new DeepPoint(intersection, DeepPoint.PointType.Intersection, DeepPoint.PointStatus.Undetermined));
                        //TODO: also add to c1Deep
                    }
                }

                //TODO: sort intersections between point 1 and point 2 by proximity to point1
                //TODO: more intersections to be in deepShape, after point1
                
                //IMPLEMENT SORT HERE <-------

                //loop through intersections between p1 and p2
                for(int j = 0; j < p1.intersections.Count; j++)
                {
                    DeepPoint intersection = p1.intersections[j];
                    //if there's a previous intersection
                    if(j>0)
                    {
                        if (p1.intersections[j-1].status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter pStatus to Out
                        else intersection.status = DeepPoint.PointStatus.In; //TODO: set inter as In
                    }
                    else if(p1.status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter as Out
                    else intersection.status = DeepPoint.PointStatus.In; //set inter as In
                }
            }
            //TODO: put above for loop into a method and run it with shape/clip reversed

            List<List<PointF>> output = new List<List<PointF>>();
            List<PointF> currentShape = new List<PointF>();

            IntegrateIntersections(ref deepShape);
            IntegrateIntersections(ref deepClip);

            //Use these to jump from list to list
            Dictionary<DeepPoint, int> shapeIntersectionToClipIndex = new Dictionary<DeepPoint, int>();
            Dictionary<DeepPoint, int> clipIntersectionToShapeIndex = new Dictionary<DeepPoint, int>();
            BuildIntersectionMap(ref shapeIntersectionToClipIndex, deepShape, ref clipIntersectionToShapeIndex, deepClip);

            //start from entering points
            List<int> iEntering = new List<int>();

            //Get entering intersections
            for (int i = 0; i < deepShape.Count; i++)
            {
                DeepPoint point = deepShape[i];
                if(point.type == DeepPoint.PointType.Intersection && point.status == DeepPoint.PointStatus.In)
                    iEntering.Add(i);
            }

            //TODO: implement this into a loop of sorts + check if count > 0
            int goToIndex = iEntering[0]; //should be set to loop through these points

            for (int i = 0; i < deepShape.Count; i++)
            {
                DeepPoint p1 = deepShape[i];
                DeepPoint p2 = deepShape[(i + 1) % deepShape.Count];

                if (p1.type == DeepPoint.PointType.Normal)
                {
                    if (p1.status == DeepPoint.PointStatus.In)
                    {
                        //break when we get back to start
                        if (currentShape[0] == p1.p) break;
                        currentShape.Add(p1.p);

                        //point2 must be heading outwards
                        if (p2.type == DeepPoint.PointType.Intersection)
                        {
                            //go to clipPoints loop and start from intersection
                            goToIndex = shapeIntersectionToClipIndex[p2];
                            break;
                        }
                    }
                    //we don't care about point2 here
                    //if point1 is an outside normal point,
                    //	then point2 must either be an outside normal point OR an intersection going inwards.
                    //		The former doesn't not need to be handled, the latter will be handled upon looping

                }
                else //p1 is an intersection
                {
                    //break when we get back to start
                    if (currentShape[0] == p1.p) break;

                    //we must add point 1 since it's on the border
                    currentShape.Add(p1.p);

                    //exiting
                    if (p1.status == DeepPoint.PointStatus.Out)
                    {
                        //go to clipPoints loop and start from after intersection;
                        goToIndex = (shapeIntersectionToClipIndex[p1]+1) % deepClip.Count;
                        break;
                    }
                }
            } //end deepShape for


            //we should only get here from a go to from shapePoints
            for (int i = 0; i < deepClip.Count; i++)
            {
                DeepPoint p1 = deepClip[i];
                DeepPoint p2 = deepClip[(i + 1) % deepClip.Count];

                if(p1.type == DeepPoint.PointType.Intersection)
                {
                    //break when we get back to start
                    if (currentShape[0] == p1.p) break;

                    //we must add point 1 since it's on the border
                    currentShape.Add(p1.p);

                    if(p1.status == DeepPoint.PointStatus.In)
                    {
                        //go to shapePoints loop and start from after point1
                        goToIndex = (clipIntersectionToShapeIndex[p1] + 1) % deepShape.Count;
                        break;
                    }
                }
                else //p1 is normal
                {
                    if(p1.status == DeepPoint.PointStatus.In)
                    {
                        //break when we get back to start
                        if (currentShape[0] == p1.p) break;

                        //we must add point 1 since it's on the border
                        currentShape.Add(p1.p);
                    }
                }
            } //end deepClip for

        } //end doClip

        //TODO: test this
        private void IntegrateIntersections(ref List<DeepPoint> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                DeepPoint normal = list[i];
                for (int j = 0; j < normal.intersections.Count; j++)
                {
                    DeepPoint intersection = normal.intersections[j];

                    list.Insert(i+1, intersection);
                    i++;
                }
            }
        }

        //TODO: test
        private void BuildIntersectionMap(ref Dictionary<DeepPoint, int> fromMap, List<DeepPoint> from, ref Dictionary<DeepPoint, int> toMap, List<DeepPoint> to)
        {
            for (int i = 0; i < from.Count; i++)
            {
                DeepPoint point = from[i];

                if (point.type == DeepPoint.PointType.Intersection)
                {
                    //we can do both at once since we should have the same number of intersections
                    fromMap.Add(point, to.IndexOf(point));
                    toMap.Add(point, i);
                }
            }
        }
    }
}
