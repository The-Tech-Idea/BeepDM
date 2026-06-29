using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Read-only catalog of tree-building metadata (branches, functions,
    /// object types, category folders). Wraps ConfigEditor reads so that
    /// Blazor tree components never reach into ConfigEditor directly.
    /// </summary>
    public class TreeCatalogService
    {
        private readonly IDMEEditor _dme;

        public TreeCatalogService(IDMEEditor dme)
        {
            _dme = dme ?? throw new ArgumentNullException(nameof(dme));
        }

        public List<AssemblyClassDefinition> GetBranchesClasses()
            => _dme.ConfigEditor.BranchesClasses;

        public List<AssemblyClassDefinition> GetGlobalFunctions()
            => _dme.ConfigEditor.GlobalFunctions;

        public List<ObjectTypes> GetObjectTypes()
            => _dme.ConfigEditor.objectTypes;

        public List<CategoryFolder> GetCategoryFolders()
            => _dme.ConfigEditor.CategoryFolders;

        public void SaveCategoryFolders()
            => _dme.ConfigEditor.SaveCategoryFoldersValues();
    }
}
