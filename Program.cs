using CurrencyConvert.Data;
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// IIS ile entegrasyon
builder.WebHost.UseIISIntegration();

// HttpClient hizmetini ekliyoruz
builder.Services.AddHttpClient<CurrencyService>();

// CurrencyService ve CurrencyDataManager servislerini ekliyoruz
builder.Services.AddScoped<CurrencyService>();
builder.Services.AddScoped<CurrencyDataManager>();

// Veritaban� ba�lant�s�n� ekliyoruz ApplicationDbContext ile
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity servisini ekliyoruz
builder.Services.AddIdentity<IdentityUser, IdentityRole>() // Identity ve roller eklenir
    .AddEntityFrameworkStores<ApplicationDbContext>() // Entity Framework ile kimlik do�rulama
    .AddDefaultTokenProviders(); // Token olu�turucu eklenir

// T�m sayfalarda yetkilendirme zorunlulu�u ekliyoruz
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser() // Giri� yap�lmadan eri�imi engelle
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy)); // T�m controller'lar i�in zorunlu kimlik do�rulamas�
});

var app = builder.Build();

// Seed kullan�c� eklemek i�in bu k�sm� ekleyin
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // E�er admin kullan�c� yoksa olu�tur
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        var user = new IdentityUser { UserName = "admin", Email = "admin@example.com" };
        var result = await userManager.CreateAsync(user, "Password123!");

        if (result.Succeeded)
        {
            // Kullan�c� ba�ar�yla olu�turuldu
        }
    }
}

// HTTP istek yap�land�rmas�
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
app.UseAuthentication();  // Kimlik do�rulama middleware'ini ekleyin
app.UseAuthorization();   // Yetkilendirme middleware'ini ekleyin

// Rota tan�mlamalar�
app.MapControllerRoute(
    name: "currencyList",
    pattern: "{controller=CurrencyList}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Home",
    pattern: "{controller=CurrencyCalculation}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Graph}/{action=Graph}/{currencyCode?}");

app.MapControllerRoute(
    name: "account",
    pattern: "{controller=Account}/{action=Register}/{id?}");

app.Run();
