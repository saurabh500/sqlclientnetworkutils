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
            Parallel.For(0, 100, (i) =>
            {
                //for (int i = 0; i < 10; i++)
                {
                    SQLConnection connection = new SQLConnection("ss-desktop2", 1433);
                    connection.Connect();
                }
            });
        }

    }

}
