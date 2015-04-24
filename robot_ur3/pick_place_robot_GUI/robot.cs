using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pick_place_robot_GUI;

namespace HiEmgu
{
    class robot
    {
        private double joint1;
        private double joint2;
        private double endEff;
        private bool magnet;
        enum eeState { pick, move, drop };
        const int pick = 90;
        const int move = 40;
        const int drop = 10;        
        private bool isInitialized;

        public robot()
        {
            isInitialized = true;
            magnet = false;
            joint1 = 60;
            joint2 = 60;
            endEff = (int)eeState.move;            
        }
        private void updateBot()
        {
            updateScara();          //created a method in form1?
        }

        private void updateScara()
        {
            throw new NotImplementedException();
        }
        public void setJ1(double value)
        {
            joint1 = value;
            updateBot();
        }
        public void setJ2(double value)
        {
            joint2 = value;
            updateBot();
        }
        public void setEE(int val)
        {
            switch(val)
            {
                case 0:
                    endEff = (int)eeState.pick;
                    magnet = true;
                    break;
                case 1:
                    endEff = (int)eeState.move;
                    magnet = true;
                    break;
                case 2:
                    endEff = (int)eeState.drop;
                    magnet = false;
                    break;
            }
            updateBot();
        }
        public double getJ1()
        { return joint1; }
        public double getJ2()
        { return joint2; }
        public double getEE()
        { return endEff; }
    }
}









                
                    
                    