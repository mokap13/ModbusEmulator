using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator
{
    public class Emulator
    {
        private Random random;
        private List<SerialPort> serialPorts;
        public Emulator()
        {
            random = new Random();
        }
        public void Start(string[] comPortNumbers)
        {
            serialPorts = GetSerialPorts(comPortNumbers);
            serialPorts.ForEach(s => SerialPortHandler(s));
        }
        public void Stop()
        {
            serialPorts.ForEach(s => s.Close());
        }
        
        public List<SerialPort> GetSerialPorts(string[] comPortNumbers)
        {
            return comPortNumbers
                .Select(s => new SerialPort()
                {
                    PortName = $"COM{s}",
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                })
                .ToList();
        }
        private void SerialPortHandler(SerialPort serialPort)
        {
            serialPort.Open();
            serialPort.DataReceived += SlavePort_DataReceived;

            //await new Task(() => { while (true) ; });
            
        }

        private void SlavePort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (sender as SerialPort);
            byte[] receivedBytes = new byte[port.BytesToRead];
            port.Read(receivedBytes, 0, port.BytesToRead);

            UInt16 calculatedCrc = ModbusCrc
                .CalculateCrc(receivedBytes.Take(receivedBytes.Length - 2)
                .ToArray());
            UInt16 receivedCrc = (UInt16)(receivedBytes.ElementAt(receivedBytes.Length - 1) << 8
                | receivedBytes.ElementAt(receivedBytes.Length - 2));
            if (calculatedCrc == receivedCrc)
            {
                byte slaveAddress = receivedBytes[0];
                byte function = receivedBytes[1];
                UInt16 registerAddress = (UInt16)(receivedBytes[2] << 8 | receivedBytes[3]);
                UInt16 registerCount = (UInt16)(receivedBytes[4] << 8 | receivedBytes[5]);

                if ((slaveAddress >= 1 && slaveAddress <= 247)
                    && (function == 0x04)
                    && (registerAddress == 0x20)
                    && (registerCount == 0x01))
                {

                    byte[] sendBytes = new byte[] {
                        slaveAddress,
                        function,
                        0x02,
                        (byte)random.Next(0, 0xFF), (byte)random.Next(0, 0xFF),
                        0x00, 0x00 };
                    UInt16 crc = ModbusCrc.CalculateCrc(sendBytes.Take(sendBytes.Length - 2).ToArray());
                    sendBytes[sendBytes.Length - 1] = (byte)(crc >> 8);
                    sendBytes[sendBytes.Length - 2] = (byte)crc;
                    port.BaseStream.Write(sendBytes, 0, sendBytes.Length);
                }
            }
        }

    }
}
