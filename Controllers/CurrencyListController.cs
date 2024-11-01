using CurrencyConvert.Models;
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConvert.Controllers
{
    public class CurrencyListController : Controller
    {
        private readonly CurrencyDataManager _currencyDataManager;

        public CurrencyListController(CurrencyDataManager currencyDataManager)
        {
            _currencyDataManager = currencyDataManager;
        }

        // GET: Index
        public IActionResult Index()
        {
            return View(new List<CurrencyTableViewModel>()); // Boş bir model ile view'i render ediyoruz
        }

        // POST: Index (Tarihe göre döviz kurlarını getir)
        [HttpPost]
        public async Task<IActionResult> Index(DateTime date)
        {
            var formattedDate = date.ToString("yyyyMMdd"); // Seçilen tarihi formatlıyoruz
            var currencies = await _currencyDataManager.FetchCurrencyRatesByDate(formattedDate); // Doğru metod çağrısı

            if (currencies == null || !currencies.Any())
            {
                ViewBag.Error = "Seçilen tarihe ait döviz verisi bulunamadı.";
                return View(new List<CurrencyTableViewModel>()); // Boş model gönderiyoruz
            }

            return View(currencies);
        }
    }
}
