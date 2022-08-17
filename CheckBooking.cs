using System.Text.Json.Serialization;

namespace Parser;

public class CheckBooking
{
    public CheckBooking(int localizationId, int placeId, int serviceNumber, string[] daysTable)
    {
        LocalizationId = localizationId;
        PlaceId = placeId;
        ServiceNumber = serviceNumber;
        DaysTable = daysTable;
    }

    [JsonPropertyName("idLokalizacji")]
    public int LocalizationId { get; set; }
    [JsonPropertyName("idPlacowki")]
    public int PlaceId { get; set; }
    [JsonPropertyName("rodzajUslugi")]
    public int ServiceNumber { get; set; }
    [JsonPropertyName("tabelaDni")]
    public string[] DaysTable { get; set; }
    [JsonPropertyName("token")]
    public string Token { get; set; }

}