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
using System.Threading;
using System.Windows.Forms;
using System.IO.Ports;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using HiEmgu;


namespace pick_place_robot_GUI
{
    public partial class Form1 : Form        //static was not here originally
    {
        Capture _capture = null;
        int threshold = 211;
        delegate void displayStringDelegate(String s, Label label);
        delegate void displayTextDelegate(String S, TextBox textbox);
        //global_data count = new global_data();
        global_data data = new global_data();
        
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

        double A_Length;
        double B_Length;

        PointF eb_center = new PointF(5, 5);
        SizeF eb_size = new SizeF(10, 10);
        PointF mb_center = new PointF(5, 5);
        SizeF mb_size = new SizeF(10, 10);
        PointF t_p1 = new PointF(5, 4);
        PointF t_p2 = new PointF(4, 6);
        PointF t_p3 = new PointF(6, 6);
        PointF boxTileCenter = new PointF(0, 0);
       
        robot scara = new robot();
        int sequence = 0;

        PointF previous;
        double X_multiple, X_addition, Y_multiple, Y_addition;

        public Form1()
        {
            //blue
            joint1.hue = 83;
            joint1.saturation = 152;
            joint1.value = 220;
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
            catch { }

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            X_multiple = (double)X_Proportional.Value;
            X_addition = (double)X_plus.Value;
            Y_multiple = (double)Y_Proportional.Value;
            Y_addition = (double)Y_plus.Value;
            Bicep_Length.Value = (decimal)152.5;
            Forearm_Length.Value = (decimal)62;
            A_Length = (int)Bicep_Length.Value;
            B_Length = (int)Forearm_Length.Value;
            trackBar6.Value = threshold;
            radioButton1.Checked = true;
            trackBar4.Value = 45;
            trackBar5.Value = 60;
            textBox1.Text = trackBar4.Value.ToString();
            textBox2.Text = trackBar5.Value.ToString();
            textBox6.Text = serPort.BaudRate.ToString();
            numericUpDown1.Value = decimal.Parse(ComPort);
            radioButton5.Checked = true;

            SerReady = true;
            Ser_Alternate = 0;

            _capture = new Capture(0);
            _capture.ImageGrabbed += Display_Captured;	//grab event handler
            _capture.Start();
        }

        void Display_Captured(object sender, EventArgs e)
        {
            PointF origin = new PointF(30, 153);
            PointF origin_end = new PointF(250, 153);
            MCvBox2D endBox = new MCvBox2D(eb_center, eb_size, 90);
            MCvBox2D midBox = new MCvBox2D(mb_center, mb_size, 90);
            Triangle2DF baseTri = new Triangle2DF(t_p1, t_p2, t_p3);
            LineSegment2DF oline = new LineSegment2DF(origin, origin_end);

            //if (firstCatch == 0)
            //{
            //    eb_center = new PointF(5, 5);
            //    mb_center = new PointF(5, 5);
            //    firstCatch = 1;
            //}

            //PointF origin = new PointF(0, 0);



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
            Image<Gray, Byte> bFilter1 = imgBlue1.InRange(new Gray(joint1.hue - 40), new Gray(joint1.hue + 40));
            Image<Gray, Byte> gFilter1 = imgGreen1.InRange(new Gray(joint1.saturation - 40), new Gray(joint1.saturation + 40));
            Image<Gray, Byte> rFilter1 = imgRed1.InRange(new Gray(joint1.value - 40), new Gray(joint1.value + 40));

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
            //tile shape detection is being chnaged to handle only one square and one triangle

            Triangle2DF triTile = new Triangle2DF();
            MCvBox2D boxTile = new MCvBox2D();
            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
                {   // a contour: list of pixels that can represent a curve
                    Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.05, storage); // adjust here

                    if (contours.Area > 110 && contours.Area < 200) //only consider contours with area greater than 250
                    {
                        if (currentPolygon.Total == 3) //The contour has 3 vertices, it is a triangle
                        {
                            Point[] pts = currentPolygon.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
                            triTile = new Triangle2DF(pts[0], pts[1], pts[2]);
                            dispString("Tri Center" + data.getTri().ToString(), label6);
                            data.setTri(triTile.Centeroid);
                        }
                    }
                    else if (contours.Area > 200 && contours.Area < 800)
                        if (currentPolygon.Total == 4) //The contour has 4 vertices.
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
                            {
                                boxTile = currentPolygon.GetMinAreaRect();
                                dispString("Box Center: " + data.getBox().ToString(), label5);
                                data.setBox(boxTile.center);
                            }
                        }
                }

            Image<Bgr, Byte> triangleRectangleImage = img.CopyBlank();

            comb_color.Draw(triTile, new Bgr(Color.Yellow), 1);
            comb_color.Draw(boxTile, new Bgr(Color.Yellow), 1);

            //***********************************************************************************************************
            //************************************HSV Shape Detection****************************************************
            //***********************************************************************************************************
            //Triangle2DF baseTri;// = new Triangle2DF();      // = new Triangle2DF();
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
                            baseTri = new Triangle2DF(pts[0], pts[1], pts[2]);
                            data.setj1(baseTri.Centeroid);
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


            comb_color.Draw(baseTri, new Bgr(Color.AliceBlue), 2);

            if (boxList1.Count == 2)
            {
                mb_center = boxList1[1].center;
                eb_center = boxList1[0].center;
                MCvBox2D mb = boxList1[1];
                MCvBox2D eb = boxList1[0];

                if (mb.center.X > eb.center.X)
                {
                    boxList1[1] = eb;
                    boxList1[0] = mb;
                }

                data.setj2(boxList1[1].center);     //midbox
                data.setj3(boxList1[0].center);     //end effector
                midBox.center = data.getj2();
                endBox.center = data.getj3();

                LineSegment2DF radial_line = new LineSegment2DF(data.getj2(), data.getj3());
                dispString(radial_line.Length.ToString(), label26);
                LineSegment2DF tri_radial_line = new LineSegment2DF(data.getj1(), data.getj2());
                dispString(tri_radial_line.Length.ToString(), label25);

                comb_color.Draw(radial_line, new Bgr(Color.Silver), 2);
                comb_color.Draw(tri_radial_line, new Bgr(Color.Silver), 2);
                comb_color.Draw(endBox, new Bgr(Color.Green), 2);
                comb_color.Draw(midBox, new Bgr(Color.Brown), 2);

                //dispString(data.getj2().X.ToString(), label9);
                dispString(data.getj3().X.ToString(), label18);
            }

            int[] arm_angle = new int[2];
            int[] darm_angle = new int[2];
            int D_ang1 = new int();
            int ang1 = new int();

            #region Math for desired angles
            //data.setBox(eeAlter(data.getBox()));  
            //if (boxList1.Count == 2)            // && isSeen == false)
            {
                ang1 = (int)(-(Math.Atan2(data.getj3().Y - data.getj1().Y, data.getj3().X - data.getj1().X)
                        - Math.Atan2(153 - 153, 30 - 25)) * 180 / Math.PI);

                if ((data.getj1() != null) && (data.getj3() != null) && (data.getj2() != null))   //find angle between arms curently
                    arm_angle = arm_trig(data.getj1(), data.getj2(), data.getj3());
                if ((data.getj1() != null) && (data.getj3() != null) && (data.getBox() != null))        // BOX: find desired arm angle and angle from origin line to desired hyp line
                {                 
                    darm_angle = arm_trig(data.getj1(), data.getj2(), eeAlter(data.getBox()));
                    D_ang1 = (int)(-(Math.Atan2(data.getBox().Y - data.getj1().Y, data.getBox().X - data.getj1().X)
                        - Math.Atan2(153 - 153, 30 - 25)) * 180 / Math.PI);

                    if (sameSpot(previous, 0) == false)
                    {
                        data.setdj1(darm_angle[0]);
                        data.setdj2(darm_angle[1]);
                    }

                    //jAlter(180 -(data.getdj2()))
                    //if(data.getj3() != null && data.getBox() != null)
                        updateScara((data.getdj1() + D_ang1), (180 - data.getdj2()), data.getj3(), eeAlter(data.getBox()));
                    //updateScara((data.getdj1()), data.getdj2(), data.getj3(), data.getBox());
                }
                else if (data.getj1() != null && data.getj3() != null && data.getTri() != null) //TRI: find desired arm angle and angle from origin line to desired hyp line
                {
                    darm_angle = arm_trig(data.getj1(), data.getj2(), data.getTri());
                    D_ang1 = (int)(-(Math.Atan2(data.getTri().Y - data.getj1().Y, data.getTri().X - data.getj1().X)
                        - Math.Atan2(153 - 153, 30 - 25)) * 180 / Math.PI);
                    updateScara((D_ang1 + darm_angle[0]), darm_angle[1], data.getj3(), data.getBox());
                }
            }
            //else if (boxList1.Count < 2)
                //updateScara(60, 90, data.getTri(), data.getBox());

#endregion
            dispString("EE point" + data.getj3().ToString(), label22);
            dispString("Ang_B = " + arm_angle[0].ToString(), label10);
            dispString("Ang_C = " + arm_angle[1].ToString(), label11);
            dispString("D_Ang_B = " + data.getdj1().ToString(), label8); //desired angles
            dispString("D_Ang_C = " + data.getdj2().ToString(), label9); //desired angles
            dispString("Hyp we need " + D_ang1.ToString(), label15);
            dispString("Hyp we have " + ang1.ToString(), label21);

            comb_color.Draw(oline, new Bgr(Color.Red), 1);
            imageBox4.Image = comb_color;

            //record preious boxtile
            previous = data.getBox();

            //END OF CAPTURE
        }

        public void updateScara(int one, int two, PointF EE, PointF tile)
        {
            if (checkBox2.Checked == false)
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
                    outByte[2] = (byte)Convert.ToInt32(100); //was 40
                }
                else if (radioButton6.Checked)    //drop
                {
                    outByte[2] = (byte)Convert.ToInt32(25);
                }

                //State of Checkbox determines state of magnet
                if (checkBox1.Checked)
                    outByte[3] = (byte)Convert.ToInt32(1);
                else
                    outByte[3] = (byte)Convert.ToInt32(0);
            }
            else if(checkBox2.Checked == true)//Autonomous Mode
            {
                tile = eeAlter(tile);       //EE != tile
                
                //if((EE.X > (tile.X+1)) && (EE.X < (tile.X-1)) && (EE.Y > (tile.Y+1)) && (EE.Y < (tile.Y - 1)))
                if ((sequence == 1) && (EE.X < (tile.X + 100)) && (EE.X > (tile.X - 100)) && (EE.Y < (tile.Y + 100)) && (EE.Y > (tile.Y - 100)))
                {//Grab part
                    outByte[0] = (byte)Convert.ToInt32(one);  //joint1
                    outByte[1] = (byte)Convert.ToInt32(two);  //joint2
                    outByte[2] = (byte)Convert.ToInt32(95);         //EE angle
                    outByte[3] = (byte)Convert.ToInt32(1);
                    
                    sequence++;
                }
                else if(sequence == 0)
                {//Go to part
                    outByte[0] = (byte)Convert.ToInt32((int)one);  //joint1
                    outByte[1] = (byte)Convert.ToInt32((int)two);  //joint2
                    outByte[2] = (byte)Convert.ToInt32(30);
                    outByte[3] = (byte)Convert.ToInt32(0);
                    sequence++;
                }
                else if(sequence == 2)
                {//move to drop zone alpha
                    outByte[0] = (byte)Convert.ToInt32(0);  //joint1
                    outByte[1] = (byte)Convert.ToInt32(90);  //joint2
                    outByte[2] = (byte)Convert.ToInt32(30);
                    outByte[3] = (byte)Convert.ToInt32(1);
                    sequence++;
                }
                else if(sequence == 3)
                {//turn off magnet
                    outByte[0] = (byte)Convert.ToInt32(0);  //joint1
                    outByte[1] = (byte)Convert.ToInt32(90);  //joint2
                    outByte[2] = (byte)Convert.ToInt32(30);
                    outByte[3] = (byte)Convert.ToInt32(0);
                    sequence++;
                }
                else if(sequence == 4)
                {//move out of the way
                    outByte[0] = (byte)Convert.ToInt32(45);  //joint1
                    outByte[1] = (byte)Convert.ToInt32(60);  //joint2
                    outByte[2] = (byte)Convert.ToInt32(40);
                    outByte[3] = (byte)Convert.ToInt32(0);
                    sequence = 0;
                }

                
            }
            //if ((point.X < (data.getBox().X + 2) && point.X > (data.getBox().X - 2)) &&
            //       (point.Y < (data.getBox().Y + 2) & point.Y > (data.getBox().Y - 2)))

            if (serPort.IsOpen)
            {
                if (!checkBox2.Checked)//Send data when not in autonomous
                {
                    if (SerReady)
                    {
                        serPort.Write(outByte, 0, 4);
                        SerReady = false;
                    }
                    else if (!SerReady)
                    {
                        if ((Ser_Alternate % 2) == 1)//alternate between two button clicks
                        {
                            serPort.Write(outByte, 0, outByte.Length);
                            Ser_Alternate++;
                        }

                        if (Ser_Alternate == 4)
                            Ser_Alternate = 0;
                    }
                }
                else //Send data in Autonomous Mode
                {
                    if(SerReady)
                    {
                        serPort.Write(outByte, 0, outByte.Length);
                        SerReady = false;
                    }
                }
            }
            //End of updata SCARA
        }

        #region Form Control
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

        public void dispText(String S, TextBox textbox)
        {
            if (InvokeRequired)
            {
                displayTextDelegate dispStrDel = dispText;
                this.BeginInvoke(dispStrDel, S, textbox);
            }
            else
                textbox.Text = S;
        }

        //Serial Communication

        //Serial Communication: serPort_DataReceived
        void serPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int num = serPort.ReadByte();
            if (num == 1)
                SerReady = true;

            if (!checkBox2.Checked)//do not use in autonomous mode
            {
                if ((Ser_Alternate % 2) == 0)//alternate between two button clicks
                {
                    serPort.Write(outByte, 0, outByte.Length);
                    Ser_Alternate++;
                }
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
            if (radioButton1.Checked)
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
            if (radioButton1.Checked)
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

        public void button9_Click(object sender, EventArgs e)   //this was originally private
        {
            //updateScara();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)    //pick
        {
            textBox3.Text = Convert.ToString(90);
            checkBox1.Checked = true;
            button9.PerformClick();
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)    //move
        {
            textBox3.Text = Convert.ToString(40);
            button9.PerformClick();
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)    //drop
        {
            textBox3.Text = Convert.ToString(25);
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
            if (int.Parse(textBox2.Text) > 180)
            { textBox2.Text = "180"; }

            if (int.Parse(textBox2.Text) < 10)
            { textBox2.Text = "10"; }

            if (trackBar5.Value.ToString() != textBox2.Text)
            { trackBar5.Value = int.Parse(textBox2.Text); }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (int.Parse(textBox1.Text) > 180)
            { textBox1.Text = "170"; }

            if (int.Parse(textBox1.Text) < 0)
            { textBox1.Text = "0"; }

            if (trackBar4.Value.ToString() != textBox1.Text)
            { trackBar4.Value = int.Parse(textBox1.Text); }

        }
#endregion

        //this is where the trig will be done to determine joint angles
        public int[] arm_trig(PointF j1, PointF j2, PointF jE)
        {
            LineSegment2DF sideA = new LineSegment2DF(j1, j2);
            LineSegment2DF sideB = new LineSegment2DF(j2, jE);
            LineSegment2DF sideC = new LineSegment2DF(jE, j1);          //create hypotenuse line
            //LineSegment2DF newSideC = new LineSegment2DF(j1, destination);

            double a_lgth = A_Length;
            double b_lgth = B_Length;
            double c_lgth = sideC.Length;                               //get hypotenuse length
            double ang_a;
            double ang_b;
            double ang_c;

            ang_b = (Math.Acos((b_lgth * b_lgth - a_lgth * a_lgth - c_lgth * c_lgth) / (-2 * a_lgth * c_lgth)));
            ang_c = (Math.Acos((c_lgth*c_lgth - a_lgth*a_lgth - b_lgth*b_lgth)/(-2*a_lgth*b_lgth)));

            ang_b = ((ang_b * 180) / Math.PI);
            ang_c = ((ang_c * 180) / Math.PI);

            int[] angle = new int[2];

            angle[0] = (int)ang_b;
            angle[1] = (int)ang_c;

            return angle;

        }

        private bool sameSpot(PointF point, int identifier)
        {
            if(identifier == 0)
            {
                if ((point.X < (data.getBox().X + 2) && point.X > (data.getBox().X - 2)) &&
                     (point.Y < (data.getBox().Y + 2) && point.Y > (data.getBox().Y - 2)))
                    return true;
                else
                    return false;
            }
            else if(identifier == 1)
            {
                if ((point.X < (data.getTri().X + 2) && point.X > (data.getTri().X - 2)) &&
                    (point.Y < (data.getTri().Y + 2) && point.Y > (data.getTri().Y - 2)))
                    return true;
                else 
                    return false;          
            }
            else
                return false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            button9.PerformClick();
        }

        private void Bicep_Length_ValueChanged(object sender, EventArgs e)
        {
            A_Length = (int)Bicep_Length.Value;
        }

        private void Forearm_Length_ValueChanged(object sender, EventArgs e)
        {
            B_Length = (int)Forearm_Length.Value;
        }

        private void Reset_Lengths_Click(object sender, EventArgs e)
        {
            Bicep_Length.Value = (decimal)152.5;
            Forearm_Length.Value = (decimal)62;
            X_Proportional.Value = (decimal)1.1925;
            X_plus.Value = (decimal)37.566;
            Y_Proportional.Value = (decimal)1.2137;
            Y_plus.Value = (decimal)12.925;
        }

        private PointF eeAlter(PointF point)
        {
            //y= 1.2519*y - 22.875
            //x= 1.1988*x - 35.531

            point.X = (float)(X_multiple * point.X - X_addition);
            point.Y = (float)(Y_multiple * point.Y - Y_addition);

            return point;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            Y_multiple = (double)Y_Proportional.Value;
        }

        private void X_Proportional_ValueChanged(object sender, EventArgs e)
        {
            X_multiple = (double)X_Proportional.Value;
        }

        private void X_plus_ValueChanged(object sender, EventArgs e)
        {
            X_addition = (double)X_plus.Value;
        }

        private void Y_plus_ValueChanged(object sender, EventArgs e)
        {
            Y_addition = (double)Y_plus.Value;
        }
    }
}
