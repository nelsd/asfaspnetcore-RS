using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace WSinternetfacing
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WSinternetfacing : StatelessService
    {
        public WSinternetfacing(StatelessServiceContext context)
            : base(context)
        { }


        private X509Certificate2 FindCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindByThumbprint,
                    thumbprint, false); // Don't validate certs, since the test root isn't installed.
                if (col == null || col.Count == 0)
                    return null;
                return col[0];
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        //ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel for http on IPAddress.Loopback on port 50000");
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel for https on IPAddress.Loopback on port 443");

                        return new WebHostBuilder()
                                    .UseKestrel(options =>
                                    {
                                        //options.Listen(IPAddress.Loopback, 5000);
                                        //options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                                        //{
                                        //    listenOptions.UseHttps("testCert.pfx", "testPassword");
                                        //}

                                        options.Listen(IPAddress.Any, 50000);
                                        options.Listen(IPAddress.Any, 443, listenOptions =>
                                        {
                                            //https://stackoverflow.com/questions/40607261/both-http-and-https-endpoints-on-service-fabric-asp-net-core-stateless-service
                                            listenOptions.UseHttps(FindCertificate("94A0D7680E8B10C795D061239F3B916964960FCB"));
                                            //listenOptions.UseHttps("testCert.pfx", "testPassword");
                                        }
                                        );
                                    })
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatelessServiceContext>(serviceContext))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)                                    
                                    //.UseUrls(url)
                                    .Build();
                    }))
            };
        }
    }
}
