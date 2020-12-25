using System;
using System.Threading;
using System.Diagnostics;
using Windows.Devices.SerialCommunication;
using nanoFramework.Hardware.Esp32;
using Windows.Storage.Streams;
using System.Collections;
namespace Nuedge.NF.Device
{
    public class PN532
    {
        SerialDevice SerialPort;
        DataWriter dataWriter { get; set; }
        DataReader dataReader { get; set; }
        public const uint maxReadLength = 512;
        public bool IsInitialize { get; set; }
        public bool IsTagReading { get; set; }
        public bool IsTagDetected { get; set; }
        public bool IsWakeUp { get; set; }
        byte[] InListPassive = { 0x00, 0x00, 0xFF, 0x04, 0xFC, 0xD4, 0x4A, 0x01, 0x00, 0xE1, 0x00 };

        public bool Initialize(string Port)
        {
            try
            {
                IsInitialize = false;
                //string aqs = SerialDevice.GetDeviceSelector("UART0");                   /* Find the selector string for the serial device   */
                //var dis = await DeviceInformation.FindAllAsync(aqs);                    /* Find the serial device with our selector string  */
                //SerialPort = await SerialDevice.FromIdAsync(dis[0].Id);    /* Create an serial device with our selected device */

                var serialPorts = SerialDevice.GetDeviceSelector();


                Debug.WriteLine("available serial ports: " + serialPorts);

                //Configuration
                Configuration.SetPinFunction(Gpio.IO04, DeviceFunction.COM2_TX);
                Configuration.SetPinFunction(Gpio.IO05, DeviceFunction.COM2_RX);

                //Initialization
                SerialPort = SerialDevice.FromId("COM2");
                SerialPort.BaudRate = 115200;
                SerialPort.Parity = SerialParity.None;
                SerialPort.StopBits = SerialStopBitCount.One;
                SerialPort.Handshake = SerialHandshake.None;
                SerialPort.DataBits = 8;


              
                IsInitialize = true;
            }
            catch (Exception ex)
            {
                IsInitialize = false;
            }
            return IsInitialize;

        }
        public  bool WakeUp()
        {
            try
            {
                IsWakeUp = false;
                byte[] GetFirmware = { 0x55, 0x55, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x02, 0xFE, 0xD4, 0x02, 0x2A, 0x00 };
                byte[] SAMConfig = { 0x55, 0x55, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03, 0xFD, 0xD4, 0x14, 0x01, 0x17, 0x00 };
                if (IsInitialize)
                {
                    SerialPort.WriteTimeout = new TimeSpan(0, 0, 1);
                    DataWriter outputDataWriter = new DataWriter(SerialPort.OutputStream);
                    outputDataWriter.WriteBytes(GetFirmware);
                    uint bw1 = outputDataWriter.Store();
                    Debug.WriteLine("Sent " + bw1 + " bytes over " + SerialPort.PortName + ".");

                    //const uint maxReadLength = 512;
                    SerialPort.ReadTimeout = new TimeSpan(0, 0, 1);
                    DataReader inputDataReader = new DataReader(SerialPort.InputStream);
                    inputDataReader.InputStreamOptions = InputStreamOptions.ReadAhead;
                    uint FirmwareReadbytes = inputDataReader.Load(maxReadLength);

                    ArrayList Firmware;
                    Firmware = new ArrayList();
                    while (FirmwareReadbytes-- > 0)
                    {

                        Firmware.Add(inputDataReader.ReadByte());
                    }

                    outputDataWriter.WriteBytes(SAMConfig);
                    uint bw2 = outputDataWriter.Store();
                    Debug.WriteLine("Sent " + bw2 + " bytes over " + SerialPort.PortName + ".");
                    uint SAMconfigReadbytes = inputDataReader.Load(maxReadLength);

                    //SAMconfig Read
                    ArrayList SAM_config;
                    SAM_config = new ArrayList();
                    while (SAMconfigReadbytes-- > 0)
                    {

                        SAM_config.Add(inputDataReader.ReadByte());
                    }




                   
                    IsWakeUp = true;
                }
            }
            catch (Exception ex)
            {
                IsWakeUp = false;
            }
            return IsWakeUp;
        }

        public delegate void CallBack(string card);

        public CallBack CardDetected;
        public void Register(CallBack _CardDetected)
        {
            CardDetected = _CardDetected;
        }
        public  void Start()
        {
            while (true)
            {
                if (this.IsTagReading == false)
                {
                    //Console.Write("TagReading..");

                    var Tag =  this.ReadTag();
                    if (this.IsTagDetected)
                    {
                        this.CardDetected(Tag);
                    }
                }
            }
        }

        public delegate void MyDelegate(string text);
        private  string ReadTag()
        {
            string nfcstring = "";

            try
            {
                if (IsInitialize && IsWakeUp)
                {
                    IsTagDetected = false;
                    IsTagReading = true;
                    SerialPort.WriteTimeout = new TimeSpan(0, 0, 1);

                    DataWriter outputDataWriter = new DataWriter(SerialPort.OutputStream);

                    //Write InListPassive
                    outputDataWriter.WriteBytes(InListPassive);
                    uint bw3 = outputDataWriter.Store();
                    //Thread.Sleep(100);
                    Debug.WriteLine("Sent " + bw3 + " bytes over " + SerialPort.PortName + ".");
                    SerialPort.ReadTimeout = new TimeSpan(0, 0, 1);

                    DataReader inputDataReader = new DataReader(SerialPort.InputStream);
                    //Thread.Sleep(100);
                    uint TagBytes = inputDataReader.Load(maxReadLength);
                   

                    //SAMconfig Read

                    ArrayList TagID;
                    TagID = new ArrayList();
                    while (TagBytes-- > 0)
                    {

                        TagID.Add(inputDataReader.ReadByte());
                    }
                   //String nfcString;
                    if (TagID.Count > 6)
                    {
                        //nfcString = TagID[19].ToString() + " " + TagID[20].ToString() + "" + TagID[21].ToString() + "" + TagID[22].ToString();
                        nfcstring = TagID[19] + " " + TagID[20] + " " + TagID[21] + " " + TagID[22];
                        Debug.WriteLine("TagID: >>" + nfcstring + "<< ");
                        if (nfcstring == "0 0 255 0")
                            IsTagDetected = false;
                        else

                        {
                            IsTagDetected = true;

                        }
                    }
                    outputDataWriter.Dispose();
                    inputDataReader.Dispose();
                    Thread.Sleep(100);

                }
                IsTagReading = false;
                return nfcstring;
            }
                       

            catch (Exception ex)
            {
                IsTagReading = false;
                return nfcstring;
            }


         
        }

    }

}
