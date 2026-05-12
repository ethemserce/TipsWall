namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureWeatherDto
    {
        public decimal? TemperatureDay { get; init; }
        public decimal? TemperatureEvening { get; init; }
        public decimal? WindSpeed { get; init; }
        public int? WindDirection { get; init; }
        public string? Humidity { get; init; }
        public int? Pressure { get; init; }
        public string? Clouds { get; init; }
        public string? Description { get; init; }
        public string? Icon { get; init; }
        public string? Metric { get; init; }
    }
}
