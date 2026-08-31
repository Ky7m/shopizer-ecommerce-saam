---
name: saam-source-reading-dotnet
description: "Source code analysis guide for legacy .NET Framework applications, WCF, WebForms, and MVC."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# Source Reading Guide: .NET Framework (C#, VB.NET, WCF, WinForms, ASP.NET)

## When to Activate
Activate this guide when the legacy system is built on .NET Framework (4.x or earlier), including WCF services, WinForms/WPF desktop apps, ASP.NET WebForms/MVC, and Windows Services.

## Common .NET Legacy Patterns

### Solution Structure
```
Solution.sln
├── ProjectName.Core/           # Domain/business logic
├── ProjectName.Data/           # Data access (EF, ADO.NET, stored procs)
├── ProjectName.Services/       # WCF service contracts
├── ProjectName.Web/            # ASP.NET WebForms/MVC
├── ProjectName.WinForms/       # Desktop UI
├── ProjectName.WindowsService/ # Background processing
└── ProjectName.Common/         # Shared DTOs, utilities
```

### Key File Types
| Extension | Purpose | Business Relevance |
|-----------|---------|-------------------|
| `.cs` / `.vb` | Source code | All logic |
| `.svc` | WCF service endpoint | API entry point |
| `.asmx` | Legacy web service | SOAP API |
| `.aspx` / `.aspx.cs` | WebForms page | UI + code-behind logic |
| `.config` | Configuration (web.config, app.config) | Connection strings, bindings |
| `.edmx` | Entity Framework model | Data model definition |
| `.resx` | Resources | Localization, constants |
| `.sql` | Database scripts / stored procs | Data logic |

## WCF Services

### Service Contract Pattern
```csharp
[ServiceContract]
public interface IOrderService
{
    [OperationContract]
    OrderResponse CreateOrder(OrderRequest request);

    [OperationContract]
    OrderStatus GetOrderStatus(string orderId);
}

public class OrderService : IOrderService
{
    public OrderResponse CreateOrder(OrderRequest request)
    {
        // Business logic here
    }
}
```

### What to Extract from WCF
- `[ServiceContract]` interfaces → API surface / service boundaries
- `[OperationContract]` methods → individual operations
- `[DataContract]` / `[DataMember]` classes → data models / DTOs
- `[FaultContract]` → error handling contracts
- Binding configurations in web.config → communication patterns (HTTP, TCP, MSMQ)

## Data Access Patterns

### Entity Framework (Database First / Model First)
```csharp
using (var context = new OrderContext())
{
    var order = context.Orders
        .Include(o => o.LineItems)
        .FirstOrDefault(o => o.OrderId == id);
}
```

### ADO.NET / Stored Procedures
```csharp
using (var cmd = new SqlCommand("sp_ProcessOrder", connection))
{
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("@OrderId", orderId);
    cmd.ExecuteNonQuery();
}
```

### Repository Pattern
```csharp
public class OrderRepository : IOrderRepository
{
    public Order GetById(int id) { ... }
    public void Save(Order order) { ... }
}
```

### What to Extract from Data Layer
- DbContext / ObjectContext classes → entity relationships
- `.edmx` files → full data model with navigation properties
- Stored procedure calls → business logic in database
- Connection strings in config → database dependencies
- Transaction scopes → consistency boundaries

## ASP.NET WebForms

### Code-Behind Pattern
```csharp
public partial class OrderForm : System.Web.UI.Page
{
    protected void btnSubmit_Click(object sender, EventArgs e)
    {
        // Business logic often embedded in event handlers
        if (ValidateOrder())
        {
            SaveOrder();
            Response.Redirect("Confirmation.aspx");
        }
    }
}
```

### What to Extract from WebForms
- Event handlers (`btn_Click`, `Page_Load`) → business workflows
- Validation logic in code-behind → business rules
- Session/ViewState usage → state management patterns
- Master pages → shared layout / navigation structure
- User controls (.ascx) → reusable UI components with logic

## ASP.NET MVC

### Controller Pattern
```csharp
public class OrderController : Controller
{
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public ActionResult Create(OrderViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        _orderService.CreateOrder(model.ToDto());
        return RedirectToAction("Index");
    }
}
```

### What to Extract from MVC
- Controller actions → API operations / business workflows
- `[Authorize]` attributes → authorization rules
- Model validation (DataAnnotations) → input validation rules
- Action filters → cross-cutting concerns
- Areas → domain boundaries (potential service splits)

## Windows Services / Background Processing

### Common Patterns
```csharp
public class OrderProcessingService : ServiceBase
{
    private Timer _timer;

    protected override void OnStart(string[] args)
    {
        _timer = new Timer(ProcessPendingOrders, null, 0, 60000);
    }

    private void ProcessPendingOrders(object state)
    {
        // Batch processing logic
    }
}
```

### What to Extract
- Timer intervals → scheduling requirements
- Queue consumers (MSMQ, RabbitMQ) → async integration points
- Batch processing logic → business rules operating on collections
- Error handling / retry patterns → resilience requirements

## WinForms / WPF Desktop

### Common Patterns
```csharp
public partial class OrderForm : Form
{
    private void CalculateTotal()
    {
        decimal subtotal = _lineItems.Sum(li => li.Quantity * li.UnitPrice);
        decimal tax = subtotal * GetTaxRate(_customerRegion);
        decimal discount = ApplyDiscount(subtotal, _customerTier);
        txtTotal.Text = (subtotal + tax - discount).ToString("C");
    }
}
```

### What to Extract
- Form logic → business rules (often tightly coupled to UI)
- Data binding patterns → entity relationships
- Print/report logic → reporting requirements
- MDI patterns → workflow/navigation structure

## Configuration Patterns

### web.config / app.config
```xml
<connectionStrings>
    <add name="OrderDB" connectionString="..." />
</connectionStrings>
<appSettings>
    <add key="MaxOrderAmount" value="50000" />
    <add key="TaxRate" value="0.08" />
</appSettings>
<system.serviceModel>
    <services>
        <service name="OrderService">
            <endpoint binding="basicHttpBinding" contract="IOrderService" />
        </service>
    </services>
</system.serviceModel>
```

### What to Extract
- Connection strings → database dependencies
- App settings → configurable business parameters
- WCF bindings → communication protocols
- Custom config sections → domain-specific configuration

## Dependency Injection (if present)

### Common DI Containers
- Unity, Autofac, Ninject, StructureMap, Castle Windsor

```csharp
container.RegisterType<IOrderService, OrderService>();
container.RegisterType<IOrderRepository, SqlOrderRepository>();
```

### What to Extract
- Registration mappings → service dependencies and composition
- Lifetime scopes → state management patterns
- Named registrations → strategy/factory patterns

## MSMQ / Message-Based Integration

```csharp
var queue = new MessageQueue(@".\private$\orders");
queue.Send(new OrderMessage { OrderId = id, Action = "Process" });
```

### What to Extract
- Queue names → integration channels
- Message types → event/command contracts
- Send/receive patterns → async communication flows

## Business Rule Extraction Priorities

1. **Service contract methods** → API operations and boundaries
2. **Validation logic** (DataAnnotations, FluentValidation, manual) → input rules
3. **Conditional logic in services** (if/switch) → business decisions
4. **Stored procedures** → data-level business logic (often critical)
5. **Configuration values** → business parameters and thresholds
6. **Authorization attributes** → access control rules
7. **Transaction scopes** → consistency boundaries
8. **Event handlers in UI** → workflows (untangle from presentation)
9. **Timer/batch logic** → scheduled business processes
10. **Exception handling** → error scenarios and recovery rules

## Common Anti-Patterns to Watch For

| Anti-Pattern | Impact on Extraction |
|-------------|---------------------|
| Logic in code-behind / event handlers | Must separate business rules from UI |
| God classes (3000+ LOC service) | Need to identify sub-domains within |
| Stored procedure heavy | Business logic split between app and DB |
| Static helper classes | Hidden dependencies, shared state |
| Service Locator pattern | Non-obvious dependencies |
| Tight coupling to HttpContext | Session-dependent business logic |
| Magic strings / numbers | Undocumented business constants |

## Technology-Specific Concerns

### .NET Remoting → Replace with REST/gRPC
### COM Interop → Identify native dependencies
### Crystal Reports → Map to reporting service
### SSRS Integration → Reporting requirements
### Active Directory auth → Map to OAuth2/OIDC
### Windows Authentication → Identify auth flow for modernization

## What to Extract — Summary

1. `[ServiceContract]` / Controller actions → service operations
2. `[DataContract]` / Entity classes → domain models
3. Validation attributes and manual checks → business rules
4. `[Authorize]` / role checks → authorization rules
5. Stored procedure bodies → data-level business logic
6. Config values (appSettings) → business parameters
7. MSMQ / event patterns → integration points
8. Timer / scheduled tasks → batch processing rules
9. Transaction scopes → consistency boundaries
10. Exception types / fault contracts → error handling contracts
