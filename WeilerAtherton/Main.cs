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
            pen = new Pen(Color.Red, 2);
            txtInput.Text = "100,100|200,100|200,200|100,200||150,50|175,50|175,250|150,250";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
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
            List<DeepPoint> deepClip = new List<DeepPoint>(Array.ConvertAll(clip, p => new DeepPoint(p, DeepPoint.PointType.Normal, p.InOrOut(shape))));

            for (int i = 0; i < deepShape.Count; i++)
            {
                DeepPoint p1 = deepShape[i];
                DeepPoint p2 = deepShape[(i + 1) % deepShape.Count];

                //check for intersections
                for (int j = 0; j < deepClip.Count; j++)
                {
                    DeepPoint c1 = deepClip[j];
                    DeepPoint c2 = deepClip[(j + 1) % deepClip.Count];

                    PointF interOutput;
                    if (Line.Intersection(p1, p2, c1, c2, out interOutput))
                    {
                        //This ensures that we have the same intersection added to both (avoid precision errors)
                        DeepPoint intersection = new DeepPoint(interOutput, DeepPoint.PointType.Intersection, DeepPoint.PointStatus.Undetermined);
                        p1.intersections.Add(intersection);
                        c1.intersections.Add(intersection);
                    }
                }

                //sort intersections by distance to p1
                p1.SortIntersections();

                
                //loop through intersections between p1 and p2
                for(int j = 0; j < p1.intersections.Count; j++)
                {
                    //TODO: test that changing intersection.status affects the intersection in clip as well
                    DeepPoint intersection = p1.intersections[j];

                    //if there's a previous intersection
                    if(j>0)
                    {
                        DeepPoint prev = p1.intersections[j-1];

                        //TODO: could handle "if" by simply removing the duplicate (wouldn't have to remove dupes later + less processing
                        if (intersection.p == prev.p) intersection.status = prev.status; //handle duplicate intersections (caused by overlap normal point)
                        else if (prev.status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter pStatus to Out
                        else intersection.status = DeepPoint.PointStatus.In; //set inter as In
                    }
                    else if(p1.status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter as Out
                    else intersection.status = DeepPoint.PointStatus.In; //set inter as In
                }
            }

            //sort all intersections in clip
            for(int i = 0; i < deepClip.Count; i++)
            {
                deepClip[i].SortIntersections();
            }

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

            //no intersecion = nothing to return
            if (iEntering.Count == 0) return;

            List<List<DeepPoint>> output = new List<List<DeepPoint>>();
            List<DeepPoint> currentShape = new List<DeepPoint>();

            //TODO: add method to ignore entering points that were included in an output shape already

            //go through all of our entering points
            for (int mainCount = 0; mainCount < iEntering.Count; mainCount++)
            {
                int goToIndex = iEntering[mainCount];

                bool complete = false;

                while (!complete)
                {
                    //loop through all shape points starting at goToIndex
                    for (int iCount = goToIndex; iCount < deepShape.Count + goToIndex; iCount++)
                    {
                        int i = iCount % deepShape.Count;
                        DeepPoint p1 = deepShape[i];
                        DeepPoint p2 = deepShape[(i + 1) % deepShape.Count];

                        if (p1.type == DeepPoint.PointType.Normal)
                        {
                            if (p1.status == DeepPoint.PointStatus.In)
                            {
                                //break when we get back to start
                                if (currentShape.Count > 0 && currentShape[0] == p1)
                                {
                                    complete = true;
                                    break;
                                }

                                currentShape.Add(p1);

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
                            if (currentShape.Count > 0 && currentShape[0] == p1)
                            {
                                complete = true;
                                break;
                            }

                            //we must add point 1 since it's on the border
                            currentShape.Add(p1);

                            //exiting
                            if (p1.status == DeepPoint.PointStatus.Out)
                            {
                                //go to clipPoints loop and start from after intersection;
                                goToIndex = (shapeIntersectionToClipIndex[p1] + 1) % deepClip.Count;
                                break;
                            }
                        }
                    } //end deepShape for

                    //break while loop if complete
                    if (complete) break;

                    //loop through all clip points starting at goToIndex
                    //we should only get here from a go to from shapePoints
                    for (int iCount = goToIndex; iCount < deepClip.Count + goToIndex; iCount++)
                    {
                        int i = iCount % deepClip.Count;
                        DeepPoint p1 = deepClip[i];
                        DeepPoint p2 = deepClip[(i + 1) % deepClip.Count];

                        if (p1.type == DeepPoint.PointType.Intersection)
                        {
                            //break when we get back to start
                            if (currentShape.Count > 0 && currentShape[0] == p1)
                            {
                                complete = true;
                                break;
                            }

                            //we must add point 1 since it's on the border
                            currentShape.Add(p1);

                            //if it was going inwards
                            if (p1.status == DeepPoint.PointStatus.In)
                            {
                                //go to shapePoints loop and start from after point1
                                goToIndex = (clipIntersectionToShapeIndex[p1] + 1) % deepShape.Count;
                                break;
                            }
                        }
                        else //p1 is normal
                        {
                            if (p1.status == DeepPoint.PointStatus.In)
                            {
                                //break when we get back to start
                                if (currentShape.Count > 0 && currentShape[0] == p1)
                                {
                                    complete = true;
                                    break;
                                }

                                //we must add point 1 since it's on the border
                                currentShape.Add(p1);
                            }
                        }
                    } //end deepClip for
                }//end while loop

                output.Add(currentShape);
                currentShape = new List<DeepPoint>();
            }//end main for loop
            
            //remove duplicate entries
            for (int i = 0; i < output.Count; i++)
            {
                for(int j = 0; j < output[i].Count; j++)
                {
                    //remove duplicates
                    if(output[i][j].p == output[i][(j + 1) % output[i].Count].p)
                    {
                        //remove current
                        output[i].RemoveAt(j);
                        j--;
                    }
                }
            }

            pen.Width = 5;
            DrawLines(Array.ConvertAll(output[0].ToArray(), p => p.p), Color.Green);
            pen.Width = 2;
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
