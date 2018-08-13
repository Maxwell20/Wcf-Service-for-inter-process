using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WcfServiceA
{

    [ServiceContract(CallbackContract = typeof(IServiceAEvents))]
    public interface IServiceA
    {
        [OperationContract(IsOneWay = true)]
        void RegisterClient();

        [OperationContract(IsOneWay = true)]
        void DeregisterClient(IServiceAEvents client);
        [OperationContract(IsOneWay = false)]
        int GetValue();
    }


    [ServiceContract]
    public interface IServiceAEvents
    {
        [OperationContract(IsOneWay = true)]
        void SendStatus(int status);
    }

    public static class ConnectionFactory
    {

        private static IServiceA ServiceAConnection;// =null?
        public readonly static string ConnectionAddress;
        public static string ServerPath = "";
        public readonly static TimeSpan InactivityTimeoutTimeSpan = TimeSpan.FromHours(1);
        public readonly static string ServerExeName = "WcfServiceA.exe";
        private static Mutex serverMutex = new Mutex();

        public static string GetAddress()
        {
            return "net.pipe://localhost/ServiceASession";
        }

        public static NetNamedPipeBinding GetBinding()
        {
            NetNamedPipeBinding binding = new NetNamedPipeBinding((NetNamedPipeSecurityMode.None));
            binding.OpenTimeout = TimeSpan.FromMinutes(5);
            binding.ReceiveTimeout = TimeSpan.MaxValue;
            binding.CloseTimeout = TimeSpan.MaxValue;
            binding.SendTimeout = TimeSpan.MaxValue;
            binding.MaxConnections = 100;
            binding.MaxBufferSize = int.MaxValue;
            binding.MaxReceivedMessageSize = binding.MaxBufferSize;
            binding.Security = new NetNamedPipeSecurity() { Mode = NetNamedPipeSecurityMode.None };
            return binding;
        }

        static string GetServerPath()
        {
            string path = string.Empty;

            // Try finding the app in this assembly's directory
            string dirpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(dirpath, ServerExeName);
            if (!File.Exists(path))
            {
                // Try finding the app in the current directory
                path = Path.Combine(System.Environment.CurrentDirectory, ServerExeName);
            }

            return path;
        }
        public static IServiceA Connect(Object callbackObject)
        {
            string remoteAddress = GetAddress();
            DuplexChannelFactory<IServiceA> pipeFactory = null;
            NetNamedPipeBinding binding = ConnectionFactory.GetBinding();

            serverMutex.WaitOne();
            try
            {
                try
                {
                    Console.WriteLine("Connecting to ServiceA @ " + remoteAddress);
                    pipeFactory = new DuplexChannelFactory<IServiceA>(
                    callbackObject,
                    binding,
                    new EndpointAddress(remoteAddress));

                    ServiceAConnection = pipeFactory.CreateChannel();
     
                    ServiceAConnection.RegisterClient();
                }
                catch (EndpointNotFoundException)
                {
                    try
                    {
                        Console.WriteLine("Failed to connect to service. Lets try to start the executeable.");

                        if (ServerPath.Length == 0)
                            ServerPath = GetServerPath();
             
                        Process.Start(ServerPath);

                        Thread.Sleep(2000);

                        ServiceAConnection = pipeFactory.CreateChannel();

                        ServiceAConnection.RegisterClient();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("");
                }
            }
            catch { }
            finally
            {
                serverMutex.ReleaseMutex();
            }
            return ServiceAConnection;
        }
    }
}

