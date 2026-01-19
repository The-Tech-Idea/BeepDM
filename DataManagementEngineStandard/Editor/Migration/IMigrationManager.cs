using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Helpers;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public interface IMigrationManager
    {
        IDMEEditor DMEEditor { get; }
        IDataSource MigrateDataSource { get; set; }

        IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true);
        IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true);
        IReadOnlyList<EntityField> GetMissingColumns(EntityStructure current, EntityStructure desired);

        IErrorsInfo CreateEntity(EntityStructure entity);
        IErrorsInfo DropEntity(string entityName);
        IErrorsInfo TruncateEntity(string entityName);
        IErrorsInfo RenameEntity(string oldName, string newName);
        IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn);
        IErrorsInfo DropColumn(string entityName, string columnName);
        IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName);
        IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null);
        
    }
}
