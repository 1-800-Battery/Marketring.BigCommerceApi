namespace Fusionary.BigCommerce.Operations;

public class BcApiCartsOverview(IBcApi api) : IBcApiOperation
{
    public BcApiCart Cart() => new(api);
}