namespace Fusionary.BigCommerce.Types;

public record BcRequestOptions
{
    private BcRequestOverride? _requestOverrides;

    internal bool OmitSensitiveResponseDetails { get; init; }

    public BcRequestOverride RequestOverrides
    {
        get => LazyInitializer.EnsureInitialized(ref _requestOverrides);
        set => _requestOverrides = value;
    }
}
