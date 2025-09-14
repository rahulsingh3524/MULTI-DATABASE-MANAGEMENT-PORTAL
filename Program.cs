using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;

var builder = WebApplication.CreateBuilder(args);

//// Retrieve the master connection string from your app's configuration (e.g., appsettings.json)
//var masterConnectionString = builder.Configuration.GetConnectionString("MasterDbConnection");

//// Register the DatabaseHelper service with the connection string
//builder.Services.AddSingleton(new DatabaseHelper(masterConnectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
