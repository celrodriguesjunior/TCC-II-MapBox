using System.Text.Json.Serialization;

namespace Tcc.Collector;

/// <summary>
/// Uma leitura de tráfego. **Toda amostra agendada gera um registro**, inclusive quando falha.
///
/// Essa é a correção do defeito mais grave do coletor de 2022: lá, o <c>$.getJSON</c> não tinha
/// callback de erro, então timeout, 429 e 5xx eram descartados em silêncio — tornando
/// "a requisição falhou" indistinguível de "a amostra nunca foi agendada". É por isso que a
/// completude daquele dataset é desconhecível. Aqui, uma lacuna é sempre explicável.
/// </summary>
public sealed record TrafficObservation
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Instante nominal do ciclo, alinhado à grade. Chave de junção entre segmentos.</summary>
    [JsonPropertyName("cycle_id")]
    public required string CycleId { get; init; }

    [JsonPropertyName("segment_id")]
    public required string SegmentId { get; init; }

    [JsonPropertyName("street_name")]
    public required string StreetName { get; init; }

    [JsonPropertyName("tier")]
    public required string Tier { get; init; }

    [JsonPropertyName("lat")]
    public required double Lat { get; init; }

    [JsonPropertyName("lon")]
    public required double Lon { get; init; }

    [JsonPropertyName("fetched_at_utc")]
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary><c>ok</c>, <c>error</c> ou <c>budget_exhausted</c>.</summary>
    [JsonPropertyName("fetch_status")]
    public required string FetchStatus { get; init; }

    [JsonPropertyName("http_status")]
    public int? HttpStatus { get; init; }

    [JsonPropertyName("error_kind")]
    public string? ErrorKind { get; init; }

    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "tomtom-flow-v4";

    [JsonPropertyName("current_speed_kph")]
    public int? CurrentSpeedKph { get; init; }

    [JsonPropertyName("free_flow_speed_kph")]
    public int? FreeFlowSpeedKph { get; init; }

    [JsonPropertyName("current_travel_time_s")]
    public int? CurrentTravelTimeSeconds { get; init; }

    [JsonPropertyName("free_flow_travel_time_s")]
    public int? FreeFlowTravelTimeSeconds { get; init; }

    [JsonPropertyName("frc")]
    public string? Frc { get; init; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    [JsonPropertyName("road_closure")]
    public bool? RoadClosure { get; init; }

    /// <summary>
    /// Distância entre o ponto configurado e o segmento que a API casou.
    ///
    /// Gravada a cada leitura porque o casamento pode mudar com o tempo: uma atualização do mapa
    /// da TomTom pode passar a encaixar o ponto noutra via, e sem esta métrica a série mudaria de
    /// significado sem nenhum sinal.
    /// </summary>
    [JsonPropertyName("match_offset_m")]
    public double? MatchOffsetMeters { get; init; }
}

/// <summary>
/// Uma leitura de clima. Um ponto por ciclo cobre o bairro inteiro — em 2022 eram 33 consultas
/// para obter a mesma informação, num raio onde o tempo é idêntico.
/// </summary>
public sealed record WeatherObservation
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("cycle_id")]
    public required string CycleId { get; init; }

    [JsonPropertyName("lat")]
    public required double Lat { get; init; }

    [JsonPropertyName("lon")]
    public required double Lon { get; init; }

    [JsonPropertyName("fetched_at_utc")]
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary>Instante ao qual a observação meteorológica se refere, informado pelo provedor.</summary>
    [JsonPropertyName("observed_at_utc")]
    public DateTimeOffset? ObservedAtUtc { get; init; }

    [JsonPropertyName("fetch_status")]
    public required string FetchStatus { get; init; }

    [JsonPropertyName("http_status")]
    public int? HttpStatus { get; init; }

    [JsonPropertyName("error_kind")]
    public string? ErrorKind { get; init; }

    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "open-meteo-v1";

    /// <summary>
    /// Código WMO cru. Guardado sem tradução: o mapeamento para as 8 categorias de 2022 é lossy
    /// e unidirecional, então converter na coleta destruiria sinal que não volta.
    /// </summary>
    [JsonPropertyName("weather_code_wmo")]
    public int? WeatherCodeWmo { get; init; }

    [JsonPropertyName("temperature_c")]
    public double? TemperatureC { get; init; }

    [JsonPropertyName("apparent_temperature_c")]
    public double? ApparentTemperatureC { get; init; }

    [JsonPropertyName("relative_humidity_pct")]
    public int? RelativeHumidityPct { get; init; }

    [JsonPropertyName("precipitation_mm")]
    public double? PrecipitationMm { get; init; }

    [JsonPropertyName("rain_mm")]
    public double? RainMm { get; init; }

    [JsonPropertyName("wind_speed_kph")]
    public double? WindSpeedKph { get; init; }

    [JsonPropertyName("wind_gusts_kph")]
    public double? WindGustsKph { get; init; }

    [JsonPropertyName("cloud_cover_pct")]
    public int? CloudCoverPct { get; init; }

    [JsonPropertyName("is_day")]
    public bool? IsDay { get; init; }
}
