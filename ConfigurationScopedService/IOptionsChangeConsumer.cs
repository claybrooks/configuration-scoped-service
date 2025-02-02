namespace ConfigurationScopedService;

public interface IOptionsChangeConsumer<in TOptionsType>
{
    void ConsumeChange(TOptionsType options);
}