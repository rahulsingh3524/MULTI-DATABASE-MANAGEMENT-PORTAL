using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor for CookieService (if used)
builder.Services.AddHttpContextAccessor();

// Register DatabaseHelper with factory lambda to provide config and JSON path
builder.Services.AddScoped<DatabaseHelper>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    // Adjust the path below according to your actual JSON file location relative to app root
    var connStringsConfigPath = "connStringContext.json";
    return new DatabaseHelper(config, connStringsConfigPath);
});

// Register other services
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
