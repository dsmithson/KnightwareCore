﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Diagnostics
{
    public class TraceMessage
    {
        public TracingLevel Level { get; set; }

        public string Message { get; set; }

        public object Sender { get; set; }

        public DateTime LogTime { get; set; }

        public string SenderShortName
        {
            get
            {
                if(Sender == null)
                    return "<Unknown>";
                else
                    return Sender.GetType().Name;
            }
        }

        public TraceMessage()
        {
            LogTime = DateTime.Now;
        }
    }
}
