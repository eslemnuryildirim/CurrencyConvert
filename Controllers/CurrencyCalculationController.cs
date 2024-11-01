using CurrencyConvert.Extensions; // Extensions kullan�m� i�in ekleme
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Mvc;
using CurrencyConvert.Services;


namespace CurrencyConvert.Controllers
{
    public class CurrencyCalculationController : Controller
    {
        private readonly CurrencyService _currencyService; // CurrencyService'in instance'�
        private readonly string _apiUrl;

        public CurrencyCalculationController(CurrencyService currencyService, IConfiguration configuration)
        {
            _currencyService = currencyService;
            _apiUrl = configuration.GetValue<string>("CurrencyApi:Url");
        }

        // GET: Index
        public async Task<IActionResult> Index()
        {
            // Enum'dan d�viz t�rlerini getiriyoruz
            ViewBag.Currencies = EnumExtensions.GetCurrencyTypes();

            // Tarihe g�re d�viz kurlar�n� �ekiyoruz
            var exchangeRates = await _currencyService.GetCurrenciesByDateAsync(DateTime.Today.ToString("yyyyMMdd"));

            if (exchangeRates == null)
            {
                ViewBag.ErrorMessage = "D�viz kurlar� y�klenemedi."; // Hata mesaj� ayarlama
            }
            else
            {
                ViewBag.ExchangeRates = exchangeRates; // D�viz kurlar�n� ViewBag'e ekliyoruz
            }

            return View();
        }

        // POST: ConvertCurrency
        [HttpPost]
        public async Task<IActionResult> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
        {
            ViewBag.Currencies = EnumExtensions.GetCurrencyTypes();

            var convertedAmount = await _currencyService.ConvertAmount(amount, fromCurrency, toCurrency, DateTime.Today.ToString("yyyyMMdd"));

            if (convertedAmount == null)
            {
                ViewBag.ErrorMessage = "D�viz �evirme i�lemi ba�ar�s�z oldu veya kurlar y�klenemedi.";
                return View("Index");
            }

            // Sonucu ve detaylar� ViewBag ile g�r�n�mde kullanmak i�in ayarl�yoruz
            ViewBag.ConversionResult = convertedAmount;
            ViewBag.FromCurrency = fromCurrency;
            ViewBag.ToCurrency = toCurrency;
            ViewBag.Amount = amount;

            return View("Index");
        }

    }
}
