# 🛒 ProductStore — Enterprise Multi-Vendor E-Commerce Platform

[![Live Demo](https://img.shields.io/badge/Live%20Demo-productstore.runasp.net-2ea44f?style=for-the-badge&logo=googlechrome&logoColor=white)](http://productstore.runasp.net/)
[![.NET 8](https://img.shields.io/badge/.NET%208.0%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core%20MVC-512BD4?style=for-the-badge&logo=microsoft&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![Entity Framework Core 8](https://img.shields.io/badge/EF%20Core%208.0-512BD4?style=for-the-badge&logo=nuget&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![Microsoft SQL Server](https://img.shields.io/badge/Microsoft%20SQL%20Server-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![JWT Authentication](https://img.shields.io/badge/Auth-JWT%20Bearer%20%2B%20PBKDF2-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)](https://jwt.io/)

A full-stack, enterprise-grade **Multi-Vendor E-Commerce Web Application** built using **ASP.NET Core 8.0 MVC**, **Entity Framework Core 8**, **Microsoft SQL Server**, and **cryptographic JSON Web Token (JWT) Authentication**. 

The platform supports a 3-tier Role-Based Access Control model (**Master Admin**, **Product Owner / Merchant**, and **Customer**), inventory synchronization, shopping cart state management, and a versatile **multi-channel payment system** (Mobile Banking via bKash, Nagad, Rocket with TrxID audit verification, Direct Bank Wire Transfers, and Cash on Delivery).

---

## 🌐 Live Production Deployment

* **Production URL**: [http://productstore.runasp.net](http://productstore.runasp.net/)
* **Database Host**: MonsterASP.NET Managed Microsoft SQL Server (`db76499.databaseasp.net`)
* **Environment**: Windows Server IIS + ASP.NET Core Hosting Bundle

### Pre-Configured Test Personas

You can test any role directly using the credentials below:

| Persona | Email | Password | Permissions & Dashboard |
| :--- | :--- | :--- | :--- |
| 🛡️ **Master Admin** | `admin@productstore.com` | `Admin@123` | Full platform control: revenue analytics, 1-click TrxID verification, user banning, role elevation, and SQL View reports. |
| 🏪 **Product Owner** | `seller@productstore.com` | `Seller@123` | Seller Hub: launch products, stock management, tenant data isolation, and customer sales order tracking. |
| 🛍️ **Customer** | `customer@productstore.com` | `Customer@123` | Storefront catalog, dynamic shopping cart, multi-channel checkout (bKash/Bank/COD), and live order tracking. |

> [!TIP]
> The login screen at `http://productstore.runasp.net/Account/Login` includes **1-click quick-fill profile buttons** to instantly log in as Admin, Seller, or Customer without typing.

---

## 📑 Table of Contents

- [System Architecture](#-system-architecture)
- [Authentication & Security Flow](#-authentication--security-flow)
- [Order & Multi-Channel Payment Lifecycle](#-order--multi-channel-payment-lifecycle)
- [Database Entity-Relationship (ER) Diagram](#-database-entity-relationship-er-diagram)
- [Role-Based Access Control (RBAC) Matrix](#-role-based-access-control-rbac-matrix)
- [Technology Stack & Architectural Rationale](#-technology-stack--architectural-rationale)
- [Project Directory Structure](#-project-directory-structure)
- [REST API Endpoints](#-rest-api-endpoints)
- [Local Development Setup](#-local-development-setup)
- [Database Migrations & SQL Views](#-database-migrations--sql-views)
- [License & Authors](#-license--authors)

---

## 🏛️ System Architecture

ProductStore follows the **Layered Clean MVC Architecture**, enforcing separation of concerns between client presentation, HTTP controllers, application business domain services, and database persistence.

```mermaid
flowchart TD
    subgraph ClientLayer ["Client & Presentation Layer"]
        Browser["User Browser (Desktop / Mobile)"]
        UI_Views["Razor View Engine (.cshtml)"]
        UI_Components["Bootstrap 5 + FontAwesome 6 UI"]
    end

    subgraph MiddlewareLayer ["ASP.NET Core HTTP Request Pipeline"]
        Kestrel["Kestrel / IIS Reverse Proxy"]
        HTTPS["HTTPS Redirection & Static Files (wwwroot)"]
        JwtFilter["JWT Authentication & Cookie Interceptor"]
        RoutingAuth["Endpoint Routing & Role Authorization"]
    end

    subgraph ControllerLayer ["Controller Layer (Application Routing)"]
        AccCtrl["AccountController<br/>(Auth, JWT, Profile)"]
        HomeCtrl["HomeController<br/>(Storefront, Search, Filter)"]
        ProdCtrl["ProductsController<br/>(Catalog, Multipart Uploads)"]
        CartCtrl["CartController<br/>(Session & DB Cart)"]
        ChkCtrl["CheckoutController<br/>(Payment Processing)"]
        OrdCtrl["OrdersController<br/>(Invoices, Tracking)"]
        AdmCtrl["AdminController<br/>(KPIs, Verifications, Users)"]
        SelCtrl["SellerController<br/>(Seller Hub, Sales)"]
    end

    subgraph DomainServices ["Domain Services Layer"]
        JwtSvc["JwtService<br/>(HMAC-SHA256 Token Engine)"]
        HashSvc["PasswordHasher<br/>(PBKDF2 SHA-512 + Salt)"]
        CartSvc["CartService<br/>(Inventory Boundary Enforcer)"]
        DbInit["DbInitializer<br/>(Auto-Migrations & SQL Views)"]
    end

    subgraph DataAccessLayer ["Persistence Layer (EF Core 8 ORM)"]
        DbCtx["ApplicationDBContext"]
        Entities["Domain Models (Product, Order, Payment...)"]
    end

    subgraph DatabaseLayer ["Database Engine (Microsoft SQL Server)"]
        Tables[("Relational Tables<br/>(Users, Products, Orders, Payments...)")]
        SQLView[("Native SQL View<br/>[dbo].[vw_OrderSummaries]")]
        DiskStorage[("Physical Disk Storage<br/>wwwroot/ProductImages/")]
    end

    Browser <-->|HTTP/HTTPS Requests| Kestrel
    Kestrel --> HTTPS --> JwtFilter --> RoutingAuth
    RoutingAuth --> ControllerLayer
    ControllerLayer --> DomainServices
    ControllerLayer --> DataAccessLayer
    DomainServices --> DataAccessLayer
    DataAccessLayer --> DatabaseLayer
    ProdCtrl -.->|Write Uploaded Stream| DiskStorage
    UI_Views --- UI_Components
    UI_Views -.->|HTML5 SSR Response| Browser
```

---

## 🔐 Authentication & Security Flow

The platform utilizes a **Hybrid JWT Architecture**:
* **JSON Web Tokens (HMAC-SHA256)** are generated upon login and stored in an `HttpOnly`, `SameSite=Lax` secure cookie.
* This allows standard browser navigation, form submissions, and page refreshes to remain seamlessly authenticated without requiring client-side JavaScript headers on every link.
* Concurrently, the token is returned in REST API responses for external integrations.

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Browser
    participant Controller as AccountController
    participant Hasher as PasswordHasher (PBKDF2)
    participant JwtEngine as JwtService
    participant DB as SQL Server (Users Table)

    Client->>Controller: POST /Account/Login (Email, Password, RememberMe)
    Controller->>DB: Query User by Email
    DB-->>Controller: Return User Entity (PasswordHash, Salt, Role, IsActive)

    alt Account Disabled (IsActive == false)
        Controller-->>Client: 403 / Error "Account suspended by Administrator"
    else Password Mismatch
        Controller->>Hasher: VerifyPassword(rawPassword, storedHash)
        Hasher-->>Controller: False
        Controller-->>Client: Return 401 / Invalid Credentials
    else Authentication Successful
        Controller->>Hasher: VerifyPassword(rawPassword, storedHash)
        Hasher-->>Controller: True (Cryptographic match)
        Controller->>JwtEngine: GenerateToken(UserClaims: Id, Name, Email, Role)
        JwtEngine-->>Controller: Signed JWT String (HMAC-SHA256)
        
        Note over Controller: Writes token to HttpOnly Secure Cookie: jwt_token
        Controller->>Controller: Merge Guest Cart Items into User Account
        
        alt Role == Admin
            Controller-->>Client: 302 Redirect to /Admin/Index
        else Role == ProductOwner
            Controller-->>Client: 302 Redirect to /Seller/Index
        else Role == Customer
            Controller-->>Client: 302 Redirect to /Home/Index
        end
    end
```

### Password Hashing Specification
* **Algorithm**: PBKDF2 (Password-Based Key Derivation Function 2)
* **Underlying PRF**: HMAC-SHA512
* **Salt**: 128-bit (16 bytes) cryptographically secure random entropy via `RandomNumberGenerator`
* **Iteration Count**: 100,000 rounds
* **Comparison**: Constant-time execution via `CryptographicOperations.FixedTimeEquals` to eliminate side-channel timing attacks.

---

## 💳 Order & Multi-Channel Payment Lifecycle

```mermaid
flowchart TD
    Start(["Customer adds products to Cart"]) --> Checkout["Proceeds to Checkout (/Checkout)"]
    Checkout --> SelectPay{"Select Payment Method"}

    SelectPay -->|Mobile Banking| MobForm["Selects bKash / Nagad / Rocket<br/>Enters Sender Mobile No. & TrxID"]
    SelectPay -->|Direct Banking| BankForm["Selects City Bank / DBBL<br/>Enters Account No. & Deposit Reference"]
    SelectPay -->|Cash On Delivery| CODForm["Selects COD (Pay upon courier delivery)"]

    MobForm --> SubmitOrder["Submit Order"]
    BankForm --> SubmitOrder
    CODForm --> SubmitOrder

    SubmitOrder --> StockCheck{"Stock Available?"}
    StockCheck -->|No| RejectOrder["Reject with out-of-stock notification"]
    StockCheck -->|Yes| CreateRecords["1. Decrement StockQuantity<br/>2. Create Order (Status: Pending)<br/>3. Create Payment (Status: Pending)<br/>4. Empty Cart"]

    CreateRecords --> Confirmation["Invoice / Order Receipt Displayed"]

    Confirmation --> AdminReview{"Payment Method Review"}

    AdminReview -->|Mobile / Direct Bank| AdminVerif["Admin reviews bank ledger<br/>Matches TrxID in Admin Portal"]
    AdminReview -->|Cash on Delivery| Dispatch["Courier dispatches package"]

    AdminVerif -->|TrxID Matches| ApprovePay["Admin clicks 'Verify TrxID'<br/>Payment: Verified | Order: Processing"]
    AdminVerif -->|TrxID Invalid| RejectPay["Admin clicks 'Reject'<br/>Payment: Failed | Order: Cancelled"]

    ApprovePay --> Dispatch
    Dispatch --> Delivery["Courier delivers package"]
    Delivery --> Complete["Order marked Delivered<br/>Customer tracks timeline live"]
```

---

## 🗄️ Database Entity-Relationship (ER) Diagram

The relational schema is managed through Entity Framework Core Code-First migrations with referential integrity constraints, unique indexes, and cascading behaviors.

```mermaid
erDiagram
    USERS ||--o{ PRODUCTS : "launches / owns"
    USERS ||--o{ ORDERS : "places"
    USERS ||--o{ CART_ITEMS : "maintains"
    PRODUCTS ||--o{ ORDER_ITEMS : "included in"
    PRODUCTS ||--o{ CART_ITEMS : "added to"
    ORDERS ||--|{ ORDER_ITEMS : "contains"
    ORDERS ||--|| PAYMENTS : "settled via"

    USERS {
        int Id PK "Auto Increment"
        string FullName "nvarchar(100)"
        string Email "nvarchar(150), Unique Index"
        string PasswordHash "nvarchar(max), PBKDF2"
        string Role "nvarchar(30), Admin | ProductOwner | Customer"
        string PhoneNumber "nvarchar(20), Nullable"
        string Address "nvarchar(250), Nullable"
        bit IsActive "Default: 1 (Soft ban switch)"
        datetime2 CreatedAt "UTC Timestamp"
    }

    PRODUCTS {
        int Id PK "Auto Increment"
        string Name "nvarchar(100)"
        string Brand "nvarchar(100)"
        string Category "nvarchar(100)"
        decimal Price "decimal(10,2)"
        string Description "nvarchar(max)"
        string ImageFileName "nvarchar(100)"
        int StockQuantity "Default: 20"
        bit IsActive "Default: 1"
        int OwnerId FK "Nullable, OnDelete: SetNull"
        datetime2 CreatedAt "UTC Timestamp"
    }

    CART_ITEMS {
        int Id PK "Auto Increment"
        string CartSessionId "nvarchar(100), Nullable (Guest tracking)"
        int UserId FK "Nullable (Member tracking)"
        int ProductId FK "OnDelete: Cascade"
        int Quantity "Quantity requested"
        datetime2 CreatedAt "UTC Timestamp"
    }

    ORDERS {
        int Id PK "Auto Increment"
        string OrderNumber "nvarchar(50), e.g. ORD-20261029-XXXX"
        int UserId FK "OnDelete: Cascade"
        datetime2 OrderDate "UTC Timestamp"
        decimal SubTotal "decimal(10,2)"
        decimal ShippingFee "decimal(10,2), Fixed: 50.00"
        decimal TotalAmount "decimal(10,2)"
        string OrderStatus "Pending | Processing | Shipped | Delivered | Cancelled"
        string ShippingFullName "nvarchar(100)"
        string ShippingEmail "nvarchar(150)"
        string ShippingPhone "nvarchar(30)"
        string ShippingAddress "nvarchar(300)"
        string OrderNotes "nvarchar(500), Nullable"
    }

    ORDER_ITEMS {
        int Id PK "Auto Increment"
        int OrderId FK "OnDelete: Cascade"
        int ProductId FK "OnDelete: Cascade"
        decimal UnitPrice "decimal(10,2) (Price snapshot)"
        int Quantity "Units bought"
        decimal TotalPrice "decimal(10,2)"
    }

    PAYMENTS {
        int Id PK "Auto Increment"
        int OrderId FK "Unique Index, OnDelete: Cascade"
        string PaymentMethod "MobileBanking | DirectBanking | CashOnDelivery"
        string Provider "bKash | Nagad | Rocket | City Bank | COD"
        string AccountNumber "nvarchar(60), Nullable"
        string TransactionId "nvarchar(100), TrxID / Ref"
        decimal Amount "decimal(10,2)"
        string Status "Pending | Verified | Failed | Refunded"
        datetime2 PaymentDate "UTC Timestamp"
        datetime2 VerifiedAt "Nullable"
        string AdminNotes "nvarchar(300), Nullable"
    }

    ORDER_SUMMARY_VIEW {
        int OrderId "From Orders"
        string OrderNumber "From Orders"
        datetime2 OrderDate "From Orders"
        decimal TotalAmount "From Orders"
        string OrderStatus "From Orders"
        string CustomerName "From Users"
        string CustomerEmail "From Users"
        string PaymentMethod "From Payments"
        string PaymentStatus "From Payments"
        string TransactionId "From Payments"
        int TotalItems "Aggregated SUM(Quantity)"
    }
```

---

## 👥 Role-Based Access Control (RBAC) Matrix

| Feature / Action | Anonymous Guest | Customer | Product Owner (Seller) | Master Admin |
| :--- | :---: | :---: | :---: | :---: |
| **Browse Catalog, Filter & Search** | ✅ | ✅ | ✅ | ✅ |
| **Add to Cart & Session Persistence** | ✅ | ✅ | ✅ | ✅ |
| **Checkout & Submit Payments** | ❌ (Prompts Login) | ✅ | ✅ | ✅ |
| **View Personal Orders & Invoices** | ❌ | ✅ | ✅ | ✅ |
| **Update Personal Profile / Address** | ❌ | ✅ | ✅ | ✅ |
| **Seller Hub (Sales Analytics)** | ❌ | ❌ | ✅ | ✅ |
| **Launch Products (With File Upload)** | ❌ | ❌ | ✅ | ✅ |
| **Edit/Delete Owned Products** | ❌ | ❌ | ✅ (Own Only) | ✅ (All) |
| **1-Click TrxID Payment Verification** | ❌ | ❌ | ❌ | ✅ |
| **Update Order Fulfillment Status** | ❌ | ❌ | ❌ | ✅ |
| **User Ban / Activation Toggle** | ❌ | ❌ | ❌ | ✅ |
| **User Role Elevation / Demotion** | ❌ | ❌ | ❌ | ✅ |
| **Query Database View Reports** | ❌ | ❌ | ❌ | ✅ |

---

## 🛠️ Technology Stack & Architectural Rationale

| Category | Technology | Rationale |
| :--- | :--- | :--- |
| **Runtime & Core** | **.NET 8 LTS (C# 12)** | Long Term Support, optimal JIT compilation throughput, modern asynchronous pipelines (`async`/`await`). |
| **Web Framework** | **ASP.NET Core MVC** | Clean Model-View-Controller abstraction, strongly typed Razor views, Tag Helpers, and built-in Dependency Injection. |
| **Database ORM** | **Entity Framework Core 8** | High-performance ORM supporting Code-First migrations, Linq-to-Entities query translation, and native Keyless Views. |
| **Database Engine** | **Microsoft SQL Server** | Enterprise relational DBMS, ACID transaction guarantees, clustered primary keys, and view materialization. |
| **Authentication** | **JWT Bearer + PBKDF2** | Cryptographic token signing (HMAC-SHA256) combined with `HttpOnly` cookie delivery to defeat Cross-Site Scripting (XSS). |
| **Frontend Styling** | **Bootstrap 5.3 + FontAwesome 6** | Mobile-first responsive grid system, flexible modal dialogs, and SVG icon telemetry without heavy JavaScript dependencies. |
| **Containerization** | **Multi-Stage Dockerfile** | Self-contained Linux container compilation (`mcr.microsoft.com/dotnet/sdk:8.0` build ➔ `dotnet/aspnet:8.0` runtime). |

---

## 📁 Project Directory Structure

```text
ProductStore/
├── .github/                       # CI/CD Workflows
├── ProductStore/                  # Main Web Project Root
│   ├── Controllers/               # MVC Controllers
│   │   ├── AccountController.cs   # Authentication, Registration, JWT issuing, Profile
│   │   ├── AdminController.cs     # KPI Dashboards, Payment Verification, User Management
│   │   ├── CartController.cs      # Cart additions, quantity updates, badge counter
│   │   ├── CheckoutController.cs  # Mobile Banking, Direct Bank, COD processing
│   │   ├── HomeController.cs      # Catalog landing page, search, filtering
│   │   ├── OrdersController.cs    # Customer order history, invoice details
│   │   ├── ProductsController.cs  # CRUD catalog operations, file upload handling
│   │   └── SellerController.cs    # Seller inventory tracking, customer sales orders
│   ├── Migrations/                # EF Core Code-First Database Migrations
│   │   ├── 20241205133138_FirstMigration.cs
│   │   └── 20261027182317_AddEcommerceAndAuth.cs
│   ├── Models/                    # Domain Entities & ViewModels
│   │   ├── CartItem.cs            # Cart model
│   │   ├── Order.cs               # Order model
│   │   ├── OrderItem.cs           # Order line item model
│   │   ├── OrderSummaryView.cs    # Keyless mapping to SQL View vw_OrderSummaries
│   │   ├── Payment.cs             # Multi-channel payment model
│   │   ├── Product.cs             # Database product entity
│   │   ├── ProductDto.cs          # Form Data Transfer Object with IFormFile
│   │   ├── User.cs                # User account entity with Role constants
│   │   └── ViewModels/            # Strongly-typed presentation ViewModels
│   ├── Properties/                # Launch settings and Web Deploy profiles
│   ├── Services/                  # Application Domain Services
│   │   ├── ApplicationDBContext.cs# EF Core DbContext with Fluent API rules
│   │   ├── CartService.cs         # Cart state & guest-to-member merger
│   │   ├── DbInitializer.cs       # Auto-migration, SQL View creator, seed engine
│   │   ├── JwtService.cs          # HMAC-SHA256 JWT Token generator & validator
│   │   └── PasswordHasher.cs      # PBKDF2 SHA-512 cryptographic hasher
│   ├── Views/                     # Razor Presentation Views (.cshtml)
│   │   ├── Account/               # Login, Register, Profile, AccessDenied
│   │   ├── Admin/                 # Dashboard, Orders, Users, SQL Reports
│   │   ├── Cart/                  # Shopping cart view with quantity selectors
│   │   ├── Checkout/              # Multi-channel payment checkout & invoice
│   │   ├── Home/                  # Storefront showcase
│   │   ├── Orders/                # Order history and printable invoices
│   │   ├── Products/              # Details, Create, Edit, Index
│   │   ├── Seller/                # Seller inventory hub & customer sales
│   │   └── Shared/                # Master _Layout.cshtml with dynamic cart badge
│   ├── wwwroot/                   # Web Root (Static Files)
│   │   ├── css/ & js/             # Custom site styling and client scripts
│   │   └── ProductImages/         # Uploaded product image files
│   ├── appsettings.json           # Production configuration & connection strings
│   ├── appsettings.Development.json # Local development connection overrides
│   ├── Program.cs                 # Application bootstrap, DI container, Middleware
│   └── ProductStore.csproj        # Dependencies & compilation target (.NET 8)
├── Dockerfile                     # Multi-stage production container manifest
├── ProductStore.sln               # Solution manifest
└── README.md                      # Enterprise documentation
```

---

## 📡 REST API Endpoints

While designed as a Server-Side Rendered (SSR) MVC application, the platform exposes headless REST APIs for client scripts or mobile consumers:

| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Public | Authenticates credentials and returns raw `{ success, token, role, fullName }` JSON. |
| `GET` | `/Cart/Count` | Public | Returns real-time cart badge item count `{ count: X }` JSON. |
| `POST` | `/Cart/AddToCart` | Public (AJAX) | Asynchronously appends item to cart without refreshing page. |

---

## 💻 Local Development Setup

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Microsoft SQL Server](https://www.microsoft.com/sql-server) (LocalDB, Express, or Developer edition)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

### 1. Clone & Restore
```bash
git clone https://github.com/Bahar0900/Productstore.git
cd Productstore
dotnet restore
```

### 2. Configure Database Connection
Review `ProductStore/appsettings.Development.json` to verify your local SQL Server instance:
```json
{
  "ConnectionStrings": {
    "defaultConnection": "Data Source=.;Initial Catalog=productstoreDB;Integrated Security=True;Trust Server Certificate=True"
  }
}
```

### 3. Run Migrations & Start Server
```bash
# Apply migrations to create the local database
dotnet ef database update --project ProductStore

# Run the development server
dotnet run --project ProductStore
```

The application will start listening at:
* **HTTP**: `http://localhost:5246`
* **HTTPS**: `https://localhost:7178`

---

## 📄 License & Authors

* **Repository**: [https://github.com/Bahar0900/Productstore](https://github.com/Bahar0900/Productstore)
* **Author**: Md. Sagor Chowdhury ([@Bahar0900](https://github.com/Bahar0900))
* **License**: MIT Open Source License
