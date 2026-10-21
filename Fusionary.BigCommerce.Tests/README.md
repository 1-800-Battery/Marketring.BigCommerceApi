# BigCommerce tests

The default suite is offline and needs no store credentials:

```sh
dotnet build --configuration Release
dotnet test --configuration Release --no-build -- NUnit.ExplicitIncludes=false
dotnet pack Fusionary.BigCommerce/Fusionary.BigCommerce.csproj --configuration Release --no-build --output bin/packages
```

Response fixtures and their provenance are documented in [Fixtures/README.md](Fixtures/README.md). Existing live integration tests, including both tests in `CartSimpleOperationsTest`, are `[Explicit]`; some create/update orders, customers, prices, images or carts. Run only deliberately selected tests against a configured test store. For example:

```sh
dotnet test --filter FullyQualifiedName~GeographyTests -- NUnit.ExplicitIncludes=true
```

Store credentials can be configured through the test project's user secrets (`BigCommerce:StoreHash`, `BigCommerce:AccessToken`). Storefront tests additionally require `BigCommerce:StorefrontUrl`, `BigCommerce:StorefrontChannelId` and `BigCommerce:StorefrontAccessToken`. Never use live mutation tests as a package gate.

The schema regression suite checks that the live cart fixture remains explicit. Review round 1 corrected its missing annotation; results reported before that correction did not establish an offline gate. When verifying results, inspect the TRX outcomes: NUnit may display explicit tests as skipped while excluding them from its summary total.
