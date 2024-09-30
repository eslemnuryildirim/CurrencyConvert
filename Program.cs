using CurrencyConvert.Data;
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// IIS ile entegrasyon
builder.WebHost.UseIISIntegration();

// HttpClient hizmetini ekliyorum
builder.Services.AddHttpClient<CurrencyService>();

// CurrencyService servisini ekliyorum
builder.Services.AddScoped<CurrencyService>();

// Veritabaný baðlantýsýný ekliyorum ApplicationDbContext ile
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity servisini ekliyorum
builder.Services.AddIdentity<IdentityUser, IdentityRole>() // Identity ve roller eklenir
    .AddEntityFrameworkStores<ApplicationDbContext>() // Entity Framework ile kimlik doðrulama
    .AddDefaultTokenProviders(); // Token oluþturucu eklenir

// Tüm sayfalarda yetkilendirme zorunluluðu ekliyoruz
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser() // Giriþ yapýlmadan eriþimi engelle
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy)); // Tüm controller'lar için zorunlu kimlik doðrulamasý
});

var app = builder.Build();

// Seed kullanýcý eklemek için bu kýsmý ekleyin
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Eðer admin kullanýcý yoksa oluþtur
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        var user = new IdentityUser { UserName = "admin", Email = "admin@example.com" };
        var result = await userManager.CreateAsync(user, "Password123!");

        if (result.Succeeded)
        {
            // Kullanýcý baþarýyla oluþturuldu
        }
    }
}

// HTTP istek yapýlandýrmasý
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();  // Kimlik doðrulama middleware'ini ekleyin
app.UseAuthorization();   // Yetkilendirme middleware'ini ekleyin

app.MapControllerRoute(
    name: "currencyTable",
    pattern: "{controller=CurrencyTable}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Home",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Graph}/{action=Graph}/{currencyCode?}");

app.MapControllerRoute(
    name: "account",
    pattern: "{controller=Account}/{action=Register}/{id?}");

app.Run();
