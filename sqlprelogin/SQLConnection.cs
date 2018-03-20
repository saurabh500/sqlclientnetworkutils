using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace sqlprelogin
{
    internal class SQLConnection
    {
        byte[] buffer = new byte[4096];
        int bufferPointer = 8; // Initialize at header len
        private string _hostname;
        private int _port;

        public SQLConnection(string hostname, int port)
        {
            this._hostname = hostname;
            this._port = port;
        }

        public long Connect()
        {
            Socket socket = Connect(this._hostname, this._port, TimeSpan.FromSeconds(10));
            if (socket == null || !socket.Connected)
            {
                if (socket != null)
                {
                    socket.Dispose();
                    socket = null;
                }
                throw new Exception($"Couldn't connect to {this._hostname} on port {this._port}");
            }
            NetworkStream stream = new NetworkStream(socket); 
            DoPreLoginSend(stream);
            DoPreLoginReceive(stream);
            return DoSslHandShake(stream);
            
        }

        private long DoSslHandShake(NetworkStream stream)
        {
            SslOverTdsStream sslOverTdsStream = new SslOverTdsStream(stream);
            SslStream sslStream = new SslStream(sslOverTdsStream, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            sslStream.AuthenticateAsClient(this._hostname);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Alway trust server certificate for this stripped version of the application
            return true;
        }

        private void DoPreLoginReceive(NetworkStream stream)
        {
            byte[] readBuffer = new byte[4096];
            int read = stream.Read(readBuffer, 0, readBuffer.Length);
            if( read == 0)
            {
                throw new Exception("No data received during Pre-Login receive");
            }
            

        }

        private static Socket Connect(string serverName, int port, TimeSpan timeout)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(serverName);
            IPAddress serverIPv4 = null;
            IPAddress serverIPv6 = null;
            foreach (IPAddress ipAdress in ipAddresses)
            {
                if (ipAdress.AddressFamily == AddressFamily.InterNetwork)
                {
                    serverIPv4 = ipAdress;
                }
                else if (ipAdress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    serverIPv6 = ipAdress;
                }
            }
            ipAddresses = new IPAddress[] { serverIPv4, serverIPv6 };
            Socket[] sockets = new Socket[2];

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            void Cancel()
            {
                for (int i = 0; i < sockets.Length; ++i)
                {
                    try
                    {
                        if (sockets[i] != null && !sockets[i].Connected)
                        {
                            sockets[i].Dispose();
                            sockets[i] = null;
                        }
                    }
                    catch { }
                }
            }
            cts.Token.Register(Cancel);

            Socket availableSocket = null;
            for (int i = 0; i < sockets.Length; ++i)
            {
                try
                {
                    if (ipAddresses[i] != null)
                    {
                        sockets[i] = new Socket(ipAddresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        sockets[i].Connect(ipAddresses[i], port);
                        if (sockets[i] != null) // sockets[i] can be null if cancel callback is executed during connect()
                        {
                            if (sockets[i].Connected)
                            {
                                availableSocket = sockets[i];
                                break;
                            }
                            else
                            {
                                sockets[i].Dispose();
                                sockets[i] = null;
                            }
                        }
                    }
                }
                catch { }
            }

            return availableSocket;
        }

        private readonly int MAX_PRELOGIN_PAYLOAD_LENGTH = 1024;
        private const int GUID_SIZE = 16;

        private void DoPreLoginSend(NetworkStream stream)
        {
            byte[] buffer = new byte[4096];
            int offset = (int)PreLoginOptions.NUMOPT * 5 + 1;

            byte[] payload = new byte[(int)PreLoginOptions.NUMOPT * 5 + MAX_PRELOGIN_PAYLOAD_LENGTH];
            int payloadLength = 0;
            for (int option = (int)PreLoginOptions.VERSION; option < (int)PreLoginOptions.NUMOPT; option++)
            {
                int optionDataSize = 0;

                // Fill in the option
                WriteByte((byte)option);

                // Fill in the offset of the option data
                WriteByte((byte)((offset & 0xff00) >> 8)); // send upper order byte
                WriteByte((byte)(offset & 0x00ff)); // send lower order byte

                switch (option)
                {
                    case (int)PreLoginOptions.VERSION:
                        Version systemDataVersion = Version.Parse("4.6.26315.0");

                        // Major and minor
                        payload[payloadLength++] = (byte)(systemDataVersion.Major & 0xff);
                        payload[payloadLength++] = (byte)(systemDataVersion.Minor & 0xff);

                        // Build (Big Endian)
                        payload[payloadLength++] = (byte)((systemDataVersion.Build & 0xff00) >> 8);
                        payload[payloadLength++] = (byte)(systemDataVersion.Build & 0xff);

                        // Sub-build (Little Endian)
                        payload[payloadLength++] = (byte)(systemDataVersion.Revision & 0xff);
                        payload[payloadLength++] = (byte)((systemDataVersion.Revision & 0xff00) >> 8);
                        offset += 6;
                        optionDataSize = 6;
                        break;

                    case (int)PreLoginOptions.ENCRYPT:
                        
                        payload[payloadLength] = (byte)0;
                        
                        payloadLength += 1;
                        offset += 1;
                        optionDataSize = 1;
                        break;

                    case (int)PreLoginOptions.INSTANCE:
                        int i = 0;
                        byte[] instanceName = new byte[256];
                        while (instanceName[i] != 0)
                        {
                            payload[payloadLength] = instanceName[i];
                            payloadLength++;
                            i++;
                        }

                        payload[payloadLength] = 0; // null terminate
                        payloadLength++;
                        i++;

                        offset += i;
                        optionDataSize = i;
                        break;

                    case (int)PreLoginOptions.THREADID:
                        Int32 threadID = 256;

                        payload[payloadLength++] = (byte)((0xff000000 & threadID) >> 24);
                        payload[payloadLength++] = (byte)((0x00ff0000 & threadID) >> 16);
                        payload[payloadLength++] = (byte)((0x0000ff00 & threadID) >> 8);
                        payload[payloadLength++] = (byte)(0x000000ff & threadID);
                        offset += 4;
                        optionDataSize = 4;
                        break;

                    case (int)PreLoginOptions.MARS:
                        payload[payloadLength++] = 0;
                        offset += 1;
                        optionDataSize += 1;
                        break;

                    case (int)PreLoginOptions.TRACEID:
                        byte[] connectionIdBytes = Guid.NewGuid().ToByteArray();
                        Buffer.BlockCopy(connectionIdBytes, 0, payload, payloadLength, GUID_SIZE);
                        payloadLength += GUID_SIZE;
                        offset += GUID_SIZE;
                        optionDataSize = GUID_SIZE;

                        connectionIdBytes = Guid.NewGuid().ToByteArray();
                        Buffer.BlockCopy(connectionIdBytes, 0, payload, payloadLength, GUID_SIZE);
                        payloadLength += GUID_SIZE;
                        payload[payloadLength++] = (byte)(0x000000ff & 1);
                        payload[payloadLength++] = (byte)((0x0000ff00 & 1) >> 8);
                        payload[payloadLength++] = (byte)((0x00ff0000 & 1) >> 16);
                        payload[payloadLength++] = (byte)((0xff000000 & 1) >> 24);
                        int actIdSize = GUID_SIZE + sizeof(UInt32);
                        offset += actIdSize;
                        optionDataSize += actIdSize;
                        break;

                    default:
                        break;
                }

                // Write data length
                WriteByte((byte)((optionDataSize & 0xff00) >> 8));
                WriteByte((byte)(optionDataSize & 0x00ff));
            }

            // Write out last option - to let server know the second part of packet completed
            WriteByte((byte)PreLoginOptions.LASTOPT);

            // Write out payload
            WriteByteArray(payload, payloadLength, 0);

            // Flush packet
            WritePacket(18, 0x1, stream);

        }

        private void WritePacket(byte messageType, byte status, NetworkStream stream)
        {
            byte outputPacketNumber = 1;
            buffer[0] = messageType;
            buffer[1] = status;
            buffer[2] = (byte)(bufferPointer >> 8);
            buffer[3] = (byte)(bufferPointer & 0xff);
            buffer[4] = 0;                          // channel
            buffer[5] = 0;
            buffer[6] = outputPacketNumber;          // packet
            buffer[7] = 0;
            stream.Write(buffer, 0, bufferPointer);
        }

        private void WriteByteArray(byte[] b, int len, int offset)
        {
            Buffer.BlockCopy(b, offset, buffer, bufferPointer, len);
            bufferPointer += len;
        }

        private void WriteByte(byte option)
        {
            buffer[bufferPointer++] = option;
        }
    }

    internal enum PreLoginOptions
    {
        VERSION,
        ENCRYPT,
        INSTANCE,
        THREADID,
        MARS,
        TRACEID,
        NUMOPT,
        LASTOPT = 255
    }
}
