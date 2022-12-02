using OWSExternalLoginProviders.Interfaces;
using SimpleInjector;
using System;
using System.Collections.Generic;
using Serilog;

namespace OWSExternalLoginProviders.Extensions
{
    public class ExternalLoginProviderFactory : IExternalLoginProviderFactory
    {

        private readonly Container _container;

        private readonly Dictionary<string, InstanceProducer<IExternalLoginProvider>> _externalLoginProviders
            = new Dictionary<string, InstanceProducer<IExternalLoginProvider>>(StringComparer.OrdinalIgnoreCase);

        public ExternalLoginProviderFactory(Container container)
        {
            _container = container;
        }

        /// <summary>
        /// Gets an Registered ExternalLoginProvider
        /// </summary>
        /// <param name="name">The name of the ExternalLoginProvider</param>
        /// <returns><see cref="IExternalLoginProvider"/></returns>
        public IExternalLoginProvider Get(string name)
        {
            return _externalLoginProviders[name].GetInstance();
        }

        /// <summary>
        /// Registers an ExternalLoginProvider
        /// </summary>
        /// <typeparam name="IExternalLoginProvider"></typeparam>
        /// <param name="name">The name of the ExternalLoginProvider</param>
        public void Register<TImplementation>(string name) where TImplementation : class, IExternalLoginProvider
        {
            if (!IsValid(name))
            {
                var provider = Lifestyle.Transient.CreateProducer<IExternalLoginProvider, TImplementation>(_container);
                _externalLoginProviders.Add(name, provider);
            }
            else
            {
                Log.Error($"ExternalLoginProvider {name} is already Registered.");
            }
            
        }

        /// <summary>
        /// Checks if the ExternalLoginProvider Exists
        /// </summary>
        /// <param name="name">The name of the ExternalLoginProvider</param>
        /// <returns><see cref="bool"/></returns>
        public bool IsValid(string name)
        {
            return _externalLoginProviders.ContainsKey(name);
        }
    }
}
