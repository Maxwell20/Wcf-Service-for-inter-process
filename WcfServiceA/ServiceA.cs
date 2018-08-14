using System;
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
    /*
     * For security purpose, an exception message doesn’t contain any information about the actual exception.
     * During development we can configure the service to return more detailed exception information.
     */
    IncludeExceptionDetailInFaults = true,
    /*
     *    ConcurrencyMode = ConcurrencyMode.Multiple/Reenterant/Single
     *    Multiple - allows multiple threads to call into the service - must manually assure thread safty.
     *    Reenterent - allows one thread to call into the service and queues the rest.
     *    allows interupts for the service to call out durring execution.
     *    Single -allows one thread to call into the service and queues the rest.
     *    the service must complete its action and cannot call out.
     */
    ConcurrencyMode = ConcurrencyMode.Multiple,
    /*
     * Important note about UseSynchronizationContext beahvior!!!!!
     * if UseSynchronizationContext = true
     * then the value from the System.Threading.SynchronizationContext.
     * Current is read and cached so that when a request
     * comes to the service, the host can marshal the request onto the
     * thread that the host was created on using the cached SynchronizationContext.
     * If you try to host a service in, for example,
     * a WPF application and also call that service from the same thread in the WPF application, 
     * you will notice that you get a deadlock when the client tries to call the service.
     * The reason for this is that the default value of the UseSynchronizationContext is 
     * true and so when you create the ServiceHost on the UI thread of the WPF application,
     * then the current synchronization context is a DispatcherSynchronizationContext which
     * holds a reference to a System.Windows.Threading.Dispatcher
     * object which then holds a reference to the current thread. 
     * The DispatcherSynchronizationContext will then be used when a request comes in to marshal requests onto the UI thread.
     * But if you are calling the service from the UI thread then you have a deadlock when it tries to do this.
     */
    UseSynchronizationContext = true)]
    public partial class ServiceA : IServiceA, IDisposable
    {
        private int mServiceData = 0;
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
            try
            {
                Random rnd = new Random();
                int value = rnd.Next(1, 6);
                Console.WriteLine("Recieved request from client.");
                //throw new Exception("We broke here.");
                return value;
            }
            //if an exception goes unhandled the service will become faulted.
            catch(Exception ex)
            {
                Console.WriteLine("Exception was thrown in the service: " + ex.Message);
                /*
                 * throw fault exception back to the client.
                 * Use FaultException Class to customize the Error Message the service returns.
                 * Clients can’t distinguish types of faults
                 * Use FaultCode to specify a SOAP fault code
                 * Use FaultReason to specify the description of the fault. It supports locale based translation of message
                 * Use a fault exception in order to prevent the client from thinking the service is faulted.
                 * WCF does not communicate CLR Exceptions.
                 * WCF Exceptions are passed as SOAP Messages. The underlying principle of service-oriented error handling consists of
                 * SOAP fault messages, which convey the failure semantics and additional information associated with the failure (such
                 * as the reason).
                 */

                throw new FaultException("We broke over here...");
           
            }
        }

        public void GetValueAsnyc()
        {
            Console.WriteLine("Calcualting something difficult...");
            Thread.Sleep(5000);
            Console.WriteLine("All done! - Lets tell everyone...");

            foreach (IServiceAEvents client in mCallbackList)
            {
                client.SendValueBack("All done here...");
            }
        }

        public void UpdateService(int serviceData)
        {
            mServiceData = serviceData;
            Console.WriteLine("Service was updated by client: " + serviceData);
        }
    }
}
