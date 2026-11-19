namespace Fusionary.BigCommerce.Operations;

public class BcApiOrderRefunds(IBcApi api) : IBcApiOperation
{
    public BcApiOrderRefundsGet Get() => new(api);
}
