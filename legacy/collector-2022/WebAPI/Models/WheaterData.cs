using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Controllers
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Coord
    {
        public double lon { get; set; }
        public double lat { get; set; }
        public override string ToString()
        {
            var retorno = "";
            retorno += "lat:" + lat + ";";
            retorno += "lon:" + lon + ";";
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
            retorno += "id:" + id + ";";
            retorno += "main:"+main + ";";
            retorno += "description:" + description + ";";
            retorno += "icon:" + icon+";";
            return retorno;
        }
    }

    public class Main
    {
        public double temp { get; set; }
        public double feels_like { get; set; }
        public double temp_min { get; set; }
        public double temp_max { get; set; }
        public int pressure { get; set; }
        public int humidity { get; set; }
        public int sea_level { get; set; }
        public int grnd_level { get; set; }
        public override string ToString()
        {
            var retorno = "";
            retorno += "temp:" + temp + ";";
            retorno += "feels_like:" + feels_like + ";";
            retorno += "temp_min:" + temp_min + ";";
            retorno += "temp_max:" + temp_max + ";";
            retorno += "humidity:" + humidity + ";";
            retorno += "sea_level:" + sea_level + ";";
            retorno += "grnd_level:"+grnd_level + ";";
            return retorno;
        }
    }

    public class Wind
    {
        public double speed { get; set; }
        public int deg { get; set; }
        public double gust { get; set; }
        public override string ToString()
        {
            var retorno = "";
            retorno += "speed:" + speed + ";";
            retorno += "deg:" + deg + ";";
            retorno += "gust:" + gust + ";";
            return retorno;
        }
    }

    public class Clouds
    {
        public int all { get; set; }
        public override string ToString()
        {
            var retorno = "";
            retorno += "all:" + all + ";";
            return retorno;
        }
    }

    public class Sys
    {
        public int type { get; set; }
        public int id { get; set; }
        public string country { get; set; }
        public int sunrise { get; set; }
        public int sunset { get; set; }
        public override string ToString()
        {
            var retorno = "";
            retorno += "type:" + type + ";";
            retorno += "id:" + id + ";";
            retorno += "country:" + country + ";";
            retorno += "sunrise:" + sunrise + ";";
            retorno += "sunset:" + sunset + ";";
            return retorno;
        }
    }

    public class WheaterData
    {
        public int nroRua { get; set; }
        public Coord coord { get; set; }
        public List<Weather> weather { get; set; }
        public string @base { get; set; }
        public Main main { get; set; }
        public int visibility { get; set; }
        public Wind wind { get; set; }
        public Clouds clouds { get; set; }
        public int dt { get; set; }
        public Sys sys { get; set; }
        public int timezone { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int cod { get; set; }
        public override string ToString()
        {
            var retorno = "";
            retorno += coord.ToString();
            foreach (Weather w in weather)
                retorno += w.ToString();
            retorno += "@base:" + @base + ";";
            retorno += main.ToString();
            retorno += wind.ToString();
            retorno += clouds.ToString();
            retorno += "dt:" + dt + ";";
            retorno += sys.ToString();
            retorno += "timezone:" + timezone + ";";
            retorno += "id:" + id + ";";
            retorno += "name:" + name + ";";
            retorno += "cod:" + cod + ";";
            return retorno;
        }
    }



}