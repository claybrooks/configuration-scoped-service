namespace ConfigurationScopedService.Internal;

internal interface IServiceFactory<in TConfigType, out TServiceType> where TConfigType : class where TServiceType : class
{
    TServiceType Create(TConfigType config);
}
