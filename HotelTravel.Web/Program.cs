using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // Database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Dependency Injection
            builder.Services.AddScoped<IAppDbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());

            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IHotelService, HotelService>();
            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}