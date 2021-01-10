using System;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Lightning
{
    class Program
    {
        static void Main(string[] args)
        {
            Ping pinger = new Ping();
            AutoResetEvent waiter = new AutoResetEvent(false);

            pinger.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

            string data = "Ping!";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 12000;

            pinger.SendAsync("address", timeout, buffer, waiter);

            waiter.WaitOne();
            Console.WriteLine("Ping completed!");
        }
        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                Console.WriteLine("Ping cancelled!");
                ((AutoResetEvent)e.UserState).Set();
            }
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());
                ((AutoResetEvent)e.UserState).Set();
            }
            PingReply reply = e.Reply;
            DisplayReply(reply);
            ((AutoResetEvent)e.UserState).Set();
        }
        private static void DisplayReply(PingReply reply)
        {
            if (reply == null)
                return;

            Console.WriteLine($"Ping Status: {reply.Status}");
            if(reply.Status == IPStatus.Success)
            {
                Console.WriteLine($"Address: {reply.Address.ToString()}");
                Console.WriteLine($"Round Trip Time: {reply.RoundtripTime}");
                Console.WriteLine($"Buffer Size: {reply.Buffer.Length}");
            }
        }
    }   
}
