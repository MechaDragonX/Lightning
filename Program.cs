using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lightning
{
    class Program
    {
        public class StateObject
        {
            public const int BufferSize = 1024;
            public byte[] Buffer = new byte[BufferSize];
            public StringBuilder Builder = new StringBuilder();
            public Socket WorkSocket = null;
        }
        public class AsynchronousSocketListner
        {
            public static ManualResetEvent complete = new ManualResetEvent(false);

            public AsynchronousSocketListner() { }

            public static void StartListening()
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);
                    while(true)
                    {
                        complete.Reset();
                        Console.WriteLine("Waiting for a connection. . .");
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                        complete.WaitOne();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("\nPress ENTER to continue. . .");
                Console.ReadKey();
            }
            public static void AcceptCallback(IAsyncResult ar)
            {
                complete.Set();

                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                StateObject state = new StateObject();
                state.WorkSocket = handler;
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            public static void ReadCallback(IAsyncResult ar)
            {
                string content = string.Empty;

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.WorkSocket;

                int bytesRead = handler.EndReceive(ar);

                if(bytesRead > 0)
                {
                    state.Builder.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));

                    content = state.Buffer.ToString();
                    if(content.IndexOf("<EOF>") > -1)
                    {
                        Console.WriteLine($"Read {content.Length} bytes from socket.\nData: {content}");
                        Send(handler, content);
                    }
                    else
                        handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }

            private static void Send(Socket handler, string data)
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    Socket handler = (Socket)ar.AsyncState;
                    
                    int bytesSent = handler.EndSend(ar);
                    Console.WriteLine($"Sent {bytesSent} bytes to client.");

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            static int Main(string[] args)
            {
                StartListening();
                return 0;
            }
        }
    }   
}
