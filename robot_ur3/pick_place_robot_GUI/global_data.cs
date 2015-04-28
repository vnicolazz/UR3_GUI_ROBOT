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

        public global_data()
        {
            counter = 0;
            boxTile = new PointF(0, 0);
            triTile = new PointF(0, 0);
            j1 = new PointF(0, 0);
            j2 = new PointF(0, 0);
            j3 = new PointF(0, 0);
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
        
    }
}
