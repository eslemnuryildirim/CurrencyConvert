using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace CurrencyConvert.Services
{
    public class CurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public CurrencyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["CurrencyApi:Url"] ?? "https://www.tcmb.gov.tr/kurlar";
        }

        // Currency verilerini API'den çeker
        public async Task<XDocument> GetCurrenciesByDateAsync(string date)
        {
            string year = date.Substring(0, 4);  // yyyy
            string month = date.Substring(4, 2); // MM
            string day = date.Substring(6, 2);   // dd

            var url = date == DateTime.Today.ToString("yyyyMMdd")
                ? $"{_baseUrl}/today.xml"
                : $"{_baseUrl}/{year}{month}/{day}{month}{year}.xml";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                return XDocument.Parse(response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Hatası: {ex.Message}, URL: {url}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Beklenmeyen Hata: {ex.Message}, URL: {url}");
                return null;
            }
        }



        // Belirli bir tarih için döviz kurlarını alır
        public decimal? GetCurrencyRate(XDocument exchangeRates, string currencyCode)
        {
            if (currencyCode == "TRY")
                return 1;

            var currency = exchangeRates.Descendants("Currency")
                .FirstOrDefault(c => c.Attribute("CurrencyCode")?.Value == currencyCode);

            if (currency == null)
                return null;

            var forexSelling = currency.Element("ForexSelling")?.Value;

            if (forexSelling != null && forexSelling.Contains(","))
            {
                forexSelling = forexSelling.Replace(",", ".");
            }

            if (decimal.TryParse(forexSelling, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                return rate;

            return null;
        }

        // İki döviz arasındaki çevirme işlemi
        public async Task<decimal?> ConvertAmount(decimal amount, string fromCurrencyCode, string toCurrencyCode, string date)
        {
            var exchangeRates = await GetCurrenciesByDateAsync(date);

            if (exchangeRates == null)
                return null;

            var fromRate = GetCurrencyRate(exchangeRates, fromCurrencyCode);
            var toRate = GetCurrencyRate(exchangeRates, toCurrencyCode);

            if (fromRate == null || toRate == null)
                return null;

            var amountInTRY = amount * fromRate.Value; // Amount'u TRY cinsinden dönüştür
            var convertedAmount = amountInTRY / toRate.Value; // TRY'den hedef para birimine dönüşüm

            return convertedAmount;
        }
    }
}
