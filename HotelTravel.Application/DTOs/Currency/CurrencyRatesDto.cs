namespace HotelTravel.Application.DTOs.Currency;

// 1 AZN neçə vahid edir (məs. USD: 0.5882)
public class CurrencyRatesDto
{
    public string Base { get; set; } = "AZN";

    public Dictionary<string, decimal> Rates { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    // false -> internet yoxdur, təxmini sabit məzənnələr
    public bool IsLive { get; set; }

    public List<CurrencyInfoDto> Currencies { get; set; } = new();
}

public class CurrencyInfoDto
{
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
