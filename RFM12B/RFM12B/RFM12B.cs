using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;

namespace RFM12B
{
    public sealed class Rfm12b
    {
        private string spiBusName;
        private int spiChipSelect;
        private SpiDevice spiDevice;
        private GpioController gpioController;

        // GPIO Pins used to control the RFM12B Module
        private GpioPin pinReset = null;
        private GpioPin pinNIRQ = null;
        private GpioPin pinNFFS = null;
        private GpioPin pinFFIT = null;
        private GpioPin pinNINT = null;

        // GPIO Pin Numbers - These should be changed to reflect how the display is actually connected
        // TODO: Split this out into object creation
        private readonly int NRES_PIN = 4;
        private readonly int NIRQ_PIN = 12;
        private readonly int NFFS_PIN = 6;
        private readonly int FFIT_PIN = 5;
        private readonly int NINT_PIN = 27;

        // Various RFM12B commands
        private readonly byte[] Reset = { 0xFE, 0x00 };
        private readonly byte[] Status = { 0x00, 0x00 };
        private readonly byte[] Clock_1Mhz = { 0xC0, 0x00 };
        private readonly byte[] Clock_10Mhz = { 0xC0, 0xE0 };

        // Test command definitions - From http://tools.jeelabs.org/rfm12b.html
        private readonly byte[] Config = { 0x80, 0x37 };        // 915MHz, 12pF [el=0]
        private readonly byte[] EL_Config = { 0x80, 0xB7 };     // 915MHz, 12pF, TX on [el=1]
        private readonly byte[] Power = { 0x82, 0x58 };         // Base Band Block, Tx off [et=0], Syn On, Osc on 
        private readonly byte[] ET_Power = { 0x82, 0x78 };      // Base Band Block, Tx on [et=1], Syn On, Osc on 
        private readonly byte[] Frequency = { 0xA6, 0x40 };     // 912MHz Center
        private readonly byte[] DataRate = { 0xC6, 0x11 };      // 19.157kbps - SPI speed dependent - RGUR thrown
        private readonly byte[] RecControl = { 0x94, 0xA2 };    // LNA Max, RX BW 134 KHz, DRSSI -91bB, VDI, VDI Fast
        private readonly byte[] DataFilter = { 0xC2, 0xAC };    // Digital Filter, Thres 4, Auto, Slow
        private readonly byte[] FIFO = { 0xCA, 0x83 };          // Level 8, Sync, Fill On, Sync 2, Low
        private readonly byte[] SyncPattern = { 0xCE, 0xD4 };   // Pattern D4
        private readonly byte[] AFC = { 0xC4, 0x83 };           // AFC On, Offset On, No restrict, Keep offset during VDI
        private readonly byte[] TxControl = { 0x98, 0x50 };     // Pos, 90 KHz
        private readonly byte[] PLL = { 0xCC, 0x57 };           // Slow Rise, Dither off, 256kbps band
        private readonly byte[] WakeUp = { 0xEA, 0x00 };        // Off
        private readonly byte[] LowDutyCycle = { 0xC8, 0x00 };  // Off
        private readonly byte[] BattClock = { 0xC0, 0x60 };     // 2.2V, Clk 2 MHz


        public Rfm12b (string spiBusName, int spiChipSelect)
        {
            this.spiBusName = spiBusName;
            this.spiChipSelect = spiChipSelect;
        }

        public IAsyncOperation<int> BeginAsync()
        {
            return this.BeginAsyncHelper().AsAsyncOperation<int>();
        }

        private async Task<int> BeginAsyncHelper()
        {
            try
            {
                // Setup provider and buses
                InitLightningProvider();
                await InitGPIO();
                await InitSPI();

                // Initalize Radio
                InitRadio();
            }
            catch (Exception ex)
            {
                throw new Exception("RFM12B setup failed", ex);
            }

            return 0;
        }

        private void InitRadio()
        {
            byte[] status = SpiReadStatus();

            // Config for demo settings
            SpiSendCmd(Config);
            SpiSendCmd(Power);
            SpiSendCmd(Frequency);
            SpiSendCmd(DataRate);
            SpiSendCmd(RecControl);
            SpiSendCmd(DataFilter);
            SpiSendCmd(FIFO);
            SpiSendCmd(SyncPattern);
            SpiSendCmd(AFC);
            SpiSendCmd(TxControl);
            SpiSendCmd(PLL);
            SpiSendCmd(WakeUp);
            SpiSendCmd(LowDutyCycle);
            SpiSendCmd(BattClock);

            status = SpiReadStatus();
        }

        public IAsyncOperation<int> HardReset()
        {
            return this.HardResetHelper().AsAsyncOperation();
        }

        private async Task<int> HardResetHelper()
        {
            byte[] status = SpiReadStatus();

            pinReset.Write(GpioPinValue.Low);
            await Task.Delay(300);
            pinReset.Write(GpioPinValue.High);
            await Task.Delay(300);

            status = SpiReadStatus();

            return 0;
        }

        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(spiChipSelect);
                settings.ClockFrequency = 10000000;
                settings.Mode = SpiMode.Mode0;

                SpiController controller = await SpiController.GetDefaultAsync();
                spiDevice = controller.GetDevice(settings);
            }
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed.", ex);
            }
        }

        private async Task InitGPIO()
        {
            // Get the GPIO controller
            try
            {
                gpioController = await GpioController.GetDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("GPIO Initialization failed.", ex);
            }

            // Setup additional GPIO pins
            pinReset = CreateWritePin(NRES_PIN);
            pinNIRQ = CreateReadPin(NIRQ_PIN);
            //pinNFFS = CreateWritePin(NFFS_PIN);
            //pinFFIT = CreateWritePin(FFIT_PIN);
            //pinNINT = CreateWritePin(NINT_PIN);
        }

        private GpioPin CreateWritePin(int pinNumber, bool high = true)
        {
            var pin = gpioController.OpenPin(pinNumber, GpioSharingMode.Exclusive);
            pin.SetDriveMode(GpioPinDriveMode.Output);
            if (high)
                pin.Write(GpioPinValue.High);
            else
                pin.Write(GpioPinValue.Low);

            return pin;
        }

        private GpioPin CreateReadPin(int pinNumber)
        {
            var pin = gpioController.OpenPin(pinNumber, GpioSharingMode.Exclusive);
            pin.SetDriveMode(GpioPinDriveMode.Input);

            return pin;
        }

        private void InitLightningProvider()
        {
            //Set the Lightning Provider as the default if Lightning driver is enabled on the target device
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
        }

        private void SpiSendCmd(byte[] cmd)
        {
            spiDevice.Write(cmd);
        }

        private byte[] SpiReadStatus()
        {
            byte[] result = new byte[2] { 0x00, 0x00 };
            spiDevice.TransferFullDuplex(Status, result);

            return result;
        }

        public void SpiTransmitData(byte txData)
        {
            byte[] data = new byte[2] { 0xB8, txData };
            spiDevice.Write(data);
        }

        public void SendData([ReadOnlyArray]byte[] data)
        {
            byte[] preamble = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
            byte[] sync = new byte[] { 0x2D, 0xD4 };
            byte[] dummy = new byte[] { 0x00 };

            //var result = SpiReadStatus();

            // el=1
            SpiSendCmd(EL_Config);
            // et=1
            SpiSendCmd(ET_Power);

            SpiWriteData(preamble);

            SpiWriteData(sync);

            SpiWriteData(data);

            SpiWriteData(dummy);

            // et = 0
            SpiSendCmd(Power);
            // el = 0 
            SpiSendCmd(Config);
        }

        private void SpiWriteData(byte[] data)
        {
            byte[] result = null;
            byte[] txData = new byte[2] { 0xB8, 0x00 };

            foreach (byte txbyte in data)
            {
                // wait till NIRQ == Low
                while (pinNIRQ.Read() != GpioPinValue.Low) ;

                result = SpiReadStatus();

                // SPI write tx byte
                txData[1] = txbyte;
                SpiSendCmd(txData);
            }
        }
    }
}
