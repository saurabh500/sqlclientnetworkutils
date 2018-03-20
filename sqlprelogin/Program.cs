using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace sqlprelogin
{
    class Program
    {
        private static string hostName = null;
        private static int port = 1433;

        private static int threadCount = 10;

        static void Main(string[] args)
        {
            if(!parseArgs(args))
            {
                return;
            }
            WriteLine($"Executing for {hostName} {port} {threadCount}");

            int runCount = threadCount;
            long[] timetaken = new long[runCount];
            Task[] tasks = new Task[runCount];

            for (int i = 0; i < runCount; i++)
            {
                Task t = Task.Factory.StartNew((ctr) =>
                {
                    SQLConnection connection = new SQLConnection(hostName, port);

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

            WriteLine($"{sum / runCount} milliseconds per execution for SSL Handshake for {threadCount} threads ");
        }

        private static bool parseArgs(string[] args)
        {
            if(args == null || args.Length < 3)
            {
                WriteLine("Usage dotnet run --framework <F/W> <hostname> <port> <threadCount>");
                return false;
            }

            hostName = args[0];
            if(!int.TryParse(args[1], out port))
            {
                WriteLine("Usage dotnet run --framework <F/W> <hostname> <port> <threadCount>");
                return false;
            }
            if (!int.TryParse(args[2], out threadCount))
            {
                WriteLine("Usage dotnet run --framework <F/W> <hostname> <port> <threadCount>");
                return false;
            }

            return true;

        }
    }

}
