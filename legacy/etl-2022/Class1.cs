using System;
using System.Collections.Generic;
using System.Text;

namespace ConversorDeTxtParaSQL
{
    class WheaterData
    {
        public string lat { get; set; }
        public string longi { get; set; }
        public string timezone { get; set; }
        public string timezone_offset { get; set; }
        public Current current { get; set; }

        public override string ToString()
        {
            var retorno = "";

            retorno += "lat:"+ lat + ";";
            retorno += "longi:"+ longi + ";";
            retorno += "timezone:"+ timezone + ";";
            retorno += "timezone_offset:"+ timezone_offset + ";";
            retorno += current.ToString();

            return retorno;
        }
    }

    public class Current
    {
        public long dt { get; set; }
        public long sunrise { get; set; }
        public long sunset { get; set; }
        public decimal temp { get; set; }
        public decimal feels_like { get; set; }
        public decimal pressure { get; set; }
        public decimal humidity { get; set; }
        public decimal dew_point { get; set; }
        public decimal uvi { get; set; }
        public decimal clouds { get; set; }
        public decimal visibility { get; set; }
        public decimal wind_speed { get; set; }
        public decimal wind_deg { get; set; }
        public decimal wind_gust { get; set; }
        public Weather weather { get; set; }

        public override string ToString()
        {
            var retorno = "";

            retorno += "dt:" + dt + ";";
            retorno += "sunrise:" + sunrise + ";";
            retorno += "sunset:" + sunset + ";";
            retorno += "temp:" + temp + ";";
            retorno += "feels_like:" + feels_like + ";";
            retorno += "pressure:" + pressure + ";";
            retorno += "humidity:" + humidity + ";";
            retorno += "dew_point:" + dew_point + ";";
            retorno += "uvi:" + uvi + ";";
            retorno += "clouds:" + clouds + ";";
            retorno += "visibility:" + visibility + ";";
            retorno += "wind_speed:" + wind_speed + ";";
            retorno += "wind_deg:" + wind_deg + ";";
            retorno += "wind_gust:" + wind_gust + ";";


            retorno += weather.ToString();

            return retorno;
        }

    }

    public class Weather
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }

        public override string ToString()
        {
            var retorno = "";

            retorno = "id:" + id + ";" + "main:" + main + ";" + "description:" + description + ";" + "icon:" + icon + ";";

            return retorno;
        }
    }

    /*
         "lat": -26.913,
    "lon": -49.0814,
    "timezone": "America/Sao_Paulo",
    "timezone_offset": -10800,
    "current": {
        "dt": 1651114508,
        "sunrise": 1651052325,
        "sunset": 1651092534,
        "temp": 23.8,
        "feels_like": 24.69,
        "pressure": 1012,
        "humidity": 94,
        "dew_point": 22.78,
        "uvi": 0,
        "clouds": 76,
        "visibility": 10000,
        "wind_speed": 0.68,
        "wind_deg": 256,
        "wind_gust": 0.66,
        "weather": [
            {
                "id": 803,
                "main": "Clouds",
                "description": "nublado",
                "icon": "04n"
            }
        ]
    }
     */
}
