namespace dashboard.Models;

public class GpsData
{
    [System.Text.Json.Serialization.JsonPropertyName("distanceKm")]
    public double DistanceKm { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("bearingDegrees")]
    public double BearingDegrees { get; set; }
}