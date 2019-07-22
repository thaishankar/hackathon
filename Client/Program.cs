using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Client
    {
        private int serverPort;
        private string serverIp;
        TcpClient _client;
        NetworkStream _stream;
        Stopwatch _watch;

        class Constants
        {
            public const string Localhost = "127.0.0.1";
            public const string ServerIp = "10.0.176.9";

            // Ports
            public const int HTTP_PORT = 80;
            public const int DEFAULT_SERVER_PORT = 50001;

            // Buffer sizes
            public const int CLIENT_READ_BUFFER = 16 * 1024 * 1024;
            public const int CLIENT_WRITE_BUFFER = 1024;
        }

        public Client(string serverIp = Constants.Localhost, int serverPort = Constants.DEFAULT_SERVER_PORT)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            _watch = Stopwatch.StartNew();
            _client = new TcpClient(this.serverIp, this.serverPort);
            _stream = _client.GetStream();
        }

        public void Send(String message)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

            _stream.Write(data, 0, data.Length);

            Console.WriteLine("Client Sent: {0}", message);
        }

        public void Receive()
        {
            //Thread.Sleep(1000);
            int bytesRead = 0;
            Random rand = new Random();

            // Request for a file
            string requestFile = string.Format(@"File{0}mb_{1}.zip", 10, rand.Next(10));
            Console.WriteLine("Client Requeting For File: {0}", requestFile);

            byte[] requestFileBytes = Encoding.ASCII.GetBytes(requestFile);
            _stream.Write(requestFileBytes, 0, requestFileBytes.Length);

            // Get length from server
            byte[] lengthMsg = new byte[100];
            bytesRead = _stream.Read(lengthMsg, 0, lengthMsg.Length);

            if(bytesRead == 0)
            {
                throw new Exception("Length not received from Server");
            }

            int fileLength = BitConverter.ToInt32(lengthMsg, 0);
            Console.WriteLine("File Length received is : {0}", fileLength);

            if(fileLength <= 0)
            {
                Console.WriteLine("File Not Found");
                return;
            }

            // Get data from Server
            byte[] data = new byte[fileLength];

            int currentChunk;
            bytesRead = 0;
            while ((currentChunk = _stream.Read(data, bytesRead, fileLength - bytesRead)) != 0)
            { 
                bytesRead += currentChunk;
                // Console.WriteLine("Read : {0} bytes", bytesRead);

                if(bytesRead >= fileLength)
                {
                    break;
                }
            }

            // Download Time
            long downloadTime = _watch.ElapsedMilliseconds;

            // Write to File
            string outFileName = string.Format("Out_{0}", requestFile);
            if (File.Exists(outFileName))
            {
                File.Delete(outFileName);
            }

            File.WriteAllBytes(outFileName, data);

            long copyTime = _watch.ElapsedMilliseconds - downloadTime;
            
            // Log download stats
            Console.WriteLine("Client Received: {0} in {1} ms. Written in : {2} ms", bytesRead, downloadTime, copyTime);
            Console.WriteLine("Total time: {0}", downloadTime + copyTime);
        }

        public void Close()
        {
            _stream.Close();
            _client.Close();
        }

        static void StartClient()
        {
            for (int i = 0; i < 1; i++)
            {
                Client newClient = new Client(Constants.ServerIp, Constants.DEFAULT_SERVER_PORT);
                newClient.Receive();
                newClient.Close();
            }

        }

        static void Main(string[] args)
        {
            while (true)
            {
                //new Thread(StartClient).Start();
                StartClient();
                Thread.Sleep(10000);
            }
        }
    }
}
