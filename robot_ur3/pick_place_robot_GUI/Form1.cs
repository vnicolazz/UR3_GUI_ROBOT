using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;

//typedef struct HSV_joint joint1;

namespace pick_place_robot_GUI
{
    public partial class Form1 : Form
    {
        struct HSV_joint
        {
            public int hue;
            public int saturation;
            public int value;
        };

        Capture _capture = null;
        int threshold = 150;
        delegate void displayStringDelegate(String s, Label label);
        int s_min = 80;
        int s_max = 255;
        int v_min = 100;
        int v_max = 255;
        int h_min = 80;
        int h_max = 120;
        HSV_joint joint1;
        HSV_joint joint2;
        HSV_joint joint3;



        
  

        //Serial Communication
        enum State { enabled, disabled };
        State s = State.disabled;
        SerialPort serPort;
        delegate void displayStringDel(String data);
        string ComPort = "COM5";    //must check by going into Device Manager

        public Form1()
        {
            try
            {
                serPort = new SerialPort(ComPort);
                serPort.BaudRate = 115200;
                serPort.DataBits = 8;
                serPort.Parity = Parity.None;
                serPort.StopBits = StopBits.One;
                serPort.Open();
                serPort.DataReceived += new SerialDataReceivedEventHandler(serPort_DataReceived);
                label17.Text = ComPort;
            }
            catch { }

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            trackBar6.Value = threshold;
            _capture = new Capture();
            _capture.ImageGrabbed += Display_Captured;	//grab event handler
            _capture.Start();
        
            //display_still();
            
        }

        //void display_still()
        //{
        //    bool hasPlate = false;
        //    int tri_angle = 0;
        //    int rect_angle = 0;
        //    int blue_rect_angle = 0;
        //    int tri_radial_line_length = 0;
        //    int rect_radial_line_length = 0;
        //    int blue_rect_radial_line_length = 0;


        //    Point origin = new Point(137, 162);
        //    Point origin_shifted = new Point(319, 162);
        //    LineSegment2DF origin_line = new LineSegment2DF(origin, origin_shifted);

        //    //Image<Bgr, Byte> frame = _capture.RetrieveBgrFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    //Image<Bgr, Byte> img = frame;

        //    Image<Bgr, Byte> img = null;

        //    ///*
        //    //Loading an image from a file:
        //    //img = new Image<Bgr, byte>("C:/Users/alessnau/Pictures/Camera Roll/WIN_20141213_204539.jpg");
        //    //imageBox1.Image = img.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    //
        //    //Interactive File Loading
        //    //OpenFileDialog openFile = new OpenFileDialog();
        //    //if (openFile.ShowDialog() == DialogResult.OK)
        //    //    img = new Image<Bgr, byte>(openFile.FileName);
        //    //img2 = img.Resize(imageBox2.Width, imageBox2.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    // imageBox2.Image = img2;
        //    //*/

        //    dispString("Image Size = " + Convert.ToString(img.Width) + " x " + Convert.ToString(img.Height), label1);
        //    dispString("# of pixels = " + pixel_counter(img).ToString(), label2);
        //    img.Draw(new CircleF(origin, 5), new Bgr(Color.Red), 1);
        //    imageBox1.Image = img;

        //    Image<Gray, Byte> gray = img.Convert<Gray, Byte>().ThresholdBinary(new Gray(threshold), new Gray(255));//.PyrDown().PyrUp();
        //    Image<Gray, Byte> cannyEdges = gray.Canny(0, 255);

        //    Image<Bgr, Byte> color_img = img.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    Image<Hsv, Byte> blue_hsv = img.Convert<Hsv, Byte>();
        //    Image<Hsv, float> img2 = img.Convert<Hsv, float>();


        //    gray = gray.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    blue_hsv = blue_hsv.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    img2 = img2.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

        //    //

        //    // imageBox3.Image = gray.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        //    Image<Gray, Byte>[] channels = blue_hsv.Split();
        //    Image<Gray, Byte> imgBlue = channels[0];
        //    Image<Gray, Byte> imgGreen = channels[1];
        //    Image<Gray, Byte> imgRed = channels[2];

        //    Image<Gray, Byte> bFilter = imgBlue.InRange(new Gray(h_min), new Gray(h_max));
        //    Image<Gray, Byte> gFilter = imgGreen.InRange(new Gray(s_min), new Gray(s_max));
        //    Image<Gray, Byte> rFilter = imgRed.InRange(new Gray(v_min), new Gray(v_max));

        //    // Combine the filtered componets into one image; Remove noise
        //    Image<Gray, Byte> combined = bFilter.And(gFilter).And(rFilter).SmoothMedian(3);
        //    Image<Bgr, Byte> canny_img = combined.Convert<Bgr, Byte>();

        //    dispString("Image Size = " + Convert.ToString(combined.Width) + " x " + Convert.ToString(combined.Height), label7);
        //    //dispString("# of pixels = " + pixel_counter((Image<Bgr, Byte>())combined).ToString(), label2);
        //    imageBox3.Image = combined.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

        //    Image<Gray, Byte> cannyHsv = combined.Canny(0, 255);
        //    Image<Bgr, Byte> comb_color = img.CopyBlank();


        //    List<Triangle2DF> triangleList = new List<Triangle2DF>();
        //    List<MCvBox2D> boxList = new List<MCvBox2D>();
        //    using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
        //        for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
        //        {   // a contour: list of pixels that can represent a curve
        //            Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here

        //            if (contours.Area > 200) //only consider contours with area greater than 250
        //            {
        //                if (currentPolygon.Total == 3) //The contour has 3 vertices, it is a triangle
        //                {
        //                    //bool isTriangle = true;
        //                    Point[] pts = currentPolygon.ToArray();
        //                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
        //                    triangleList.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
        //                }
        //                else if (currentPolygon.Total == 4) //The contour has 4 vertices.
        //                {
        //                    bool isRectangle = true;
        //                    Point[] pts = currentPolygon.ToArray();
        //                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

        //                    for (int i = 0; i < edges.Length; i++)
        //                    {
        //                        double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));

        //                        // determine if all the angles in the contour are within the range of [80, 100], close to 90 degrees 
        //                        if (angle < 80 || angle > 100)
        //                        {
        //                            isRectangle = false;
        //                            break;
        //                        }
        //                    }
        //                    if (isRectangle)
        //                        boxList.Add(currentPolygon.GetMinAreaRect());
        //                }
        //            }
        //        }
        //    Image<Bgr, Byte> triangleRectangleImage = img.CopyBlank();
        //    //triangleRectangleImage.Resize(imageBox5.Width, imageBox5.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

        //    foreach (Triangle2DF triangle in triangleList)
        //    {
        //        LineSegment2DF tri_radial_line = new LineSegment2DF(origin, triangleList[0].Centeroid);
        //        triangleRectangleImage.Draw(tri_radial_line, new Bgr(Color.Yellow), 2);
        //        triangleRectangleImage.Draw(triangle, new Bgr(Color.Yellow), 2);

        //        comb_color.Draw(tri_radial_line, new Bgr(Color.Yellow), 1);
        //        comb_color.Draw(triangle, new Bgr(Color.Yellow), 1);

        //        double _angle = Math.Atan2(162 - triangleList[0].Centeroid.Y, 137 - triangleList[0].Centeroid.X);
        //        tri_angle = (int)Math.Abs(_angle * (180 / Math.PI));
        //        tri_radial_line_length = (int)tri_radial_line.Length;

        //        dispString("Triangle centroid: " + triangleList[0].Centeroid.ToString(), label6);
        //        dispString("angle =  " + _angle.ToString(), label14);
        //        dispString("length =  " + tri_radial_line.Length.ToString(), label15);
        //    }
        //    foreach (MCvBox2D box in boxList)
        //    {
        //        LineSegment2DF rect_radial_line = new LineSegment2DF(origin, boxList[0].center);
        //        triangleRectangleImage.Draw(rect_radial_line, new Bgr(Color.Pink), 2);
        //        triangleRectangleImage.Draw(box, new Bgr(Color.Pink), 2);
        //        comb_color.Draw(rect_radial_line, new Bgr(Color.Pink), 1);
        //        comb_color.Draw(box, new Bgr(Color.Pink), 1);

        //        double _angle = Math.Atan2(boxList[0].center.Y - 162, boxList[0].center.X - 137);
        //        rect_angle = (int)Math.Abs(_angle * (180 / Math.PI));
        //        rect_radial_line_length = (int)rect_radial_line.Length;

        //        dispString("Rectangle center: " + boxList[0].center.ToString(), label5);
        //        dispString("angle =  " + _angle.ToString(), label22);
        //        dispString("length =  " + rect_radial_line.Length.ToString(), label23);
        //    }


        //    List<MCvBox2D> boxList1 = new List<MCvBox2D>();
        //    using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
        //        for (Contour<Point> contours = cannyHsv.FindContours(); contours != null; contours = contours.HNext)
        //        {   // a contour: list of pixels that can represent a curve
        //            Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here
        //            if (contours.Area > 100 && contours.Area < 500)//only consider contours with area greater than 250
        //            {
        //                if (currentPolygon.Total == 4) //The contour has 4 vertices.
        //                {
        //                    bool isRectangle = true;
        //                    Point[] pts = currentPolygon.ToArray();
        //                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

        //                    for (int i = 0; i < edges.Length; i++)
        //                    {
        //                        double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));

        //                        // determine if all the angles in the contour are within the range of [80, 100], close to 90 degrees 
        //                        if (angle < 70 || angle > 110)
        //                        {
        //                            isRectangle = false;
        //                            break;
        //                        }
        //                    }
        //                    if (isRectangle)
        //                        boxList1.Add(currentPolygon.GetMinAreaRect());
        //                }
        //            }
        //        }

        //    //combined = combined.Convert<Bgr, Byte>();

        //    foreach (MCvBox2D box in boxList1)
        //    {
        //        LineSegment2DF radial_line = new LineSegment2DF(origin, boxList1[0].center);
        //        comb_color.Draw(box, new Bgr(Color.Blue), 1);
        //        comb_color.Draw(radial_line, new Bgr(Color.Blue), 1);

        //        double _angle = Math.Atan2(boxList1[0].center.Y - 162, boxList1[0].center.X - 137);
        //        blue_rect_angle = (int)Math.Abs(_angle * (180 / Math.PI));
        //        blue_rect_radial_line_length = (int)radial_line.Length;

        //        dispString("Rectangle center: " + boxList1[0].center.ToString(), label24);
        //        dispString("angle =  " + _angle.ToString(), label25);
        //        dispString("length =  " + radial_line.Length.ToString(), label26);
        //    }

        //    comb_color.Draw(new CircleF(origin, 5), new Bgr(Color.Red), 1);
        //    comb_color.Draw(origin_line, new Bgr(Color.Red), 1);
        //    triangleRectangleImage.Draw(new CircleF(origin, 5), new Bgr(Color.Red), 1);

        //    dispString("Image Size = " + Convert.ToString(triangleRectangleImage.Width) + " x " + Convert.ToString(triangleRectangleImage.Height), label3);
        //    dispString("# of pixels = " + pixel_counter(triangleRectangleImage).ToString(), label4);
        //    imageBox2.Image = triangleRectangleImage.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

        //    dispString("Image Size = " + Convert.ToString(comb_color.Width) + " x " + Convert.ToString(comb_color.Height), label10);
        //    dispString("# of pixels = " + pixel_counter(comb_color).ToString(), label11);
        //    imageBox4.Image = comb_color;//.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

        //    if (rect_angle + rect_radial_line_length != 0)
        //    {
        //        if (rect_angle > blue_rect_angle)
        //            serialTalk("serv_OUT");
        //        else if (rect_angle < blue_rect_angle)
        //            serialTalk("serv_IN");
        //        else if (rect_radial_line_length > blue_rect_radial_line_length)
        //            serialTalk("step_OUT");
        //        else if (rect_radial_line_length < blue_rect_radial_line_length)
        //            serialTalk("step_IN");
        //        else if (rect_angle == blue_rect_angle && rect_radial_line_length == blue_rect_radial_line_length)
        //            serialTalk("mag_ON");
        //        //else
        //        //{
        //        //    serialTalk("step_OFF");
        //        //    serialTalk("serv_OFF");
        //        //    serialTalk("serv2_OFF");
        //        //    serialTalk("mag_OFF");
        //        //}

        //    }
        //}

        void Display_Captured(object sender, EventArgs e)
        {
            bool hasPlate = false;
            int tri_angle = 0;
            int rect_angle = 0;
            int blue_rect_angle = 0;
            int tri_radial_line_length=0;
            int rect_radial_line_length=0;
            int blue_rect_radial_line_length=0;


            //Point origin = new Point(137, 162);
            //Point origin_shifted = new Point(319, 162);
            //LineSegment2DF origin_line = new LineSegment2DF(origin, origin_shifted);

            Image<Bgr, Byte> frame = _capture.RetrieveBgrFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            Image<Bgr, Byte> img = frame;       

            dispString("Image Size = " + Convert.ToString(img.Width) + " x " + Convert.ToString(img.Height), label1);
            dispString("# of pixels = " + pixel_counter(img).ToString(), label2);
            //img.Draw(new CircleF(origin, 5), new Bgr(Color.Red), 1);
            imageBox1.Image = img;

            Image<Gray, Byte> gray = img.Convert<Gray, Byte>().ThresholdBinary(new Gray(threshold), new Gray(255));//.PyrDown().PyrUp();
            Image<Gray, Byte> cannyEdges = gray.Canny(0, 255);

            Image<Bgr, Byte> color_img = img.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            Image<Hsv, Byte> blue_hsv = img.Convert<Hsv, Byte>();
            Image<Hsv, float> img2 = img.Convert<Hsv, float>();


            gray = gray.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            blue_hsv = blue_hsv.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            img2 = img2.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            //

           // imageBox3.Image = gray.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            Image<Gray, Byte>[] channels = blue_hsv.Split();
            Image<Gray, Byte> imgBlue = channels[0];
            Image<Gray, Byte> imgGreen = channels[1];
            Image<Gray, Byte> imgRed = channels[2];

            Image<Gray, Byte> bFilter = imgBlue.InRange(new Gray(h_min), new Gray(h_max));
            Image<Gray, Byte> gFilter = imgGreen.InRange(new Gray(s_min), new Gray(s_max));
            Image<Gray, Byte> rFilter = imgRed.InRange(new Gray(v_min), new Gray(v_max));

            // Combine the filtered componets into one image; Remove noise
            Image<Gray, Byte> combined = bFilter.And(gFilter).And(rFilter).SmoothMedian(3);
            Image<Bgr, Byte> canny_img = combined.Convert<Bgr, Byte>();

            dispString("Image Size = " + Convert.ToString(combined.Width) + " x " + Convert.ToString(combined.Height), label7);
            //dispString("# of pixels = " + pixel_counter((Image<Bgr, Byte>())combined).ToString(), label2);
            imageBox3.Image = combined.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            Image<Gray, Byte> cannyHsv = combined.Canny(0, 255);
            Image<Bgr, Byte> comb_color = img.CopyBlank();


            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<MCvBox2D> boxList = new List<MCvBox2D>();
            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
                {   // a contour: list of pixels that can represent a curve
                    Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here

                    if (contours.Area > 200) //only consider contours with area greater than 250
                    {
                        if (currentPolygon.Total == 3) //The contour has 3 vertices, it is a triangle
                        {
                            //bool isTriangle = true;
                            Point[] pts = currentPolygon.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
                            triangleList.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
                        }
                        else if (currentPolygon.Total == 4) //The contour has 4 vertices.
                        {
                            bool isRectangle = true;
                            Point[] pts = currentPolygon.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                            for (int i = 0; i < edges.Length; i++)
                            {
                                double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));

                                // determine if all the angles in the contour are within the range of [80, 100], close to 90 degrees 
                                if (angle < 80 || angle > 100)
                                {
                                    isRectangle = false;
                                    break;
                                }
                            }
                            if (isRectangle)
                                boxList.Add(currentPolygon.GetMinAreaRect());
                        }
                    }
                }
            Image<Bgr, Byte> triangleRectangleImage = img.CopyBlank();
            //triangleRectangleImage.Resize(imageBox5.Width, imageBox5.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            foreach (Triangle2DF triangle in triangleList)
            {
                //LineSegment2DF tri_radial_line = new LineSegment2DF(origin, triangleList[0].Centeroid);
                //triangleRectangleImage.Draw(tri_radial_line, new Bgr(Color.Yellow), 2);
               // triangleRectangleImage.Draw(triangle, new Bgr(Color.Yellow), 2);

                //comb_color.Draw(tri_radial_line, new Bgr(Color.Yellow), 1);
                comb_color.Draw(triangle, new Bgr(Color.Yellow), 1);

                double _angle = Math.Atan2(triangleList[0].Centeroid.Y-162, triangleList[0].Centeroid.X-137);
                tri_angle = (int)Math.Abs(_angle * (180 / Math.PI));
                //tri_radial_line_length = (int)tri_radial_line.Length;

                dispString("Triangle centroid: " + triangleList[0].Centeroid.ToString(), label6);
                dispString("angle =  " + tri_angle.ToString(), label14);
                dispString("length =  " + tri_radial_line_length.ToString(), label15);
            }
            foreach (MCvBox2D box in boxList)
            {
                //LineSegment2DF rect_radial_line = new LineSegment2DF(origin, boxList[0].center);
                //triangleRectangleImage.Draw(rect_radial_line, new Bgr(Color.Pink), 2);
                //triangleRectangleImage.Draw(box, new Bgr(Color.Pink), 2);
                //comb_color.Draw(rect_radial_line, new Bgr(Color.Pink), 1);
                comb_color.Draw(box, new Bgr(Color.Pink), 1);

                double _angle = Math.Atan2(boxList[0].center.Y - 162, boxList[0].center.X - 137);
                rect_angle = (int)Math.Abs(_angle * (180 / Math.PI));
                //rect_radial_line_length = (int)rect_radial_line.Length;

                dispString("Rectangle center: " + boxList[0].center.ToString(), label5);
                dispString("angle =  " + rect_angle.ToString(), label22);
                dispString("length =  " + rect_radial_line_length.ToString(), label23);
            }


            List<MCvBox2D> boxList1 = new List<MCvBox2D>();
            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                for (Contour<Point> contours = cannyHsv.FindContours(); contours != null; contours = contours.HNext)
                {   // a contour: list of pixels that can represent a curve
                    Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here
                    if (contours.Area > 100 && contours.Area < 500)//only consider contours with area greater than 250
                    {
                        if (currentPolygon.Total == 4) //The contour has 4 vertices.
                        {
                            bool isRectangle = true;
                            Point[] pts = currentPolygon.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                            for (int i = 0; i < edges.Length; i++)
                            {
                                double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));

                                // determine if all the angles in the contour are within the range of [80, 100], close to 90 degrees 
                                if (angle < 70 || angle > 110)
                                {
                                    isRectangle = false;
                                    break;
                                }
                            }
                            if (isRectangle)
                                boxList1.Add(currentPolygon.GetMinAreaRect());
                        }
                    }
                }

            //combined = combined.Convert<Bgr, Byte>();

            foreach (MCvBox2D box in boxList1)
            {
                //LineSegment2DF radial_line = new LineSegment2DF(origin, boxList1[0].center);
                comb_color.Draw(box, new Bgr(Color.Blue), 1);
               //comb_color.Draw(radial_line, new Bgr(Color.Blue), 1);
               
                double _angle = Math.Atan2(boxList1[0].center.Y - 162, boxList1[0].center.X - 137);
                blue_rect_angle = (int)Math.Abs(_angle * (180 / Math.PI)); 
                //blue_rect_radial_line_length = (int)radial_line.Length;

                dispString("Rectangle center: " + boxList1[0].center.ToString(), label24);
                dispString("angle =  " + blue_rect_angle.ToString(), label25);
                dispString("length =  " + blue_rect_radial_line_length.ToString(), label26);
            }

            //comb_color.Draw(new CircleF(origin, 5), new Bgr(Color.Red), 1);
            //comb_color.Draw(origin_line, new Bgr(Color.Red), 1);
            //triangleRectangleImage.Draw(new CircleF(origin, 5), new Bgr(Color.Red), 1);

            dispString("Image Size = " + Convert.ToString(triangleRectangleImage.Width) + " x " + Convert.ToString(triangleRectangleImage.Height), label3);
            dispString("# of pixels = " + pixel_counter(triangleRectangleImage).ToString(), label4);
            imageBox2.Image = triangleRectangleImage.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            dispString("Image Size = " + Convert.ToString(comb_color.Width) + " x " + Convert.ToString(comb_color.Height), label10);
            dispString("# of pixels = " + pixel_counter(comb_color).ToString(), label11);
            imageBox4.Image = comb_color;//.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            //if (rect_angle + rect_radial_line_length != 0)
            //{
            //    if (rect_angle > blue_rect_angle)
            //        serialTalk("serv_OUT");
            //    else if (rect_angle < blue_rect_angle)
            //        serialTalk("serv_IN");
            //    else if (rect_radial_line_length > blue_rect_radial_line_length)
            //        serialTalk("step_OUT");
            //    else if (rect_radial_line_length < blue_rect_radial_line_length)
            //        serialTalk("step_IN");
            //    else if (rect_angle == blue_rect_angle && rect_radial_line_length == blue_rect_radial_line_length)
            //        serialTalk("mag_ON");
                //else
                //{
                //    serialTalk("step_OFF");
                //    serialTalk("serv_OFF");
                //    serialTalk("serv2_OFF");
                //    serialTalk("mag_OFF");
                //}
            //}
        }

        public int pixel_counter(Image<Bgr, Byte> image)
        {
            int pixel_count = 0;
            for (int h = 0; h < image.Height; h++)
                for (int w = 0; w < image.Width; w++)
                    pixel_count++;
            return pixel_count;
        }

        public void dispString(string s, Label label)
        {
            if (InvokeRequired)
            {
                displayStringDelegate dispStrDel = dispString;
                this.BeginInvoke(dispStrDel, s, label);
            }
            else
                label.Text = s;
        }

        //Serial Communication
        
                //Serial Communication: serPort_DataReceived
                void serPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
                {
                    int num = serPort.ReadByte();
                    displayNumber(num.ToString());
                }

                //Serial Communication: displayNumber
                public void displayNumber(String str)
                {   // UI control label1 can be updated by only the UI thread that created the control
                    if (this.label1.InvokeRequired) // Another Thread invoked "serPort_DataReceived" method
                    {                               // Without this part, runtime exception will occur
                        displayStringDel deligation = displayNumber;
                        //Invoke(deligation, new object[] { str });
                        this.BeginInvoke(deligation, str);
                    }
                    else
                    {
                        this.label20.Text = str;
                    }
                }       

                //public void serialTalk(string s)
                //{
                //   if (serPort.IsOpen)
                //    {
                //        switch(s)
                //        {
                //            case "step_OUT":
                //                serPort.Write("1");
                //                dispString("step_OUT", label19);
                //                break;
                //            case "step_OFF":
                //                serPort.Write("2");
                //                dispString("step_OFF", label19);
                //                break;
                //            case "step_IN":
                //                serPort.Write("3");
                //                dispString("step_IN", label19);
                //                break;
                //            case "serv_OUT":
                //                serPort.Write("4");
                //                dispString("serv_OUT", label19);
                //                break;
                //            case "serv_OFF":
                //                serPort.Write("5");
                //                dispString("serv_OFF", label19);
                //                break;
                //            case "serv_IN":
                //                serPort.Write("6");
                //                dispString("serv_IN", label19);
                //                break;
                //            case "mag_ON":
                //                serPort.Write("7");
                //                dispString("mag_ON", label19);
                //                break;
                //            case "mag_OFF":
                //                serPort.Write("8");
                //                dispString("mag_OFF", label19);
                //                break;
                //            //default:                     //need to make a case that accounts for the distance the arm will be traveling
                //                //serPort.Write
                //        }
                //    }
                //}

                private void button1_Click(object sender, EventArgs e)
                { serPort.Write("1"); }

                private void button2_Click(object sender, EventArgs e)
                {serPort.Write("2");                }

                private void button3_Click(object sender, EventArgs e)
                {serPort.Write("3");}

                private void button4_Click(object sender, EventArgs e)
                {serPort.Write("4");                }

                private void button5_Click(object sender, EventArgs e)
                {serPort.Write("5");                }

                private void button6_Click(object sender, EventArgs e)
                {serPort.Write("6");                }

                private void button7_Click(object sender, EventArgs e)
                {serPort.Write("7");                }

                private void button8_Click(object sender, EventArgs e)
                { serPort.Write("8"); }

                private void trackBar6_Scroll(object sender, EventArgs e)
                {
                    threshold = trackBar6.Value;
                }
       

    }
}
