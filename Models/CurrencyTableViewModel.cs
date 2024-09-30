namespace CurrencyConvert.Models
{
    public class CurrencyTableViewModel
    {
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal EffectiveBuying { get; set; }
        public decimal EffectiveSelling { get; set; }
        public decimal Buying { get; set; }
        public decimal Selling { get; set; }
        public DateTime Date { get; set; }
    }
}
