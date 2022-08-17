using System.Text.Json.Serialization;

namespace Parser;

public class CaptchaGenerator
{
    public CaptchaGenerator(string id, ushort countOfSymbols, string imageInBase64)
    {
        this.id = id;
        this.countOfSymbols = countOfSymbols;
        this.imageInBase64 = imageInBase64;
    }
[JsonPropertyName("id")]
    public string id { get; set; }
[JsonPropertyName("iloscZnakow")]
    public ushort countOfSymbols { get; set; }
[JsonPropertyName("image")]
    public string imageInBase64 { get; set; }
}