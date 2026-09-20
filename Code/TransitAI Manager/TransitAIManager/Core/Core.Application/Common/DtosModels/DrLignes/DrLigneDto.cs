namespace Core.Application.Common.DtosModels.DrLignes
{
    public class DrLigneDto
    {
        public long? IdLigne { get; set; }
        public string? NomligneFr { get; set; }
        public string? NomligneAr { get; set; }

        // [Column("dectyli")] public decimal? CategoryLignes { get; set; } 
        // [Column("denbrkm")] public decimal? Denbrkm { get; set; }
        //
        // [Column("deccent")] public int? Deccent { get; set; }
        // [Column("decdeleg")] public int? DelegLigne { get; set; }
        //
        // [Column("denatli")] public string? Denatli { get; set; }
        // [Column("dectyta")] public string? Dectyta { get; set; }
        //
        // [Column("deactif")] public string? Deactif { get; set; }
        // [Column("integ_bi")] public string? IntegBi { get; set; }
        // [Column("siv")] public string? Siv { get; set; }
        // [Column("sae")] public string? Sae { get; set; }
    }
}