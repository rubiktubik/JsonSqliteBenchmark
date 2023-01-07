
using System.Text.Json.Serialization;

public class Customer
{
    [property: JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [property: JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    public Guid Id { get; set; }
}