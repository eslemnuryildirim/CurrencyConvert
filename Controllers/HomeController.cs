using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml.Linq;

namespace CurrencyConvert.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _currencies = { "USD", "EUR", "GBP", "TRY" };
        private readonly string _apiUrl;

        // IConfiguration'ý constructor'a enjekte ediyoruz
        public HomeController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // appsettings.json'dan CurrencyApi:Url deðerini okuyoruz
            _apiUrl = configuration.GetValue<string>("CurrencyApi:Url");
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Currencies = _currencies;
            var exchangeRates = await GetCurrencyDataAsync();

            if (exchangeRates == null)
                ViewBag.ErrorMessage = "Döviz kurlarý yüklenemedi.";
            else
                ViewBag.ExchangeRates = exchangeRates;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
        {
            var exchangeRates = await GetCurrencyDataAsync();
            if (exchangeRates == null)
            {
                ViewBag.ErrorMessage = "Döviz kurlarý yüklenemedi.";
                return View("Index");
            }

            ViewBag.Currencies = _currencies;
            ViewBag.ConversionResult = ConvertAmount(amount, fromCurrency, toCurrency, exchangeRates);

            return View("Index");
        }

        private async Task<XDocument> GetCurrencyDataAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_apiUrl);
                return XDocument.Parse(response);
            }
            catch
            {
                return null;
            }
        }

        private decimal? GetCurrencyRate(XDocument exchangeRates, string currencyCode)
        {
            if (currencyCode == "TRY")
                return 1;

            var currencyElement = exchangeRates.Descendants("Currency")
                .FirstOrDefault(c => c.Attribute("CurrencyCode")?.Value == currencyCode);

            if (currencyElement == null)
                return null;

            var forexSellingValue = currencyElement.Element("ForexSelling")?.Value;

            if (forexSellingValue != null && forexSellingValue.Contains(","))
                forexSellingValue = forexSellingValue.Replace(",", ".");

            if (decimal.TryParse(forexSellingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                return rate;

            return null;
        }

        private decimal? ConvertAmount(decimal amount, string fromCurrencyCode, string toCurrencyCode, XDocument exchangeRates)
        {
            var fromRate = GetCurrencyRate(exchangeRates, fromCurrencyCode);
            var toRate = GetCurrencyRate(exchangeRates, toCurrencyCode);
            return fromRate != null && toRate != null ? amount * fromRate / toRate : (decimal?)null;
        }
    }
}
