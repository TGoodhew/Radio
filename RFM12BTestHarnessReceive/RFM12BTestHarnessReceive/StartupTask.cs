﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace RFM12BTestHarnessReceive
{
    public sealed class StartupTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
            var deferral = taskInstance.GetDeferral();

            RFM12B.Rfm12b rfm12b = new RFM12B.Rfm12b("SPI0", 0);

            int result = await rfm12b.BeginAsync();

            while (true)
            {
                rfm12b.ReadData();
            }

            deferral.Complete();
        }
    }
}
