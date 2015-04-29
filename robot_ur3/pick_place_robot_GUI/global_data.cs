using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace HiEmgu
{
    class global_data
    {
        //public const string GlobalString = "Important Text";
        private static int counter;
        private static PointF boxTile;
        private static PointF triTile;
        private static PointF j1;
        private static PointF j2;
        private static PointF j3;
        private static int dj1;
        private static int dj2;


        public global_data()
        {
            counter = 0;
            boxTile = new PointF(0, 0);
            triTile = new PointF(0, 0);
            j1 = new PointF(0, 0);
            j2 = new PointF(0, 0);
            j3 = new PointF(0, 0);
            dj1 = 0;
            dj2 = 0;

        }
        public void addCnt()
        { counter++; }
        public void setBox(PointF point)
        { boxTile = point; }
        public void setTri(PointF point)
        { triTile = point; }
        public void setj1(PointF point)
        { j1 = point; }
        public void setj2(PointF point)
        { j2 = point; }
        public void setj3(PointF point)
        { j3 = point; }
        public void setdj1(int val)
        { dj1 = val; }
        public void setdj2(int val)
        { dj2 = val; }

        public PointF getBox()
        { return boxTile; }
        public PointF getTri()
        { return triTile; }
        public PointF getj1()
        { return j1; }
        public PointF getj2()
        { return j2; }
        public PointF getj3()
        { return j3; }
        public int getdj1()
        { return dj1; }
        public int getdj2()
        { return dj2; }        
    }
}
