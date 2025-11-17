# Invoicing System API

A simple invoicing system built with ASP.NET Core 8 and SQL Server for managing customers and invoices with email notifications.

## Features

- Customer management (CRUD operations)
- Invoice creation and management
- **Real email notifications** using MailKit (with simulation mode)
- Professional HTML email templates
- API key authentication
- SQL Server database integration
- Search and filtering capabilities
- Email testing endpoint

## Technology Stack

- ASP.NET Core 8
- SQL Server
- Entity Framework Core 9
- API Key Authentication
- Swagger/OpenAPI  

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB/Express/Full)

### Setup
1. Clone the repository
2. Restore packages: `dotnet restore`
3. Update database: `dotnet ef database update`
4. Run the application: `dotnet run`
5. Access Swagger UI at: `https://localhost:5001/swagger`

## Authentication

All API endpoints require an API key in the header:
```
X-API-Key: invoicing-api-key-2024
```

Available API keys:
- `invoicing-api-key-2024` (Production)
- `dev-api-key-12345` (Development)

## API Endpoints

### Customers
- `POST /api/customers` - Create customer
- `GET /api/customers` - Get all customers
- `GET /api/customers/{id}` - Get customer by ID
- `PUT /api/customers/{id}` - Update customer

### Invoices
- `POST /api/invoices` - Create invoice (auto-sends email)
- `GET /api/invoices/{id}` - Get invoice by ID
- `PUT /api/invoices/{id}` - Update invoice
- `PUT /api/invoices/{id}/void` - Void invoice
- `GET /api/invoices/search` - Search invoices
- `POST /api/invoices/{id}/resend-email` - Resend email
- `POST /api/invoices/test-email` - Send test email

## Example Usage

### Create a Customer
```bash
curl -X POST "https://localhost:5001/api/customers" \
  -H "X-API-Key: invoicing-api-key-2024" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "+1-555-0123",
    "billingAddress": "123 Main St",
    "city": "New York",
    "state": "New York",
    "zipCode": "10001",
    "country": "USA"
  }'
```

### Create an Invoice
```bash
curl -X POST "https://localhost:5001/api/invoices" \
  -H "X-API-Key: invoicing-api-key-2024" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "dueDate": "2025-12-31T00:00:00",
    "taxRate": 8.25,
    "notes": "Thank you for your business!",
    "items": [
      {
        "productName": "Professional Services",
        "quantity": 10,
        "unitPrice": 150.00,
        "unit": "hours"
      }
    ]
  }'
```

## Database

The system uses SQL Server with three main tables:
- **Customers** - Customer information and addresses
- **Invoices** - Invoice headers with totals and status
- **InvoiceItems** - Individual line items for each invoice

## Configuration

Update `appsettings.json` for database connection:
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=(localdb)\\mssqllocaldb;Database=InvoicingSystemDb;Trusted_Connection=true"
  },
  "ApiSettings": {
    "ApiKeys": [
      "invoicing-api-key-2024",
      "dev-api-key-12345"
    ]
  }
}
```

## Testing

1. Run the application: `dotnet run`
2. Open Swagger UI: `https://localhost:5001/swagger`
3. Click "Authorize" and enter API key: `invoicing-api-key-2024`
4. Test the endpoints
