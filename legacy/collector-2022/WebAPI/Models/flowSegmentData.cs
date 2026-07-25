using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Controllers
{
    public class flowSegmentData
    {
        public string frc { get; set; }
        public int currentSpeed { get; set; }
        public int freeFlowSpeed { get; set; }
        public int currentTravelTime { get; set; }
        public int freeFlowTravelTime { get; set; }
        public int confidence { get; set; }
        public bool roadClosure { get; set; }
        public Coordenadas coordinates { get; set; }
        public string @version { get; set; }
        public int nroRua { get; set; }

        public override string ToString()
        {
            var retorno = "";
            if (coordinates != null)
            {
                if (coordinates.coordinate != null)
                {
                    if (coordinates.coordinate.Count > 0)
                    {
                        retorno = " lat:" + coordinates.coordinate[0].latitude + "," + " long:" + coordinates.coordinate[0].longitude + ";" +
                            "currentSpeed:" + currentSpeed + ";" +
                            "freeFlowSpeed:" + freeFlowSpeed + ";" +
                            "currentTravelTime:" + currentTravelTime + ";" +
                            "freeFlowTravelTime:" + freeFlowTravelTime + ";" +
                            "frc:"+frc;
                    }
                }
            }
            return retorno;
        }

    }

    public class coordinate
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Coordenadas
    {
        public List<coordinate> coordinate { get; set; }
    }

}