using ModbusEmulator;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace ModbusMaster
{
    public class Program
    {
        static void Main(string[] args)
        {
            /*Первый аргумент - количество опрашиваемых устройств на каждом порту*/
            int deviceAddressCount = int.Parse(args[0]);
            
            List<SerialPort> serialPorts = Emulator.GetSerialPorts(args.Skip(1).ToArray());
            serialPorts.ForEach(s => s.Open());
            /*Генерирую диапазон опрашиваемых адресов устройств*/
            List<byte> deviceAddressRange = Enumerable.Range(1, deviceAddressCount)
                    .Select(i => (byte)i)
                    .ToList();

            Console.WriteLine($"ModbusMaster запущен... Опрос портов: " +
                $"{new string(serialPorts.SelectMany(s=> s.PortName + " ").ToArray())}");
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
            const byte function = 0x04;
            const byte registerAddressHigh = 0x00;
            const byte registerAddressLow = 0x20;
            const byte registerCountHigh = 0x00;
            const byte registerCountLow = 0x01;
            await Task.Factory.StartNew(() =>
            {
                /*Основной цикл опроса одного порта*/
                while (true)
                {
                    /*Ассинхронно запускаю алгоритм опроса, и сразу же начинаю засекать период*/
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            deviceAddressRange.ForEach(address =>
                            {
                                #region Формирую и отправляю запрос
                                byte[] buffer = new byte[256];
                                buffer[0] = address;
                                buffer[1] = function;
                                buffer[2] = registerAddressHigh;
                                buffer[3] = registerAddressLow;
                                buffer[4] = registerCountHigh;
                                buffer[5] = registerCountLow;

                                buffer.AddModbusCrc(count:6);

                                string logMessage = $"{DateTime.Now.ToString("HH:mm:ss.fff")}:{serialPort.PortName}: TX: ";
                                logMessage += buffer.ToStringHex(count: 8) + "\r\n";

                                serialPort.BaseStream.Write(buffer, 0, count:8);
                                #endregion
                                #region Обрабатываю полученный ответ
                                Task<int> taskReader = serialPort.BaseStream.ReadAsync(buffer, 0, 256);
                                if (taskReader.Wait(250))
                                    buffer = buffer.Take(count: taskReader.Result).ToArray();
                                else
                                    Logger.Write($"\t\t{serialPort.PortName}:RX: TIMEOUT");

                                #endregion
                                #region Записываю историю
                                logMessage += $"{DateTime.Now.ToString("HH:mm:ss.fff")}:{serialPort.PortName}: RX: ";
                                logMessage += buffer.ToStringHex(count: taskReader.Result);
                                Logger.Write(logMessage);
                                #endregion
                            });
                        });
                    });
                    /*Период опроса*/
                    Task.Delay(1000).Wait();
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
