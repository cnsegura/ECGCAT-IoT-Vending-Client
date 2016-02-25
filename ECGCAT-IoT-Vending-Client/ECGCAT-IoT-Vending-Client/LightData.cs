using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECGCAT_IoT_Vending_Client
{ 
    public class LightData
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public RgbData rgbData { get; set; }
        public float Lux { get; set; }
        public float ColorTempinK { get; set; }
    }
}
