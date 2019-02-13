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
            Sitplugin sitplugin = null;
            JObject obj = JObject.Parse(json);
            string outputPath = (string)obj["output_path"];
            var mappingConfig = obj["mapping_config"];
            if(mappingConfig == null)
            {
                throw new Exception("missing mapping_config section");
            }
            bool initialize_mapping = mappingConfig["initialize_mapping"] == null ?
                false : (bool)mappingConfig["initialize_mapping"];

            CBMSIT.UserData.UserDataSet userData = null;
            if (obj["import_config"] != null)
            {
                var importConfig = obj["import_config"];

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
                sitplugin = new Sitplugin(outputPath, initialize_mapping, userData);
            }
            else if(obj["data"] != null)
            {
                sitplugin = new Sitplugin(outputPath, initialize_mapping);
                LoadConfigDataObjects(sitplugin, obj["data"]);
            }
            else
            {
                throw new Exception("expected one of 'import_config', or 'data' in configuration");
            }

            MapSpatialUnits(sitplugin, mappingConfig["spatial_units"]);
            MapSpecies(sitplugin, mappingConfig["species"]);
            MapNonForest(sitplugin, mappingConfig["nonforest"]);
            MapDisturbanceTypes(sitplugin, mappingConfig["disturbance_types"]);

            return sitplugin;
        }
        private static T Unpack<T>(JToken t, string key, T fallback)
        {
            return t[key] == null ? fallback : t[key].Value<T>();
        }
        private static void LoadConfigDataObjects(Sitplugin plugin, JToken dataConfig)
        {
            plugin.AddAgeClasses(
                (int)dataConfig["age_class"]["age_class_size"],
                (int)dataConfig["age_class"]["num_age_classes"]);

            foreach (var c in dataConfig["classifiers"]) {
                plugin.AddClassifier((string)c);
            }

            var parseClassifierSet = new Func<JToken, string>((t) =>
            {
                return String.Join(",", t.Select(a => (string)a));
            });
            foreach(var i in dataConfig["inventory"])
            {

                plugin.AddInventory(
                    classifierSet: parseClassifierSet(i["classifier_set"]),
                    area: (double)i["area"],
                    age: (int)i["age"],
                    spatialReference: Unpack(i, "spatial_reference", -1),
                    delay: Unpack(i, "delay", 0),
                    unfcccLandClass: Unpack(i, "unfccc_land_class", 0),
                    HistoricDisturbance: Unpack(i, "historic_disturbance", (string)null),
                    MostRecentDisturbance: Unpack(i, "last_pass_disturbance", (string)null));
            }

            foreach(var i in dataConfig["disturbance_events"])
            {
                plugin.AddDisturbanceEvent(
                    classifierSet: parseClassifierSet(i["classifier_set"]),
                    disturbanceType: (string)i["disturbance_type"],
                    timestep: (int)i["time_step"],
                    target: (double)i["target"],
                    targetType: (string)i["target_type"],
                    sort: (string)i["sort"],
                    spatialReference: Unpack(i, "spatial_reference", -1),
                    ageMin: Unpack(i, "age_min", -1),
                    ageMax: Unpack(i, "age_max", -1),
                    efficiency: Unpack(i, "efficiency", 1.0),
                    eligibility: i["eligibility"]?.Select(a => (double)a).ToArray());
            }

            foreach (var i in dataConfig["yield"])
            {
                plugin.AddYield(
                    classifierSet: parseClassifierSet(i["classifier_set"]),
                    leadingSpeciesClassifierValue: (string)i["leading_species_classifier_value"],
                    values: i["values"].Select(a => (double)a).ToList());
            }

            if (dataConfig["transition_rules"] != null)
            {
                foreach (var i in dataConfig["transition_rules"])
                {
                    plugin.AddTransitionRule(
                        classifierSetSource: parseClassifierSet(i["classifier_set_source"]),
                        classifierSetTarget: parseClassifierSet(i["classifier_set_target"]),
                        disturbanceType: (string)i["disturbance_type"],
                        percent: Unpack(i, "percent", 100.0),
                        spatialReference: Unpack(i, "spatial_reference", -1),
                        ageMin: Unpack(i, "age_min", -1),
                        ageMax: Unpack(i, "age_max", -1),
                        resetAge: Unpack(i, "reset_age", -1),
                        regenDelay: Unpack(i, "regen_delay", 0));
                }
            }

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
