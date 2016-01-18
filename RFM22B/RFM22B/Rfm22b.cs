using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;

namespace RFM22B
{
    public sealed class Rfm22b
    {
        private string spiBusName;
        private int spiChipSelect;
        private SpiDevice spiDevice;
        private GpioController gpioController;

        // GPIO Pins used to control the RFM12B Module
        private GpioPin pinNIRQ = null;

        // GPIO Pin Numbers - These should be changed to reflect how the display is actually connected
        // TODO: Split this out into object creation
        private readonly int NIRQ_PIN = 12;

        // Various RFM12B registers (from AN440.PDF)
        private readonly byte Reg_OpControl_1               = 0x07;
        private readonly byte Reg_OpControl_2               = 0x08;
        private readonly byte Reg_IntStatus_1               = 0x03;
        private readonly byte Reg_IntStatus_2               = 0x04;
        private readonly byte Reg_IntEnable_1               = 0x05;
        private readonly byte Reg_IntEnable_2               = 0x06;
        private readonly byte Reg_OscLoadCapacitance        = 0x09;
        private readonly byte Reg_GPIOConfig_0              = 0x0B;
        private readonly byte Reg_GPIOConfig_1              = 0x0C;
        private readonly byte Reg_GPIOConfig_2              = 0x0D;
        private readonly byte Reg_IOConfig                  = 0x0E;
        private readonly byte Reg_FIFO                      = 0x7F;

        // Registers in order according to the value calulation XLS
        private readonly byte Reg_IFFilerBW                 = 0x1C;
        private readonly byte Reg_AFCOverride               = 0x1D;
        private readonly byte Reg_ClockRecoveryRatio        = 0x20;
        private readonly byte Reg_ClockRecoveryOffset_2     = 0x21;
        private readonly byte Reg_ClockRecoveryOffset_1     = 0x22;
        private readonly byte Reg_ClockRecoveryOffset_0     = 0x23;
        private readonly byte Reg_ClockRecoveryGain_1       = 0x24;
        private readonly byte Reg_ClockRecoveryGain_0       = 0x25;
        private readonly byte Reg_AFCLimiter                = 0x2A;
        private readonly byte Reg_OOKCounterValue_1         = 0x2C;
        private readonly byte Reg_OOKCounterValue_2         = 0x2D;
        private readonly byte Reg_SlicerPeakHold            = 0x2E;
        private readonly byte Reg_DataAccessControl         = 0x30;
        private readonly byte Reg_HeaderControl_1           = 0x32;
        private readonly byte Reg_HeaderControl_2           = 0x33;
        private readonly byte Reg_PreambleLength            = 0x34;
        private readonly byte Reg_PreambleDetectionControl  = 0x35;
        private readonly byte Reg_SyncWord_3                = 0x36;
        private readonly byte Reg_SyncWord_2                = 0x37;
        private readonly byte Reg_SyncWord_1                = 0x38;
        private readonly byte Reg_SyncWord_0                = 0x39;
        private readonly byte Reg_TXHeader_3                = 0x3A;
        private readonly byte Reg_TXHeader_2                = 0x3B;
        private readonly byte Reg_TXHeader_1                = 0x3C;
        private readonly byte Reg_TXHeader_0                = 0x3D;
        private readonly byte Reg_TXPacketLength            = 0x3E;
        private readonly byte Reg_CheckHeader_3             = 0x3F;
        private readonly byte Reg_CheckHeader_2             = 0x40;
        private readonly byte Reg_CheckHeader_1             = 0x41;
        private readonly byte Reg_CheckHeader_0             = 0x42;
        private readonly byte Reg_HeaderEnable_3            = 0x43;
        private readonly byte Reg_HeaderEnable_2            = 0x44;
        private readonly byte Reg_HeaderEnable_1            = 0x45;
        private readonly byte Reg_HeaderEnable_0            = 0x46;
        private readonly byte Reg_TXDataRate_1              = 0x6E;
        private readonly byte Reg_TXDataRate_0              = 0x6F;
        private readonly byte Reg_ModulationModeControl_1   = 0x70;
        private readonly byte Reg_ModulationModeControl_2   = 0x71;
        private readonly byte Reg_FreqDeviation             = 0x72;
        private readonly byte Reg_FrequencyBand             = 0x75;
        private readonly byte Reg_CarrierFrequency_1        = 0x76;
        private readonly byte Reg_CarrierFrequency_0        = 0x77;

        // Various RFM12B registers (from HopeRF RFM22.PDF)
        private readonly byte Reg_DigitalTestBus            = 0x51;
        private readonly byte Reg_AGCOverride_1             = 0x69;

        // Regsister Values
        // The recommended approach is to use the calulation spreadsheet
        // and then just use the hex values directly. To make this easier
        // the values below come from  the XLS and can then be box selection
        // copied into the readonly entries

        /* XLS Values - Whitesapce matches the XLS
        9A
        40


        3C
        02
        22
        22
        07
        FF
        48
        28
        0C
        28

        AC
        8C
        02
        08
        2A
        2D
        D4
        00
        00
        00
        00
        00
        00
        00
        00
        00
        00
        00
        FF
        FF
        FF
        FF



        19
        9A

        0C
        23
        50

        73
        00
        00
        */

        // Register values - If you remove the whitespace that the cut & paste
        // will need to be done in parts otherwise you can take the above and
        // box selection paste them into the readonly definitions below

        private readonly byte Value_IFFilerBW                   = 0x9A;
        private readonly byte Value_AFCOverride                 = 0x40;


        private readonly byte Value_ClockRecoveryRatio          = 0x3C;
        private readonly byte Value_ClockRecoveryOffset_2       = 0x02;
        private readonly byte Value_ClockRecoveryOffset_1       = 0x22;
        private readonly byte Value_ClockRecoveryOffset_0       = 0x22;
        private readonly byte Value_ClockRecoveryGain_1         = 0x07;
        private readonly byte Value_ClockRecoveryGain_0         = 0xFF;
        private readonly byte Value_AFCLimiter                  = 0x48;
        private readonly byte Value_OOKCounterValue_1           = 0x28;
        private readonly byte Value_OOKCounterValue_2           = 0x0C;
        private readonly byte Value_SlicerPeakHold              = 0x28;

        private readonly byte Value_DataAccessControl           = 0xAC;
        private readonly byte Value_HeaderControl_1             = 0x8C;
        private readonly byte Value_HeaderControl_2             = 0x02;
        private readonly byte Value_PreambleLength              = 0x08;
        private readonly byte Value_PreambleDetectionControl    = 0x2A;
        private readonly byte Value_SyncWord_3                  = 0x2D;
        private readonly byte Value_SyncWord_2                  = 0xD4;
        private readonly byte Value_SyncWord_1                  = 0x00;
        private readonly byte Value_SyncWord_0                  = 0x00;
        private readonly byte Value_TXHeader_3                  = 0x00;
        private readonly byte Value_TXHeader_2                  = 0x00;
        private readonly byte Value_TXHeader_1                  = 0x00;
        private readonly byte Value_TXHeader_0                  = 0x00;
        private readonly byte Value_TXPacketLength              = 0x00;
        private readonly byte Value_CheckHeader_3               = 0x00;
        private readonly byte Value_CheckHeader_2               = 0x00;
        private readonly byte Value_CheckHeader_1               = 0x00;
        private readonly byte Value_CheckHeader_0               = 0x00;
        private readonly byte Value_HeaderEnable_3              = 0xFF;
        private readonly byte Value_HeaderEnable_2              = 0xFF;
        private readonly byte Value_HeaderEnable_1              = 0xFF;
        private readonly byte Value_HeaderEnable_0              = 0xFF;



        private readonly byte Value_TXDataRate_1                = 0x19;
        private readonly byte Value_TXDataRate_0                = 0x9A;

        private readonly byte Value_ModulationModeControl_1     = 0x0C;
        private readonly byte Value_ModulationModeControl_2     = 0x23;
        private readonly byte Value_FreqDeviation               = 0x50;

        private readonly byte Value_FrequencyBand               = 0x73;
        private readonly byte Value_CarrierFrequency_1          = 0x00;
        private readonly byte Value_CarrierFrequency_0          = 0x00;

        public Rfm22b(string spiBusName, int spiChipSelect)
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
                throw new Exception("RFM22B setup failed", ex);
            }

            return 0;
        }

        private void InitRadio()
        {
            // Do a software reset
            SpiWriteRegister(Reg_OpControl_1, 0x80);

            // Clear the interrupt status by reading them
            SpiReadRegister(Reg_IntStatus_1);
            SpiReadRegister(Reg_IntStatus_2);

            // Set the cyrstal oscillator load capacitance to the middle of the band
            SpiWriteRegister(Reg_OscLoadCapacitance, 0xD5);

            // Set IF Filter Bandwidth
            SpiWriteRegister(Reg_IFFilerBW, Value_IFFilerBW);

            // Configure the AFC 
            SpiWriteRegister(Reg_AFCOverride, Value_AFCOverride);
            SpiWriteRegister(Reg_AFCLimiter, Value_AFCLimiter);

            // Setup the clock recovery
            SpiWriteRegister(Reg_ClockRecoveryRatio, Value_ClockRecoveryRatio);
            SpiWriteRegister(Reg_ClockRecoveryOffset_2, Value_ClockRecoveryOffset_2);
            SpiWriteRegister(Reg_ClockRecoveryOffset_1, Value_ClockRecoveryOffset_1);
            SpiWriteRegister(Reg_ClockRecoveryOffset_0, Value_ClockRecoveryOffset_0);
            SpiWriteRegister(Reg_ClockRecoveryGain_1, Value_ClockRecoveryGain_1);
            SpiWriteRegister(Reg_ClockRecoveryGain_0, Value_ClockRecoveryGain_1);

            //Config Data Access Control
            SpiWriteRegister(Reg_DataAccessControl, Value_DataAccessControl);

            // Enable header control 
            SpiWriteRegister(Reg_HeaderControl_1, Value_HeaderControl_1);
            SpiWriteRegister(Reg_HeaderControl_2, Value_HeaderControl_2);

            // Set Preamble
            SpiWriteRegister(Reg_PreambleLength, Value_PreambleLength);
            SpiWriteRegister(Reg_PreambleDetectionControl, Value_PreambleDetectionControl);

            // Set Sync Word
            SpiWriteRegister(Reg_SyncWord_3, Value_SyncWord_3);
            SpiWriteRegister(Reg_SyncWord_2, Value_SyncWord_2);
            SpiWriteRegister(Reg_SyncWord_1, Value_SyncWord_1);
            SpiWriteRegister(Reg_SyncWord_0, Value_SyncWord_0);

            // Set TX Packet Length
            SpiWriteRegister(Reg_TXPacketLength, Value_TXPacketLength);

            // Configure header
            SpiWriteRegister(Reg_CheckHeader_3, Value_CheckHeader_3);
            SpiWriteRegister(Reg_CheckHeader_2, Value_CheckHeader_2);
            SpiWriteRegister(Reg_CheckHeader_1, Value_CheckHeader_1);
            SpiWriteRegister(Reg_CheckHeader_0, Value_CheckHeader_0);
            SpiWriteRegister(Reg_HeaderEnable_3, Value_HeaderEnable_3);
            SpiWriteRegister(Reg_HeaderEnable_2, Value_HeaderEnable_2);
            SpiWriteRegister(Reg_HeaderEnable_1, Value_HeaderEnable_1);
            SpiWriteRegister(Reg_HeaderEnable_0, Value_HeaderEnable_0);

            // Set Data Rate
            SpiWriteRegister(Reg_TXDataRate_1, Value_TXDataRate_1);
            SpiWriteRegister(Reg_TXDataRate_0, Value_TXDataRate_0);

            // Configure modulation control
            SpiWriteRegister(Reg_ModulationModeControl_1, Value_ModulationModeControl_1);
            SpiWriteRegister(Reg_ModulationModeControl_2, Value_ModulationModeControl_2);

            // Configure frequency
            SpiWriteRegister(Reg_FreqDeviation, Value_FreqDeviation);
            SpiWriteRegister(Reg_FrequencyBand, Value_FrequencyBand);
            SpiWriteRegister(Reg_CarrierFrequency_1, Value_CarrierFrequency_1);
            SpiWriteRegister(Reg_CarrierFrequency_0, Value_CarrierFrequency_0);

            // Configure GPIO
            SpiWriteRegister(Reg_GPIOConfig_0, 0xD9);
            SpiWriteRegister(Reg_GPIOConfig_1, 0xD4);
            SpiWriteRegister(Reg_GPIOConfig_2, 0xDB);
            SpiWriteRegister(Reg_DigitalTestBus, 0x00);

            // Enable AGC Override
            SpiWriteRegister(Reg_AGCOverride_1, 0x60);

            // Enable Interrupts
            SpiWriteRegister(Reg_IntEnable_1, 0x02);
            SpiWriteRegister(Reg_IntEnable_2, 0x00);

            // Turn the receiver on
            SpiWriteRegister(Reg_OpControl_1, 0x04);
        }

        private void InitLightningProvider()
        {
            //Set the Lightning Provider as the default if Lightning driver is enabled on the target device
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
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

            pinNIRQ = gpioController.OpenPin(NIRQ_PIN, GpioSharingMode.Exclusive);
            pinNIRQ.SetDriveMode(GpioPinDriveMode.Input);
        }

        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(spiChipSelect);
                settings.ClockFrequency = 5000000;
                settings.Mode = SpiMode.Mode0;

                SpiController controller = await SpiController.GetDefaultAsync();
                spiDevice = controller.GetDevice(settings);
            }
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed.", ex);
            }
        }

        private void SpiWriteRegister(byte register, byte value)
        {
            byte[] result = new byte[2] { 0x00, 0x00 };
            byte[] bCmd = new byte[2] { (byte)(register ^ 0x80), value };

            spiDevice.TransferFullDuplex(bCmd, result);
        }

        private byte SpiReadRegister(byte register)
        {
            byte[] result = new byte[2] { 0x00, 0x00 };
            byte[] bCmd = new byte[2] { register, 0x00 };

            spiDevice.TransferFullDuplex(bCmd, result);

            return result[1];
        }

        private byte[] SpiReadFifo()
        {
            byte[] result = new byte[64];
            byte[] cmd = new byte[] { Reg_FIFO };

            spiDevice.Write(cmd);
            spiDevice.Read(result);

            return result;
        }

        public byte[] GetData()
        {
            byte[] data = null;
            byte result;

            if (pinNIRQ.Read() == GpioPinValue.Low)
            {
                if ((SpiReadRegister(Reg_IntStatus_1) & 0x02) == 0x02)
                {
                    data = SpiReadFifo();

                    // Reset the RX FIFO
                    SpiWriteRegister(Reg_OpControl_2, 0x02);
                    SpiWriteRegister(Reg_OpControl_2, 0x00);
                }

                result = SpiReadRegister(Reg_IntStatus_1);
                result = SpiReadRegister(Reg_IntStatus_2);

                // Put the receiver back in RX mode
                SpiWriteRegister(Reg_OpControl_1, 0x04);
            }

            return data;
        }
    }
}
