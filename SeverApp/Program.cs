using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SeverApp
{
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>(); 
        static void Main(string[] args)
        {
            string fileName = "setting.txt";
            int count = 1;
            IPAddress ip;
            int port;
            using(FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using(StreamReader sr = new StreamReader(fs))
                {
                    ip = IPAddress.Parse(sr.ReadLine());
                    port = int.Parse(sr.ReadLine());
                }
            }
            TcpListener serverSocket = new TcpListener(ip, port);
            serverSocket.Start();
            while (true) {
                TcpClient client = serverSocket.AcceptTcpClient();
                lock (_lock) list_clients.Add(count, client);
                Thread t = new Thread(handle_object);
                t.Start(count);
                count++;
            }
        }
        public static void handle_object(object o)
        {
            int id = (int)o;
            TcpClient client = list_clients[id];
            while (true) {
                NetworkStream stream = client.GetStream();
                Console.WriteLine("Client endPoint :: "+client.Client.RemoteEndPoint);
                byte[] buffer = new byte[1000024];
                int byte_count = stream.Read(buffer, 0, buffer.Length);
                if(byte_count == 0)
                {
                    break;
                }
                //string data = Encoding.UTF8.GetString(buffer, 0 , byte_count);
                broadcast(buffer );
               // Console.WriteLine(data);
            }
            lock (_lock) list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        public static void broadcast(byte[] buffer) {
            //byte[] buffer = Encoding.UTF8.GetBytes(data);
            lock (_lock)
            {
                foreach(TcpClient c in list_clients.Values)
                {
                    NetworkStream stream = c.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
