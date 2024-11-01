using CurrencyConvert.Data;
using CurrencyConvert.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CurrencyConvert.Services
{
    public class CurrencyDataManager
    {
        private readonly ApplicationDbContext _context;
        private readonly CurrencyService _currencyService;
        private readonly IHttpContextAccessor _httpContextAccessor; // Kullanıcı bilgisine erişim için ekleme

        public CurrencyDataManager(ApplicationDbContext context, CurrencyService currencyService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _currencyService = currencyService;
            _httpContextAccessor = httpContextAccessor; // Dependency Injection ile ekleme
        }

        // Belirtilen tarihe göre döviz kurlarını veritabanından çeken metod

        public async Task<List<CurrencyTableViewModel>> FetchCurrencyRatesByDate(string formattedDate)
        {
            var date = DateTime.ParseExact(formattedDate, "yyyyMMdd", CultureInfo.InvariantCulture);

            // Veritabanında belirtilen tarihe ait veri var mı kontrol et
            var currencies = await _context.CurrencyRates
                .Where(c => c.Date == date)
                .Select(c => new CurrencyTableViewModel
                {
                    CurrencyCode = c.CurrencyCode,
                    CurrencyName = c.CurrencyName,
                    EffectiveBuying = c.EffectiveBuying,
                    EffectiveSelling = c.EffectiveSelling,
                    Buying = c.Buying,
                    Selling = c.Selling,
                    Date = c.Date
                })
                .ToListAsync();

            // Eğer veritabanında veri yoksa, API'den veriyi çek ve veritabanına kaydet
            if (currencies == null || !currencies.Any())
            {
                Console.WriteLine("Veritabanında veri bulunamadı, API'den veri çekiliyor...");

                // API'den veriyi çek ve veritabanına kaydet
                var apiData = await _currencyService.GetCurrenciesByDateAsync(formattedDate);
                if (apiData != null)
                {
                    var fetchedCurrencies = apiData.Descendants("Currency")
                        .Select(x => new CurrencyRate
                        {
                            CurrencyCode = x.Attribute("CurrencyCode")?.Value ?? "Unknown",
                            CurrencyName = x.Element("Isim")?.Value ?? "Unknown",
                            EffectiveBuying = ParseValidDecimal(x.Element("EffectiveBuying")?.Value),
                            EffectiveSelling = ParseValidDecimal(x.Element("EffectiveSelling")?.Value),
                            Buying = ParseValidDecimal(x.Element("ForexBuying")?.Value),
                            Selling = ParseValidDecimal(x.Element("ForexSelling")?.Value),
                            Date = date,
                            CreatedDate = DateTime.Now,
                            CreatedBy = "System" // Varsa kullanıcı adını ekleyin
                        }).ToList();

                    if (fetchedCurrencies.Any())
                    {
                        await SaveCurrencies(fetchedCurrencies);
                        // Veriyi güncellenmiş şekilde döndür
                        currencies = fetchedCurrencies.Select(c => new CurrencyTableViewModel
                        {
                            CurrencyCode = c.CurrencyCode,
                            CurrencyName = c.CurrencyName,
                            EffectiveBuying = c.EffectiveBuying,
                            EffectiveSelling = c.EffectiveSelling,
                            Buying = c.Buying,
                            Selling = c.Selling,
                            Date = c.Date
                        }).ToList();
                    }
                }
            }

            return currencies;
        }


        // Veritabanına döviz kurlarını toplu olarak kaydeden metod
        public async Task SaveCurrencies(List<CurrencyRate> currencies)
        {
            if (currencies == null || !currencies.Any()) return;
            _context.CurrencyRates.AddRange(currencies);
            await _context.SaveChangesAsync();
        }

        // Belirtilen tarih aralığı için döviz kurlarını API'den çekip veritabanına kaydeden metot
        public async Task FetchAndSaveLastXDaysCurrencyRates(string currencyCode, int days)
        {
            for (int i = 0; i <= days; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                var formattedDate = date.ToString("yyyyMMdd");
                await FetchAndSaveCurrencyRates(formattedDate, currencyCode, date);
            }
        }

        // API'den döviz kurlarını çekip veritabanına kaydeden metod
        public async Task<List<CurrencyRate>> FetchAndSaveCurrencyRates(string formattedDate, string currencyCode, DateTime parsedDate)
        {
            // Bu tarihe ve para birimine göre daha önce eklenmiş kayıt olup olmadığını kontrol edin
            var existingRecords = await _context.CurrencyRates
                .Where(c => c.Date == parsedDate && c.CurrencyCode == currencyCode)
                .ToListAsync();

            if (existingRecords.Any())
            {
                Console.WriteLine($"Veritabanında {formattedDate} tarihi için {currencyCode} verisi zaten mevcut.");
                return existingRecords; // Eğer veri varsa kaydetmeden mevcut veriyi döndür
            }

            var document = await _currencyService.GetCurrenciesByDateAsync(formattedDate);

            if (document == null)
            {
                Console.WriteLine($"No currency data found for date {formattedDate}.");
                return null;
            }

            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";

            var currencies = document.Descendants("Currency")
                .Select(x => new CurrencyRate
                {
                    CurrencyCode = x.Attribute("CurrencyCode")?.Value ?? "Unknown",
                    CurrencyName = x.Element("Isim")?.Value ?? "Unknown",
                    EffectiveBuying = ParseValidDecimal(x.Element("EffectiveBuying")?.Value),
                    EffectiveSelling = ParseValidDecimal(x.Element("EffectiveSelling")?.Value),
                    Buying = ParseValidDecimal(x.Element("ForexBuying")?.Value),
                    Selling = ParseValidDecimal(x.Element("ForexSelling")?.Value),
                    Date = parsedDate,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName
                }).ToList();

            if (currencies.Any())
            {
                await SaveCurrencies(currencies);
            }

            return currencies;
        }


        // Veritabanından belirli tarih aralığındaki döviz kurlarını getiren metot
        public async Task<List<CurrencyRate>> FetchCurrencyRates(string currencyCode, DateTime fromDate)
        {
            return await _context.CurrencyRates
                .Where(c => c.CurrencyCode == currencyCode && c.Date >= fromDate)
                .OrderBy(c => c.Date)
                .ToListAsync();
        }


        private decimal ParseValidDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Replace(".", "").Replace(",", "."); // Türkçe veya diğer kültürler için düzenleme
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result / 10000; // Ölçekleme işlemi, burada 10.000’e bölünüyor
            }
            return 0;
        }

        // Tek bir döviz kaydını veritabanına kaydeden metot
        public async Task SaveCurrency(CurrencyRate currency)
        {
            if (currency == null) return;
            _context.CurrencyRates.Add(currency);
            await _context.SaveChangesAsync();
        }
    }
}
