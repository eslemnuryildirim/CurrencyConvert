using CurrencyConvert.Services;
using Microsoft.AspNetCore.Mvc;

public class GraphController : Controller
{
    private readonly CurrencyDataManager _currencyDataManager;

    public GraphController(CurrencyDataManager currencyDataManager)
    {
        _currencyDataManager = currencyDataManager;
    }

    // Grafik Gösterimi için action metodu
    public async Task<IActionResult> Graph(string currencyCode, string period)
    {
        currencyCode = string.IsNullOrEmpty(currencyCode) ? "USD" : currencyCode;

        // Tarih aralığını seçilen periyoda göre belirleyin
        DateTime fromDate = period switch
        {
            "15days" => DateTime.Today.AddDays(-15),
            "1month" => DateTime.Today.AddMonths(-1),
            "6months" => DateTime.Today.AddMonths(-6),
            _ => DateTime.Today.AddDays(-15)
        };

        // Gün sayısını seçilen periyoda göre ayarlayın
        int days = period switch
        {
            "15days" => 15,
            "1month" => 30,
            "6months" => 180,
            _ => 15
        };

        // Seçilen zaman aralığına göre veriyi çekin ve kaydedin
        await _currencyDataManager.FetchAndSaveLastXDaysCurrencyRates(currencyCode, days);

        // Güncellenmiş verileri veritabanından tekrar çekin
        var currencyRates = await _currencyDataManager.FetchCurrencyRates(currencyCode, fromDate);

        if (!currencyRates.Any())
        {
            ViewBag.Error = "Seçilen para birimine ait veri bulunamadı.";
            return View();
        }

        return View(currencyRates);
    }

}
