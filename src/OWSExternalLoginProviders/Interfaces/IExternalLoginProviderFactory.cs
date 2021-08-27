namespace OWSExternalLoginProviders.Interfaces
{
    public interface IExternalLoginProviderFactory
    {
        /// <summary>
        /// Gets an Registered ExternalLoginProvider
        /// </summary>
        /// <param name="name">The name of the ExternalLoginProvider</param>
        /// <returns><see cref="IExternalLoginProvider"/></returns>
        IExternalLoginProvider Get(string name);

        /// <summary>
        /// Registers an ExternalLoginProvider
        /// </summary>
        /// <typeparam name="IExternalLoginProvider"></typeparam>
        /// <param name="name">The name of the ExternalLoginProvider</param>
        void Register<TImplementation>(string name) where TImplementation : class, IExternalLoginProvider;

        /// <summary>
        /// Checks if the ExternalLoginProvider is valid
        /// </summary>
        /// <param name="name">The name of the ExternalLoginProvider</param>
        /// <returns><see cref="bool"/></returns>
        bool IsValid(string name);
    }
}
