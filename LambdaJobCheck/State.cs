using System;
using System.Collections.Generic;
using System.Text;

namespace LambdaJobCheck
{
    public class State
    {
        public string CustomerId { get; set; }
        public string JobKey { get; set; }
        public string Status { get; set; }
        public bool JobAlreadyRunning { get; set; }
    }
}
