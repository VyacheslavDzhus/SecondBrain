namespace SecondBrain.Domain;

/// <summary>
/// Опции маршрутизации, маппится из routing.json
/// </summary>
public class RoutingOptions
{
    public List<RoutingDestination> Destinations { get; set; } = new();
}
