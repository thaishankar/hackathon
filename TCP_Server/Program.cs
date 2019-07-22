using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Server
{
    class Constants
    {
        public const string Localhost = "10.0.0.10";

        // Ports
        public const int HTTP_PORT = 80;
        public const int DEFAULT_SERVER_PORT = 50001;

        // Buffer sizes
        public const int CLIENT_READ_BUFFER = 1024;
        public const int CLIENT_WRITE_BUFFER = 1024;
        public const int SERVER_READ_BUFFER = 1024 * 1024; // 1 MB
        public const int SERVER_WRITE_BUFFER = 1024 * 1024; // 1 MB

    }
    
    class Listener
    {
        TcpListener _server = null;
        private int port;
        private IPAddress ip;

        public Listener(int port)
        {
            this.port = port;
            this.ip = IPAddress.Parse(Constants.Localhost);
            this._server = new TcpListener(this.ip, this.port);
        }

        public void Start()
        {
            _server.Start();
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Accept()
        {
            while (true)
            {
                TcpClient client = _server.AcceptTcpClient();

                if (client.Connected)
                {
                    Thread clientThread = new Thread(() => RequestRespond(client));
                    //Thread clientThread = new Thread(() => RequestForward(client));
                    clientThread.Start();
                }
            }
        }

        public static void RequestRespond(TcpClient client)
        {
            Console.WriteLine("Connected!");

            byte[] bytes = new byte[Constants.SERVER_READ_BUFFER];
            NetworkStream stream = client.GetStream();


            while (true)
            {
                int bytesRead = 0;
                do
                {
                    bytesRead = Read(stream, bytes);
                    if (bytesRead <= 0)
                    {
                        goto Finished;
                    }

                    byte[] msg = ProcessMsg(bytes, bytesRead);

                    int bytesWritten = Write(stream, msg);

                } while (bytesRead == Constants.SERVER_READ_BUFFER);
            }
            Finished:
            stream.Close();
            client.Close();
        }

        public static int Read(NetworkStream stream, Byte[] bytes)
        {
            int i = 0;

            // Loop to receive all the data sent by the client.
            try
            {
                i = stream.Read(bytes, 0, bytes.Length);
            }
            catch (SocketException)
            {
                return -1;
            }
            return i;
        }

        public static byte[] ProcessMsg(byte[] bytes, int bytesRead)
        {
            string data = string.Empty;

            data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytesRead);
            Console.WriteLine("Server Received len {0}: {1}", bytes.Length, data);
            data.ToUpper();

            byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
            return msg;
        }

        public static int Write(NetworkStream stream, byte[] msg)
        {
            string fileName = string.Format(@"File{0}mb_{1}.zip", 10, 1);
            // byte[] data = File.ReadAllBytes(fileName);
            byte[] b = new byte[256 * 1024];
            long totalBytesSent = 0;

            using (FileStream fs = File.OpenRead(fileName))
            {
                while (fs.Read(b, 0, b.Length) > 0)
                {
                    stream.Write(b, 0, b.Length);
                    totalBytesSent += b.Length;
                }
            }


            // Send back a response.
           //  stream.Write(data, 0, data.Length);
            Console.WriteLine("Server sent bytes: {0}", totalBytesSent);
            //Console.WriteLine("Server Sent: {0}", msg);
            return msg.Length;

        }

        static void Main(string[] args)
        {
            new Thread(Server).Start();

            Thread.Sleep(Timeout.Infinite);
        }
        static void Server()
        {
            Listener newListener = new Listener(Constants.DEFAULT_SERVER_PORT);
            newListener.Start();
            newListener.Accept();
            newListener.Stop();
        }

    }
}
