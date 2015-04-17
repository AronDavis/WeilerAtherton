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
            //TODO: use dictionaries to jump between lists...it will give you the index of where to go from one to the other
            List<DeepPoint> deepShape = new List<DeepPoint>();
            List<DeepPoint> deepClip = new List<DeepPoint>();

            //Use these to jump from list to list
            Dictionary<DeepPoint, int> intersectionToClipIndex;
            Dictionary<DeepPoint, int> intersectionToShapeIndex;

            //TODO: make a dictionary to get an intersection (DeepPoint) from two lines (4 DeepPoints) to ensure we don't get precision errors

            for(int i = 0; i < shape.Length; i++)
            {
                PointF p1 = shape[i];
                PointF p2 = shape[(i + 1) % shape.Length];

                DeepPoint p1Deep = new DeepPoint(p1, DeepPoint.PointType.Normal, p1.InOrOut(clip));
                  
                deepShape.Add(p1Deep);

                //check for intersections
                for(int j = 0; j < clip.Length; j++)
                {
                    PointF c1 = clip[j];
                    PointF c2 = clip[(j + 1) % clip.Length];

                    if (Line.HasIntersection(p1, p2, c1, c2))
                    {
                        PointF intersection = Line.Intersection(p1, p2, c1, c2);
                        p1Deep.AddIntersection(new DeepPoint(intersection, DeepPoint.PointType.Intersection, DeepPoint.PointStatus.Undetermined));
                        //TODO: also add to c1Deep
                    }
                }

                //TODO: sort intersections between point 1 and point 2 by proximity to point1
                //TODO: more intersections to be in deepShape, after point1
                
                //IMPLEMENT SORT HERE <-------

                //loop through intersections between p1 and p2
                for(int j = 0; j < p1Deep.intersections.Count; j++)
                {
                    DeepPoint intersection = p1Deep.intersections[j];
                    //if there's a previous intersection
                    if(j>0)
                    {
                        if (p1Deep.intersections[j-1].status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter pStatus to Out
                        else intersection.status = DeepPoint.PointStatus.In; //TODO: set inter as In
                    }
                    else if(p1Deep.status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter as Out
                    else intersection.status = DeepPoint.PointStatus.In; //set inter as In
                }
            }
            //TODO: put above for loop into a method and run it with shape/clip reversed

            List<List<PointF>> output = new List<List<PointF>>();
            List<PointF> currentShape = new List<PointF>();

            //start from first entering point
            List<int> iNormals = new List<int>();
            List<int> iIntersections = new List<int>();

            for (int i = 0; i < deepShape.Count; i++)
            {
                DeepPoint deepP = deepShape[i];
                for (int j = 0; j < deepP.intersections.Count; j++)
                {
                    DeepPoint intersection = deepP.intersections[j];
                    if (intersection.status == DeepPoint.PointStatus.In)
                    {
                        iNormals.Add(i);
                        iIntersections.Add(j);
                    }
                }
            }

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
                            //go to clipPoints loop and start from after point1
                            //could be done with a method
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

                    if (p1.status == DeepPoint.PointStatus.Out)
                    {
                        //go to clipPoints loop and start from after point1;
                        //could be done with a method
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
    }
}
