using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Tarification
    /// </summary>
    [Table("fare_attibute")] 
    public class FareAttribute
    {
    	[Key]
    	[Column("FARE_ID")]
    	public int FareId { get; set; }

    	[Column("PRICE")] public double Price { get; set; }
    	[Column("PRICE_CONF")] public double PriceConf { get; set; }
    	[Column("CURRENCY_TYPE")] public string CurrencyType { get; set; } = string.Empty;
    	[Column("PAYMENT_METHOD")] public int PaymentMethod { get; set; }
    	[Column("TRANSFERS")] public int Transfers { get; set; }
    	[Column("AGENCY_ID")] public string AgencyId { get; set; } = string.Empty;
	
    	[Column("PRICE_REDU")] public string? PriceRedu { get; set; } 
    
    	[Column("TYPE_TARIF")] public string TypeTarif { get; set; } = string.Empty;
    }
}
