using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetHelper
{
    class RunWorkerArgument
    {
        public RunWorkerArgument()
        {
    
        }

        public RunWorkerArgument(int runtimes,int interval)
        {
            RunTimes = runtimes;
            TimeInterval = interval;
        }

        public int RunTimes { get; set; }

        public int TimeInterval { get; set; }
    }
}
