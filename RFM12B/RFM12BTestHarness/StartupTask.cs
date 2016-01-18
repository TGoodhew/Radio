using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices;
using System.Threading.Tasks;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace RFM12BTestHarness
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

            byte[] data = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };

            var deferral = taskInstance.GetDeferral();

            RFM12B.Rfm12b rfm12b = new RFM12B.Rfm12b("SPI0", 0);

            rfm12b.HardReset();

            int result = await rfm12b.BeginAsync();

            while (true)
            {
                rfm12b.SendData(data);
                await Task.Delay(250);
            }

            deferral.Complete();
        }
    }
}
