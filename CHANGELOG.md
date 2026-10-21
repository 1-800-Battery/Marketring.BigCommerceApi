# Changelog

## 1.2.0

### Response handling

- Correct cart `DiscountAmount`, `ManualDiscountAmount`, `Discount.DiscountedAmount`, `PhysicalItem.DiscountAmount`, `PhysicalItem.CouponAmount`, `CustomItem.ListPrice`, and `GiftCertificate.Amount` from `long` to `decimal`. Correct `BcProductPricingBulkPricingItem.DiscountAmount` from `int` to `decimal` as well. Fractions are retained without conversion to binary floating point.
- Accept the documented numeric-or-array shape for physical-item `coupons` and object shape for `promotions.banners`, while retaining both public `List<object>?` members. A numeric coupon or banner object is exposed as a one-element list containing a `JsonElement`. The SDK preserves the original scalar/object JSON shape while that normalized list remains a valid singleton; replacing it with a normal list or growing it writes an array. Existing arrays, nulls and missing values retain their behavior. This shape preservation applies to the SDK serializer, not arbitrary third-party serializers.
- Correct `BcOptionConfig.NumberLowestValue` and `NumberHighestValue` from `int?` to `decimal?` so fractional modifier bounds are retained, including quoted numeric values. Null/missing bounds remain null.
- Keep existing cart `double` prices, integer IDs/quantities, nested class names, request builders, assembly name and namespaces. Integer JSON fields accept equivalent whole-valued decimal/exponent/quoted forms and reject fractional or out-of-range values. `BcId` no longer silently accepts invalid text as zero.
- Parse API money and dates with invariant culture. Nullable dates accept null/empty strings; existing nonnullable date wrappers retain their `HasValue == false` representation for missing dates. Malformed nonempty money/date values produce failures, rather than zero/default data.
- Preserve known enum names and numeric values. Unknown string enum members map to the undefined value `-1`, rather than to a meaningful zero-valued member. The original unknown string is not retained; callers must handle undefined enum values before using responses as requests.
- Capture cart extension data. Preserve existing nullable order/customer fields and product variant types; do not globally convert required numbers to nullable values.
- Deserialization errors expose the exception type plus `JsonPath` and `Field` in `ErrorDetails`. Paths are relative to the deserialized data/meta subtree. Only known model names and bounded numeric indexes are retained; dynamic keys and unrecognized paths are redacted. Exception messages, stack traces, request bodies and request URIs are omitted from failure diagnostics. Rate-limit headers and HTTP status are retained, and cancellation propagates.

`ResponseText` remains available for callers that explicitly need the original payload and remains `[JsonIgnore]`. It is **not** a safe diagnostic/log field. This preserves the existing raw-response recovery contract without including bodies in serialized deserialization errors.

### Compatibility and package metadata

The eight corrected monetary property types are source/binary changes for callers accessing those specific members. The two modifier-bound properties `BcOptionConfig.NumberLowestValue` and `NumberHighestValue` also change their getter/setter signatures from `int?` to `decimal?`. Recompile direct users and use `decimal` or `decimal?` destinations as appropriate. Existing integer assignments remain source compatible. The inspected downstream integration does not access those SDK monetary or modifier-bound members; its used price, ID, result and fluent API signatures remain unchanged. This is a targeted minor release on the Marketring package line (1.1.3 → 1.2.0), not a blanket binary-compatibility guarantee.

Local builds now produce `Marketring.BigCommerce` 1.2.0 with the fork repository metadata, README and original MIT license. MinVer is disabled for the package project so unrelated upstream `v*` tags cannot select its version. The existing `marketring-package.yml` workflow remains the publication route (GitHub Packages, `marketring-v*` tags or workflow dispatch); release properties supplied by that workflow still override the project defaults. This change does not publish a package.

### Upstream comparison

Compared upstream `fusionary/Fusionary.BigCommerceApi` at `008712370d72beca837186ec746f3e323c4bf1e3` with the common ancestor `7046cae8c0d44f922322e598798ee441082ab98d` and the fork's existing changes. Ported the two `BcOptionConfig` JSON-name corrections from upstream `0087123`: `text_characters_limited` and `text_lines_limited`.

The fork already includes epoch date parsing, shared option IDs, flexible default values and empty-array option config handling. Upstream's nullable modifier ID changes were not ported because they change signatures used by consumers; existing flexible option-value/default-value contracts were retained. B2B invoice/payment operations, shipments and category endpoints are unrelated to this regression and were not imported. Fusionary's MIT copyright and license are unchanged and included in the package.

### Validation

Offline fixture tests cover carts, supported checkout URLs, v2 orders, v3 products/customers, numeric precision/range, culture, null/empty/invalid dates, enum compatibility, safe errors and cancellation. Live integration tests, including the entire `CartSimpleOperationsTest` fixture, are explicit opt-in to prevent ordinary build/test gates from creating or modifying store resources. A regression guard checks that the live cart fixture requires opt-in. Earlier claims of a fully offline 54-test gate were incorrect: that count included two live cart tests; those tests are now excluded from the default gate.

The coupon/banner and modifier-bound corrections were verified against the official [cart OpenAPI](https://docs.bigcommerce.com/openapi/admin-management-carts.json) and [modifier OpenAPI](https://docs.bigcommerce.com/openapi/admin-catalog-product-modifiers.json), with reconstructed fixtures and tests for both new and existing shapes. This targeted audit does not claim exhaustive coverage of every API resource.
