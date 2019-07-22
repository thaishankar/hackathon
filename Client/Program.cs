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
            public const string ServerIp = "10.0.0.10";

            // Ports
            public const int HTTP_PORT = 80;
            public const int DEFAULT_SERVER_PORT = 50001;

            // Buffer sizes
            public const int CLIENT_READ_BUFFER = 16 * 1024 * 1024;
            public const int CLIENT_WRITE_BUFFER = 1024;
            public const int SERVER_READ_BUFFER = 1024 * 1024; // 1 MB
            public const int SERVER_WRITE_BUFFER = 1024 * 1024; // 1 MB

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

        public static byte[] ReadFully(NetworkStream stream, int initialLength)
        {
            // If we've been passed an unhelpful initial length, just
            // use 32K.
            if (initialLength < 1)
            {
                initialLength = 32768;
            }

            Console.WriteLine("Inside readfull");

            byte[] buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1)
                    {
                        Console.WriteLine("REached end of readfully");
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);

            Console.WriteLine("Done readfull");
            return ret;
        }

        public void ReceiveFully()
        {
            try
            {
                Console.WriteLine("calling ReadFully");
                byte[] data = ReadFully(_stream, Constants.CLIENT_READ_BUFFER);

                long downloadTime = _watch.ElapsedMilliseconds;

                Console.WriteLine(" done ReadFully");

                File.WriteAllBytes("outFile.zip", data);

                Console.WriteLine("Writing data");

                long copyTime = _watch.ElapsedMilliseconds - downloadTime;

                //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Client Received: {0} in {1} ms. Written in : {2} ms", data.Length, downloadTime, copyTime);
                Console.WriteLine("Total time: {0}", downloadTime + copyTime);
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
        }

        public string Receive()
        {
            byte[] data = new byte[Constants.CLIENT_READ_BUFFER];

            String responseData = String.Empty;

            //int bytesRead = _stream.Read(data, 0, data.Length);
            int bytesRead = 0;

            if (_stream.CanRead)
            {
                byte[] readBuffer = new byte[256 * 1024];
                int numberOfBytesRead = 0;

                // Incoming message may be larger than the buffer size.
                do
                {
                    numberOfBytesRead = _stream.Read(readBuffer, 0, readBuffer.Length);

                    Array.Copy(readBuffer, 0, data, bytesRead, numberOfBytesRead);
                    bytesRead += numberOfBytesRead;

                }
                while (_stream.DataAvailable);
            }

            long downloadTime = _watch.ElapsedMilliseconds;

            // File.WriteAllBytes("outFile.zip", data);
            string filePath = "outFile.zip";
            using (FileStream fs = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.Delete | FileShare.Read, 16 * 1024 * 1024))
            {
                fs.Write(data, 0, bytesRead);
            }

            long copyTime = _watch.ElapsedMilliseconds - downloadTime;
            
            //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Client Received: {0} in {1} ms. Written in : {2} ms", bytesRead, downloadTime, copyTime);
            Console.WriteLine("Total time: {0}", downloadTime + copyTime);
            return responseData;
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
                newClient.Send(String.Format("Hello from Client {0}", i));
                newClient.Receive();
                //newClient.ReceiveFully();
                newClient.Close();
            }

        }

        static void Main(string[] args)
        {
            while (true)
            {
                Thread.Sleep(10000);
                new Thread(StartClient).Start();
            }
        }
    }
}
