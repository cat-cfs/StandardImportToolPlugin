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
        }


        private static void Method2()
        {

        }

        /// <summary>
        /// Import an SIT formatted dataset. 
        /// mdb, accdb, xls, or xlsx are supported
        /// </summary>
        private static void Method1()
        {
            string projectPath = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Projects\tutorial 6\tutorial 6";
            string dirname = System.IO.Path.GetDirectoryName(projectPath);
            if (!System.IO.Directory.Exists(dirname))
            {
                System.IO.Directory.CreateDirectory(dirname);
            }

            var data = Sitplugin.ParseSITData(
                path: @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Tutorials\Tutorial 6\Tutorial6.xls",
                AgeClassTableName: "$AgeClasses",
                ClassifiersTableName: "$Transitions",
                DisturbanceEventsTableName: "$DistEvents",
                DisturbanceTypesTableName: "$DistType",
                InventoryTableName: "$Inventory",
                TransitionRulesTableName: "$Transitions",
                YieldTableName: "$Growth");

            Sitplugin sitplugin = new Sitplugin(
                outputPath: projectPath,
                initializeMapping: false,
                dataset: data);

            sitplugin.SetSingleSpatialUnit(42);//see archive index tblSPUDefault/tblAdminBoundaryDefault/tblEcoBoundaryDefault for code definitions
            sitplugin.MapAdminBoundary("My admin boundary classifier value 1", "British Columbia");
            sitplugin.MapAdminBoundary("My admin boundary classifier value 2", "British Columbia");

            sitplugin.MapEcoBoundary("My eco boundary classifier", "Pacific Maritime");
            

            sitplugin.Import();
        }
    }
}
