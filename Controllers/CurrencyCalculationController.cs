using CurrencyConvert.Extensions; // Extensions kullanýmý için ekleme
using CurrencyConvert.Services;
using Microsoft.AspNetCore.Mvc;
using CurrencyConvert.Services;


namespace CurrencyConvert.Controllers
{
    public class CurrencyCalculationController : Controller
    {
        private readonly CurrencyService _currencyService; // CurrencyService'in instance'ý
        private readonly string _apiUrl;

        public CurrencyCalculationController(CurrencyService currencyService, IConfiguration configuration)
        {
            _currencyService = currencyService;
            _apiUrl = configuration.GetValue<string>("CurrencyApi:Url");
        }

        // GET: Index
        public async Task<IActionResult> Index()
        {
            // Enum'dan döviz türlerini getiriyoruz
            ViewBag.Currencies = EnumExtensions.GetCurrencyTypes();

            // Tarihe göre döviz kurlarýný çekiyoruz
            var exchangeRates = await _currencyService.GetCurrenciesByDateAsync(DateTime.Today.ToString("yyyyMMdd"));

            if (exchangeRates == null)
            {
                ViewBag.ErrorMessage = "Döviz kurlarý yüklenemedi."; // Hata mesajý ayarlama
            }
            else
            {
                ViewBag.ExchangeRates = exchangeRates; // Döviz kurlarýný ViewBag'e ekliyoruz
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
                ViewBag.ErrorMessage = "Döviz çevirme iþlemi baþarýsýz oldu veya kurlar yüklenemedi.";
                return View("Index");
            }

            // Sonucu ve detaylarý ViewBag ile görünümde kullanmak için ayarlýyoruz
            ViewBag.ConversionResult = convertedAmount;
            ViewBag.FromCurrency = fromCurrency;
            ViewBag.ToCurrency = toCurrency;
            ViewBag.Amount = amount;

            return View("Index");
        }

    }
}
