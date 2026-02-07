using System;
using System.Collections.Generic;
using System.Text;

namespace Api.AspNetCore.Services
{
    public class DataSyncBackgroundServiceOptions : BackgroundServiceOptions
    {
        public int BatchCount { get; set; }
        public int BatchDelay { get; set; }
    }
}
