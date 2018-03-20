using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace sqlprelogin
{
    class Program
    {
        static void Main(string[] args)
        {
            int runCount = 100;
            long[] timetaken = new long[runCount];
            Task[] tasks = new Task[runCount];

            for (int i = 0; i < runCount; i++)
            {
                Task t = Task.Factory.StartNew((ctr) =>
                {
                    SQLConnection connection = new SQLConnection("ss-desktop2", 1433);
                    long sslHandShakeTime = connection.Connect();
                    timetaken[(int)ctr] = sslHandShakeTime;
                }, i);
                tasks[i] = t;
            }

            Task.WaitAll(tasks);

            long sum = 0;
            for (int i = 0; i < runCount; i++)
            {
                sum += timetaken[i];
            }
            Console.WriteLine(sum / runCount);
        }

    }

}
