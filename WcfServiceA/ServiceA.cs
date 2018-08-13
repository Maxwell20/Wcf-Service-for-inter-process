﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;/*->everything related to WCF*/
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WcfServiceA
{



    [ServiceBehavior(
    /*
     * InstanceContextMode = InstanceContextMode.Single/Percall/PerSession
     * Single - same service is used by each client. singleton model
     * Percall - stateless behavior - for every method called by a client a new service is created. 
     * PerSession - for every client a new intance of the service is created.
     */
    InstanceContextMode = InstanceContextMode.Single,
    IncludeExceptionDetailInFaults = true, 
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = true)]
    public partial class ServiceA : IServiceA, IDisposable
    {
        private bool mExitNow = false;
        private int mSleepTime = 3000;
        private static List<IServiceAEvents> mCallbackList = new List<IServiceAEvents>();
        void _serviceHost_Faulted(object sender, EventArgs e)
        {
            // never raise up..
            Console.WriteLine("Host has faulted...");
        }
        internal void ServiceHostThread()
        {
            try
            {
                Uri baseURI = new Uri(ConnectionFactory.GetAddress());
                var host = new ServiceHost(this);

                NetNamedPipeBinding binding = ConnectionFactory.GetBinding();

                host.Faulted += _serviceHost_Faulted;

                /*
                 *Endpoints can be defined in the app.config file also.
                 *End points consist of an endpoint adress(baseURI), binding(named pipes in this case) and a service contract(this is the interface).
                 *A client must know the adress to connect to the service.
                 */
                var se = host.AddServiceEndpoint(
                    typeof(IServiceA),
                    binding,
                    baseURI);

                host.Open();

                Console.WriteLine("Service host thread started.");
                Console.WriteLine("Host binding: " + binding);
                Console.WriteLine("Host base URI: " + baseURI);
                int value = 0;
                while (!mExitNow)
                {              
                    if (0 < mSleepTime)
                    {
                        Thread.Sleep(mSleepTime);
                        foreach(IServiceAEvents client in mCallbackList)
                        {
                            Console.WriteLine("Fire callback: " + value);
                            client.SendStatus(value);
                            value++;
                        }
                    }
                }

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught trying to start service host thread...");
            }
            finally
            {
                Console.WriteLine("Service host thread exiting...");
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void RegisterClient()
        {
            IServiceAEvents newClient =
                OperationContext.Current.GetCallbackChannel<IServiceAEvents>();

            if (!mCallbackList.Contains(newClient))
            {
                mCallbackList.Add(newClient);
                Console.WriteLine("Successfully added client: " + newClient);
            }
        }

        public void DeregisterClient(IServiceAEvents client)
        {
            if (mCallbackList.Contains(client))
            {
                mCallbackList.Remove(client);
                Console.WriteLine("Successfully removed client: " + client);
            }
        }

        public int GetValue()
        {
            Random rnd = new Random();
            int value = rnd.Next(1, 6);
            Console.WriteLine("Recieved request from client.");
            return value;
        }
    }
}