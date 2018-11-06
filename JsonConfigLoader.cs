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

            var importConfig = obj["importconfig"];
            var userData = Sitplugin.ParseSITData(
                path: (string)importConfig["path"],
                AgeClassTableName: (string)importConfig["ageclass_table_name"],
                ClassifiersTableName: (string)importConfig["classifiers_table_name"],
                DisturbanceEventsTableName: (string)importConfig["disturbance_events_table_name"],
                DisturbanceTypesTableName: (string)importConfig["disturbance_types_table_name"],
                InventoryTableName: (string)importConfig["inventory_table_name"],
                TransitionRulesTableName: (string)importConfig["transition_rules_table_name"],
                YieldTableName: (string)importConfig["yield_table_name"]
                );

            Sitplugin sitplugin = new Sitplugin(outputPath, false);

            var mappingConfig = obj["mapping"];
            MapSpatialUnits(sitplugin, mappingConfig["spatialunits"]);
            MapSpecies(sitplugin, mappingConfig["species"]);
            MapNonForest(sitplugin, mappingConfig["nonforest"]);
            MapDisturbanceTypes(sitplugin, mappingConfig["disturbancetypes"]);

        }
        private static void MapDisturbanceTypes(Sitplugin sitplugin, JToken mappingConfig)
        {
            IDictionary<string, JToken> distTypeMap = (JObject)mappingConfig["disturbancetypemapping"];
            foreach (var item in distTypeMap)
            {
                sitplugin.MapDisturbanceType(item.Key, (string)item.Value);
            }
        }

        private static void MapSpecies(Sitplugin sitplugin, JToken mappingConfig)
        {

            sitplugin.SetSpeciesClassifier((string)mappingConfig["speciesclassifier"]);
            IDictionary<string, JToken> speciesMap = (JObject)mappingConfig["speciesmapping"];
            foreach (var item in speciesMap) {
                sitplugin.MapSpecies(item.Key, (string)item.Value);
            }
        }

        private static void MapNonForest(Sitplugin sitplugin, JToken mappingConfig)
        {
            if(mappingConfig == null)
            {
                return;
            }
            sitplugin.SetNonForestClassifier((string)mappingConfig["nonforestclassifier"]);
            IDictionary<string, JToken> nonforestMap = (JObject)mappingConfig["nonforestmapping"];
            foreach (var item in nonforestMap)
            {
                sitplugin.MapNonForest(item.Key, (string)item.Value);
            }
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
                    IDictionary<string, JToken> spuValueMap = (JObject)mappingConfig["spumapping"];
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
                case SpatialUnitClassifierMode.SingleDefaultSpatialUnit:
                    sitplugin.SetSingleSpatialUnit((int)mappingConfig["DefaultSPUID"]);
                    break;
            }
        }
    }
}
