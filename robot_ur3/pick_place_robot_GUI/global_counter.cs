using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiEmgu
{
    class global_counter
    {
        //public const string GlobalString = "Important Text";
        static int _globalValue;

        public global_counter()
        {
            _globalValue = 0;            
        }
        public void addCnt()
        {
            _globalValue++;
        }
        public int Get()
        {
            return _globalValue;
        }

        public static bool GlobalBoolean;
    }
}
