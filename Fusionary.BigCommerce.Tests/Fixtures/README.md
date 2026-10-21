# Response fixtures

These are reconstructed, redacted API-shaped examples, not raw production captures. Cart numbers preserve the reported decimal-zero and fractional-discount failure (`0.0`, `62.2`, `1.8`). Identifiers, names, addresses, URLs and email addresses are synthetic; reserved example domains replace store/customer data. No credentials or checkout tokens are included.

- `cart.json`: v3 cart envelope, physical lines, discounts, nullable option values and unknown fields. Tests also exercise custom items, gift certificates and quoted monetary values.
- `checkout-urls.json`: cart redirect/embedded-checkout URLs, the checkout functionality currently exposed by this library. The library has no checkout resource model or CRUD endpoint; adding one is outside this fix.
- `order.json`: v2 unwrapped order, quoted and numeric money, RFC2822/ISO dates and empty shipment date.
- `product.json`: v3 catalog product, money and weight, optional fields and an unknown field.
- `customers.json`: v3 customer collection, quoted store credit and nullable dates/channel IDs.

Schema references: [carts](https://github.com/bigcommerce/api-specs/blob/main/reference/carts.v3.yml), [orders](https://github.com/bigcommerce/api-specs/blob/main/reference/orders.v2.oas2.yml), [products](https://github.com/bigcommerce/api-specs/blob/main/reference/catalog/products_catalog.v3.yml), [customers](https://github.com/bigcommerce/api-specs/blob/main/reference/customers.v3.yml), [pricing](https://github.com/bigcommerce/api-specs/blob/main/reference/pricing.sf.yml).
