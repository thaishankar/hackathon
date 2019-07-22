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
        public const string Localhost = "127.0.0.1";
        public const string ServerIp = "10.0.176.9";

        // Ports
        public const int HTTP_PORT = 80;
        public const int DEFAULT_SERVER_PORT = 50001;

        // Buffer 
        public const int SERVER_READ_BUFFER = 1024 * 1024; // 1 MB
        public const int SERVER_WRITE_BUFFER = 1024 * 1024; // 1 MB

    }
    
    class Listener
    {
        TcpListener _server = null;
        private int port;
        private IPAddress ip;

        public Listener(string serverIp = Constants.Localhost, int port = Constants.DEFAULT_SERVER_PORT)
        {
            this.port = port;
            this.ip = IPAddress.Parse(serverIp);
            this._server = new TcpListener(ip, this.port);
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
                    //Thread clientThread = new Thread(() => RequestRespond(client));
                    ////Thread clientThread = new Thread(() => RequestForward(client));
                    //clientThread.Start();

                    RequestRespond(client);
                }
            }
        }

        public static void RequestRespond(TcpClient client)
        {
            Console.WriteLine("Connected!");

            byte[] fileName = new byte[Constants.SERVER_READ_BUFFER];
            NetworkStream stream = client.GetStream();

            int fileLength;

            fileLength = stream.Read(fileName, 0, fileName.Length);

            if (fileLength <= 0)
            {
                return;
            }

            string requestFile = System.Text.Encoding.ASCII.GetString(fileName, 0, fileLength);
            Console.WriteLine("Server received request for: {0} ClientIp: {1}", requestFile);

            WriteFile(stream, requestFile);
        }

        public static void WriteFile(NetworkStream stream, string requestFile)
        {
            byte[] fileData = File.ReadAllBytes(requestFile);

            byte[] lengthMsg = BitConverter.GetBytes(fileData.Length);
            Console.WriteLine("Lenght of file is : {0}", fileData.Length);

            // Send length of File first
            stream.Write(lengthMsg, 0, lengthMsg.Length);

            // Send File data
            
            stream.Write(fileData, 0, fileData.Length);
        }

        static void Main(string[] args)
        {
            //new Thread(Server).Start();

            //Thread.Sleep(Timeout.Infinite);
            Server();
        }
        static void Server()
        {
            Listener newListener = new Listener(Constants.ServerIp, Constants.DEFAULT_SERVER_PORT);
            newListener.Start();
            newListener.Accept();
            newListener.Stop();
        }

    }
}
