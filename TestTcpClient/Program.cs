using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace TestTcpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var request = File.ReadAllText("Request.txt");
            var asciiRequest = request.Split('-');

            var bytes = asciiRequest.Select(asciiByte => byte.Parse(asciiByte, NumberStyles.HexNumber)).ToArray();

            if (args.Contains("-stx"))
            {
                Console.WriteLine("Prepending the STX to the request");
                bytes = PrependStx(bytes);
            }

            if (args.Contains("-parity"))
            {
                Console.WriteLine("Setting even parity on bytes");
                bytes = EvenParity.Set(bytes);
            }

            if (!args.Contains("-nolength"))
            {
                Console.WriteLine("Prepending the length to the request");
                bytes = PrependLengthHeader(bytes);
            }

            var tcpClient = new TcpClient();
            var ipAddress = ConfigurationManager.AppSettings["ipAddress"];
            var port = int.Parse(ConfigurationManager.AppSettings["port"]);
            tcpClient.Connect(ipAddress, port);

            var stream = tcpClient.GetStream();

            Console.WriteLine("Connected, sending {0} bytes to {1}({2})", bytes.Length, ipAddress, port);
            stream.Write(bytes, 0, bytes.Length);

            Console.WriteLine("Waiting for data...");

            while (!stream.DataAvailable)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Received data:");

            while (stream.DataAvailable)
            {
                var buffer = new byte[512];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var asciiResponse = BitConverter.ToString(buffer, 0, bytesRead);
                Console.Write(asciiResponse);

                File.AppendAllText("Response.txt", asciiResponse);
            }

            Console.ReadLine();
        }

        public static byte[] PrependLengthHeader(byte[] bytes)
        {
            var bytesList = bytes.ToList();

            var payloadLengthBytes = BitConverter.GetBytes((ushort)bytesList.Count);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadLengthBytes);
            }

            bytesList.InsertRange(0, payloadLengthBytes);

            return bytesList.ToArray();
        }

        private static byte[] PrependStx(byte[] bytes)
        {
            var bytesList = bytes.ToList();
            
            bytesList.Insert(0, 0x02);

            return bytesList.ToArray();
        }
    }
}
