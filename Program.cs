using System.Text;
using GenericAPI.Data;
using GenericAPI.Helpers;
using GenericAPI.Middleware;
using GenericAPI.Repositories;
using GenericAPI.Services.Interfaces;
using GenericAPI.Services.Implementations;
using GenericAPI.Services;
using GenericAPI.Tests;
using GenericAPI.Hubs;
using GenericAPI.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using DotNetEnv;
using Serilog;
using Serilog.Events;
using Prometheus;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.Diagnostics.HealthChecks;


// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    // Load environment variables from .env file
    Env.Load();
    
    var builder = WebApplication.CreateBuilder(args);

    // Add custom configuration to expand environment variables
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();

    // Create a new configuration with expanded environment variables
    var expandedConfig = new ConfigurationBuilder();
    foreach (var source in builder.Configuration.Sources)
    {
        expandedConfig.Add(source);
    }
    var tempConfig = expandedConfig.Build();

    // Expand environment variables in configuration values
    var expandedConfigData = new Dictionary<string, string?>();
    foreach (var kvp in tempConfig.AsEnumerable())
    {
        if (kvp.Value != null && kvp.Value.Contains("${") && kvp.Value.Contains("}"))
        {
            var expandedValue = kvp.Value;
            
            // Handle nested environment variables (e.g., ${VAR1}_${VAR2})
            int maxIterations = 10; // Prevent infinite loops
            int iteration = 0;
            
            while (expandedValue.Contains("${") && expandedValue.Contains("}") && iteration < maxIterations)
            {
                var startIndex = expandedValue.IndexOf("${");
                if (startIndex >= 0)
                {
                    var endIndex = expandedValue.IndexOf("}", startIndex);
                    if (endIndex > startIndex)
                    {
                        var envVarName = expandedValue.Substring(startIndex + 2, endIndex - startIndex - 2);
                        var envVarValue = Environment.GetEnvironmentVariable(envVarName) ?? "";
                        expandedValue = expandedValue.Replace($"${{{envVarName}}}", envVarValue);
                    }
                    else
                    {
                        break; // Malformed variable, exit loop
                    }
                }
                else
                {
                    break; // No more variables to expand
                }
                iteration++;
            }
            
            expandedConfigData[kvp.Key] = expandedValue;
        }
        else
        {
            expandedConfigData[kvp.Key] = kvp.Value;
        }
    }

    // Replace the configuration with the expanded version
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddInMemoryCollection(expandedConfigData);

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Test database connection
    await DatabaseConnectionTest.TestConnection();

    // Add services to the container
    builder.Services.AddControllers()
        .AddFluentValidation(fv => 
        {
            fv.RegisterValidatorsFromAssemblyContaining<Program>();
            fv.DisableDataAnnotationsValidation = false;
        });

    builder.Services.AddEndpointsApiExplorer();

    // Configure Multi-tenancy
    builder.Services.AddMultiTenant<TenantInfo>()
        .WithClaimStrategy("tenant")
        .WithInMemoryStore();

    // Register repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();

    // Register core services
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // Register new enhanced services
    builder.Services.AddScoped<IInputSanitizerService, InputSanitizerService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddSingleton<IMetricsService, MetricsService>();

    // Register helpers
    builder.Services.AddSingleton<JwtHelper>();
    builder.Services.AddMemoryCache();

    // Add MediatR for event-driven architecture
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // Add SignalR for real-time features
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    });

    // Configure rate limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Configure comprehensive health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database")
        .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", "redis")
        .AddCheck<DatabaseHealthCheck>("database_detailed")
        .AddCheck<RedisHealthCheck>("redis_detailed");

    // Add health checks UI
    builder.Services.AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(30);
        options.SetMinimumSecondsBetweenFailureNotifications(60);
        options.AddHealthCheckEndpoint("API Health", "/health");
    }).AddInMemoryStorage();

    // Configure Swagger with enhanced security
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "Enhanced Generic API", 
            Version = "v1",
            Description = "A comprehensive API with security, monitoring, and real-time features",
            Contact = new OpenApiContact
            {
                Name = "API Support",
                Email = "support@genericapi.com"
            }
        });
        
        // Configure JWT authentication in Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Configure Database with optimizations
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString ?? throw new InvalidOperationException("Connection string is null"),
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            })
            .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
            .EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

    // Configure enhanced Redis with clustering support
    var redisConfig = new ConfigurationOptions
    {
        EndPoints = { builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379" },
        AbortOnConnectFail = false,
        ConnectRetry = 5,
        ConnectTimeout = 5000,
        AsyncTimeout = 5000,
        AllowAdmin = true,
        KeepAlive = 60,
        DefaultDatabase = 0
    };

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
        ConnectionMultiplexer.Connect(redisConfig));

    // Add distributed caching with Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
        options.InstanceName = "GenericAPI";
    });

    // Configure enhanced JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured"))),
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // Configure AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    // Configure CORS with enhanced security - only for production
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials() 
                    .SetPreflightMaxAge(TimeSpan.FromDays(1));
            });
        });
    }
    else
    {
        // Permissive CORS for development/testing
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevelopmentPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    // Add Application Insights if configured
    if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
    {
        builder.Services.AddApplicationInsightsTelemetry();
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enhanced Generic API v1");
            c.DisplayRequestDuration();
            c.EnableTryItOutByDefault();
        });
    }

    // Security and monitoring middleware (order matters!)
    if (!app.Environment.IsDevelopment())
    {
        app.UseSecurityHeaders(options =>
        {
            options.CSPReportUri = "/api/security/csp-report";
            options.CustomHeaders.Add("X-API-Version", "1.0");
        });
        
        app.UseHttpsRedirection();
    }

    // Add Prometheus metrics endpoint
    app.UseMetricServer("/metrics");
    app.UseHttpMetrics();

    // Enhanced middleware pipeline
    app.UseErrorHandling();
    
    if (!app.Environment.IsDevelopment())
    {
        app.UseIpRateLimiting();
        app.UseCors("DefaultPolicy");
    }
    else
    {
        app.UseCors("DevelopmentPolicy");
    }

    // Multi-tenancy
    app.UseMultiTenant();

    // Authentication and authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Health checks
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            var response = new
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.Select(x => new
                {
                    Name = x.Key,
                    Status = x.Value.Status.ToString(),
                    Description = x.Value.Description,
                    Duration = x.Value.Duration.TotalMilliseconds
                }),
                TotalDuration = report.TotalDuration.TotalMilliseconds
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    });

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    // Health checks UI
    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/health-ui";
        options.ApiPath = "/health-ui-api";
    });

    // Map controllers and SignalR hubs
    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");

    // Apply database migrations
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
            await auditService.LogSystemEventAsync("ApplicationStarted", "Application has started successfully");
            
            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            throw;
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Tenant information for multi-tenancy
public class TenantInfo : ITenantInfo
{
    public string? Id { get; set; }
    public string? Identifier { get; set; }
    public string? Name { get; set; }
    public string? ConnectionString { get; set; }
}
