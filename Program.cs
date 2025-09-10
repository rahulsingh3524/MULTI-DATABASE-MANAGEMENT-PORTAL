using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor for services needing HttpContext access
builder.Services.AddHttpContextAccessor();

// Register AuditService (required by DatabaseService)
builder.Services.AddScoped<AuditService>();

// Register DatabaseHelper with config and JSON path as factory
builder.Services.AddScoped<DatabaseHelper>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    // Adjust the path if your connStringContext.json is elsewhere
    var connStringsConfigPath = "connStringContext.json";
    return new DatabaseHelper(config, connStringsConfigPath);
});

// Register your services
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();

// Enable session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();         // Ensure this is before UseRouting/UseEndpoints
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}"
);

app.Run();
