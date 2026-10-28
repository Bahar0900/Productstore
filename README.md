# ProductStore - Full-Stack E-Commerce Marketplace

A multi-vendor E-Commerce Web Application built using **ASP.NET Core 8.0 MVC**, **Entity Framework Core 8**, **Microsoft SQL Server**, **cryptographic JWT Authentication**, and a **multi-channel payment system** (Mobile Banking, Direct Bank Transfer, Cash on Delivery).

---

## 🌟 Key Features

### 🛍️ 1. Customer Storefront
* **Product Catalog**: Live search, filtering by category (Computers, Phones, Accessories, Printers), and sorting by price or newest.
* **Product Details**: High-resolution image showcase, brand specifications, verified merchant badge, and stock level indicators.
* **Shopping Cart**: Real-time quantity adjustments, inventory stock boundary enforcement, and dynamic cart badge.
* **Multi-Channel Checkout**:
  * **Mobile Banking**: bKash, Nagad, Rocket with merchant number guidelines, sender mobile account, and Transaction ID (TrxID) submission.
  * **Direct Banking**: Bank transfer details (Bank name, Account Number, Routing number) with deposit reference tracking.
  * **Cash On Delivery (COD)**: Payment collected upon physical courier delivery.
* **Order Tracking & Invoices**: Detailed order receipts with real-time status progression (`Pending` ➔ `Processing` ➔ `Shipped` ➔ `Delivered`).

### 🏪 2. Product Owner / Seller Hub
* **Seller Dashboard**: Real-time inventory overview, total units sold, gross sales revenue, and low-stock alerts (&le; 5 units).
* **Product Launching**: Create, edit, and manage products with image uploads and custom stock quantities.
* **Multi-Tenant Data Isolation**: Sellers can only modify and delete their own products.
* **Customer Sales Orders**: Track which customers bought their products and view fulfillment statuses.

### 🛡️ 3. Master Admin Control Center
* **Executive KPI Dashboard**: Lifetime revenue calculation, total order volume, pending payments, user count, and active products.
* **Payment Verification**: 1-Click TrxID verification to approve or reject mobile banking/bank transfer payments.
* **Order Management**: Update lifecycle statuses (`Processing`, `Shipped`, `Delivered`, `Cancelled`).
* **User & Role Administration**: Activate or suspend (ban) accounts; promote/demote users between `Customer`, `ProductOwner`, and `Admin`.
* **SQL Server View Reports**: Native execution of SQL Database View `vw_OrderSummaries` via EF Core keyless entities.

### 🔐 4. Authentication & Security
* **JSON Web Token (JWT)**: Cryptographically signed tokens (HMAC-SHA256) stored in `HttpOnly`, `SameSite=Lax` cookies for seamless MVC page transitions, while also available as raw Bearer tokens for REST API clients.
* **Password Hashing**: PBKDF2 with SHA-512, cryptographic random salt, and 100,000 iterations via `PasswordHasher`.
* **REST API**: Endpoint at `POST /api/auth/login` for mobile or external consumers.

---

## 🔑 Pre-Configured Test Accounts

| Role | Email | Password | Permissions |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@productstore.com` | `Admin@123` | Full administrative control, verify payments, manage users & orders |
| **Product Owner** | `seller@productstore.com` | `Seller@123` | Launch products, manage inventory, view product sales orders |
| **Customer** | `customer@productstore.com` | `Customer@123` | Browse catalog, add to cart, checkout (bKash/Bank/COD), track orders |

---

## 🛠️ Tech Stack

* **Framework**: ASP.NET Core 8.0 (.NET 8 LTS)
* **Architecture**: Model-View-Controller (MVC) + Service Layer + DTO Pattern
* **ORM**: Entity Framework Core 8.0 (Code-First Migrations)
* **Database**: Microsoft SQL Server
* **Authentication**: JWT Bearer + HttpOnly Secure Cookies
* **Frontend**: Razor Views (.cshtml), Bootstrap 5, FontAwesome 6, JavaScript

---

## 🚀 Running the Project Locally

1. **Clone the repository**:
   ```bash
   git clone https://github.com/Bahar0900/Productstore.git
   cd Productstore
   ```

2. **Configure Database Connection**:
   Update `ConnectionStrings:defaultConnection` in `ProductStore/appsettings.json` to point to your local SQL Server instance:
   ```json
   "ConnectionStrings": {
     "defaultConnection": "Data Source=.;Initial Catalog=productstoreDB;Integrated Security=True;Trust Server Certificate=True"
   }
   ```

3. **Apply Database Migrations & Seed**:
   ```bash
   dotnet ef database update --project ProductStore
   ```
   *(Note: The application also automatically applies migrations and seeds initial test users on first startup!)*

4. **Run the Application**:
   ```bash
   dotnet run --project ProductStore
   ```

5. **Open in Browser**:
   * **HTTP**: [http://localhost:5246](http://localhost:5246)
   * **HTTPS**: [https://localhost:7178](https://localhost:7178)
