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
        }

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
            string dirname = System.IO.Path.GetDirectoryName(projectPath);
            if (!System.IO.Directory.Exists(dirname))
            {
                System.IO.Directory.CreateDirectory(dirname);
            }

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
