// Copyright (C) Her Majesty the Queen in Right of Canada,
//  as represented by the Minister of Natural Resources Canada

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
        public Sitplugin Load(string json)
        {

            JObject obj = JObject.Parse(json);
            string outputPath = (string)obj["output_path"];

            var importConfig = obj["import_config"];
            CBMSIT.UserData.UserDataSet userData = null;
            if (importConfig["path"] != null)
            {
                userData = Sitplugin.ParseSITData(
                    path: (string)importConfig["path"],
                    AgeClassTableName: (string)importConfig["ageclass_table_name"],
                    ClassifiersTableName: (string)importConfig["classifiers_table_name"],
                    DisturbanceEventsTableName: (string)importConfig["disturbance_events_table_name"],
                    DisturbanceTypesTableName: (string)importConfig["disturbance_types_table_name"],
                    InventoryTableName: (string)importConfig["inventory_table_name"],
                    TransitionRulesTableName: (string)importConfig["transition_rules_table_name"],
                    YieldTableName: (string)importConfig["yield_table_name"]
                    );
            }
            else if (importConfig["ageclass_path"] != null)
            {
                userData = Sitplugin.ParseSITDataText(
                    ageClassPath: (string)importConfig["ageclass_path"],
                    classifiersPath: (string)importConfig["classifiers_path"],
                    disturbanceEventsPath: (string)importConfig["disturbance_events_path"],
                    disturbanceTypesPath: (string)importConfig["disturbance_types_path"],
                    inventoryPath: (string)importConfig["inventory_path"],
                    transitionRulesPath: (string)importConfig["transition_rules_path"],
                    yieldPath: (string)importConfig["yield_path"]);
            }
            else
            {
                throw new Exception("error in import_config section");
            }


            Sitplugin sitplugin = new Sitplugin(outputPath, false, userData);

            var mappingConfig = obj["mapping_config"];
            MapSpatialUnits(sitplugin, mappingConfig["spatial_units"]);
            MapSpecies(sitplugin, mappingConfig["species"]);
            MapNonForest(sitplugin, mappingConfig["nonforest"]);
            MapDisturbanceTypes(sitplugin, mappingConfig["disturbance_types"]);

            return sitplugin;
        }
        private static void MapDisturbanceTypes(Sitplugin sitplugin, JToken mappingConfig)
        {
            foreach (var item in mappingConfig["disturbance_type_mapping"])
            {
                sitplugin.MapDisturbanceType(
                    (string)item["user_dist_type"],
                    (string)item["default_dist_type"]);
            }
        }

        private static void MapSpecies(Sitplugin sitplugin, JToken mappingConfig)
        {
            sitplugin.SetSpeciesClassifier((string)mappingConfig["species_classifier"]);
            foreach (var item in mappingConfig["species_mapping"]) {
                sitplugin.MapSpecies(
                    (string)item["user_species"],
                    (string)item["default_species"]);
            }
        }

        private static void MapNonForest(Sitplugin sitplugin, JToken mappingConfig)
        {
            if(mappingConfig == null || !mappingConfig.HasValues)
            {
                return;
            }
            sitplugin.SetNonForestClassifier((string)mappingConfig["nonforest_classifier"]);
            foreach (var item in mappingConfig["nonforest_mapping"])
            {
                sitplugin.MapNonForest((string)item["user_nonforest_type"], (string)item["default_nonforest_type"]);
            }
        }
        private static void MapSpatialUnits(Sitplugin sitplugin, JToken mappingConfig)
        {
            if (!Enum.TryParse((string)mappingConfig["mapping_mode"], out SpatialUnitClassifierMode mode))
            {
                throw new ArgumentException("expected one of the SpatialUnitClassifierMode enum values");
            }
            switch (mode)
            {
                case SpatialUnitClassifierMode.JoinedAdminEcoClassifier:
                    sitplugin.SetSPUMapping((string)mappingConfig["spu_classifier"]);
                    foreach (var item in mappingConfig["spu_mapping"])
                    {
                        sitplugin.MapSpatialUnit(
                            (string)item["user_spatial_unit"],
                            (string)item["default_spatial_unit"]["admin_boundary"],
                            (string)item["default_spatial_unit"]["eco_boundary"]);
                    }
                    break;
                case SpatialUnitClassifierMode.SeperateAdminEcoClassifiers:
                    sitplugin.SetAdminEcoMapping(
                        (string)mappingConfig["admin_classifier"],
                        (string)mappingConfig["eco_classifier"]);
                    foreach (var item in mappingConfig["admin_mapping"])
                    {
                        sitplugin.MapAdminBoundary(
                            (string)item["user_admin_boundary"],
                            (string)item["default_admin_boundary"]);
                    }
                    foreach (var item in mappingConfig["eco_mapping"])
                    {
                        sitplugin.MapEcoBoundary(
                            (string)item["user_eco_boundary"],
                            (string)item["default_eco_boundary"]);
                    }
                    break;
                case SpatialUnitClassifierMode.SingleDefaultSpatialUnit:
                    sitplugin.SetSingleSpatialUnit((int)mappingConfig["default_spuid"]);
                    break;
            }
        }
    }
}
