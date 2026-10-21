# BigCommerce tests

The default suite is offline and needs no store credentials:

```sh
dotnet build --configuration Release
dotnet test --configuration Release --no-build -- NUnit.ExplicitIncludes=false
dotnet pack Fusionary.BigCommerce/Fusionary.BigCommerce.csproj --configuration Release --no-build --output bin/packages
```

Response fixtures and their provenance are documented in [Fixtures/README.md](Fixtures/README.md). Existing live integration tests are `[Explicit]`; some create/update orders, customers, prices, images or carts. Run only deliberately selected tests against a configured test store. For example:

```sh
dotnet test --filter FullyQualifiedName~GeographyTests -- NUnit.ExplicitIncludes=true
```

Store credentials can be configured through the test project's user secrets (`BigCommerce:StoreHash`, `BigCommerce:AccessToken`). Storefront tests additionally require `BigCommerce:StorefrontUrl`, `BigCommerce:StorefrontChannelId` and `BigCommerce:StorefrontAccessToken`. Never use live mutation tests as a package gate.
