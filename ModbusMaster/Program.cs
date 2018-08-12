using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using ModbusEmulator;

namespace ModbusMaster
{
    public class Program
    {
        static void Main(string[] args)
        {
            Emulator emulator = new Emulator();
            List<SerialPort> serialPorts = emulator.GetSerialPorts(args);
            serialPorts.ForEach(s => s.Open());
            /*Генерирую диапазон опрашиваемых адресов устройств*/
            List<byte> deviceAddressRange = Enumerable.Range(1, 5)
                    .Select(i => (byte)i)
                    .ToList();
            /*Ассинхронно запускаю опросы для каждого порта*/
            serialPorts.ForEach(port => StartModbusPollAsync(port, deviceAddressRange));

            Console.ReadKey();
            serialPorts.ForEach(s => s.Close());
        }
        /// <summary>
        /// Запускает цикл асинхронного опроса одного из портов
        /// </summary>
        /// <param name="serialPort">Опрашиваемый порт</param>
        /// <param name="deviceAddressRange">Адресы опрошиваемых устройст</param>
        private static async void StartModbusPollAsync(SerialPort serialPort, List<byte> deviceAddressRange)
        {
            await Task.Factory.StartNew(() =>
            {
                /*Основной цикл опроса одного порта*/
                while (true)
                {
                    /*Ассинхронно запускаю алгоритм опроса, и сразуже начинаю отсчитыать период опроса*/
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            deviceAddressRange.ForEach(address =>
                            {
                                #region Формирую и отправляю запрос
                                byte[] sendingFrame = new byte[] { 0x00, 0x04, 0x00, 0x20, 0x00, 0x01 };
                                sendingFrame[0] = address;
                                sendingFrame = ModbusCrc.AddCrc(sendingFrame);

                                serialPort.Write(sendingFrame, 0, sendingFrame.Length);

                                string message = $"{DateTime.Now.ToString("HH:mm:ss.fff")}:{serialPort.PortName}: TX: ";
                                message += GetStringAsHexView(sendingFrame);
                                Logger.Write(message);
                                Console.WriteLine(message);
                                #endregion
                                #region Обрабатываю полученный ответ
                                byte[] receivedBytes = new byte[256];
                                Task<int> task = serialPort.BaseStream.ReadAsync(receivedBytes, 0, 256);
                                if(task.Wait(100) == false)
                                {
                                    Logger.Write("timeout");
                                    Console.WriteLine("timeout");
                                }
                                int frameLength = task.Result;
                                receivedBytes = receivedBytes.Take(frameLength).ToArray();

                                message = $"{DateTime.Now.ToString("HH:mm:ss.fff")}:{serialPort.PortName}: RX: ";
                                message += GetStringAsHexView(receivedBytes);
                                Logger.Write(message);
                                Console.WriteLine(message);
                                #endregion
                            });
                        });
                    });
                    /*Период опроса*/
                    Task.Delay(1000).Wait();
                }
            }, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// Возвращает строковое представление в формате Hex, bytes -> "0A BD 14 FF"
        /// </summary>
        /// <param name="sendingFrame">Исходный массив байт</param>
        /// <returns></returns>
        private static string GetStringAsHexView(byte[] sendingFrame)
        {
            return BitConverter.ToString(sendingFrame).Replace("-", " ");
        }
    }
}
