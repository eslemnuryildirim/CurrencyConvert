using System.ComponentModel.DataAnnotations;

namespace CurrencyConvert.Models
{
    public class CurrencyRate
    {
        [Key] 
        public int Id { get; set; }

     
        [StringLength(3)] 
        public string CurrencyCode { get; set; }

       
        [StringLength(100)] 
        public string CurrencyName { get; set; }

       
        public decimal EffectiveBuying { get; set; } 
        public decimal EffectiveSelling { get; set; } 
        public decimal Buying { get; set; } 
        public decimal Selling { get; set; } 

     
        public DateTime Date { get; set; } 

        public DateTime CreatedDate { get; set; } 

        [StringLength(100)] 
        public string CreatedBy { get; set; } 
    }
}
