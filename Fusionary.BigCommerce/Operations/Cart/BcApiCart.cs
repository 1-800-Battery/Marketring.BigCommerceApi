namespace Fusionary.BigCommerce.Operations;

public class BcApiCart(IBcApi api) : IBcApiOperation
{
    public BcApiCartGetCheckoutURLs GetCartRedirects() => new(api);
    public BcApiCartLineAdd AddLineItem() => new(api);
    public BcApiCartUpdateLine UpdateLineItem() => new(api);
    public BcApiCartDeleteItem DeleteLineItem() => new(api);
    public BcApiCartCreate Create() => new(api);
    public BcApiCartDelete Delete() => new(api);
    public BcApiCartGet Get() => new(api);
}