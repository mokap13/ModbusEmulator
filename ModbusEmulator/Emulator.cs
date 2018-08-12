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
        /// <summary>
        /// Генератор случайных чисел для эмуляции тока
        /// </summary>
        private Random random;
        private List<SerialPort> serialPorts;
        public Emulator()
        {
            random = new Random();
        }
        public void Start(string[] comPortNumbers)
        {
            serialPorts = GetSerialPorts(comPortNumbers);
            serialPorts.ForEach(s => s.DataReceived += SlavePort_DataReceived);
            serialPorts.ForEach(s => s.Open());
        }
        public void Stop()
        {
            serialPorts.ForEach(s => s.DataReceived -= SlavePort_DataReceived);
            serialPorts.ForEach(s => s.Close());
        }
        /// <summary>
        /// Возвращает на основе числовых аргументов командной строки COM порты
        /// </summary>
        /// <param name="comPortNumbers">Номера COM портов</param>
        /// <returns>COM порты с настройками 9600,8,N,1</returns>
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
        /// <summary>
        /// Обработчик принятых данных, после анализа посылка в случае корректности запроса отвечает
        /// </summary>
        /// <param name="sender">SerialPort</param>
        /// <param name="e"></param>
        private void SlavePort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (sender as SerialPort);
            if (port.BytesToRead == 0)
                return;
            byte[] receivedBytes = new byte[port.BytesToRead];
            port.Read(receivedBytes, 0, port.BytesToRead);
            
            if (ModbusCrc.IsCorrectCrc(receivedBytes))
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
                    UInt16 randomData = (UInt16)random.Next(0, 1000);
                    byte[] responseBytes = new byte[] {
                        slaveAddress,
                        function,
                        0x02,
                        (byte)(randomData>>8), (byte)randomData};
                    responseBytes = ModbusCrc.AddCrc(responseBytes);
                    port.BaseStream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
        }
    }
}
