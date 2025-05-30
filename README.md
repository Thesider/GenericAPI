# Enhanced Generic API

A comprehensive, production-ready .NET 9 Web API featuring advanced security, monitoring, real-time communication, and enterprise-grade architecture patterns.

## 🚀 Features

### Core Functionality
- **RESTful API** with comprehensive CRUD operations
- **Entity Framework Core** with SQL Server support
- **JWT Authentication** with role-based authorization
- **AutoMapper** for object-object mapping
- **Swagger/OpenAPI** documentation with security definitions

### 🔒 Security Enhancements
- **Input Validation & Sanitization**
  - SQL injection prevention
  - XSS protection with HTML sanitization
  - File name sanitization
  - URL validation
- **Security Headers**
  - Content Security Policy (CSP)
  - HTTP Strict Transport Security (HSTS)
  - X-Frame-Options, X-Content-Type-Options
  - Custom security headers
- **Rate Limiting**
  - IP-based rate limiting
  - Endpoint-specific limits
  - Configurable thresholds and time windows
- **Audit Logging**
  - Comprehensive audit trail
  - Tamper-proof logging with integrity verification
  - Security event tracking

### 📊 Monitoring & Observability
- **Structured Logging** with Serilog
  - Console and file logging
  - Correlation ID tracking
  - Environment and machine enrichment
- **Metrics Collection** with Prometheus
  - HTTP request metrics
  - Business metrics (orders, users, products)
  - System metrics (database, cache, memory)
  - Custom metrics support
- **Health Checks**
  - Database connectivity
  - Redis availability
  - Detailed health reporting
  - Health checks UI at `/health-ui`

### 🏗️ Architectural Improvements
- **Multi-Tenancy Support**
  - Tenant isolation
  - Claim-based tenant resolution
  - Configurable tenant strategies
- **Event-Driven Architecture**
  - MediatR integration
  - Command/Query separation
  - Event handling patterns
- **Real-Time Features** with SignalR
  - Authenticated real-time communication
  - Group-based messaging
  - User notification system
- **Advanced Caching**
  - Redis distributed caching
  - Multi-layer caching strategy
  - Cache hit/miss metrics

### 🚀 Performance Optimizations
- **Database Optimizations**
  - Connection retry policies
  - Query timeout configuration
  - Bulk operations support
- **Response Compression**
- **Response Caching**
- **Background Services** for cleanup tasks

## 🛠️ Quick Start

### Prerequisites
- .NET 9 SDK
- SQL Server or LocalDB
- Redis (optional, for caching)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd GenericAPI
   ```

2. **Configure Database**
   Update `appsettings.json` with your connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GenericAPIDb;Trusted_Connection=true;"
     }
   }
   ```

3. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

4. **Configure Redis (Optional)**
   ```json
   {
     "Redis": {
       "ConnectionString": "localhost:6379"
     }
   }
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - Swagger UI: `https://localhost:5001/swagger`
   - Health Checks: `https://localhost:5001/health`
   - Health UI: `https://localhost:5001/health-ui`
   - Metrics: `https://localhost:5001/metrics`

## 📡 API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Token refresh

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create product (Auth required)
- `PUT /api/products/{id}` - Update product (Auth required)
- `DELETE /api/products/{id}` - Delete product (Auth required)

### Orders
- `GET /api/orders` - Get user orders (Auth required)
- `POST /api/orders` - Create order (Auth required)
- `GET /api/orders/{id}` - Get order details (Auth required)

### Admin
- `GET /api/admin/users` - Get all users (Admin only)
- `GET /api/admin/analytics` - Get analytics (Admin only)

## 🔧 Configuration

### Rate Limiting
Configure in `appsettings.json`:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### Security Headers
```json
{
  "Security": {
    "EnableSecurityHeaders": true,
    "EnableCSP": true,
    "EnableHSTS": true
  }
}
```

### Logging
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## 🔄 Real-Time Features

### SignalR Hub
Connect to `/hubs/notifications` for real-time updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
        accessTokenFactory: () => yourJwtToken
    })
    .build();

// Join a group
connection.invoke("JoinGroup", "Orders_Updates");

// Listen for notifications
connection.on("Notification", (data) => {
    console.log("Received notification:", data);
});
```

## 📈 Monitoring

### Prometheus Metrics
Available at `/metrics`:
- HTTP request duration and count
- Database query performance
- Cache hit/miss rates
- Business metrics (orders, registrations)
- System metrics (memory, CPU)

### Health Checks
- `/health` - Overall health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe
- `/health-ui` - Visual health dashboard

## 🔐 Security Features

### Input Sanitization
All user inputs are automatically sanitized:
- HTML content sanitization
- SQL injection prevention
- File name validation
- URL validation

### Audit Logging
All user actions are logged with:
- User identification
- Action details
- Timestamp
- IP address
- Integrity hash for tamper detection

### Security Headers
Automatically applied security headers:
- Content Security Policy
- HSTS
- X-Frame-Options
- X-Content-Type-Options
- Custom headers

## 🏢 Multi-Tenancy

Configure tenant resolution in `appsettings.json`:
```json
{
  "MultiTenant": {
    "TenantResolutionStrategy": "Claim",
    "EnableTenantIsolation": true
  }
}
```

## 📦 Architecture

### Project Structure
```
GenericAPI/
├── Controllers/          # API Controllers
├── Services/            # Business Logic
│   ├── Interfaces/      # Service Interfaces
│   └── Implementations/ # Service Implementations
├── Repositories/        # Data Access Layer
├── Models/             # Entity Models
├── DTOs/               # Data Transfer Objects
├── Middleware/         # Custom Middleware
├── Hubs/              # SignalR Hubs
├── HealthChecks/      # Health Check Implementations
├── Validators/        # Input Validators
├── BackgroundServices/ # Background Tasks
└── Helpers/           # Utility Classes
```

### Design Patterns
- **Repository Pattern** for data access
- **Service Layer Pattern** for business logic
- **Dependency Injection** throughout
- **CQRS** with MediatR
- **Observer Pattern** with events

## 🧪 Testing

### Health Check Testing
```bash
curl https://localhost:5001/health
```

### API Testing
Import the Swagger definition into Postman or use the included `GenericAPI.http` file with Visual Studio Code REST Client.

## 🚀 Deployment

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "GenericAPI.dll"]
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `ConnectionStrings__DefaultConnection` - Database connection
- `Redis__ConnectionString` - Redis connection
- `Jwt__Key` - JWT signing key

## 📊 Performance Considerations

### Database
- Connection pooling enabled
- Retry policies configured
- Query timeout optimization
- Bulk operations for large datasets

### Caching
- Multi-layer caching strategy
- Redis for distributed caching
- Memory cache for frequently accessed data
- Cache metrics and monitoring

### Monitoring
- Structured logging for performance analysis
- Metrics collection for bottleneck identification
- Health checks for dependency monitoring
- APM integration ready

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Implement your changes
4. Add tests
5. Update documentation
6. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

- Documentation: Check this README and inline code comments
- Issues: Use GitHub Issues for bug reports and feature requests
- Health Dashboard: Monitor application health at `/health-ui`
- Logs: Check structured logs in the `logs/` directory

## 🔮 Future Enhancements

- [ ] GraphQL support
- [ ] gRPC services
- [ ] Message queue integration (RabbitMQ/Kafka)
- [ ] Advanced analytics dashboard
- [ ] API versioning
- [ ] OAuth2/OpenID Connect integration
- [ ] Kubernetes deployment manifests
- [ ] CI/CD pipeline templates
