using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages drivers, workflows, reports, and other configuration components
    /// </summary>
    public class ComponentConfigManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private readonly string _configPath;
        private readonly ConfigPathManager _pathManager;

        public List<ConnectionDriversConfig> DataDriversClasses { get; set; }
        public List<WorkFlow> WorkFlows { get; set; }
        public List<ReportsList> ReportsList { get; set; }
        public List<AppTemplate> ReportsDefinition { get; set; }
        public List<ReportsList> AIScriptsList { get; set; }
        public List<RootFolder> Projects { get; set; }
        public List<CategoryFolder> CategoryFolders { get; set; }

        public ComponentConfigManager(IDMLogger logger, IJsonLoader jsonLoader, string configPath, ConfigPathManager pathManager)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _configPath = configPath;
            _pathManager = pathManager;

            // Initialize collections
            DataDriversClasses = new List<ConnectionDriversConfig>();
            WorkFlows = new List<WorkFlow>();
            ReportsList = new List<ReportsList>();
            ReportsDefinition = new List<AppTemplate>();
            AIScriptsList = new List<ReportsList>();
            Projects = new List<RootFolder>();
            CategoryFolders = new List<CategoryFolder>();
        }

        #region "Driver Management"

        /// <summary>
        /// Adds a driver to the connection drivers configuration.
        /// </summary>
        public int AddDriver(ConnectionDriversConfig driver)
        {
            if (driver == null || string.IsNullOrEmpty(driver.PackageName))
                return -1;

            if (DataDriversClasses.Count == 0)
            {
                DataDriversClasses.Add(driver);
                return 0;
            }

            // Check for existing driver with same package and version
            int idx = DataDriversClasses.FindIndex(c => 
                c.PackageName.Equals(driver.PackageName, StringComparison.InvariantCultureIgnoreCase) && 
                c.version == driver.version);
            
            if (idx >= 0)
                return idx;

            // Check for existing driver with same package (different version)
            idx = DataDriversClasses.FindIndex(c => 
                c.PackageName.Equals(driver.PackageName, StringComparison.InvariantCultureIgnoreCase));
            
            if (idx > -1)
            {
                DataDriversClasses[idx].version = driver.version;
                return idx;
            }

            // Add new driver
            DataDriversClasses.Add(driver);
            return DataDriversClasses.Count - 1;
        }

        /// <summary>
        /// Loads connection drivers configuration values from JSON file and syncs with in-memory list.
        /// </summary>
        public List<ConnectionDriversConfig> LoadConnectionDriversConfigValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "ConnectionConfig.json");
                if (!File.Exists(path))
                {
                    return DataDriversClasses;
                }

                var loadedConfigs = _jsonLoader.DeserializeObject<ConnectionDriversConfig>(path);
                if (loadedConfigs == null)
                {
                    return DataDriversClasses;
                }

                var loadedDict = loadedConfigs.ToDictionary(c => c.GuidID ?? string.Empty);

                // Update existing and add new
                foreach (var config in loadedConfigs)
                {
                    var existing = DataDriversClasses.FirstOrDefault(c => c.GuidID == config.GuidID);
                    if (existing != null)
                    {
                        UpdateConfig(existing, config);
                    }
                    else
                    {
                        DataDriversClasses.Add(config);
                    }
                }

                return DataDriversClasses;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading connection drivers config: {ex.Message}");
                return DataDriversClasses;
            }
        }

        /// <summary>
        /// Saves the configuration values of connection drivers to a JSON file.
        /// </summary>
        public void SaveConnectionDriversConfigValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "ConnectionConfig.json");
                _jsonLoader.Serialize(path, DataDriversClasses);
                _logger?.WriteLog("Saved connection drivers configuration");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving connection drivers config: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to update existing config.
        /// </summary>
        private void UpdateConfig(ConnectionDriversConfig target, ConnectionDriversConfig source)
        {
            target.PackageName = source.PackageName;
            target.DriverClass = source.DriverClass;
            target.version = source.version;
            target.dllname = source.dllname;
            target.AdapterType = source.AdapterType;
            target.CommandBuilderType = source.CommandBuilderType;
            target.DbConnectionType = source.DbConnectionType;
            target.DbTransactionType = source.DbTransactionType;
            target.ConnectionString = source.ConnectionString;
            target.parameter1 = source.parameter1;
            target.parameter2 = source.parameter2;
            target.parameter3 = source.parameter3;
            target.iconname = source.iconname;
            target.classHandler = source.classHandler;
            target.ADOType = source.ADOType;
            target.CreateLocal = source.CreateLocal;
            target.InMemory = source.InMemory;
            target.extensionstoHandle = source.extensionstoHandle;
            target.Favourite = source.Favourite;
            target.DatasourceCategory = source.DatasourceCategory;
            target.DatasourceType = source.DatasourceType;
            target.IsMissing = source.IsMissing;
        }

        /// <summary>
        /// Creates a string representing file extensions.
        /// </summary>
        public string CreateFileExtensionString()
        {
            try
            {
                var driversWithExtensions = DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
                string retval = null;
                
                if (driversWithExtensions.Any())
                {
                    var extensionsList = driversWithExtensions.Select(p => p.extensionstoHandle);
                    string extString = string.Join(",", extensionsList);
                    var extensions = extString.Split(',').Distinct().ToList();

                    foreach (string item in extensions)
                    {
                        retval += $"{item} files(*.{item})|*.{item}|";
                    }
                }

                retval += "All files(*.*)|*.*";
                return retval;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error creating file extension string: {ex.Message}");
                return "All files(*.*)|*.*";
            }
        }

        #endregion

        #region "Workflow Management"

        /// <summary>
        /// Reads workflow data from a JSON file.
        /// </summary>
        public void ReadWorkFlows(string workFlowPath)
        {
            try
            {
                string path = Path.Combine(workFlowPath, "DataWorkFlow.json");
                if (File.Exists(path))
                {
                    WorkFlows = _jsonLoader.DeserializeObject<WorkFlow>(path) ?? new List<WorkFlow>();
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error reading workflows: {ex.Message}");
                WorkFlows = new List<WorkFlow>();
            }
        }

        /// <summary>
        /// Saves workflows to a JSON file.
        /// </summary>
        public void SaveWorkFlows(string workFlowPath)
        {
            try
            {
                string path = Path.Combine(workFlowPath, "DataWorkFlow.json");
                _pathManager.CreateDir(workFlowPath);
                _jsonLoader.Serialize(path, WorkFlows);
                _logger?.WriteLog("Saved workflows");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving workflows: {ex.Message}");
            }
        }

        #endregion

        #region "Reports Management"

        /// <summary>
        /// Saves the values of the reports list to a JSON file.
        /// </summary>
        public void SaveReportsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "Reportslist.json");
                _jsonLoader.Serialize(path, ReportsList);
                _logger?.WriteLog("Saved reports list");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the values of reports from a JSON file.
        /// </summary>
        public List<ReportsList> LoadReportsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "Reportslist.json");
                if (File.Exists(path))
                {
                    ReportsList = _jsonLoader.DeserializeObject<ReportsList>(path) ?? new List<ReportsList>();
                }
                return ReportsList;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading reports: {ex.Message}");
                return new List<ReportsList>();
            }
        }

        /// <summary>
        /// Saves the values of report definitions to a JSON file.
        /// </summary>
        public void SaveReportDefinitionsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "reportsDefinition.json");
                _jsonLoader.Serialize(path, ReportsDefinition);
                _logger?.WriteLog("Saved report definitions");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving report definitions: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the values of the reports definition from a JSON file.
        /// </summary>
        public List<AppTemplate> LoadReportsDefinitionValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "reportsDefinition.json");
                if (File.Exists(path))
                {
                    ReportsDefinition = _jsonLoader.DeserializeObject<AppTemplate>(path) ?? new List<AppTemplate>();
                }
                return ReportsDefinition;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading report definitions: {ex.Message}");
                return new List<AppTemplate>();
            }
        }

        /// <summary>
        /// Saves the values of AI scripts to a JSON file.
        /// </summary>
        public void SaveAIScriptsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "AIScripts.json");
                _jsonLoader.Serialize(path, AIScriptsList);
                _logger?.WriteLog("Saved AI scripts");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving AI scripts: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the values of AI scripts from a JSON file.
        /// </summary>
        public List<ReportsList> LoadAIScriptsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "AIScripts.json");
                if (File.Exists(path))
                {
                    AIScriptsList = _jsonLoader.DeserializeObject<ReportsList>(path) ?? new List<ReportsList>();
                }
                return AIScriptsList;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading AI scripts: {ex.Message}");
                return new List<ReportsList>();
            }
        }

        #endregion

        #region "Projects Management"

        /// <summary>
        /// Reads projects from JSON file.
        /// </summary>
        public void ReadProjects()
        {
            try
            {
                string path = Path.Combine(_configPath, "Projects.json");
                if (File.Exists(path))
                {
                    Projects = _jsonLoader.DeserializeObject<RootFolder>(path) ?? new List<RootFolder>();
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error reading projects: {ex.Message}");
                Projects = new List<RootFolder>();
            }
        }

        /// <summary>
        /// Saves projects to JSON file.
        /// </summary>
        public void SaveProjects()
        {
            try
            {
                string path = Path.Combine(_configPath, "Projects.json");
                _jsonLoader.Serialize(path, Projects);
                _logger?.WriteLog("Saved projects");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving projects: {ex.Message}");
            }
        }

        #endregion

        #region "Category Folders Management"

        /// <summary>
        /// Adds a folder category to the collection.
        /// </summary>
        public CategoryFolder AddFolderCategory(string folderName, string rootName, string parentName, 
            string parentGuidId = null, bool isParentFolder = false, bool isParentRoot = true, bool isPhysical = false)
        {
            try
            {
                var categoryFolder = new CategoryFolder
                {
                    FolderName = folderName,
                    RootName = rootName,
                    ParentName = parentName,
                    ParentGuidID = parentGuidId,
                    IsParentFolder = isParentFolder,
                    IsParentRoot = isParentRoot,
                    IsPhysicalFolder = isPhysical
                };

                CategoryFolders.Add(categoryFolder);
                SaveCategoryFoldersValues();
                return categoryFolder;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error adding folder category: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Removes a folder category.
        /// </summary>
        public bool RemoveFolderCategory(string folderName, string rootName, string parentGuidId)
        {
            try
            {
                var categoryFolder = CategoryFolders.FirstOrDefault(y => 
                    y.FolderName.Equals(folderName, StringComparison.InvariantCultureIgnoreCase) && 
                    y.RootName.Equals(rootName, StringComparison.InvariantCultureIgnoreCase));

                if (categoryFolder == null)
                {
                    categoryFolder = CategoryFolders.FirstOrDefault(y => 
                        y.FolderName.Equals(folderName, StringComparison.InvariantCultureIgnoreCase) && 
                        y.ParentGuidID.Equals(parentGuidId, StringComparison.InvariantCultureIgnoreCase));
                }

                if (categoryFolder != null)
                {
                    CategoryFolders.Remove(categoryFolder);
                    SaveCategoryFoldersValues();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error removing folder category: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads category folders from JSON file.
        /// </summary>
        public void LoadCategoryFoldersValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "CategoryFolders.json");
                if (File.Exists(path))
                {
                    CategoryFolders = _jsonLoader.DeserializeObject<CategoryFolder>(path) ?? new List<CategoryFolder>();
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading category folders: {ex.Message}");
                CategoryFolders = new List<CategoryFolder>();
            }
        }

        /// <summary>
        /// Saves category folders to JSON file.
        /// </summary>
        public void SaveCategoryFoldersValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "CategoryFolders.json");
                _jsonLoader.Serialize(path, CategoryFolders);
                _logger?.WriteLog("Saved category folders");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving category folders: {ex.Message}");
            }
        }

        #endregion
    }
}