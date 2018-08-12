using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusMaster
{
    /// <summary>
    /// Создает поток, который принимает
    /// </summary>
    static class Logger
    {
        private static BlockingCollection<string> blockingCollection;
        private static string filename = "log.txt";

        static Logger()
        {
            blockingCollection = new BlockingCollection<string>();

            Task.Factory.StartNew(() =>
            {
                using (StreamWriter streamWriter = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    streamWriter.AutoFlush = true;

                    foreach (var s in blockingCollection.GetConsumingEnumerable())
                        streamWriter.WriteLine(s);
                }
            },
            TaskCreationOptions.LongRunning);
        }

        public static void Write(string message)
        {
            blockingCollection.Add(message);
        }
    }
}
