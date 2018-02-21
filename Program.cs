using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace simplesocket
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Usage: dotnet run hostname port");
                return;
            }

            string hostName = args[0];
            bool successfulPortParse = int.TryParse(args[1], out int port);

            if(!successfulPortParse)
            {
                Console.WriteLine("The port number should be an integer");
            }

            Console.WriteLine("Resolving DNS addresses ");
            IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
            if(ipAddresses == null || ipAddresses.Length == 0)
            {
                Console.WriteLine($"Dns resolution failed for host {hostName}");
                return;
            }

            Console.WriteLine("IP Addresses resolved ");
            foreach(IPAddress address in ipAddresses)
            {
                Console.WriteLine(address.ToString());
            }

            // Connect 
            foreach (IPAddress address in ipAddresses) {
                Console.WriteLine($"Connecting to {address} on port {port}");
                Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs asyncArgs = new SocketAsyncEventArgs();
                asyncArgs.Completed += OnCompleted;
                asyncArgs.RemoteEndPoint = new IPEndPoint(address, port);
                socket.ConnectAsync(asyncArgs);
            }
            //Wait for 5000 milliseconds for connection
            resetEvent.Wait(5000);
        }

        static ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

        private static void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket s = (Socket)sender;
            Console.WriteLine($"Received Last Operation {e.LastOperation} for {s.RemoteEndPoint}");
            resetEvent.Set();
        }
    }
}
