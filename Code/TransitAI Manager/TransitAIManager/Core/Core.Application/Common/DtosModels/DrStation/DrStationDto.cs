namespace Core.Application.Common.DtosModels.DrStation
{
    public class DrStationDto
    {
    	public int CodeStation { get; set; }
    	public string NomStationFr { get; set; } = string.Empty;
    	public string NomStationAr { get; set; } = string.Empty;
    	public double? StopLatitude { get; set; }
    	public double? StopLongtitude { get; set; }

    }
}