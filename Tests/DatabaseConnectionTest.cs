using GenericAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace GenericAPI.Tests;

public class DatabaseConnectionTest
{
    public static async Task TestConnection()
    {
        try
        {
            var factory = new DesignTimeDbContextFactory();
            using var context = factory.CreateDbContext(Array.Empty<string>());
            
            // Try to access the database
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                Console.WriteLine("Successfully connected to the database!");
                Console.WriteLine("\nChecking tables...");
                
                var userCount = await context.Users.CountAsync();
                var productCount = await context.Products.CountAsync();
                var orderCount = await context.Orders.CountAsync();
                var orderItemCount = await context.OrderItems.CountAsync();
                
                Console.WriteLine($"Users table exists. Count: {userCount}");
                Console.WriteLine($"Products table exists. Count: {productCount}");
                Console.WriteLine($"Orders table exists. Count: {orderCount}");
                Console.WriteLine($"OrderItems table exists. Count: {orderItemCount}");
            }
            else
            {
                Console.WriteLine("Could not connect to the database.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing database connection: {ex.Message}");
        }
    }
}
