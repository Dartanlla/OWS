using System;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server;
using SimpleInjector;


namespace OWSParty
{
    public class GrpcSimpleInjectorActivator<T> : IGrpcServiceActivator<T>
        where T : class
    {
        private readonly Container container;

        public GrpcSimpleInjectorActivator(Container container) => this.container = container;

        public GrpcActivatorHandle<T> Create(IServiceProvider serviceProvider) =>
            new GrpcActivatorHandle<T>(container.GetInstance<T>(), false, null);

        public ValueTask ReleaseAsync(GrpcActivatorHandle<T> service) => default;
    }
}
