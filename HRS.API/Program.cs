using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        
        // Seed initial admin if not exists
        if (!db.Users.Any())
        {
            db.Users.Add(new UserModel { Id = Guid.NewGuid().ToString(), Username = "admin", Password = "123", Role = "Admin" });
            db.Users.Add(new UserModel { Id = Guid.NewGuid().ToString(), Username = "reception", Password = "123", Role = "Receptionist" });
            db.Users.Add(new UserModel { Id = Guid.NewGuid().ToString(), Username = "finance", Password = "123", Role = "Finance" });
            
            // Seed basic room types
            db.RoomTypes.AddRange(new List<RoomTypeModel> {
                new RoomTypeModel { Id = Guid.NewGuid().ToString(), Name = "Single Standard", BasePrice = 120 },
                new RoomTypeModel { Id = Guid.NewGuid().ToString(), Name = "Double Deluxe", BasePrice = 250 },
                new RoomTypeModel { Id = Guid.NewGuid().ToString(), Name = "Presidential Suite", BasePrice = 750 }
            });
            
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration Error: {ex.Message}");
    }
}

app.Run();
