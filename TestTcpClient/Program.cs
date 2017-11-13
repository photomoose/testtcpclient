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
            bytes = PrependLengthHeader(bytes);

            var tcpClient = new TcpClient();
            tcpClient.Connect(ConfigurationManager.AppSettings["ipAddress"], int.Parse(ConfigurationManager.AppSettings["port"]));

            var stream = tcpClient.GetStream();

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
    }
}
