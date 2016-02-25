using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECGCAT_IoT_Vending_Client
{
    public class SensorData
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public float TemperatureinF { get; set; }
        public float Pressureinmb { get; set; }
    }
}
