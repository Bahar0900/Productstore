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
- [Database Schema & Entity Relationships](#-database-schema--entity-relationships)
- [Role-Based Access Control (RBAC) Matrix](#-role-based-access-control-rbac-matrix)
- [Technology Stack & Architectural Rationale](#-technology-stack--architectural-rationale)
- [Project Directory Structure](#-project-directory-structure)
- [REST API Endpoints](#-rest-api-endpoints)
- [Local Development Setup](#-local-development-setup)
- [Database Migrations & SQL Views](#-database-migrations--sql-views)
- [License & Authors](#-license--authors)

---

## 🏛️ System Architecture

ProductStore follows a modular **Clean MVC Architecture** with explicit separation between presentation, middleware security, business services, and data storage.

### 1. High-Level Macro Architecture

```mermaid
flowchart LR
    classDef client fill:#ffffff,stroke:#2563eb,stroke-width:2px,color:#0f172a;
    classDef app fill:#f8fafc,stroke:#0284c7,stroke-width:2px,color:#0f172a;
    classDef orm fill:#ffffff,stroke:#7c3aed,stroke-width:2px,color:#0f172a;
    classDef data fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#0f172a;

    Client["🌐 Client Tier<br/>Browser / Mobile<br/>(HTML5 / Bootstrap 5)"]:::client
    App["⚙️ Application Tier<br/>ASP.NET Core 8 MVC<br/>(JWT / Controllers)"]:::app
    ORM["⚡ Data Access Tier<br/>EF Core 8 ORM<br/>(LINQ / Migrations)"]:::orm
    DB[("🗄️ Storage Tier<br/>Microsoft SQL Server<br/>(Tables / SQL Views)")]:::data
    Disk[("📁 Static File Storage<br/>wwwroot/ProductImages<br/>(Media Files)")]:::data

    Client <-->|"HTTPS Requests / HTML5"| App
    App <-->|"C# Method Invocations"| ORM
    ORM <-->|"T-SQL Queries / Transactions"| DB
    App -.->|"Save Uploaded Images"| Disk
```

### 2. Request & Middleware Execution Pipeline

```mermaid
flowchart LR
    classDef step fill:#ffffff,stroke:#64748b,stroke-width:2px,color:#0f172a;
    classDef auth fill:#eff6ff,stroke:#2563eb,stroke-width:2px,color:#1e3a8a;
    classDef dest fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#14532d;

    Req["🌐 HTTP Request"]:::step --> SSL["🔒 HTTPS & Static Files"]:::step
    SSL --> JWT["🔐 JWT Cookie Interceptor"]:::auth
    JWT --> RBAC["🛡️ Role Authorization Filter"]:::auth
    RBAC --> Ctrl["🎯 Target Controller Action"]:::dest
    Ctrl --> View["📄 Razor View Engine"]:::dest
    View --> Res["🚀 HTTP 200 Response"]:::step
```

---

## 🔐 Authentication & Security Flow

The platform utilizes a **Hybrid JWT Architecture**:
* **JSON Web Tokens (HMAC-SHA256)** are generated upon login and stored in an `HttpOnly`, `SameSite=Lax` secure cookie.
* This allows standard browser navigation, form submissions, and page refreshes to remain seamlessly authenticated without requiring client-side JavaScript headers on every link.
* Concurrently, the token is returned in REST API responses for external integrations.

### 1. User Sign-In & Token Issuance Flow

```mermaid
sequenceDiagram
    autonumber
    actor User as 👤 User
    participant Controller as ⚙️ AccountController
    participant Hasher as 🔒 PasswordHasher
    participant JwtService as 🔑 JwtService
    participant DB as 🗄️ SQL Server

    User->>Controller: POST /Account/Login (Email, Password)
    Controller->>DB: Query User by Email
    DB-->>Controller: Return User Record (Hash, Salt, Role, IsActive)

    alt Password Matches and Account is Active
        Controller->>Hasher: VerifyPassword(raw, hash)
        Hasher-->>Controller: True (PBKDF2 Match)
        Controller->>JwtService: GenerateToken(Claims: Id, Name, Role)
        JwtService-->>Controller: Signed JWT (HMAC-SHA256)
        Controller->>User: Set-Cookie: jwt_token (HttpOnly, Secure)
        Controller-->>User: 302 Redirect to Role Dashboard
    else Invalid Credentials or Deactivated Account
        Controller-->>User: Display Error Message
    end
```

### 2. Request Authorization Verification Flow

```mermaid
flowchart TD
    classDef check fill:#ffffff,stroke:#0284c7,stroke-width:2px,color:#0f172a;
    classDef grant fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#14532d;
    classDef deny fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;

    Req["🌐 User Navigates to Protected Route"]:::check --> CookieCheck{"Cookie jwt_token Present?"}:::check

    CookieCheck -->|"No"| RedirectLogin["302 Redirect to /Account/Login"]:::deny
    CookieCheck -->|"Yes"| VerifyToken{"Valid Signature & Not Expired?"}:::check

    VerifyToken -->|"Invalid"| ClearCookie["Clear Cookie and Redirect to /Account/Login"]:::deny
    VerifyToken -->|"Valid"| RoleCheck{"Matches Required Role?"}:::check

    RoleCheck -->|"Role Authorized"| Execute["Execute Action & Render Dashboard"]:::grant
    RoleCheck -->|"Role Unauthorized"| AccessDenied["302 Redirect to /Account/AccessDenied"]:::deny
```

### Password Hashing Specification
* **Algorithm**: PBKDF2 (Password-Based Key Derivation Function 2)
* **Underlying PRF**: HMAC-SHA512
* **Salt**: 128-bit (16 bytes) cryptographically secure random entropy via `RandomNumberGenerator`
* **Iteration Count**: 100,000 rounds
* **Comparison**: Constant-time execution via `CryptographicOperations.FixedTimeEquals` to eliminate side-channel timing attacks.

---

## 💳 Order & Multi-Channel Payment Lifecycle

### 1. Customer Checkout & Order Creation

```mermaid
flowchart LR
    classDef action fill:#ffffff,stroke:#2563eb,stroke-width:2px,color:#0f172a;
    classDef pay fill:#eff6ff,stroke:#0284c7,stroke-width:2px,color:#1e3a8a;
    classDef complete fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#14532d;

    Cart["🛒 Items in Cart"]:::action --> SelectPay{"💳 Payment Method"}:::pay

    SelectPay -->|"Mobile Banking"| Mob["📱 bKash / Nagad / Rocket<br/>Sender Mobile & TrxID"]:::pay
    SelectPay -->|"Direct Banking"| Bank["🏦 City Bank / DBBL<br/>Account No. & Ref ID"]:::pay
    SelectPay -->|"Cash On Delivery"| COD["💵 Cash On Delivery<br/>Pay upon delivery"]:::pay

    Mob --> OrderGen["📦 Validate Stock & Create Order"]:::action
    Bank --> OrderGen
    COD --> OrderGen

    OrderGen --> Confirm["🧾 Printable Invoice Displayed"]:::complete
```

### 2. Payment Verification & Order Fulfillment

```mermaid
flowchart LR
    classDef pending fill:#fffbeb,stroke:#f59e0b,stroke-width:2px,color:#78350f;
    classDef admin fill:#eff6ff,stroke:#2563eb,stroke-width:2px,color:#1e3a8a;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#14532d;
    classDef fail fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;

    PendingOrder["⏳ Order: Pending<br/>Payment: Pending"]:::pending --> AdminReview["🛡️ Admin Audits TrxID"]:::admin

    AdminReview -->|"TrxID Verified"| Verified["✅ 1-Click Verify TrxID<br/>Payment: Verified<br/>Order: Processing"]:::success
    AdminReview -->|"TrxID Invalid"| Rejected["❌ Reject Payment<br/>Payment: Failed<br/>Order: Cancelled"]:::fail

    Verified --> Dispatch["🚚 Courier Dispatches Order<br/>Order: Shipped"]:::success
    Dispatch --> Delivered["📦 Customer Receives Package<br/>Order: Delivered"]:::success
```

---

## 🗄️ Database Schema & Entity Relationships

The relational schema is structured with strict referential integrity, foreign keys, and indexes in Microsoft SQL Server.

### Entity Relationship Map

```mermaid
flowchart TD
    classDef user fill:#eff6ff,stroke:#2563eb,stroke-width:2px,color:#1e3a8a;
    classDef product fill:#fefce8,stroke:#ca8a04,stroke-width:2px,color:#713f12;
    classDef order fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#14532d;
    classDef payment fill:#faf5ff,stroke:#9333ea,stroke-width:2px,color:#581c87;
    classDef cart fill:#f8fafc,stroke:#64748b,stroke-width:2px,color:#0f172a;

    U["👤 USERS<br/>• Id (PK)<br/>• FullName, Email (Unique)<br/>• PasswordHash, Role, IsActive"]:::user
    P["📦 PRODUCTS<br/>• Id (PK)<br/>• Name, Brand, Category, Price<br/>• StockQuantity, IsActive<br/>• OwnerId (FK)"]:::product
    C["🛒 CART_ITEMS<br/>• Id (PK)<br/>• CartSessionId, UserId (FK)<br/>• ProductId (FK), Quantity"]:::cart
    O["📋 ORDERS<br/>• Id (PK)<br/>• OrderNumber, UserId (FK)<br/>• TotalAmount, OrderStatus<br/>• ShippingAddress, Phone"]:::order
    OI["📑 ORDER_ITEMS<br/>• Id (PK)<br/>• OrderId (FK), ProductId (FK)<br/>• UnitPrice, Quantity, TotalPrice"]:::order
    PAY["💳 PAYMENTS<br/>• Id (PK)<br/>• OrderId (FK, Unique)<br/>• PaymentMethod, Provider<br/>• TransactionId, Status"]:::payment

    U -->|"1 : N (Owns)"| P
    U -->|"1 : N (Places)"| O
    U -->|"1 : N (Maintains)"| C
    P -->|"1 : N (Added to)"| C
    P -->|"1 : N (Included in)"| OI
    O -->|"1 : N (Contains)"| OI
    O -->|"1 : 1 (Paid via)"| PAY
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
