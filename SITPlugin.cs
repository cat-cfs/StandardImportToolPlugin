// Copyright (C) Her Majesty the Queen in Right of Canada,
//  as represented by the Minister of Natural Resources Canada

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

#region Toolbox DLL references

/*in order to build this, 
 * 1. Install the Operational-Scale CBM-CF3 Toolbox
 * 
 * 2. Add references to the following toolbox dll files:
 *    CBMSIT.dll
 *    StringUtilities.dll
 *    Interfaces.dll
 *    WoodStockImportTool.dll
 *    Global.dll
 * 
 *    These files can be found in your installation directory. For example
 *    here: C:\Program Files (x86)\Operational-Scale CBM-CFS3\CBMSIT.dll
 * 
 * 
 */
using CBMSIT.UserData;
using CBMSIT.Mapping;
using CBMSIT.Controllers;
using CBMSIT.ProjectCreation;
#endregion

using System.Reflection;
using log4net;
namespace StandardImportToolPlugin
{


    public class Sitplugin
    {
        public static string ForestOnly = Global.StringUtilities.GetStringResource("SLPCForestOnlyClassifierValue");// "Forest Only";

        const string DefaultArchiveIndexPath = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Admin\DBs\ArchiveIndex_Beta_Install.mdb";
        const string DefaultInputDBTemplatePath = @"C:\Program Files (x86)\Operational-Scale CBM-CFS3\Admin\DBs\InputDB_Template.mdb";

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const string DEFAULTDISTTYPES = "SELECT * FROM tblDisturbanceTypeDefault ";
        const string DEFAULTSPECIESTYPES = "SELECT * FROM tblSpeciesTypeDefault";
        const string DEFAULTNONFORESTTYPES_NAMES = "SELECT tblAfforestationPreTypeDefault.PreTypeID, tblAfforestationPreTypeDefault.Name, tblAfforestationPreTypeDefault.Description FROM tblAfforestationPreTypeDefault";
        const string DEFAULT_SPU_NAMES = "SELECT tblSPUDefault.SPUID as SPUID, [tblAdminBoundaryDefault]![AdminBoundaryName] & '-' & " + "[tblEcoBoundaryDefault]![EcoBoundaryName] AS SPUName, tblAdminBoundaryDefault.AdminBoundaryName, tblEcoBoundaryDefault.EcoBoundaryName FROM tblEcoBoundaryDefault INNER JOIN " + "(tblAdminBoundaryDefault INNER JOIN tblSPUDefault ON tblAdminBoundaryDefault.AdminBoundaryID = " + " tblSPUDefault.AdminBoundaryID) ON tblEcoBoundaryDefault.EcoBoundaryID = tblSPUDefault.EcoBoundaryID " + "ORDER BY [tblAdminBoundaryDefault]![AdminBoundaryName] & '-' & [tblEcoBoundaryDefault]![EcoBoundaryName]";
        const string DEFAULT_ADMIN_NAMES = "SELECT tblAdminBoundaryDefault.AdminBoundaryID, tblAdminBoundaryDefault.AdminBoundaryName FROM tblAdminBoundaryDefault";
        const string DEFAULT_ECO_NAMES = "SELECT * FROM tblEcoBoundaryDefault";

        string OutputPath;
        string aidbPath;
        string inputTemplatePath;
        CBMDBObject aidb;
        DefaultRows DefaultRows;
        UserDataSet data;
        MappingOptions mapping;

        private Dictionary<string, Classifier> Classifiers = new Dictionary<string, Classifier>();
        private Dictionary<int, HashSet<string>> ClassifierValues =
            new Dictionary<int, HashSet<string>>();

        /// <summary>
        /// Create a new sitplugin
        /// </summary>
        /// <param name="archiveIndexPath">the path to the archive index you 
        /// are using for default parameters</param>
        /// <param name="inputDbTemplatePath">the path to the project database
        /// template (empty project db)</param>
        /// <param name="outputPath">the output project path</param>
        /// <param name="initializeMapping">if true, initializes all default 
        /// mapped names, otherwise all mapping must be user specified</param>
        /// <param name="dataset">An SIT dataset</param>
        public Sitplugin(string archiveIndexPath, string inputDbTemplatePath, string outputPath,
            bool initializeMapping = true, UserDataSet dataset = null)
        {
            this.aidbPath = archiveIndexPath;
            this.inputTemplatePath = inputDbTemplatePath;
            this.OutputPath = outputPath;
            string dirname = System.IO.Path.GetDirectoryName(OutputPath);
            if (!System.IO.Directory.Exists(dirname))
            {
                System.IO.Directory.CreateDirectory(dirname);
            }
            aidb = new CBMDBObject(aidbPath);
            DefaultRows = FillDefaultRows(aidb);

            if (dataset == null)
            {
                data = InitializeDataset();
            }
            else
            {
                data = dataset;
                foreach (Classifier classifier in data.Classifiers)
                {
                    if (Classifiers.ContainsKey(classifier.Name))
                    {
                        throw new Exception(string.Format("Duplicate classifier name \"{0}\" detected", classifier.Name));
                    }
                    Classifiers.Add(classifier.Name, classifier);
                }
            }
            if (initializeMapping)
            {
                mapping = InitializeMapping(DefaultRows);
            }
            else
            {
                mapping = CreateEmptyMapping();
            }
        }

        /// <summary>
        /// Create a new sitplugin using the default toolbox installation directories.
        /// 
        /// This will only work if you have installed to the default directory, otherwise use 
        /// an other available constructor
        /// </summary>
        /// <param name="outputPath">The output project path</param>
        /// <param name="initializeMapping">if true, initializes all default 
        /// mapped names, otherwise all mapping must be user specified</param>
        /// <param name="dataset">An SIT dataset</param>
        /// <param name="archive_index_database_path">optional path to an archive index database, if unspecified, a default value is used</param>
        public Sitplugin(string outputPath, bool initializeMapping = true,
            UserDataSet dataset = null, string archive_index_database_path =null)
            : this(archive_index_database_path == null ? DefaultArchiveIndexPath : archive_index_database_path,
                  DefaultInputDBTemplatePath,
            outputPath, initializeMapping, dataset)
        {

        }
        /// <summary>
        /// Parse an SIT formatted database or excel file
        /// </summary>
        /// <param name="path">The path to an mdb or excel file in the SIT format</param>
        /// <param name="AgeClassTableName">name of table or worksheet with age classes</param>
        /// <param name="ClassifiersTableName">name of table or worksheet with classifiers</param>
        /// <param name="DisturbanceEventsTableName">name of table or worksheet with disturbance events</param>
        /// <param name="DisturbanceTypesTableName">name of table or worksheet with disturbance types</param>
        /// <param name="InventoryTableName">name of table or worksheet with inventory</param>
        /// <param name="TransitionRulesTableName">name of table or worksheet with transition rules</param>
        /// <param name="YieldTableName">name of table or worksheet with growth and yield</param>
        /// <returns>The populated SIT dataset</returns>
        public static UserDataSet ParseSITData(string path,
            string AgeClassTableName,
            string ClassifiersTableName,
            string DisturbanceEventsTableName,
            string DisturbanceTypesTableName,
            string InventoryTableName,
            string TransitionRulesTableName,
            string YieldTableName)
        {
            try
            {
                log.Info(string.Format("started importing {0}", path));
                CBMSIT.DBImport.DBImporter d = new CBMSIT.DBImport.DBImporter(
                    new CBMDBObject(path),
                    AgeClassTableName,
                    ClassifiersTableName,
                    DisturbanceEventsTableName,
                    DisturbanceTypesTableName,
                    "", //eligibilities are "pie in the sky"
                    InventoryTableName,
                    TransitionRulesTableName,
                    YieldTableName, false);

                d.Import();

                UserDataSet Data = new UserDataSet();
                Data.AgeClasses = d.AgeClasses;
                Data.Classifiers = d.Classifiers;
                Data.DisturbanceTypes = d.DisturbanceTypes;
                Data.DisturbanceEvents = d.DisturbanceEvents;
                Data.TransitionRules = d.TransitionRules;
                Data.Inventories = d.Inventories;
                Data.Yields = d.Yields;

                log.Info(string.Format("importing {0} done.", path));
                return Data;

            }
            catch (Exception ex)
            {
                log.Error("error parsing sit database", ex);
                throw ex;
            }

        }

        public static UserDataSet ParseSITDataText(
            string ageClassPath,
            string classifiersPath,
            string disturbanceEventsPath,
            string disturbanceTypesPath,
            string inventoryPath,
            string transitionRulesPath,
            string yieldPath)
        {
            try
            {
                log.Info("started importing SIT text");
                UserDataSet data = new UserDataSet();
                CBMSIT.TextImport.TextImporter importer = new CBMSIT.TextImport.TextImporter();
                data.AgeClasses = importer.ParseAgeClasses(ageClassPath);
                data.Classifiers = importer.ParseClassifiers(classifiersPath);
                data.DisturbanceTypes = importer.ParseDisturbanceTypes(disturbanceTypesPath);
                data.DisturbanceEvents = importer.ParseDisturbanceEvents(disturbanceEventsPath);

                data.Inventories = importer.ParseInventory(inventoryPath);
                data.TransitionRules = importer.ParseTransitionRules(transitionRulesPath);
                data.Yields = importer.ParseYields(yieldPath);
                return data;
            }
            catch (Exception ex)
            {
                log.Error("error parsing sit text", ex);
                throw ex;
            }
        }
        private void Validate()
        {

            if (mapping.SpatialUnitOptions.ClassifierMode == SpatialUnitClassifierMode.SeperateAdminEcoClassifiers)
            {
                var missingEco = mapping.SpatialUnitOptions.EcoClassifier.ClassifierValues.Select(a => a.Description).Except(
                        mapping.SpatialUnitOptions.EcoClassifierMapping.Keys);
                if (missingEco.Any())
                {
                    throw new ArgumentException(String.Format("ecoboundary {0} is not mapped to archive index", missingEco.First()));
                }

                foreach (string cval in mapping.SpatialUnitOptions.EcoClassifier.ClassifierValues.Select(a => a.Description))
                {
                    string mapped = mapping.SpatialUnitOptions.EcoClassifierMapping[cval];
                    if (!DefaultRows.DefaultEcoBoundaryRowsByName.ContainsKey(mapped))
                    {
                        throw new Exception(String.Format("cannot find eco boundary {0} in the archive index", mapped));
                    }
                }


                var missingadmin = mapping.SpatialUnitOptions.AdminClassifier.ClassifierValues.Select(a => a.Description).Except(
                    mapping.SpatialUnitOptions.AdminClassifierMapping.Keys);
                if (missingadmin.Any())
                {
                    throw new ArgumentException(String.Format("admin boundary named {0} is not mapped to archive index", missingadmin.First()));
                }
                foreach (string cval in mapping.SpatialUnitOptions.AdminClassifier.ClassifierValues.Select(a => a.Description))
                {
                    string mapped = mapping.SpatialUnitOptions.AdminClassifierMapping[cval];
                    if (!DefaultRows.DefaultAdminBoundaryRowsByName.ContainsKey(mapped))
                    {
                        throw new Exception(String.Format("cannot find admin boundary {0} in the archive index", mapped));
                    }
                }
            }
            else if (mapping.SpatialUnitOptions.ClassifierMode == SpatialUnitClassifierMode.JoinedAdminEcoClassifier)
            {
                var missingSPU = mapping.SpatialUnitOptions.SPUClassifier.ClassifierValues.Select(a => a.Description).Except(
                    mapping.SpatialUnitOptions.SPUClassifierMapping.Keys);
                if (missingSPU.Any())
                {
                    throw new ArgumentException(String.Format("spu {0} is not mapped to archive index", missingSPU.First()));
                }
                foreach (string cval in mapping.SpatialUnitOptions.SPUClassifier.ClassifierValues.Select(a => a.Description))
                {
                    string mapped = mapping.SpatialUnitOptions.SPUClassifierMapping[cval];
                    if (!DefaultRows.DefaultSPUsByName.ContainsKey(mapped))
                    {
                        throw new Exception(String.Format("cannot find spatial unit {0} in the archive index", mapped));
                    }
                }
            }
            else if (mapping.SpatialUnitOptions.ClassifierMode == SpatialUnitClassifierMode.SingleDefaultSpatialUnit)
            {
                if (!DefaultRows.DefaultSPUsByName.ContainsKey(mapping.SpatialUnitOptions.SingleDefaultSpatialUnit))
                {
                    throw new ArgumentException(String.Format("Cannot find SPU named {0} in archive index", mapping.SpatialUnitOptions.SingleDefaultSpatialUnit));
                }

            }


            //yield exists
            //inventory | initialized inventory exists
            //age classes exist

            foreach (string s in mapping.SpeciesOptions.SpeciesClassifier.ClassifierValues.Select(a => a.Description))
            {
                if (!mapping.SpeciesOptions.SpeciesTypeMappings.ContainsKey(s))
                {
                    throw new Exception(string.Format("Species \"{0}\" is not mapped to a value in the archive index", s));
                }
                if (!DefaultRows.DefaultSpeciesTypeRowsByName
                    .ContainsKey(mapping.SpeciesOptions.SpeciesTypeMappings[s]) &&
                    !DefaultRows.DefaultNonForestTypeRowsByName
                    .ContainsKey(mapping.SpeciesOptions.SpeciesTypeMappings[s]))
                {
                    throw new Exception(
                        string.Format("Unable to find species named \"{0}\" in archive index",
                        mapping.SpeciesOptions.SpeciesTypeMappings[s]));
                }
            }
            foreach(var y in data.Yields)
            {
                if(y.YieldCurve.Count != data.AgeClasses.Count)
                {
                    throw new Exception(
                        string.Format("yield curve with classifier set '{0}' has incorrect number of yield values. Expected: {1} got: {2}",
                            string.Join(",", y.Classifiers.OrderBy(a => a.Key).Select(a => a.Value)),
                            data.AgeClasses.Count,
                            y.YieldCurve.Count));
                }
            }
        }
        /// <summary>
        /// Import the data that is bound to this instance
        /// </summary>
        public void Import()
        {

            System.IO.File.Copy(inputTemplatePath, OutputPath, true);
            CBMSIT.Log cbmsitlog = new CBMSIT.Log();
            cbmsitlog.Initialize(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(OutputPath), "SITLog.txt"));
            CBMSIT.ProjectCreation.ProjectCreationStatus status = new CBMSIT.ProjectCreation.ProjectCreationStatus(cbmsitlog);
            log.Info("validating project");
            Validate();
            log.Info("importing project");
            CBMProjectWriter writer = new CBMProjectWriter(data, mapping, OutputPath, aidb, DefaultRows, cbmsitlog, status);
            writer.Write();
        }

        private DefaultRows FillDefaultRows(CBMDBObject DBObj)
        {
            DefaultRows defaultRows = new DefaultRows();

            DataTable _dtDefaultSPUs = DBObj.GetDataTable(DEFAULT_SPU_NAMES);
            DataTable _dtDefaultAdminBoundaries = DBObj.GetDataTable(DEFAULT_ADMIN_NAMES);
            DataTable _dtDefaultEcoBoundaries = DBObj.GetDataTable(DEFAULT_ECO_NAMES);
            DataTable _dtDefaultDistTypes = DBObj.GetDataTable(DEFAULTDISTTYPES);
            DataTable _dtDefaultSpeciesTypes = DBObj.GetDataTable(DEFAULTSPECIESTYPES);
            DataTable _dtDefaultNonForestTypes = DBObj.GetDataTable(DEFAULTNONFORESTTYPES_NAMES);

            foreach (DataRow dr in _dtDefaultDistTypes.Rows)
            {
                defaultRows.DefaultDisturbanceTypeRowsByName.Add(dr["DistTypeName"].ToString(), dr);
            }

            //put the default non forest types in the right hand listbox of the non forest tab
            foreach (DataRow dr in _dtDefaultNonForestTypes.Rows)
            {
                defaultRows.DefaultNonForestTypeRowsByName.Add(dr["Name"].ToString(), dr);
            }

            foreach (DataRow dr in _dtDefaultAdminBoundaries.Rows)
            {
                defaultRows.DefaultAdminBoundaryRowsByName.Add(dr["AdminBoundaryName"].ToString(), dr);
            }

            foreach (DataRow dr in _dtDefaultEcoBoundaries.Rows)
            {
                defaultRows.DefaultEcoBoundaryRowsByName.Add(dr["EcoBoundaryName"].ToString(), dr);
                defaultRows.DefaultEcoBoundaryRowsByDefaultEcoBoundaryID.Add(Convert.ToInt32(dr["EcoBoundaryID"]), dr);
            }
            foreach (DataRow dr in _dtDefaultSPUs.Rows)
            {
                defaultRows.DefaultSPUsByName.Add(dr["SPUName"].ToString(), dr);
            }
            foreach (DataRow dr in _dtDefaultSpeciesTypes.Rows)
            {
                defaultRows.DefaultSpeciesTypeRowsByName.Add(dr["SpeciesTypeName"].ToString(), dr);
            }

            return defaultRows;
        }
        private UserDataSet InitializeDataset()
        {
            UserDataSet Data = new UserDataSet();
            Data.AgeClasses = new List<AgeClass>();
            Data.Classifiers = new List<Classifier>();
            Data.DisturbanceTypes = new List<DisturbanceType>();
            Data.DisturbanceEvents = new List<DisturbanceEvent>();
            Data.TransitionRules = new List<TransitionRule>();
            Data.Inventories = new List<Inventory>();
            Data.Yields = new List<Yield>();

            return Data;
        }

        #region age classes
        private void AddAgeClass(AgeClass age)
        {
            log.Info("adding age class");
            data.AgeClasses.Add(age);
        }
        /// <summary>
        /// Add the age classes to the dataset
        /// </summary>
        /// <param name="ageClassSize">The size of the age class in years</param>
        /// <param name="numClasses">the number of age classes</param>
        public void AddAgeClasses(int ageClassSize, int numClasses)
        {
            for (int i = 0; i < numClasses; i++)
            {
                if (i == 0)
                {
                    this.data.AgeClasses.Add(new AgeClass("AgeID" + 0, 0, 0));
                }
                else
                {
                    this.data.AgeClasses.Add(new AgeClass("AgeID" + i, ageClassSize, (i - 1) * ageClassSize + 1));
                }
            }
        }
        /// <summary>
        /// Get an age class object from the sit dataset
        /// </summary>
        /// <param name="index">The 0 based age class index</param>
        /// <returns>The age class object</returns>
        public AgeClass GetAgeClass(int index)
        {
            return this.data.AgeClasses[index];
        }
        #endregion
        #region classifiers
        /// <summary>
        /// Add a classifier to the dataset
        /// </summary>
        /// <param name="name">The classifier name</param>
        public void AddClassifier(string name)
        {
            int classifierIndex = data.Classifiers.Count;
            log.Info(String.Format("adding classifier {0}", name));
            AddClassifier(new Classifier(classifierIndex, classifierIndex + 1, name));
            ClassifierValues.Add(classifierIndex, new System.Collections.Generic.HashSet<string>());
        }

        private void AddClassifier(Classifier classifier)
        {
            if (Classifiers.ContainsKey(classifier.Name))
            {
                throw new Exception(string.Format("Duplicate classifier name \"{0}\" detected", classifier.Name));
            }
            Classifiers.Add(classifier.Name, classifier);
            data.Classifiers.Add(classifier);
        }
        private Dictionary<int, string> AddClassifierSet(string classifierSet)
        {
            string[] tokens = classifierSet.Split(',');
            if (tokens.Count() != this.Classifiers.Count)
            {
                throw new ArgumentException(
                    String.Format("Classifier set string {0} does not have {1} classifiers",
                    classifierSet, this.Classifiers.Count));
            }
            Dictionary<int, string> cset = new Dictionary<int, string>();
            for (int i = 0; i < tokens.Count(); i++)
            {
                cset.Add(i + 1, tokens[i]);
                AddClassifierValue(i, tokens[i]);
            }
            return cset;
        }
        private void AddClassifierValue(int index, string value)
        {
            if (value != "?")
            {
                if (!ClassifierValues[index].Contains(value))
                {
                    int i = data.Classifiers[index].ClassifierValues.Count();
                    data.Classifiers[index].ClassifierValues.Add(new ClassifierValue(i + 1, value, value));
                    ClassifierValues[index].Add(value);
                }
            }
        }

        #endregion
        #region disturbance events
        private Dictionary<string, DisturbanceType> disttypes
            = new Dictionary<string, DisturbanceType>();
        private void AddDisturbanceType(DisturbanceType distType)
        {
            data.DisturbanceTypes.Add(distType);
            if (!mapping.DisturbanceTypeOptions.Mapping.ContainsKey(distType.Name))
            {
                throw new Exception(String.Format("disturbance type named \"{0}\" is not mapped", distType.Name));
            }
            if (!DefaultRows.DefaultDisturbanceTypeRowsByName.ContainsKey(mapping.DisturbanceTypeOptions.Mapping[distType.Name]))
            {
                throw new Exception(
                    String.Format("Unable to find disturbance type named \"{0}\" in archive index",
                    mapping.DisturbanceTypeOptions.Mapping[distType.Name]));
            }
        }
        private DisturbanceType AddDisturbanceType(string name)
        {
            if (!disttypes.ContainsKey(name))
            {
                DisturbanceType dt = new DisturbanceType(name, name, name);
                AddDisturbanceType(dt);
                disttypes.Add(name, dt);
                return dt;
            }
            return disttypes[name];
        }
        /// <summary>
        /// Add a disturbance event to the sit dataset
        /// </summary>
        /// <param name="classifierSet">The disturbance event classifier set</param>
        /// <param name="disturbanceType">The disturbance type name</param>
        /// <param name="timestep">the time step on which the event occurs</param>
        /// <param name="target">the target amount, the unit is dependant on the targetType parameter</param>
        /// <param name="targetType">the target type, may be merchantable carbon, area, or proportion
        /// Area = 0,
        /// Proportion = 1,
        /// Merchantable = 2
        /// </param>
        /// <param name="sort">the sort type. One of:
        /// UNKNOWNSORT = 0,
        /// PROPORTION_OF_EVERY_RECORD = 1,
        /// MERCHCSORT_TOTAL = 2,
        /// SORT_BY_SW_AGE = 3,
        /// YEARS_SINCE_ELIGIBLE_FOR_SW_HARVEST = 4,
        /// SVOID = 5,
        /// RANDOMSORT = 6,
        /// TOTALSTEMSNAG = 7,
        /// SWSTEMSNAG = 8,
        /// HWSTEMSNAG = 9,
        /// MERCHCSORT_SW = 10,
        /// MERCHCSORT_HW = 11,
        /// SORT_BY_HW_AGE = 12,
        /// YEARS_SINCE_ELIGIBLE_FOR_HW_HARVEST = 13
        /// </param>
        /// <param name="spatialReference">The optional spatial reference. May be set to an integer value for spatial targetting.  Use with SortType.SVOID</param>
        /// <param name="ageMin">The minimum (inclusive) eligible age of stands considered for this event</param>
        /// <param name="ageMax">The maximum (inclusive) eligible age of stands considered for this event</param>
        /// <param name="efficiency">The efficency (proportion of area) for stands affected by this event</param>
        /// <param name="eligibility">The eligibility parameters for the event</param>
        /// <param name="canFire">The can fire parameters for the event</param>
        public void AddDisturbanceEvent(string classifierSet, string disturbanceType, int timestep,
            double target, string targetType = "Area", string sort = "SORT_BY_SW_AGE",
            int spatialReference = -1, int ageMin = -1, int ageMax = -1,
            double efficiency = 1.0, double[] eligibility = null, CanFireParameter canFire = null)
        {
            log.Info("adding disturbance event");
            DisturbanceType dist = AddDisturbanceType(disturbanceType);
            DisturbanceEvent distevent = new DisturbanceEvent();
            // age class criteria, a -1 indicates any age is eligble
            // otherwise put a positive integer to filter a disturbance event by age with the following definition:
            // index 0: SWStartAge
            // index 1: SWEndAge
            // index 2: HWStartAge
            // index 3: HWEndAge
            distevent.AgeClasses[0] = ageMin;
            distevent.AgeClasses[1] = ageMax;
            distevent.AgeClasses[2] = ageMin;
            distevent.AgeClasses[3] = ageMax;

            distevent.ClassifierValues = AddClassifierSet(classifierSet);
            distevent.DisturbanceType = dist.ID;
            distevent.DisturbanceYear = timestep;
            distevent.TargetSum = target;
            distevent.target = (TargetType)Enum.Parse(typeof(TargetType), targetType);
            distevent.SpatialReference = spatialReference;
            distevent.Efficiency = efficiency;
            distevent.Sort = (SortType)Enum.Parse(typeof(SortType), sort);
            if (eligibility == null)
            {
                for (int counter = 0; counter <= 21 - 1; counter += 1)
                {
                    distevent.EligibilityParameters[counter] = -1;
                }
            }
            else
            {
                distevent.EligibilityParameters = eligibility;
            }
            distevent.CanFire = canFire;
            AddDisturbanceEvent(distevent);
        }
        private void AddDisturbanceEvent(DisturbanceEvent distevent)
        {
            data.DisturbanceEvents.Add(distevent);
        }
        /// <summary>
        /// Add a transition rule
        /// </summary>
        /// <param name="classifierSetSource">The source classifier set for this transition rule</param>
        /// <param name="classifierSetTarget">The destination classifier set for this transition rule</param>
        /// <param name="disturbanceType">The disturbance type</param>
        /// <param name="percent">if this transtion rule has multiple destinations, this is the percentage that this destination will receive</param>
        /// <param name="spatialReference">If this is a spatially explicit simulation then set this to the records spatial ID</param>
        /// <param name="ageMin">The minimum (inclusive) eligible age of stands considered for this transition rule</param>
        /// <param name="ageMax">The maximum (inclusive) eligible age of stands considered for this transition rule</param>
        /// <param name="resetAge">the age to which the stand will be set after the event.  A -1 indicates the age will be left at the pre-disturbance age.</param>
        /// <param name="regenDelay">The number of years which will elapse before this stand will grow</param>
        public void AddTransitionRule(string classifierSetSource, string classifierSetTarget,
            string disturbanceType, double percent = 100.0, int spatialReference = -1,
            int ageMin = -1, int ageMax = -1, int resetAge = -1, int regenDelay = 0)
        {
            DisturbanceType dist = AddDisturbanceType(disturbanceType);
            TransitionRule trule = new TransitionRule();
            // index 0: SWStartAge
            // index 1: SWEndAge
            // index 2: HWStartAge
            // index 3: HWEndAge
            trule.AgeClasses[0] = ageMin;
            trule.AgeClasses[1] = ageMax;
            trule.AgeClasses[2] = ageMin;
            trule.AgeClasses[3] = ageMax;

            trule.DisturbanceType = dist.ID;
            trule.SourceClassifierValues = AddClassifierSet(classifierSetSource);
            trule.TargetClassifierValues = AddClassifierSet(classifierSetTarget);

            trule.ResetAge = resetAge;
            //-1 indicates do not change the original age, otherwise it will be set to the positive integer you specify

            trule.RegenDelay = regenDelay;

            trule.Percent = percent;

            trule.SpatialReference = spatialReference;

            AddTransitionRule(trule);
        }
        private void AddTransitionRule(TransitionRule transitionRule)
        {
            data.TransitionRules.Add(transitionRule);
        }
        #endregion
        #region inventory
        private void AddInventory(Inventory inventory)
        {
            data.Inventories.Add(inventory);
        }
        /// <summary>
        /// Add an inventory record.
        /// </summary>
        /// <param name="classifierSet">The inventory classifier set.  Must not contain wildcards</param>
        /// <param name="area">The area of the inventory record in hectares</param>
        /// <param name="age">The age, in years, of the record</param>
        /// <param name="spatialReference">The optional spatial ID of the inventory record.  The default is not spatial.</param>
        /// <param name="delay">The number of years which will elapse before the stand starts growing</param>
        /// <param name="unfcccLandClass">The unfccc land class id</param>
        /// <param name="HistoricDisturbance">the name of the historic disturbance type</param>
        /// <param name="MostRecentDisturbance">the name of the last pass disturbance type</param>
        public void AddInventory(string classifierSet, double area, int age,
            int spatialReference = -1, int delay = 0, int unfcccLandClass = 0,
            string HistoricDisturbance = null, string MostRecentDisturbance = null)
        {
            log.Info("Adding inventory");
            Dictionary<int, string> cset = AddClassifierSet(classifierSet);
            Inventory inv = new Inventory()
            {
                ClassifierValues = cset,
                Age = age,
                Area = area,

                SpatialReference = spatialReference,
                Delay = delay,

                UNFCCCLandClass = unfcccLandClass,

                ageClass = null,
                UsingAgeClass = false
            };

            if (HistoricDisturbance != null)
            {
                DisturbanceType dt = AddDisturbanceType(HistoricDisturbance);
                inv.HistoricDisturbance = dt.ID;
            }
            if (MostRecentDisturbance != null)
            {
                DisturbanceType dt = AddDisturbanceType(MostRecentDisturbance);
                inv.MostRecentDisturbance = dt.ID;
            }
            AddInventory(inv);
        }
        /// <summary>
        /// Add a set of inventory records in an age class
        /// </summary>
        /// <param name="classifierSet">The inventory classifier set.  Must not contain wildcards</param>
        /// <param name="area">The area of the inventory record in hectares</param>
        /// <param name="ageclass">The ageclass of the record</param>
        /// <param name="spatialReference">The optional spatial ID of the inventory record.  The default is not spatial.</param>
        /// <param name="delay">The number of years which will elapse before the stand starts growing</param>
        /// <param name="unfcccLandClass">The unfccc land class id</param>
        /// <param name="HistoricDisturbance">the name of the historic disturbance type</param>
        /// <param name="MostRecentDisturbance">the name of the last pass disturbance type</param>
        public void AddInventory(string classifierSet, double area, AgeClass ageclass,
            int spatialReference = -1, int delay = 0, int unfcccLandClass = 0,
            string HistoricDisturbance = null, string MostRecentDisturbance = null)
        {

            Inventory inv = new Inventory()
            {
                ClassifierValues = AddClassifierSet(classifierSet),
                Age = -1,
                Area = area,

                SpatialReference = spatialReference,
                Delay = delay,

                HistoricDisturbance = HistoricDisturbance,
                UNFCCCLandClass = unfcccLandClass,
                MostRecentDisturbance = MostRecentDisturbance,

                ageClass = ageclass,
                UsingAgeClass = true
            };

            if (HistoricDisturbance != null)
            {
                DisturbanceType dt = AddDisturbanceType(HistoricDisturbance);
                inv.HistoricDisturbance = dt.ID;
            }
            if (MostRecentDisturbance != null)
            {
                DisturbanceType dt = AddDisturbanceType(MostRecentDisturbance);
                inv.MostRecentDisturbance = dt.ID;
            }
            AddInventory(inv);
        }
        #endregion
        #region yields
        private void AddYield(Yield yield)
        {
            data.Yields.Add(yield);
        }

        public void AddYield(string classifierSet, string leadingSpeciesClassifierValue, List<double> values)
        {
            log.Info("Adding yield curve");
            Yield yield = new Yield();

            yield.Classifiers = AddClassifierSet(classifierSet);
            if (mapping.SpeciesOptions.SpeciesClassifier == null)
            {
                throw new ArgumentException("you must set the species classifier before adding yield curves");
            }

            AddClassifierValue(mapping.SpeciesOptions.SpeciesClassifier.Index, leadingSpeciesClassifierValue);
            yield.SpeciesClassifier = leadingSpeciesClassifierValue;
            foreach (var item in values)
            {
                yield.YieldCurve.Add(Convert.ToDouble(item));
            }
            AddYield(yield);
        }

        #endregion
        #region mapping
        /// <summary>
        /// Set the classifier which defines species type in your inventory
        /// </summary>
        /// <param name="speciesClassifier">The name of the classifier which corresponds to species in your inventory</param>
        public void SetSpeciesClassifier(string speciesClassifier)
        {
            if (!this.Classifiers.ContainsKey(speciesClassifier))
            {
                throw new ArgumentException(string.Format("classifier \"{0}\" not found in added classifiers", speciesClassifier));
            }
            SetSpeciesClassifier(this.Classifiers[speciesClassifier]);
        }
        private void SetSpeciesClassifier(Classifier classifer)
        {
            mapping.SpeciesOptions.SpeciesClassifier = classifer;
        }
        /// <summary>
        /// Set the plugin to import your data as a single default spatial unit
        /// </summary>
        /// <param name="DefaultSPUID"></param>
        public void SetSingleSpatialUnit(int DefaultSPUID)
        {
            mapping.SpatialUnitOptions.ClassifierMode = SpatialUnitClassifierMode.SingleDefaultSpatialUnit;
            var row = from a in DefaultRows.DefaultSPUsByName
                      where Convert.ToInt32(a.Value["SPUID"]) == DefaultSPUID
                      select a.Key;

            if (!row.Any())
            {
                throw new ArgumentException(string.Format("Cannot find DefaultSPUID {0} in archive index", DefaultSPUID));
            }
            mapping.SpatialUnitOptions.SingleDefaultSpatialUnit = row.First();
        }
        /// <summary>
        /// Used for cases where your data has a classifier which can be mapped to administrative boundaries,
        /// and a classifier which can be mapped to ecological boundaries
        /// </summary>
        /// <param name="adminClassifier">The name of the classifier which corresponds to administrative boundary</param>
        /// <param name="ecoClassifier">The name of the classifier which corresponds to ecological boundary</param>
        public void SetAdminEcoMapping(string adminClassifier, string ecoClassifier)
        {
            SetAdminEcoMapping(Classifiers[adminClassifier], Classifiers[ecoClassifier]);
        }
        private void SetAdminEcoMapping(Classifier Admin, Classifier Eco)
        {
            mapping.SpatialUnitOptions.ClassifierMode = SpatialUnitClassifierMode.SeperateAdminEcoClassifiers;
            mapping.SpatialUnitOptions.EcoClassifier = Eco;
            mapping.SpatialUnitOptions.AdminClassifier = Admin;
        }
        /// <summary>
        /// Used for cases where your data has a classifier which can be mapped to spatial units.
        /// </summary>
        /// <param name="spuClassifier">The name of the classifier which corresponds to spatial units</param>
        public void SetSPUMapping(string spuClassifier)
        {
            SetSPUMapping(Classifiers[spuClassifier]);
        }
        private void SetSPUMapping(Classifier SPU)
        {
            mapping.SpatialUnitOptions.ClassifierMode = SpatialUnitClassifierMode.JoinedAdminEcoClassifier;
            mapping.SpatialUnitOptions.SPUClassifier = SPU;
        }
        /// <summary>
        /// use for the case where a seperate classifier defines non-forest cover types
        /// </summary>
        /// <param name="nonForestClassifier">the non-forest classifier name</param>
        public void SetNonForestClassifier(string nonForestClassifier)
        {
            SetNonForestClassifier(Classifiers[nonForestClassifier]);
        }
        private void SetNonForestClassifier(Classifier nonforestClassifier)
        {
            mapping.SpeciesOptions.SpeciesANDNonForestClassifier = false;
            mapping.NonForestOptions.HasClassifier = true;
            mapping.NonForestOptions.NonForestClassifier = nonforestClassifier;
        }
        /// <summary>
        /// add mapping for a disturbance type name
        /// </summary>
        /// <param name="ProjectDistName">the name of a disturbance type as defined in your data</param>
        /// <param name="DefaultName">a CBM default disturbance type name as defined in the archive index</param>
        public void MapDisturbanceType(string ProjectDistName, string DefaultName)
        {
            if (!DefaultRows.DefaultDisturbanceTypeRowsByName.ContainsKey(DefaultName))
            {
                throw new ArgumentException(
                    string.Format("Can't find disturbance type name {0} in archive index", DefaultName));
            }
            mapping.DisturbanceTypeOptions.Mapping.Add(ProjectDistName, DefaultName);
        }
        /// <summary>
        /// Set a name map for a CBM admin boundary which appears in the archive index
        /// </summary>
        /// <param name="ProjectAdminName">The name of your admin boundary as defined in your data</param>
        /// <param name="DefaultName">A CBM default admin boundary as it appears in the archive index</param>
        public void MapAdminBoundary(string ProjectAdminName, string DefaultName)
        {
            if (!DefaultRows.DefaultAdminBoundaryRowsByName.ContainsKey(DefaultName))
            {
                throw new ArgumentException(
                    string.Format("Can't find admin boundary name {0} in archive index", DefaultName));
            }
            mapping.SpatialUnitOptions.AdminClassifierMapping.Add(ProjectAdminName, DefaultName);
        }
        /// <summary>
        /// Set a name map for a CBM admin boundary which appears in the archive index
        /// </summary>
        /// <param name="ProjectEcoName">The name of your eco boundary as defined in your data</param>
        /// <param name="DefaultName">A CBM default eco boundary as it appears in the archive index</param>
        public void MapEcoBoundary(string ProjectEcoName, string DefaultName)
        {
            if (!DefaultRows.DefaultEcoBoundaryRowsByName.ContainsKey(DefaultName))
            {
                throw new ArgumentException(
                    string.Format("Can't find eco boundary name {0} in archive index", DefaultName));
            }
            mapping.SpatialUnitOptions.EcoClassifierMapping.Add(ProjectEcoName, DefaultName);
        }
        /// <summary>
        /// Set a name map for a CBM spatial unit ID which appears in the archive index
        /// </summary>
        /// <param name="ProjectSPUName">The name of your spatial unit as defined in your data</param>
        /// <param name="AdminBoundaryName">the admin boundary name of the spatial unit to map</param>
        /// <param name="EcoBoundaryName">the eco boundary name of the spatial unit to map</param>
        public void MapSpatialUnit(string ProjectSPUName, string AdminBoundaryName, string EcoBoundaryName)
        {
            var matches = DefaultRows.DefaultSPUsByName
                .Where(a => a.Key.Contains(AdminBoundaryName) && a.Key.Contains(EcoBoundaryName));
            if (!matches.Any())
            {
                throw new ArgumentException(String.Format("Cannot find Default SPUID {0} - {1}",
                    AdminBoundaryName, EcoBoundaryName));
            }
            mapping.SpatialUnitOptions.SPUClassifierMapping.Add(ProjectSPUName, matches.First().Key);
        }
        /// <summary>
        /// Set a name map for a CBM species which appears in the archive index
        /// </summary>
        /// <param name="ProjectSpeciesName">The name of your species as defined in your data</param>
        /// <param name="DefaultName">A CBM default species as it appears in the archive index</param>
        public void MapSpecies(string ProjectSpeciesName, string DefaultName)
        {
            if (!DefaultRows.DefaultSpeciesTypeRowsByName.ContainsKey(DefaultName))
            {
                if (DefaultRows.DefaultNonForestTypeRowsByName.ContainsKey(DefaultName))
                {
                    if (mapping.NonForestOptions.HasClassifier)
                    {
                        throw new ArgumentException("if a non forest " +
                            "classifier is specified, do not map values " +
                            "from the species classifier to non-forest types");
                    }
                    mapping.SpeciesOptions.SpeciesANDNonForestClassifier = true;
                }
                else {

                    throw new ArgumentException(
                        string.Format("Can't find species name {0} in archive index", DefaultName));
                }
            }
            mapping.SpeciesOptions.SpeciesTypeMappings.Add(ProjectSpeciesName, DefaultName);
        }

        public void MapNonForest(string projectNonForestName, string defaultName)
        {
            mapping.NonForestOptions.NonForestTypeMapping.Add(projectNonForestName, defaultName);
        }
        private static MappingOptions CreateEmptyMapping()
        {
            MappingOptions Mapping = new MappingOptions();

            Mapping.DisturbanceTypeOptions = new DisturbanceTypeMapping();
            Mapping.NonForestOptions = new NonForestOptions();
            Mapping.SpatialUnitOptions = new SpatialUnitOptions();
            Mapping.SpeciesOptions = new SpeciesOptions();
            return Mapping;
        }
        private static MappingOptions InitializeMapping(DefaultRows DefaultRows)
        {
            MappingOptions Mapping = new MappingOptions();

            Mapping.DisturbanceTypeOptions = new DisturbanceTypeMapping();
            foreach (string defaultDistType in DefaultRows.DefaultDisturbanceTypeRowsByName.Keys)
            {
                Mapping.DisturbanceTypeOptions.Mapping.Add(defaultDistType, defaultDistType);
            }

            Mapping.NonForestOptions = new NonForestOptions();

            Mapping.SpatialUnitOptions = new SpatialUnitOptions();
            Mapping.SpatialUnitOptions.SPUClassifierMapping = new Dictionary<string, string>();
            foreach (var kvp in DefaultRows.DefaultSPUsByName)
            {
                Mapping.SpatialUnitOptions.SPUClassifierMapping.Add(kvp.Key, kvp.Key);
            }

            Mapping.SpatialUnitOptions.AdminClassifierMapping = new Dictionary<string, string>();
            foreach (var kvp in DefaultRows.DefaultAdminBoundaryRowsByName)
            {
                Mapping.SpatialUnitOptions.AdminClassifierMapping.Add(kvp.Key, kvp.Key);
            }
            Mapping.SpatialUnitOptions.EcoClassifierMapping = new Dictionary<string, string>();
            foreach (var kvp in DefaultRows.DefaultEcoBoundaryRowsByName)
            {
                Mapping.SpatialUnitOptions.EcoClassifierMapping.Add(kvp.Key, kvp.Key);
            }
            Mapping.SpeciesOptions = new SpeciesOptions();

            // You need to assign this if your classifier is mappable to both
            // species types and non-forest cover types.
            Mapping.SpeciesOptions.SpeciesANDNonForestClassifier = true;

            foreach (string defaultSpecies in DefaultRows.DefaultSpeciesTypeRowsByName.Keys)
            {
                Mapping.SpeciesOptions.SpeciesTypeMappings.Add(defaultSpecies,
                                                               defaultSpecies);
            }

            foreach (string defaultNonForestCoverType in DefaultRows.DefaultNonForestTypeRowsByName.Keys)
            {
                Mapping.SpeciesOptions.SpeciesTypeMappings.Add(defaultNonForestCoverType,
                                                               defaultNonForestCoverType);
            }
            return Mapping;
        }
        #endregion
    }
}

