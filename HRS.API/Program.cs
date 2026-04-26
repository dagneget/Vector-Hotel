using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;
using HRS.API.Helpers;
using HRS.API.Data.Repositories;
using HRS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();

// Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        
        // Seed initial admin if not exists
        if (!db.Users.Any())
        {
            db.Users.Add(new UserModel { Id = Guid.NewGuid().ToString(), Username = "admin", Password = SecurityHelper.HashPassword("admin123"), Role = "Admin" });
            db.Users.Add(new UserModel { Id = Guid.NewGuid().ToString(), Username = "reception", Password = SecurityHelper.HashPassword("reception123"), Role = "Receptionist" });
            db.Users.Add(new UserModel { Id = Guid.NewGuid().ToString(), Username = "finance", Password = SecurityHelper.HashPassword("finance123"), Role = "Finance" });
            
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
        Console.WriteLine($"[CRITICAL] Database Initialization Failed: {ex.Message}");
        if (ex.InnerException != null) Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
        Console.WriteLine(ex.ToString());
    }
}

app.Run();
