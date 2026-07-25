namespace Tcc.Domain;

/// <summary>Ponto geográfico em graus decimais (WGS84).</summary>
public readonly record struct GeoPoint(double Lat, double Lon)
{
    public override string ToString() =>
        $"{Lat.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)}," +
        $"{Lon.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Cálculos geodésicos sobre polilinhas.
///
/// As distâncias usam projeção equirretangular local em torno de uma latitude de referência.
/// Para os ~3 km do recorte de Rio Branco o erro é da ordem de centímetros — irrelevante frente
/// aos limiares de dezenas de metros usados aqui, e muito mais barato que Vincenty.
/// </summary>
public static class Geo
{
    private const double EarthRadiusMeters = 6_371_008.8;
    private const double DegToRad = Math.PI / 180.0;

    /// <summary>Distância em metros entre dois pontos (haversine).</summary>
    public static double Distance(GeoPoint a, GeoPoint b)
    {
        var dLat = (b.Lat - a.Lat) * DegToRad;
        var dLon = (b.Lon - a.Lon) * DegToRad;
        var lat1 = a.Lat * DegToRad;
        var lat2 = b.Lat * DegToRad;

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
    }

    /// <summary>Comprimento total de uma polilinha, em metros.</summary>
    public static double Length(IReadOnlyList<GeoPoint> line)
    {
        var total = 0.0;
        for (var i = 1; i < line.Count; i++)
        {
            total += Distance(line[i - 1], line[i]);
        }
        return total;
    }

    /// <summary>
    /// Menor distância, em metros, entre um ponto e uma polilinha — considerando os segmentos,
    /// não apenas os vértices. Sem isso, um ponto no meio de um segmento longo seria reportado
    /// como distante mesmo estando exatamente sobre a via.
    /// </summary>
    public static double DistanceToPolyline(GeoPoint p, IReadOnlyList<GeoPoint> line)
    {
        if (line.Count == 0)
        {
            return double.PositiveInfinity;
        }
        if (line.Count == 1)
        {
            return Distance(p, line[0]);
        }

        // Plano local em metros, ancorado no ponto consultado.
        var mPerDegLat = 111_132.0;
        var mPerDegLon = 111_320.0 * Math.Cos(p.Lat * DegToRad);

        double X(GeoPoint q) => (q.Lon - p.Lon) * mPerDegLon;
        double Y(GeoPoint q) => (q.Lat - p.Lat) * mPerDegLat;

        var best = double.PositiveInfinity;

        for (var i = 1; i < line.Count; i++)
        {
            double ax = X(line[i - 1]), ay = Y(line[i - 1]);
            double bx = X(line[i]), by = Y(line[i]);

            var dx = bx - ax;
            var dy = by - ay;
            var lenSq = (dx * dx) + (dy * dy);

            double cx, cy;
            if (lenSq < 1e-9)
            {
                cx = ax;
                cy = ay;
            }
            else
            {
                // Projeção do ponto (origem) sobre o segmento, com clamp nas extremidades.
                var t = Math.Clamp(-((ax * dx) + (ay * dy)) / lenSq, 0.0, 1.0);
                cx = ax + (t * dx);
                cy = ay + (t * dy);
            }

            var d = Math.Sqrt((cx * cx) + (cy * cy));
            if (d < best)
            {
                best = d;
            }
        }

        return best;
    }

    /// <summary>
    /// Distância percorrida ao longo da polilinha até o ponto de maior aproximação de
    /// <paramref name="p"/>. Localiza onde, dentro do segmento, o ponto consultado se encaixa.
    /// </summary>
    public static double DistanceAlongToClosest(IReadOnlyList<GeoPoint> line, GeoPoint p)
    {
        if (line.Count < 2)
        {
            return 0;
        }

        var mPerDegLat = 111_132.0;
        var mPerDegLon = 111_320.0 * Math.Cos(p.Lat * DegToRad);

        double X(GeoPoint q) => (q.Lon - p.Lon) * mPerDegLon;
        double Y(GeoPoint q) => (q.Lat - p.Lat) * mPerDegLat;

        var best = double.PositiveInfinity;
        var bestAlong = 0.0;
        var travelled = 0.0;

        for (var i = 1; i < line.Count; i++)
        {
            double ax = X(line[i - 1]), ay = Y(line[i - 1]);
            double bx = X(line[i]), by = Y(line[i]);

            var dx = bx - ax;
            var dy = by - ay;
            var lenSq = (dx * dx) + (dy * dy);
            var segment = Distance(line[i - 1], line[i]);

            var t = lenSq < 1e-9 ? 0 : Math.Clamp(-((ax * dx) + (ay * dy)) / lenSq, 0.0, 1.0);
            var cx = ax + (t * dx);
            var cy = ay + (t * dy);
            var d = Math.Sqrt((cx * cx) + (cy * cy));

            if (d < best)
            {
                best = d;
                bestAlong = travelled + (t * segment);
            }

            travelled += segment;
        }

        return bestAlong;
    }

    /// <summary>
    /// Pontos amostrados ao longo da polilinha numa janela em torno da posição
    /// <paramref name="centerAlong"/>, a cada <paramref name="stepMeters"/>.
    ///
    /// Amostrar a linha, e não seus vértices, torna a comparação independente da densidade de
    /// vértices que o provedor devolve — um segmento com 3 vértices e outro com 300 são
    /// avaliados do mesmo jeito.
    /// </summary>
    public static List<GeoPoint> SampleWindow(
        IReadOnlyList<GeoPoint> line,
        double centerAlong,
        double halfWindowMeters,
        double stepMeters)
    {
        var samples = new List<GeoPoint>();
        if (line.Count == 0)
        {
            return samples;
        }
        if (line.Count == 1)
        {
            samples.Add(line[0]);
            return samples;
        }

        var total = Length(line);
        var from = Math.Max(0, centerAlong - halfWindowMeters);
        var to = Math.Min(total, centerAlong + halfWindowMeters);

        for (var s = from; s <= to; s += stepMeters)
        {
            samples.Add(PointAtDistance(line, s));
        }

        if (samples.Count == 0)
        {
            samples.Add(PointAtDistance(line, centerAlong));
        }

        return samples;
    }

    /// <summary>
    /// Menor distância entre um ponto e um conjunto de polilinhas — usado para comparar contra
    /// uma rua inteira, que no OSM costuma vir partida em vários trechos.
    /// </summary>
    public static double DistanceToPolylines(GeoPoint p, IReadOnlyList<IReadOnlyList<GeoPoint>> lines)
    {
        var best = double.PositiveInfinity;
        foreach (var line in lines)
        {
            var d = DistanceToPolyline(p, line);
            if (d < best)
            {
                best = d;
            }
        }
        return best;
    }

    /// <summary>
    /// Pontos amostrados ao longo da polilinha, espaçados de <paramref name="spacingMeters"/> e
    /// recuados de <paramref name="edgeMarginMeters"/> das extremidades.
    ///
    /// O recuo importa: o flowSegmentData resolve mal perto de cruzamento, onde a chance de
    /// encaixar na via transversal é maior.
    /// </summary>
    public static List<GeoPoint> SampleAlong(
        IReadOnlyList<GeoPoint> line,
        double spacingMeters,
        double edgeMarginMeters,
        int maxPoints)
    {
        var result = new List<GeoPoint>();
        if (line.Count < 2 || maxPoints <= 0)
        {
            return result;
        }

        var total = Length(line);

        // Via curta demais para recuar as duas pontas: usa o ponto médio.
        if (total <= (2 * edgeMarginMeters))
        {
            result.Add(PointAtDistance(line, total / 2));
            return result;
        }

        var usable = total - (2 * edgeMarginMeters);
        var count = Math.Max(1, (int)Math.Floor(usable / spacingMeters) + 1);
        count = Math.Min(count, maxPoints);

        if (count == 1)
        {
            result.Add(PointAtDistance(line, total / 2));
            return result;
        }

        var step = usable / (count - 1);
        for (var i = 0; i < count; i++)
        {
            result.Add(PointAtDistance(line, edgeMarginMeters + (i * step)));
        }

        return result;
    }

    /// <summary>Ponto situado a <paramref name="target"/> metros do início da polilinha.</summary>
    public static GeoPoint PointAtDistance(IReadOnlyList<GeoPoint> line, double target)
    {
        if (line.Count == 0)
        {
            throw new ArgumentException("Polilinha vazia.", nameof(line));
        }
        if (line.Count == 1 || target <= 0)
        {
            return line[0];
        }

        var travelled = 0.0;
        for (var i = 1; i < line.Count; i++)
        {
            var segment = Distance(line[i - 1], line[i]);
            if (travelled + segment >= target)
            {
                var t = segment < 1e-9 ? 0 : (target - travelled) / segment;
                return new GeoPoint(
                    line[i - 1].Lat + (t * (line[i].Lat - line[i - 1].Lat)),
                    line[i - 1].Lon + (t * (line[i].Lon - line[i - 1].Lon)));
            }
            travelled += segment;
        }

        return line[^1];
    }
}
