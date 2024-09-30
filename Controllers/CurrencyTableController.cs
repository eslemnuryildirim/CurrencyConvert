using CurrencyConvert.Data;
using CurrencyConvert.Models;
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CurrencyConvert.Enums;

public class CurrencyTableController : Controller
{
    private readonly CurrencyService _currencyService;
    private readonly ApplicationDbContext _context;

    public CurrencyTableController(CurrencyService currencyService, ApplicationDbContext context)
    {
        _currencyService = currencyService;
        _context = context;
    }

    // Döviz kodlarını ViewBag'e yollayan yardımcı metot
    private void SetCurrencyCodes() =>
        ViewBag.CurrencyCodes = Enum.GetValues(typeof(CurrencyCodes))
                                    .Cast<CurrencyCodes>()
                                    .Select(c => c.ToString())
                                    .ToList();

    // API'den veri çekip veritabanına kaydeden ortak metot
    private async Task<List<CurrencyRate>> FetchAndSaveCurrencyRates(string formattedDate, string currencyCode, DateTime parsedDate)
    {
        var document = await _currencyService.GetCurrencyDataAsync(formattedDate);
        if (document == null) return null;

        var currencies = document.Descendants("Currency")
            .Where(x => currencyCode == (string)x.Attribute("CurrencyCode"))
            .Select(x => new CurrencyRate
            {
                CurrencyCode = x.Attribute("CurrencyCode")?.Value ?? "Unknown",
                CurrencyName = x.Element("Isim")?.Value ?? "Unknown",
                EffectiveBuying = (decimal?)x.Element("EffectiveBuying") ?? 0,
                EffectiveSelling = (decimal?)x.Element("EffectiveSelling") ?? 0,
                Buying = (decimal?)x.Element("ForexBuying") ?? 0,
                Selling = (decimal?)x.Element("ForexSelling") ?? 0,
                Date = parsedDate,
                CreatedDate = DateTime.Now,
                CreatedBy = "ESLEM"
            }).ToList();

        if (currencies.Any())
        {
            _context.CurrencyRates.AddRange(currencies);
            await _context.SaveChangesAsync();
        }

        return currencies;
    }

    // GET: Index
    public IActionResult Index()
    {
        SetCurrencyCodes();
        return View();
    }

    // POST: Index - Tarihe göre döviz verisi alma ve kaydetme
    [HttpPost]
    public async Task<IActionResult> Index(string date)
    {
        if (!DateTime.TryParse(date, out DateTime parsedDate))
        {
            ViewBag.Error = "Geçersiz tarih formatı.";
            return View();
        }

        var formattedDate = parsedDate.ToString("yyyyMMdd");
        var existingCurrencyRates = await _context.CurrencyRates
            .Where(cr => cr.Date == parsedDate)
            .ToListAsync();

        if (existingCurrencyRates.Any())
        {
            ViewBag.Message = "Veritabanındaki mevcut veriler gösteriliyor.";
            return View(existingCurrencyRates);
        }

        try
        {
            var fetchTask = _currencyService.GetCurrencyDataAsync(formattedDate);
            var completedTask = await Task.WhenAny(fetchTask, Task.Delay(60000));

            if (completedTask != fetchTask)
            {
                ViewBag.Error = "API'den veri alınamadı, zaman aşımı.";
                return View();
            }

            var currencies = await FetchAndSaveCurrencyRates(formattedDate, "USD", parsedDate);
            if (currencies == null || !currencies.Any())
            {
                ViewBag.Error = "API'den veri bulunamadı.";
                return View();
            }

            ViewBag.Message = "API'den alınan veriler gösteriliyor.";
            return View(currencies);
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Bir hata oluştu: {ex.Message}";
            return View();
        }
    }

    // POST: Yeni döviz verisini veritabanına kaydetme
    [HttpPost]
    public async Task<IActionResult> Save(CurrencyRate model)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        model.CreatedDate = DateTime.Now;
        model.CreatedBy = "ESLEM";
        _context.CurrencyRates.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}
