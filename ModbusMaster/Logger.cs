using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ModbusMaster
{
    /// <summary>
    /// Создает поток, который потокобезопасно 
    /// принимает сообщения и записвает их в один файл
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
                using (StreamWriter streamWriter = new StreamWriter(filename, false, Encoding.UTF8))
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
