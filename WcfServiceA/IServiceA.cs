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
    /*•	
     * Service Contract describes what the service can do.
     *  It defines some properties about the service,
     *  and a set of actions called Operation Contracts.
     *  Operation Contracts are equivalent to web methods.
     */
    [ServiceContract(CallbackContract = typeof(IServiceAEvents)  
     /*,CallbackContract = property specifies the return contract in a two - way(duplex) conversation,
     ConfigurationName = property specifies the name of the service element in the configuration file to use,
     Name/Namespace = properties control the name and namespace of the contract in the WSDL<portType> element.
     SessionMode = property specifies whether the contract requires a binding that supports sessions,
     ProtectionLevel = ProtectionLevel properties indicate whether
     all messages supporting the contract have a explicit ProtectionLevel value, and if so, what that level is*/
     )]
    public interface IServiceA
    {
        [OperationContract(IsOneWay = true)]
        void RegisterClient();

        [OperationContract(IsOneWay = true)]
        void DeregisterClient(IServiceAEvents client);


        //There are three types of Message Exchange Patterns:
        /*
         * Request/Reply - Client sends a message to the service
         * and waits for the service to send the message back.
         */
        [OperationContract(IsOneWay = false)]
        int GetValue();
        /*
         * Duplex - Client and Service can initiate communication by sending a message to each other.
         * Use when service needs to notify the client that it finished processing an operation or when
         * service can publish events to which client can subscribe.
         */
        [OperationContract(IsOneWay = true)]
        void GetValueAsnyc();
        /*
         * One way - Client sends a message to the service and doesn’t wait for the reply.
         */
        [OperationContract(IsOneWay = true)]
        void UpdateService(int serviceData);
    }


    [ServiceContract]
    public interface IServiceAEvents
    {
        [OperationContract(IsOneWay = true)]
        void SendStatus(int status);

        [OperationContract(IsOneWay = true)]
        void SendValueBack(string callbackStr);
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

