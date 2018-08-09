using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusEmulator
{
    public class Program
    {
        
        static void Main(string[] args)
        {
            Emulator emulator = new Emulator();
            try
            {
                emulator.Start(args);
                Console.WriteLine("Modbus эмулятор запущен...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
    }
}
