using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WcfServiceA
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting service host thread...");
                var Service = new ServiceA();
                Thread serviceHostThread = new Thread(Service.ServiceHostThread);
                serviceHostThread.Start();
                Console.WriteLine("Service host started...");

                while (serviceHostThread.IsAlive)
                {
                    Thread.Sleep(5000);
                }
                Console.WriteLine("Service host thread exited...");

                try { serviceHostThread.Abort(); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in program.cs: " + ex.Message);
            }
        }
    }
}
