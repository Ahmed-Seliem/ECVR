using ECM.ReservationSystem.Application;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Infrastructure;
using ECM.ReservationSystem.InfrastructureExtensions;
using ECM.ReservationSystem.Services;
using ECM.ReservationSystem.Services.Implementations;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


AsposeLicenseHelper.SetLicense(builder.Configuration);
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});




// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Register Services
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IUnitService, UnitService>();


builder.Services.AddHostedService<ExpiredHoldsCleanupService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSerilogRequestLogging();

app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");



app.Run();
