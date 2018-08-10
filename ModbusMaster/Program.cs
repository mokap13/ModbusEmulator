using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Device;
using Modbus.Serial;
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

            List<IModbusSerialMaster> masters = serialPorts
                .Select(s => (IModbusSerialMaster)ModbusSerialMaster.CreateRtu(new SerialPortAdapter(s)))
                .ToList();

            masters.ForEach(m => ReadRegistersAsync(m));

            Console.ReadKey();
            serialPorts.ForEach(s => s.Close());
        }
        async static Task ReadRegistersAsync(IModbusSerialMaster master)
        {
            await Task.Run(() =>
            {
                List<byte> addresses = Enumerable.Range(1, 247)
                        .Select(i => (byte)i)
                        .ToList();
                while (true)
                {
                    var values = addresses
                        .Select(a => master.ReadInputRegisters(a, 0x20, 0x01).First())
                        .ToArray();
                    values.ToList().ForEach(v => Console.Write($"{v.ToString()} "));
                    Console.WriteLine();
                    Task.Delay(1000).Wait();
                }
            }); ;
        }
    }
}
