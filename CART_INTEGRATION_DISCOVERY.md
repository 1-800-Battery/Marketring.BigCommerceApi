# Cart API Integration Discovery - Phase 1

## Cart-Related Commits to Cherry-Pick

### Primary Cart Commits (Chronological Order)
1. `97e28e9` - LineItems Class Change
2. `119ad0b` - Add Cart Operations to API ⭐ (Main commit)
3. `269a073` - Update CartResponse Variable
4. `bae145b` - Add Methods Update and AddItem to Cart
5. `7ccc302` - Update BcCartPost Nesting
6. `1b25301` - Update Add Line item
7. `2cf64bf` - Update BC Cart Customer
8. `cf91329` - Update LineItemQuantity

### Files Created/Modified by Cart Implementation

#### Operations (9 classes)
- `/Operations/Cart/BcApiCart.cs` - Main Cart API entry point
- `/Operations/Cart/BcApiCartOverview.cs` - Cart overview operations
- `/Operations/Cart/BcApiCartCreate.cs` - Cart creation
- `/Operations/Cart/BcApiCartGet.cs` - Cart retrieval
- `/Operations/Cart/BcApiCartDelete.cs` - Cart deletion
- `/Operations/Cart/BcApiCartLineAdd.cs` - Add line items
- `/Operations/Cart/BcApiCartUpdateLine.cs` - Update line items
- `/Operations/Cart/BcApiCartLineDelete.cs` - Delete line items
- `/Operations/Cart/BcApiCartUpdateCustomer.cs` - Customer association (likely added later)

#### Types (4 classes)
- `/Types/BcCartResponseFull.cs` - Complete cart response model
- `/Types/BcCartPost.cs` - Cart creation payload
- `/Types/BcCartLineItem.cs` - Individual line item
- `/Types/BcCartLineItems.cs` - Line items collection

#### Integration Points Modified
- `ExtensionsForBcApi.cs` - Added `.Carts()` extension method
- `Operations/BcEndpoint.cs` - Added Cart API endpoints
- `Fusionary.BigCommerce.Tests/OrderTests.cs` - Tests modified/added

## Integration Pattern Analysis

### Current Upstream Pattern (C# 12)
```csharp
public class BcApiManagement(IBcApi api) : IBcApiOperation
{
    public BcApiCustomer Customer() => new(api);
    public BcApiOrdersOverview Orders() => new(api);
    // etc...
}
```

### Cart Integration Decision Point ⚠️

**Fork Pattern**: Cart exposed at root level
```csharp
public static BcApiCartsOverview Carts(this IBcApi bc) => new(bc);
```

**Option A**: Keep at root level (matches fork)
```csharp
// In ExtensionsForBcApi.cs
public static BcApiCartOverview Carts(this IBcApi bc) => new(bc);
```

**Option B**: Move under Management (follows upstream organization)
```csharp
// In BcApiManagement.cs
public class BcApiManagement(IBcApi api) : IBcApiOperation
{
    // ... existing methods ...
    
    /// <summary>
    /// Manage Shopping Carts
    /// </summary>
    public BcApiCartOverview Carts() => new(api);
}
```

**Recommendation**: Start with Option A (root level) to minimize integration conflicts, then refactor to Option B if desired.

## Conflict Resolution Strategy

### 1. Constructor Pattern Migration
**From (Fork):**
```csharp
public class BcApiCart : IBcApiOperation
{
    private readonly IBcApi _api;
    public BcApiCart(IBcApi api) { _api = api; }
}
```

**To (Upstream):**
```csharp
public class BcApiCart(IBcApi api) : IBcApiOperation
{
    public BcApiCartCreate Create() => new(api);
    public BcApiCartGet Get() => new(api);
    // etc...
}
```

### 2. Test Framework Migration
- Convert XUnit tests to NUnit
- Check for Cart-specific test files
- Ensure test patterns match upstream

### 3. Endpoint Registration
- Verify all Cart endpoints are added to `BcEndpoint.cs`
- Follow upstream URL pattern conventions

## Cherry-Pick Order Recommendation

1. **Start with core structure** - `119ad0b` (main Cart API)
2. **Apply incremental updates** in chronological order
3. **Handle conflicts** per file:
   - Constructor patterns → Transform to C# 12
   - Test files → Convert to NUnit
   - Integration points → Merge carefully

## Risk Assessment

| Component | Risk | Mitigation |
|-----------|------|-----------|
| Constructor Pattern | High | Systematic transformation using template |
| Test Migration | High | Convert one test at a time |
| Endpoint Conflicts | Low | Additive changes only |
| Type Definitions | Low | No conflicts expected |
| Integration Point | Medium | Add to BcApiManagement following pattern |

## Next Steps
1. Create backup branch: `git checkout -b cart-integration-backup`
2. Start cherry-picking from `119ad0b`
3. Transform constructors as we go
4. Run tests after each major change

---
**Generated**: 2025-07-07  
**Status**: Ready for Phase 2 execution