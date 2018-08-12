using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

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
        /// <summary>
        /// Открывает COM порты и инициализирует обработчики для портов
        /// </summary>
        /// <param name="comPortNumbers">Номера COM портов - "1" "2" "5"</param>
        public void Start(string[] comPortNumbers)
        {
            serialPorts = GetSerialPorts(comPortNumbers);
            serialPorts.ForEach(s => s.DataReceived += SlavePort_DataReceived);
            serialPorts.ForEach(s => s.Open());
            serialPorts.ForEach(s => s.DiscardInBuffer());
            serialPorts.ForEach(s => s.DiscardOutBuffer());
        }
        /// <summary>
        /// Отписывает обработчики от портов, и закрывает порты
        /// </summary>
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
        public static List<SerialPort> GetSerialPorts(string[] comPortNumbers)
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
        /// Обработчик принятых данных, после анализа посылка, 
        /// в случае корректности запроса отвечает,но только по функции 0x04, адресу 0x20
        /// по всем адресам приборов 1-247
        /// </summary>
        /// <param name="sender">SerialPort</param>
        /// <param name="e"></param>
        private void SlavePort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (sender as SerialPort);
            if (serialPort.BytesToRead == 0)
                return;
            byte[] buffer = new byte[256];
            int count = serialPort.Read(buffer, 0, serialPort.BytesToRead);

            if (count > 0 && buffer.IsCorrectCrc(count))
            {
                byte slaveAddress = buffer[0];
                byte function = buffer[1];
                UInt16 registerAddress = (UInt16)(buffer[2] << 8 | buffer[3]);
                UInt16 registerCount = (UInt16)(buffer[4] << 8 | buffer[5]);

                if ((slaveAddress >= 1 && slaveAddress <= 247)
                    && (function == 0x04)
                    && (registerAddress == 0x20)
                    && (registerCount == 0x01))
                {
                    UInt16 randomData = (UInt16)random.Next(0, 1000);
                    buffer[0] = slaveAddress;
                    buffer[1] = function;
                    buffer[2] = 0x02;
                    buffer[3] = (byte)(randomData >> 8);
                    buffer[4] = (byte)randomData;
                    buffer.AddModbusCrc(count:5);
                    serialPort.BaseStream.Write(buffer, 0, count:7);
                }
            }
        }
    }
}
