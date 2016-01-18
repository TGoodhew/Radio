using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace RFM22BTestHarness
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

            byte[] text;

            var deferral = taskInstance.GetDeferral();

            RFM22B.Rfm22b rfm22b = new RFM22B.Rfm22b("SPI0", 0);

            int result = await rfm22b.BeginAsync();

            while (true)
            {
                text = rfm22b.GetData();
            }   
            deferral.Complete();
        }
    }
}
