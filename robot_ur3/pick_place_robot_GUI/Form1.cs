/*
TO DO: 
 
 * find "sweet spot" for blue "painter's tape"
 * eliminate flashing shape problem
      * maybe by saving previous shape location and drawing previous
 * clean up unused functions
Note: 
 * line 83: Nathan added something here to show exceptions but did not upload to GitHub
 * line 98: Capture() (internal cam) or Capture(1) (external cam) used to rid errors
 * to show line numbers: "TOOLS"-->"Options"-->"Text Editor"-->"C#"-->"General"-->"Line Numbers"
Resources:
 * http://colorizer.org/
*/
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


namespace pick_place_robot_GUI
{
    public partial class Form1 : Form
    {
        Capture _capture = null;
        int threshold = 211;
        delegate void displayStringDelegate(String s, Label label);

        struct HSV_joint
        {
            public int hue;
            public int saturation;
            public int value;
        }; HSV_joint joint1; HSV_joint joint2; HSV_joint joint3; 
        
        //Serial Communication
        enum State { enabled, disabled };
        State s = State.disabled;
        SerialPort serPort;
        delegate void displayStringDel(String data);
        string ComPort = "4";    //must check by going into Device Manager, User can now change in GUI
        bool SerReady;
        int Ser_Alternate;
        byte[] outByte = new byte[4];

        PointF eb_center = new PointF(5, 5);
        SizeF eb_size = new SizeF(10, 10);
        PointF mb_center = new PointF(5, 5);
        SizeF mb_size = new SizeF(10, 10);
        PointF t_p1 = new PointF(5, 4);
        PointF t_p2 = new PointF(4, 6);
        PointF t_p3 = new PointF(6, 6);
        

        int firstCatch = 0;

        


        public Form1()
        {
            //blue
            joint1.hue = 83;
            joint1.saturation = 152;
            joint1.value = 255;
            //red
            joint2.hue = 255;
            joint2.saturation = 255;
            joint2.value = 255;
            //green
            joint3.hue = 255;               //120
            joint3.saturation = 255;        //70    
            joint3.value = 255;             //50


            try
            {
                serPort = new SerialPort();
                serPort.BaudRate = 9600;
                serPort.DataBits = 8;
                serPort.Parity = Parity.None;
                serPort.StopBits = StopBits.One;
                serPort.DataReceived += new SerialDataReceivedEventHandler(serPort_DataReceived);
            }
            catch { }   //NATHAN: DIDNT YOU ADD SOMETHING HERE?

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            trackBar6.Value = threshold;
            radioButton1.Checked = true;
            trackBar4.Value = 70;
            trackBar5.Value = 40;
            textBox1.Text = trackBar4.Value.ToString();
            textBox2.Text = trackBar5.Value.ToString();
            textBox6.Text = serPort.BaudRate.ToString();
            numericUpDown1.Value = decimal.Parse(ComPort);
            radioButton5.Checked = true;           

            SerReady = false;
            Ser_Alternate = 0;

            _capture = new Capture();
            _capture.ImageGrabbed += Display_Captured;	//grab event handler
            _capture.Start();            
        }

        void Display_Captured(object sender, EventArgs e)
        {
             MCvBox2D endBox = new MCvBox2D(eb_center, eb_size, 90);
             MCvBox2D midBox = new MCvBox2D(mb_center, mb_size, 90);
             Triangle2DF baseTri = new Triangle2DF(t_p1, t_p2, t_p3);

             //if (firstCatch == 0)
             //{
             //    eb_center = new PointF(5, 5);
             //    mb_center = new PointF(5, 5);
             //    firstCatch = 1;
             //}

            int tri_angle = 0;
            int rect_angle = 0;
            int blue_rect_angle = 0;
            int tri_radial_line_length=0;
            int rect_radial_line_length=0;
            PointF origin = new PointF(0,0);
            //bool hasPlate = false;
            //int blue_rect_radial_line_length=0;
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
            Image<Hsv, float> img2 = img.Convert<Hsv, float>();

            Image<Hsv, Byte> blue_hsv = img.Convert<Hsv, Byte>();
            Image<Hsv, Byte> red_hsv = img.Convert<Hsv, Byte>();
            Image<Hsv, Byte> green_hsv = img.Convert<Hsv, Byte>();
            gray = gray.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            blue_hsv = blue_hsv.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            img2 = img2.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            imageBox2.Image = gray;
           // imageBox3.Image 
            //blue
            Image<Gray, Byte>[] channels1 = blue_hsv.Split();
            Image<Gray, Byte> imgBlue1 = channels1[0];
            Image<Gray, Byte> imgGreen1 = channels1[1];
            Image<Gray, Byte> imgRed1 = channels1[2];
            Image<Gray, Byte> bFilter1 = imgBlue1.InRange(new Gray(joint1.hue-30), new Gray(joint1.hue+30));
            Image<Gray, Byte> gFilter1 = imgGreen1.InRange(new Gray(joint1.saturation-30), new Gray(joint1.saturation+30));
            Image<Gray, Byte> rFilter1 = imgRed1.InRange(new Gray(joint1.value-30), new Gray(joint1.value+30));

            Image<Gray, Byte> combined = bFilter1.And(gFilter1).And(rFilter1).SmoothMedian(3);                   //.And(bFilter2).And(gFilter2)
            //Image<Gray, Byte>[] hsvImg = new Image<Gray,Byte>[3];
            //hsvImg[0] = combined;
            Image<Bgr, Byte> canny_img = combined.Convert<Bgr, Byte>();

            dispString("Image Size = " + Convert.ToString(combined.Width) + " x " + Convert.ToString(combined.Height), label7);
            imageBox3.Image = combined.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            //Image<Gray, Byte> cannyHsv = combined1.Canny(0, 255);
            Image<Bgr, Byte> comb_color = img.CopyBlank();

//***********************************************************************************************
//******************************************Tile Shape Detection*********************************
//***********************************************************************************************

            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<MCvBox2D> boxList = new List<MCvBox2D>();
            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
                {   // a contour: list of pixels that can represent a curve
                    Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here

                    if (contours.Area > 100 && contours.Area < 500) //only consider contours with area greater than 250
                    {
                        if (currentPolygon.Total == 3) //The contour has 3 vertices, it is a triangle
                        {
                            //bool isTriangle = true;
                            Point[] pts = currentPolygon.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
                            triangleList.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
                        }
                    }
                    else if(contours.Area > 200 && contours.Area < 800) 
                        if(currentPolygon.Total == 4) //The contour has 4 vertices.
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
                //dispString("angle =  " + tri_angle.ToString(), label14);
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

                dispString("box center: " + boxList[0].center.ToString(), label5);
                dispString("box angle =  " + boxList[0].angle.ToString(), label22);
                dispString("box size =  " + boxList[0].size.ToString(), label23);
            }

//***********************************************************************************************************
//************************************HSV Shape Detection****************************************************
//***********************************************************************************************************

                List<MCvBox2D> boxList1 = new List<MCvBox2D>();
                using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                    for (Contour<Point> contours = combined.FindContours(); contours != null; contours = contours.HNext)
                    {   // a contour: list of pixels that can represent a curve
                        Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here
                        if (contours.Area > 20 && contours.Area < 900)//only consider contours with area greater than 250
                        {
                            if (currentPolygon.Total == 3) //The contour has 3 vertices, it is a triangle
                            {
                                //bool isTriangle = true;
                                Point[] pts = currentPolygon.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
                                
                                triangleList.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
                            }
                            if (currentPolygon.Total == 4) //The contour has 4 vertices.
                            {
                                bool isRectangle = true;
                                Point[] pts = currentPolygon.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int i = 0; i < edges.Length; i++)
                                {
                                    double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));

                                    // determine if all the angles in the contour are within the range of [80, 100], close to 90 degrees 
                                    if (angle < 60 || angle > 120)
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
                foreach (Triangle2DF triangle in triangleList)
                {
                    if(triangleList.Count ==1)
                    {
                        origin = new PointF(triangleList[0].Centeroid.X, triangleList[0].Centeroid.Y);
                        //comb_color.Draw(tri_radial_line, new Bgr(Color.Yellow), 1);
                        comb_color.Draw(triangle, new Bgr(Color.Blue), 1);

                        t_p1 = triangle.V0;
                        t_p2 = triangle.V1;
                        t_p3 = triangle.V2;

                        comb_color.Draw(baseTri, new Bgr(Color.AliceBlue), 2);

                        dispString("Triangle centroid: " + triangleList[0].Centeroid.ToString(), label6);
                        //dispString("angle =  " + tri_angle.ToString(), label14);
                        dispString("length =  " + tri_radial_line_length.ToString(), label15);
                    }
                }

                if (boxList1.Count == 2)
                { 
                    mb_center = boxList1[0].center;
                    eb_center = boxList1[1].center;

                    LineSegment2DF radial_line = new LineSegment2DF(midBox.center, endBox.center);
                    LineSegment2DF tri_radial_line = new LineSegment2DF(baseTri.Centeroid, midBox.center);
                    MCvBox2D temp;

                    double X_distance, Y_distance, distance;
                    distance = 0;

                    //if(tri_radial_line.Length > 110 || tri_radial_line.Length < 90)
                    //    if (endBox.center.X < midBox.center.X)//this shouldn't be the case
                    //    {
                    //        //So we swap the boxes
                    //        temp = midBox;
                    //        midBox = endBox;
                    //        endBox = temp;


                    //        radial_line = new LineSegment2DF(midBox.center, endBox.center);
                    //        tri_radial_line = new LineSegment2DF(baseTri.Centeroid, midBox.center);
                       
                    //    }

                    //foreach(MCvBox2D box in boxList1)
                    //{
                    //    X_distance = box.center.X - baseTri.Centeroid.X;
                    //    Y_distance = box.center.Y - baseTri.Centeroid.Y;
                    //    distance = Math.Sqrt(X_distance * X_distance + Y_distance * Y_distance);   
                    //}

                    //Draw Lines
                    foreach (MCvBox2D box in boxList1)
                    {
                        comb_color.Draw(box, new Bgr(Color.Blue), 1);
                    }
                    comb_color.Draw(radial_line, new Bgr(Color.Silver), 2);
                    comb_color.Draw(tri_radial_line, new Bgr(Color.Silver), 2);
                    comb_color.Draw(endBox, new Bgr(Color.Green), 2);
                    comb_color.Draw(midBox, new Bgr(Color.Brown), 2);

                    dispString(midBox.center.X.ToString(), label9);
                    dispString(endBox.center.X.ToString(), label18);
                }   
                
                
                


            dispString("Image Size = " + Convert.ToString(triangleRectangleImage.Width) + " x " + Convert.ToString(triangleRectangleImage.Height), label3);
            dispString("# of pixels = " + pixel_counter(triangleRectangleImage).ToString(), label4);
            //imageBox2.Image = triangleRectangleImage.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            dispString("Image Size = " + Convert.ToString(comb_color.Width) + " x " + Convert.ToString(comb_color.Height), label10);
            dispString("# of pixels = " + pixel_counter(comb_color).ToString(), label11);
            imageBox4.Image = comb_color;           //.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        }

        public int pixel_counter(Image<Bgr, Byte> image)
        {
            int pixel_count = 0;
            for (int h = 0; h < image.Height; h++)
                for (int w = 0; w < image.Width; w++)
                    pixel_count++;
            return pixel_count;
        }

        public void dispString(String s, Label label)
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
                    if(num == 1)
                        SerReady = true;

                    if ((Ser_Alternate % 2) == 0)//alternate between two button clicks
                    {
                        serPort.Write(outByte, 0, outByte.Length);
                        Ser_Alternate++;
                    }
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

                private void trackBar6_Scroll(object sender, EventArgs e)
                {
                    threshold = trackBar6.Value;
                    textBox4.Text = trackBar6.Value.ToString();

                }

                private void radioButton1_CheckedChanged(object sender, EventArgs e)
                {
                    if(radioButton1.Checked)
                    {
                        radioButton2.Checked = false;
                        radioButton3.Checked = false;
                        hueBar.Value = joint1.hue;
                        saturationBar.Value = joint1.saturation;
                        valueBar.Value = joint1.value;
                        dispString(joint1.hue.ToString(), label12);
                        dispString(joint1.saturation.ToString(), label13);
                        dispString(joint1.value.ToString(), label14);
                    }
                }

                private void radioButton2_CheckedChanged(object sender, EventArgs e)
                {
                    if (radioButton2.Checked)
                    {
                        radioButton1.Checked = false;
                        radioButton3.Checked = false;
                        hueBar.Value = joint2.hue;
                        saturationBar.Value = joint2.saturation;
                        valueBar.Value = joint2.value;
                        dispString(joint2.hue.ToString(), label12);
                        dispString(joint2.saturation.ToString(), label13);
                        dispString(joint2.value.ToString(), label14);
                    }
                }

                private void radioButton3_CheckedChanged(object sender, EventArgs e)
                {
                    if (radioButton3.Checked)
                    {
                        radioButton1.Checked = false;
                        radioButton2.Checked = false;
                        hueBar.Value = joint3.hue;
                        saturationBar.Value = joint3.saturation;
                        valueBar.Value = joint3.value;
                        dispString(joint3.hue.ToString(), label12);
                        dispString(joint3.saturation.ToString(), label13);
                        dispString(joint3.value.ToString(), label14);
                    }
                }

                private void hueBar_Scroll(object sender, EventArgs e)
                {
                    if(radioButton1.Checked)
                    {
                        joint1.hue = hueBar.Value;
                        dispString(joint1.hue.ToString(), label12);
                    }
                    if (radioButton2.Checked)
                    {
                        joint2.hue = hueBar.Value;
                        dispString(joint2.hue.ToString(), label12);
                    }
                    if (radioButton3.Checked)
                    {
                        joint3.hue = hueBar.Value;
                        dispString(joint3.hue.ToString(), label12);
                    }
                }

                private void saturationBar_Scroll(object sender, EventArgs e)
                {
                    if (radioButton1.Checked)
                    {
                        joint1.saturation = saturationBar.Value;
                        dispString(joint1.saturation.ToString(), label13);
                    }
                    if (radioButton2.Checked)
                    {
                        joint2.saturation = saturationBar.Value;
                        dispString(joint2.saturation.ToString(), label13);
                    }
                    if (radioButton3.Checked)
                    {
                        joint3.saturation = saturationBar.Value;
                        dispString(joint3.saturation.ToString(), label13);
                    }
                }

                private void valueBar_Scroll(object sender, EventArgs e)
                {
                    if (radioButton1.Checked)
                    {
                        joint1.value = valueBar.Value;
                        dispString(joint1.value.ToString(), label14);
                    }
                    if (radioButton2.Checked)
                    {
                        joint2.value = valueBar.Value;
                        dispString(joint2.value.ToString(), label14);
                    }
                    if (radioButton3.Checked)
                    {
                        joint3.value = valueBar.Value;
                        dispString(joint3.value.ToString(), label14);
                    }
                }

                private void button9_Click(object sender, EventArgs e)
                {
                    
                    outByte[0] = (byte)Convert.ToInt32(textBox1.Text);  //joint1
                    outByte[1] = (byte)Convert.ToInt32(textBox2.Text);  //joint2
                    //outByte[2] = (byte)Convert.ToInt32(textBox3.Text);  //joint3

                    if (radioButton4.Checked)    //pick
                    {
                        outByte[2] = (byte)Convert.ToInt32(90);
                    }
                    else if (radioButton5.Checked)    //move
                    {
                        outByte[2] = (byte)Convert.ToInt32(40);
                    }
                    else if (radioButton6.Checked)    //drop
                    {
                        outByte[2] = (byte)Convert.ToInt32(110);
                    }

                    //State of Checkbox determines state of magnet
                    if (checkBox1.Checked)
                        outByte[3] = (byte)Convert.ToInt32(1);
                    else
                        outByte[3] = (byte)Convert.ToInt32(0);

                    if(serPort.IsOpen)
                    {
                        if(SerReady)
                        {
                            serPort.Write(outByte, 0, 4);
                            SerReady = false;
                        }
                        else if(!SerReady)
                        {
                            //while (!SerReady) { /*Do nothing while Serial Port is not ready*/}
                            if ((Ser_Alternate % 2) == 1)//alternate between two button clicks
                            {
                                serPort.Write(outByte, 0, outByte.Length);
                                Ser_Alternate++;
                            }

                            if (Ser_Alternate == 4)
                                Ser_Alternate = 0;
                        }
                    }  

                    //label1.Text = Convert.ToString(outByte[0]);
                    //label2.Text = Convert.ToString(outByte[1]);
                    //label3.Text = Convert.ToString(outByte[2]);
                    //label4.Text = Convert.ToString(outByte[3]);
                }

                private void radioButton4_CheckedChanged(object sender, EventArgs e)
                {
                    textBox3.Text = Convert.ToString(90);
                    checkBox1.Checked = true;
                    button9.PerformClick();
                }

                private void radioButton5_CheckedChanged(object sender, EventArgs e)
                {
                    textBox3.Text = Convert.ToString(40);
                    button9.PerformClick();
                }

                private void radioButton6_CheckedChanged(object sender, EventArgs e)
                {
                    textBox3.Text = Convert.ToString(110);
                    button9.PerformClick();
                }

                private void trackBar4_Scroll(object sender, EventArgs e)
                {
                    textBox1.Text = trackBar4.Value.ToString();
                    button9.PerformClick();
                }

                private void trackBar5_Scroll(object sender, EventArgs e)
                {
                    textBox2.Text = trackBar5.Value.ToString();
                    button9.PerformClick();
                }

                private void checkBox1_CheckedChanged(object sender, EventArgs e)
                {
                    button9.PerformClick();
                }

                private void button10_Click(object sender, EventArgs e)
                {
                    if (button10.Text == "Stop")//To stop the communication
                    {
                        button10.Text = "Start";

                        textBox6.Enabled = true;
                        numericUpDown1.Enabled = true;
                        serPort.Close();
                    }
                    else// try to connect serial port
                    {
                        try 
                        { 
                            button10.Text = "Stop";
                            serPort.BaudRate = int.Parse(textBox6.Text);
                            serPort.PortName = "COM" + numericUpDown1.Value.ToString();
                            serPort.Open();
                            textBox6.Enabled = false;
                            numericUpDown1.Enabled = false;
                            SerReady = true;
                        }
                        catch
                        { button10.Text = "Try again"; }
                    }

                }

                private void textBox2_TextChanged(object sender, EventArgs e)
                {
                    if(int.Parse(textBox2.Text) > 180)
                    { textBox2.Text = "180";}

                    if (int.Parse(textBox2.Text) < 0)
                    { textBox2.Text = "0"; }

                    if(trackBar5.Value.ToString() != textBox2.Text)
                    { trackBar5.Value = int.Parse(textBox2.Text); }
                }

                private void textBox1_TextChanged(object sender, EventArgs e)
                {
                    if (int.Parse(textBox1.Text) > 180)
                    { textBox1.Text = "180"; }

                    if (int.Parse(textBox1.Text) < 0)
                    { textBox1.Text = "0"; }

                    if (trackBar4.Value.ToString() != textBox1.Text)
                    { trackBar4.Value = int.Parse(textBox1.Text); }

                }
       

    }
}
