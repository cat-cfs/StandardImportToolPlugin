// Copyright (C) Her Majesty the Queen in Right of Canada,
//  as represented by the Minister of Natural Resources Canada

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardImportToolPlugin
{
    class ExampleProgram
    {
        static void Main(string[] args)
        {
            Method1();
            Method2();
            Method3();
        }

        /// <summary>
        /// use a json configuration
        /// </summary>
        private static void Method3()
        {
            //creates a config object equivalent to Method1
            var config = new
            {
                output_path = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Projects\tutorial6_json\tutorial6_json.mdb",
                import_config = new
                {
                    path = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Tutorials\Tutorial 6\Tutorial6.xls",
                    ageclass_table_name = "AgeClasses$",
                    classifiers_table_name = "Classifiers$",
                    disturbance_events_table_name = "DistEvents$",
                    disturbance_types_table_name = "DistType$",
                    inventory_table_name = "Inventory$",
                    transition_rules_table_name = "Transitions$",
                    yield_table_name = "Growth$"
                },
                mapping_config = new
                {
                    spatial_units = new
                    {
                        mapping_mode = "SingleDefaultSpatialUnit",
                        default_spuid = 42
                    },
                    disturbance_types = new
                    {
                        disturbance_type_mapping = new[] 
                        {
                            new { user_dist_type= "Fire", default_dist_type="Wildfire" },
                            new { user_dist_type= "Firewood collection", default_dist_type="Firewood Collection - post logging" },
                            new { user_dist_type= "Clearcut", default_dist_type="Clear-cut with slash-burn" },
                            new { user_dist_type= "Afforestation", default_dist_type="Afforestation" },
                            new { user_dist_type= "Hurricane", default_dist_type="Generic 50% mortality" },
                        }
                    },
                    species = new
                    {
                        species_classifier = "Species",
                        species_mapping = new[]
                        {
                            new { user_species = "Hispaniolan pine", default_species = "Pine" },
                            new { user_species = "Nonforest", default_species = "Not stocked" },
                            new { user_species = "Improved pine stock", default_species = "Pine" }
                        }
                    },
                    nonforest = new
                    {
                        nonforest_classifier = "Forest type",
                        nonforest_mapping = new[]
                        {
                            new { user_nonforest_type = "Afforestation", default_nonforest_type = "Gleysolic" },
                            new { user_nonforest_type = "Natural forest", default_nonforest_type = Sitplugin.ForestOnly },
                            new { user_nonforest_type = "Control", default_nonforest_type = Sitplugin.ForestOnly  }
                        }
                    }
                }
            };
            var jsonObject = JObject.FromObject(config);
            string json = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
            JsonConfigLoader jsonConfigLoader = new JsonConfigLoader();
            Sitplugin sitplugin = jsonConfigLoader.Load(json);
            sitplugin.Import();
        }
        //"Hispaniolan pine", "Pine");
        //"Nonforest", "Not stocked");
        //"Improved pine stock", "Pine");
        /// <summary>
        /// build up a dataset with code instead of import an SIT formatted dataset
        /// </summary>
        private static void Method2()
        {
            string projectPath = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Projects\myproject\myproject.mdb";
            string dirname = System.IO.Path.GetDirectoryName(projectPath);
            if (!System.IO.Directory.Exists(dirname))
            {
                System.IO.Directory.CreateDirectory(dirname);
            }
            //when initialize mapping is true, incoming sit 
            //data must be named exactly as the definitions in the archive index
            Sitplugin sitplugin = new Sitplugin(
                outputPath: projectPath,
                initializeMapping: true);

            //define ten year age class size, with 20 values (200 years)
            int numAgeClasses = 20;
            sitplugin.AddAgeClasses(10, numAgeClasses);

            sitplugin.AddClassifier("admin");
            sitplugin.AddClassifier("eco");
            sitplugin.SetAdminEcoMapping("admin", "eco");

            sitplugin.AddClassifier("species");
            sitplugin.SetSpeciesClassifier("species");

            sitplugin.AddClassifier("group");

            sitplugin.AddInventory(
                classifierSet: "British Columbia,Pacific Maritime,Spruce,g1",//note exact naming is required when " initializeMapping: true"
                age: 100,
                area: 10,
                spatialReference: 100,
                HistoricDisturbance: "Wildfire",
                MostRecentDisturbance: "Clearcut harvesting with salvage");

            List<double> FakeGrowthCurve = Enumerable.Range(0, numAgeClasses)
                .Select(a => 200 / (1 + Math.Exp(-0.5 * (a - numAgeClasses/2.0)))).ToList();

            sitplugin.AddYield(
                classifierSet: "British Columbia,Pacific Maritime,Spruce,?",
                leadingSpeciesClassifierValue: "Spruce",
                values: FakeGrowthCurve);

            sitplugin.AddDisturbanceEvent(
                classifierSet: "?,?,?,?",//when spatial reference is used, events do not use a classifier set
                disturbanceType: "Clearcut harvesting with salvage",
                timestep: 100,
                target: 5,
                targetType: "Area",
                spatialReference: 100);

            sitplugin.Import();
        }

        /// <summary>
        /// Import an SIT formatted dataset. 
        /// mdb, accdb, xls, or xlsx are supported
        /// </summary>
        private static void Method1()
        {
            string excelPath = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Tutorials\Tutorial 6\Tutorial6.xls";
            string projectPath = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Projects\tutorial6\tutorial6.mdb";

            var data = Sitplugin.ParseSITData(
                path: excelPath,
                AgeClassTableName: "AgeClasses$",
                ClassifiersTableName: "Classifiers$",
                DisturbanceEventsTableName: "DistEvents$",
                DisturbanceTypesTableName: "DistType$",
                InventoryTableName: "Inventory$",
                TransitionRulesTableName: "Transitions$",
                YieldTableName: "Growth$");

            Sitplugin sitplugin = new Sitplugin(
                outputPath: projectPath,
                initializeMapping: false,
                dataset: data);

            sitplugin.SetSingleSpatialUnit(42);
            //see archive index tblSPUDefault/tblAdminBoundaryDefault/tblEcoBoundaryDefault for code definitions

            sitplugin.SetSpeciesClassifier("Species");//the classifier name as defined in the spreadsheet
            sitplugin.SetNonForestClassifier("Forest type");

            // in the following mappings, the left value is something that appears in the import data,
            // and the right value is something that appears in the archive index database.
            sitplugin.MapSpecies("Hispaniolan pine", "Pine");
            sitplugin.MapSpecies("Nonforest", "Not stocked");
            sitplugin.MapSpecies("Improved pine stock", "Pine");

            sitplugin.MapNonForest("Afforestation", "Gleysolic");
            sitplugin.MapNonForest("Natural forest", Sitplugin.ForestOnly);
            sitplugin.MapNonForest("Control", Sitplugin.ForestOnly);

            sitplugin.MapDisturbanceType("Fire", "Wildfire");
            sitplugin.MapDisturbanceType("Firewood collection", "Firewood Collection - post logging");
            sitplugin.MapDisturbanceType("Clearcut", "Clear-cut with slash-burn");
            sitplugin.MapDisturbanceType("Afforestation", "Afforestation");
            sitplugin.MapDisturbanceType("Hurricane", "Generic 50% mortality");


            sitplugin.Import();
        }
    }
}
