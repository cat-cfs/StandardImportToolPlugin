using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CBMSIT.Mapping;
using Newtonsoft.Json.Linq;

namespace StandardImportToolPlugin
{
    public class JsonConfigLoader
    {
        public void Load(string json)
        {

            JObject obj = JObject.Parse(json);
            string outputPath = (string)obj["outputpath"];

            Sitplugin sitplugin = new Sitplugin(outputPath, false);

            var mappingConfig = obj["mapping"];
            MapSpatialUnits(sitplugin, mappingConfig);

        }

        private static void MapSpatialUnits(Sitplugin sitplugin, JToken mappingConfig)
        {
            if (!Enum.TryParse((string)mappingConfig["mappingmode"], out SpatialUnitClassifierMode mode))
            {
                throw new ArgumentException("expected one of the SpatialUnitClassifierMode enum values");
            }
            switch (mode)
            {
                case SpatialUnitClassifierMode.JoinedAdminEcoClassifier:
                    sitplugin.SetSPUMapping((string)mappingConfig["SPUClassifier"]);
                    IDictionary<string, JToken> spuValueMap = (JObject)mappingConfig["adminmapping"];
                    foreach (var item in spuValueMap)
                    {
                        sitplugin.MapSpatialUnit(item.Key,
                            item.Value["adminboundary"].ToString(),
                            item.Value["ecoboundary"].ToString());
                    }
                    break;
                case SpatialUnitClassifierMode.SeperateAdminEcoClassifiers:
                    sitplugin.SetAdminEcoMapping(
                        (string)mappingConfig["AdminClassifier"],
                        (string)mappingConfig["EcoClassifier"]);
                    IDictionary<string, JToken> adminValueMap = (JObject)mappingConfig["adminmapping"];
                    foreach (var item in adminValueMap)
                    {
                        sitplugin.MapAdminBoundary(item.Key, item.Value.ToString());
                    }
                    IDictionary<string, JToken> ecoValueMap = (JObject)mappingConfig["ecomapping"];
                    foreach (var item in ecoValueMap)
                    {
                        sitplugin.MapEcoBoundary(item.Key, item.Value.ToString());
                    }
                    break;
                case SpatialUnitClassifierMode.SingleDefaultSpatialUnit:]
                    sitplugin.SetSingleSpatialUnit((int)mappingConfig["DefaultSPUID"]);
                    break;
            }
        }
    }
}
