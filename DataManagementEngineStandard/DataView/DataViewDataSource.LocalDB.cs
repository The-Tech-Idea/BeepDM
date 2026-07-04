using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Partial: ILocalDB surface for DataView. DataView is virtual and routes file-level
    /// operations to the materialized temp DB when one is available.
    /// </summary>
    public partial class DataViewDataSource
    {
        // ── ILocalDB (file lifecycle — DataView is a logical source, not a single file) ──
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".dataview";

        public bool CreateDB()
        {
            // DataView is virtual; "creating" means opening a connection that loads the view.
            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(bool inMemory)
        {
            InMemory = inMemory;
            return CreateDB();
        }

        public bool CreateDB(string filepathandname)
        {
            // Allow callers to point at a saved DataView JSON. Reload the view file at the given path.
            try
            {
                DatasourceName = System.IO.Path.GetFileNameWithoutExtension(filepathandname);
                var path = System.IO.Path.GetDirectoryName(filepathandname);
                if (!string.IsNullOrEmpty(path) && !System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
                Dataconnection.ConnectionProp.FilePath = path ?? string.Empty;
                Dataconnection.ConnectionProp.FileName = System.IO.Path.GetFileName(filepathandname);
                return Openconnection() == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"DataView CreateDB('{filepathandname}') failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        public bool DeleteDB()
        {
            // Closes any in-flight connections and clears the materialized temp DB.
            return Closeconnection() == ConnectionState.Closed;
        }

        public bool CopyDB(string destDbName, string destPath)
        {
            // Writes the current DataView definition to a JSON file.
            try
            {
                if (string.IsNullOrWhiteSpace(destPath)) return false;
                if (!System.IO.Directory.Exists(destPath))
                    System.IO.Directory.CreateDirectory(destPath);
                var fileName = string.IsNullOrWhiteSpace(destDbName) ? ViewName + ".dataview" : destDbName;
                if (!fileName.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(Extension, System.StringComparison.OrdinalIgnoreCase))
                    fileName += ".json";
                WriteDataViewFile(destPath, fileName);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"DataView CopyDB failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>Routes to the materialized temp DB when present; otherwise removes the entity from the view itself.</summary>
        public IErrorsInfo DropEntity(string EntityName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_tempDb != null && _tempDb.ConnectionStatus == ConnectionState.Open)
                {
                    // IDataSource has no DropEntity; fall back to DeleteEntity(name, null) which IDataSource supports.
                    _tempDb.DeleteEntity(EntityName, null);
                    InvalidateCache();
                    return DMEEditor.ErrorObject;
                }
                // No temp DB: remove the entity from the view definition.
                var ent = DataView.Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, System.StringComparison.OrdinalIgnoreCase));
                if (ent != null)
                {
                    DataView.Entities.Remove(ent);
                    EntitiesNames.Remove(EntityName);
                }
                InvalidateCache();
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.AddLogMessage("Fail", $"DataView DropEntity('{EntityName}') failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
}