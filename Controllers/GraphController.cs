using CurrencyConvert.Data;
using CurrencyConvert.Models;
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class GraphController : Controller
{
    private readonly CurrencyService _currencyService;
    private readonly ApplicationDbContext _context;

    public GraphController(CurrencyService currencyService, ApplicationDbContext context)
    {
        _currencyService = currencyService;
        _context = context;
    }

    // Grafik Gösterimi için action metodu
    public async Task<IActionResult> Graph(string currencyCode)
    {
        currencyCode = string.IsNullOrEmpty(currencyCode) ? "USD" : currencyCode;
        var last10Days = DateTime.Today.AddDays(-10);

        var currencyRates = await _context.CurrencyRates
            .Where(cr => cr.CurrencyCode == currencyCode && cr.Date >= last10Days)
            .OrderBy(cr => cr.Date)
            .ToListAsync();

        // Eğer veritabanında veri yoksa, API'den veri çek ve kaydet
        if (!currencyRates.Any())
        {
            for (int i = 0; i < 10; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                var formattedDate = date.ToString("yyyyMMdd");
                await FetchAndSaveCurrencyRates(formattedDate, currencyCode, date);
            }

            currencyRates = await _context.CurrencyRates
                .Where(cr => cr.CurrencyCode == currencyCode && cr.Date >= last10Days)
                .OrderBy(cr => cr.Date)
                .ToListAsync();
        }

        // Eğer hala veri yoksa hata göster
        if (!currencyRates.Any())
        {
            ViewBag.Error = "Seçilen para birimine ait veri bulunamadı.";
            return View();
        }

        // Verileri grafik için View'e gönder
        return View(currencyRates);
    }

    // API'den veri çekip veritabanına kaydeden yardımcı metot
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
}
