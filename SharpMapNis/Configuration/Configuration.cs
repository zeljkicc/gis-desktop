using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GISprojekat4.Configuration
{
    public class Configuration
    {
        public Dictionary<string, PointLayerConfiguration> pointLayersDictionary;
        public Dictionary<string, LineLayerConfiguration> lineLayersDictionary;
        public Dictionary<string, PolygonLayerConfiguration> polygonLayersDictionary;

        public Configuration()
        {
            pointLayersDictionary = new Dictionary<string, PointLayerConfiguration>();
            lineLayersDictionary = new Dictionary<string, LineLayerConfiguration>();
            polygonLayersDictionary = new Dictionary<string, PolygonLayerConfiguration>();
        }

        public void saveConfiguration(string type, string layerName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "styles\\" + layerName + ".json");
            string json = null;
            switch (type)
            {              
                case "point":
                    PointLayerConfiguration plc = new PointLayerConfiguration();
                    pointLayersDictionary.TryGetValue(layerName, out plc);
                   json = Newtonsoft.Json.JsonConvert.SerializeObject(plc, Newtonsoft.Json.Formatting.Indented);
        
                    break;
                case "line":
                    LineLayerConfiguration llc = new LineLayerConfiguration();
                    lineLayersDictionary.TryGetValue(layerName, out llc);
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(llc, Newtonsoft.Json.Formatting.Indented);
                    break;
                case "polygon":
                    PolygonLayerConfiguration pollc = new PolygonLayerConfiguration();
                    polygonLayersDictionary.TryGetValue(layerName, out pollc);
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(pollc, Newtonsoft.Json.Formatting.Indented);
                    break;
                    
            }

            if(json!=null)
            System.IO.File.WriteAllText(filePath, json);
            
        }

        public bool loadConfiguration(string type, string layerName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "styles\\" + layerName + ".json");

            string text = "";
            if(File.Exists(filePath)){
                text = System.IO.File.ReadAllText(filePath);
                switch (type)
                {
                    case "point":
                        PointLayerConfiguration plc = new PointLayerConfiguration();
                        plc = Newtonsoft.Json.JsonConvert.DeserializeObject<PointLayerConfiguration>(text);
                        pointLayersDictionary.Add(layerName, plc);
                        return true;
                        break;
                    case "line":
                        LineLayerConfiguration llc = new LineLayerConfiguration();
                        llc = Newtonsoft.Json.JsonConvert.DeserializeObject<LineLayerConfiguration>(text);
                        lineLayersDictionary.Add(layerName, llc);
                        return true;
                        break;
                    case "polygon":
                        PolygonLayerConfiguration pollc = new PolygonLayerConfiguration();
                        pollc = Newtonsoft.Json.JsonConvert.DeserializeObject<PolygonLayerConfiguration>(text);
                        polygonLayersDictionary.Add(layerName, pollc);
                        return true;
                        break;

                }
            }

                return false;
            
        }

        /*   public Configuration DeserializeConfiguration()
           {

           } */




    }
}
