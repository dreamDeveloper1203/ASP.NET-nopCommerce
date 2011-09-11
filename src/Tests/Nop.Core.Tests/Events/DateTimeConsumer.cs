﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nop.Core.Events;

namespace Nop.Core.Tests.Events
{
    public class DateTimeConsumer : IConsumer<DateTime>
    {
        public void Handle(DateTime eventMessage)
        {
            DateTime = eventMessage;
        }

        // For testing
        public static DateTime DateTime { get; set; }
    }
}
