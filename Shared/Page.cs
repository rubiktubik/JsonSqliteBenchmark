
using System.Text.Json.Serialization;

public class Page
{
    [property: JsonPropertyName("minute")]
    public int Minute { get; set; }
    [property: JsonPropertyName("customer")]
    public Customer? Customer { get; set; } 
}