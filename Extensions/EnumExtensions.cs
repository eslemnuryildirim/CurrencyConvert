using CurrencyConvert.Enums; // Enum'u dahil ettik

namespace CurrencyConvert.Extensions
{
    public static class EnumExtensions
    {
        public static List<string> GetCurrencyTypes()
        {
            return Enum.GetValues(typeof(CurrencyTypes))
                       .Cast<CurrencyTypes>()
                       .Select(c => c.ToString())
                       .ToList();
        }
    }
}
