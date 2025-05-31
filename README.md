# Enhanced Generic API

A comprehensive, production-ready .NET 8 Web API featuring advanced security, monitoring, real-time communication, and enterprise-grade architecture patterns.

## 🚀 Features Overview

This GenericAPI has been transformed from a basic web API into a production-ready, enterprise-grade solution with comprehensive features including advanced security, monitoring, validation, real-time capabilities, and scalability considerations.

### 🔒 Advanced Security
- **Enhanced Security Headers Middleware** - Complete HTTP security protection (CSP, HSTS, X-Frame-Options, etc.)
- **Input Sanitization Service** - XSS and injection attack prevention with HtmlSanitizer
- **Enhanced Error Handling** - Secure error responses with correlation tracking
- **Rate Limiting** - Smart IP-based throttling with AspNetCoreRateLimit
- **JWT Authentication** - Secure token-based authentication with role-based authorization
- **CORS Configuration** - Secure cross-origin resource sharing

### 📊 Comprehensive Monitoring & Observability
- **OpenTelemetry Integration** - Distributed tracing and metrics ready
- **Metrics Service** - Business and system metrics collection (requests, errors, performance)
- **Audit Logging Service** - Complete security and compliance audit trails
- **Enhanced Health Checks** - Detailed system monitoring with UI dashboard
- **Prometheus Metrics** - Production-ready metrics endpoint at `/metrics`
- **Structured Logging** - Serilog with enrichment and correlation IDs

### ⚡ Real-time Features
- **SignalR Hub** - Real-time notifications and messaging with JWT authentication
- **Multi-channel Notification Service** - Email, SMS, push, in-app notifications
- **WebSocket Support** - Bi-directional real-time communication

### 🏗️ Enterprise Architecture
- **FluentValidation Integration** - Advanced validation rules for Products, Orders, and Auth
- **Multi-tenancy Support** - Tenant isolation with Finbuckle.MultiTenant
- **Background Services** - Automated maintenance and cleanup tasks
- **Distributed Caching** - Redis-based scalable caching with failover
- **Event-driven Architecture** - MediatR integration for clean separation
- **Repository Pattern** - Clean data access abstraction

### 🚀 Performance Optimizations
- **Database Optimizations** - Connection pooling, retry policies, bulk operations
- **Response Compression** - Reduce bandwidth usage
- **Cache Strategy** - Multi-layer caching with Redis clustering
- **Async Operations** - Non-blocking I/O throughout

## 📁 Enhanced Project Structure

```
GenericAPI/
├── Controllers/          # API Controllers with enhanced security
├── Services/            
│   ├── Interfaces/      # IInputSanitizerService, IAuditLogService, IMetricsService, INotificationService
│   └── Implementations/ # InputSanitizerService, AuditLogService, MetricsService, NotificationService
├── Validators/          # NEW: FluentValidation rules
│   ├── ProductValidators.cs      # Product validation rules
│   ├── OrderValidators.cs        # Order validation rules
│   ├── AuthValidators.cs         # Authentication validation
│   └── CustomValidationAttributes.cs  # Reusable validation components
├── Middleware/          # NEW: Enhanced middleware
│   ├── SecurityHeadersMiddleware.cs      # Security headers
│   └── ErrorHandlingMiddleware.cs       # Advanced error handling with correlation IDs
├── HealthChecks/        # NEW: System monitoring
│   └── RedisHealthCheck.cs              # Redis connectivity monitoring
├── Hubs/               # NEW: Real-time features
│   └── NotificationHub.cs               # SignalR real-time hub
├── BackgroundServices/  # NEW: Automated tasks
│   └── CleanupBackgroundService.cs     # Automated maintenance
├── Repositories/        # Data Access Layer
├── Models/             # Entity Models
├── DTOs/               # Data Transfer Objects
└── Helpers/           # Utility Classes
```

## 🛠️ Quick Start Guide

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Redis Server
- Visual Studio 2022 or VS Code

### Installation & Setup

1. **Clone and Navigate**
   ```bash
   git clone <repository-url>
   cd GenericAPI
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure Environment**
   Create or update `.env` file:
   ```env
   # Database
   DB_CONNECTION_STRING=Server=(localdb)\\mssqllocaldb;Database=GenericApiDb;Trusted_Connection=true;MultipleActiveResultSets=true
   DB_READ_CONNECTION_STRING=Server=(localdb)\\mssqllocaldb;Database=GenericApiDb;Trusted_Connection=true;MultipleActiveResultSets=true

   # Redis
   REDIS_CONNECTION_STRING=localhost:6379

   # JWT
   JWT_KEY=your-super-secret-jwt-key-here-must-be-at-least-32-characters
   JWT_ISSUER=GenericAPI
   JWT_AUDIENCE=GenericAPI-Users
   JWT_EXPIRE_MINUTES=60

   # Email (Optional)
   SMTP_HOST=smtp.gmail.com
   SMTP_PORT=587
   SMTP_USERNAME=your-email@gmail.com
   SMTP_PASSWORD=your-app-password

   # Security
   ENCRYPTION_KEY=your-32-character-encryption-key-here
   API_KEY=your-api-key-for-external-calls

   # Features
   ENABLE_RATE_LIMITING=true
   ENABLE_AUDIT_LOGGING=true
   ENABLE_REAL_TIME_NOTIFICATIONS=true
   ```

4. **Update Database**
   ```bash
   dotnet ef database update
   ```

5. **Start Redis** (if not running)
   ```bash
   # Windows (if Redis is installed)
   redis-server

   # Docker alternative
   docker run -d -p 6379:6379 redis:alpine
   ```

6. **Run the API**
   ```bash
   dotnet run
   ```

### Access Points

- **Swagger UI**: `https://localhost:7001/swagger`
- **Health Checks**: `https://localhost:7001/health`
- **Health Check UI**: `https://localhost:7001/health-ui`
- **Metrics**: `https://localhost:7001/metrics`
- **SignalR Hub**: `wss://localhost:7001/hubs/notifications`

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

## 🧪 Testing the Enhanced Features

### 1. Register a User
```bash
POST https://localhost:7001/api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Test123!@#",
  "firstName": "Test",
  "lastName": "User"
}
```

### 2. Login
```bash
POST https://localhost:7001/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#"
}
```

### 3. Create a Product (with JWT token)
```bash
POST https://localhost:7001/api/products
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "name": "Test Product",
  "description": "A test product for the enhanced API",
  "price": 29.99,
  "stockQuantity": 100,
  "imageUrl": "https://example.com/image.jpg"
}
```

### 4. Test Real-time Notifications (JavaScript)
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7001/hubs/notifications", {
        accessTokenFactory: () => "YOUR_JWT_TOKEN"
    })
    .build();

connection.start().then(() => {
    console.log("Connected to notification hub");
});

connection.on("ReceiveNotification", (message) => {
    console.log("Notification:", message);
});
```

## 🔧 Enhanced Configuration

### Rate Limiting (`appsettings.json`)
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "POST:/api/auth/*",
        "Period": "15m",
        "Limit": 5
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
    "EnableHSTS": true,
    "CSPReportUri": "/api/security/csp-report"
  }
}
```

### Enhanced Logging with Serilog
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithEnvironmentName", "WithMachineName", "WithProcessId", "WithThreadId", "WithCorrelationId"]
  }
}
```

## 🔄 Real-Time Features

### SignalR Hub Usage
```javascript
// Connect to the hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
        accessTokenFactory: () => yourJwtToken
    })
    .build();

// Join specific groups
connection.invoke("JoinGroup", "Orders_Updates");
connection.invoke("JoinGroup", "User_" + userId);

// Listen for different notification types
connection.on("ReceiveNotification", (data) => {
    console.log("General notification:", data);
});

connection.on("OrderStatusUpdate", (orderId, status) => {
    console.log(`Order ${orderId} status: ${status}`);
});

connection.on("UserNotification", (message) => {
    console.log("Personal notification:", message);
});
```

### Multi-Channel Notifications
The API supports sending notifications through multiple channels:
- **In-App**: Real-time via SignalR
- **Email**: SMTP-based email notifications
- **SMS**: SMS gateway integration (configurable)
- **Push**: Mobile push notifications (configurable)

## 📈 Monitoring & Metrics

### Prometheus Metrics (`/metrics`)
Available metrics include:
```
# HTTP request metrics
http_requests_total{method="GET",endpoint="/api/products",status_code="200"}
http_request_duration_seconds{method="GET",endpoint="/api/products"}

# Business metrics
user_registrations_total
successful_logins_total
failed_logins_total
orders_created_total
revenue_total

# System metrics
database_connections_active
cache_hits_total
cache_misses_total
memory_usage_bytes
```

### Health Checks (`/health`)
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection is healthy",
      "duration": 45.2
    },
    {
      "name": "redis",
      "status": "Healthy", 
      "description": "Redis connection is healthy",
      "duration": 12.8
    }
  ],
  "totalDuration": 58.0
}
```

### Audit Logging
All user actions are automatically logged with:
- **User identification** and authentication context
- **Action details** with before/after states
- **Timestamp** and **IP address**
- **Correlation ID** for request tracing
- **Integrity hash** for tamper detection
- **Security events** for compliance

## 🛡️ Security Features

### Input Validation & Sanitization
- **XSS Protection**: HTML encoding and script tag removal
- **SQL Injection Prevention**: Parameterized queries and input validation
- **Command Injection Protection**: Input validation and sanitization
- **Path Traversal Prevention**: File access security
- **FluentValidation**: Comprehensive validation rules with custom attributes

### Security Headers Applied
- **Content Security Policy (CSP)**: XSS protection with configurable policies
- **HTTP Strict Transport Security (HSTS)**: HTTPS enforcement
- **X-Frame-Options**: Clickjacking protection
- **X-Content-Type-Options**: MIME type sniffing protection
- **Referrer Policy**: Referrer information control
- **Custom Security Headers**: API versioning and additional security context

### Enhanced Error Handling
- **Correlation IDs**: Track requests across services and logs
- **Secure Error Responses**: No sensitive information leaked
- **Audit Integration**: Security events logged automatically
- **Metrics Integration**: Error rates and patterns tracked
- **FluentValidation Support**: Detailed validation error responses

## 🏢 Multi-Tenancy Support

Configure tenant resolution in `appsettings.json`:
```json
{
  "MultiTenant": {
    "TenantResolutionStrategy": "Claim",
    "EnableTenantIsolation": true,
    "DefaultTenant": "default"
  }
}
```

Tenant information is resolved from JWT claims and used for:
- **Data isolation** between tenants
- **Feature toggles** per tenant
- **Configuration overrides** per tenant
- **Audit logging** with tenant context

## 📦 Enhanced Package Dependencies

### Core Packages
- **OpenTelemetry** - Distributed tracing and metrics
- **FluentValidation** - Advanced validation
- **Serilog** - Structured logging
- **AspNetCoreRateLimit** - Rate limiting
- **SignalR** - Real-time communication
- **MediatR** - Event-driven architecture

### Security Packages
- **HtmlSanitizer** - XSS protection
- **AspNetCore.Authentication.JwtBearer** - JWT authentication
- **Microsoft.AspNetCore.DataProtection** - Data encryption

### Monitoring Packages
- **prometheus-net** - Metrics collection
- **AspNetCore.HealthChecks** - Health monitoring
- **ApplicationInsights** - Cloud monitoring (optional)

## 🚀 Performance Considerations

### Database Optimizations
- **Connection pooling** with retry policies
- **Query timeout** configuration
- **Bulk operations** for large datasets
- **Read replicas** support for scaling

### Caching Strategy
- **Distributed caching** with Redis clustering
- **Memory caching** for frequently accessed data
- **Cache warming** strategies
- **Cache invalidation** patterns

### Response Optimization
- **Compression** enabled
- **Pagination** for large result sets
- **Field selection** to return only needed data
- **Conditional requests** with ETags

## 🔧 Development & Debugging

### Logging and Monitoring
Logs are written to:
- **Console** (structured format)
- **Files** (`logs/app-YYYYMMDD.log`)
- **Application Insights** (if configured)

### Debug Issues Checklist
1. **Database Connection** - Check connection string in .env
2. **Redis Connection** - Ensure Redis is running (`redis-cli ping`)
3. **JWT Issues** - Verify JWT_KEY is set and > 32 characters
4. **CORS Issues** - Check origin configuration in appsettings.json
5. **Rate Limiting** - Check if requests are being throttled
6. **Health Checks** - Visit `/health` for system status

### Development Tips
- Set `ASPNETCORE_ENVIRONMENT=Development` for detailed errors
- Use `/health-ui` for visual system status
- Monitor metrics at `/metrics` endpoint
- Check correlation IDs in response headers for request tracing

## 🚀 Production Deployment

### Environment Checklist
- [ ] **HTTPS enabled** with valid certificates
- [ ] **JWT keys are secure** and properly managed
- [ ] **Database credentials encrypted** and secured
- [ ] **Rate limiting configured** appropriately
- [ ] **CORS origins restricted** to known domains
- [ ] **Security headers enabled** and configured
- [ ] **Health checks configured** for load balancers
- [ ] **Metrics collection enabled** for monitoring
- [ ] **Audit logging active** for compliance
- [ ] **Error tracking setup** for alerting

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1
ENTRYPOINT ["dotnet", "GenericAPI.dll"]
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: generic-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: generic-api
  template:
    metadata:
      labels:
        app: generic-api
    spec:
      containers:
      - name: api
        image: generic-api:latest
        ports:
        - containerPort: 80
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
```

## 🧪 Testing

### Load Testing with Rate Limiting
Test rate limiting by making multiple rapid requests:
```bash
for i in {1..20}; do
  curl -w "%{http_code}\n" https://localhost:7001/api/products
done
```

### Security Testing
1. **XSS Testing**: Try submitting `<script>alert('xss')</script>` in form fields
2. **SQL Injection**: Test with `'; DROP TABLE Users; --` in inputs
3. **Authentication**: Test endpoints without valid JWT tokens
4. **Rate Limiting**: Exceed configured request limits

### Performance Testing
Use the health checks and metrics endpoints to monitor:
- Response times under load
- Memory usage patterns
- Database connection efficiency
- Cache hit/miss ratios

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Implement your changes following the established patterns
4. Add tests and ensure all tests pass
5. Update documentation as needed
6. Submit a pull request

### Code Standards
- Follow established middleware patterns
- Add comprehensive logging with correlation IDs
- Include health checks for new dependencies
- Add metrics for new features
- Implement proper validation with FluentValidation
- Ensure security best practices

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support & Troubleshooting

### Getting Help
1. **Check logs** in `logs/` directory for detailed error information
2. **Visit `/health`** endpoint for system status
3. **Use Swagger UI** at `/swagger` for API testing and documentation
4. **Monitor metrics** at `/metrics` endpoint for performance insights
5. **Check correlation IDs** in response headers for request tracing

### Common Issues

**Application Won't Start**
- Verify all environment variables are set
- Check database connectivity
- Ensure Redis is running (if configured)
- Validate JWT key length (must be 32+ characters)

**Performance Issues**
- Check metrics endpoint for bottlenecks
- Review health check results
- Monitor database connection pool
- Verify cache hit rates

**Security Concerns**
- Review audit logs for suspicious activity
- Check rate limiting effectiveness
- Validate input sanitization
- Verify security headers are applied

## 🔮 Future Enhancements

- [ ] **GraphQL support** with Hot Chocolate
- [ ] **gRPC services** for high-performance communication
- [ ] **Message queue integration** (RabbitMQ/Kafka)
- [ ] **Advanced analytics dashboard** with real-time metrics
- [ ] **API versioning** with backward compatibility
- [ ] **OAuth2/OpenID Connect integration** for enterprise SSO
- [ ] **Kubernetes operators** for automated deployment
- [ ] **Advanced caching strategies** with Redis Streams
- [ ] **Machine learning integration** for predictive analytics
- [ ] **Microservices decomposition** with service mesh

---

**The GenericAPI is now a production-ready, enterprise-grade web API** that follows industry best practices and is ready for deployment in enterprise environments. It includes comprehensive security, monitoring, real-time capabilities, and robust validation systems that can scale with your business needs.
