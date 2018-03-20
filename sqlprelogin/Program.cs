using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace sqlprelogin
{
    class Program
    {
        static void Main(string[] args)
        {
            for(int i = 0; i < 10; i++) { 
                SQLConnection connection = new SQLConnection("ss-desktop2", 1433);
                connection.Connect();
            }
        }

    }

}
