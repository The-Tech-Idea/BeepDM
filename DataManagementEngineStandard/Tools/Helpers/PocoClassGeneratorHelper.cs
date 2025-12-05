using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating POCO classes and basic class structures
    /// </summary>
    public class PocoClassGeneratorHelper : IPocoClassGenerator
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public PocoClassGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Creates a simple POCO class with basic properties
        /// </summary>
        public string CreatePOCOClass(string classname, EntityStructure entity, string usingheader, 
            string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true)
        {
            return CreateClassFromTemplate(classname, entity, "public :FIELDTYPE? :FIELDNAME { get; set; }", 
                usingheader, implementations, extracode, outputpath, nameSpacestring, generateCSharpCodeFiles);
        }

        /// <summary>
        /// Creates multiple POCO classes from a list of entities
        /// </summary>
        public string CreatePOCOClass(string classname, List<EntityStructure> entities, string usingheader, 
            string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true)
        {
            var results = new StringBuilder();
            foreach (var entity in entities)
            {
                var result = CreatePOCOClass(entity.EntityName, entity, usingheader, implementations, 
                    extracode, outputpath, nameSpacestring, generateCSharpCodeFiles);
                results.AppendLine($"Generated {entity.EntityName}.cs");
            }
            return results.ToString();
        }

        /// <summary>
        /// Creates a class that implements INotifyPropertyChanged
        /// </summary>
        public string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations, 
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", 
            bool generateCSharpCodeFiles = true)
        {
            string implementations2 = string.IsNullOrEmpty(implementations) 
                ? "INotifyPropertyChanged" 
                : implementations + ", INotifyPropertyChanged";

            string extracode2 = Environment.NewLine + @"
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property.
        /// The CallerMemberName attribute that is applied to the optional propertyName
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = """")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }";

            string template2 = GenerateNotifyPropertyTemplate();
            
            return CreateClassFromTemplate(entity.EntityName, entity, template2, usingheader, 
                implementations2, extracode2, outputpath, nameSpacestring, generateCSharpCodeFiles);
        }

        /// <summary>
        /// Creates multiple INotifyPropertyChanged classes from a list of entities
        /// </summary>
        public string CreateINotifyClass(List<EntityStructure> entities, string usingheader, string implementations, 
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", 
            bool generateCSharpCodeFiles = true)
        {
            var results = new StringBuilder();
            foreach (var entity in entities)
            {
                var result = CreateINotifyClass(entity, usingheader, implementations, 
                    extracode, outputpath, nameSpacestring, generateCSharpCodeFiles);
                results.AppendLine($"Generated {entity.EntityName}.cs");
            }
            return results.ToString();
        }

        /// <summary>
        /// Creates an Entity class that inherits from Entity base class
        /// Uses proper inheritance from TheTechIdea.Beep.Editor.Entity
        /// </summary>
        public string CreateEntityClass(EntityStructure entity, string usingHeader, string extraCode, 
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true)
        {
            // Ensure proper using statements for Entity base class
            var defaultUsingHeader = @"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;";

            if (string.IsNullOrWhiteSpace(usingHeader))
            {
                usingHeader = defaultUsingHeader;
            }
            else if (!usingHeader.Contains("TheTechIdea.Beep.Editor"))
            {
                usingHeader += "\nusing TheTechIdea.Beep.Editor;";
            }

            // Entity class inherits from Entity base class which already implements INotifyPropertyChanged
            const string implementations = "Entity";
            
            // Note: Entity base class already provides PropertyChanged event and SetProperty method
            // So we don't need to add them here, but we can add custom code if needed
            var notificationCode = extraCode ?? @"
        // Entity base class provides:
        // - PropertyChanged event (from INotifyPropertyChanged)
        // - OnPropertyChanged method
        // - SetProperty<T> method for property change notifications
";

            var fieldTemplate = @"
private :FIELDTYPE? :BACKINGFIELD;

public :FIELDTYPE? :FIELDNAME
{
    get => :BACKINGFIELD;
    set => SetProperty(ref :BACKINGFIELD, value);
}";

            return CreateClassFromTemplate(entity.EntityName, entity, fieldTemplate, usingHeader, 
                implementations, notificationCode, outputPath, namespaceString, generateFiles);
        }

        /// <summary>
        /// Creates multiple Entity classes from a list of entities
        /// </summary>
        public string CreateEntityClass(List<EntityStructure> entities, string usingHeader, string extraCode, 
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true)
        {
            var results = new StringBuilder();
            foreach (var entity in entities)
            {
                var result = CreateEntityClass(entity, usingHeader, extraCode, 
                    outputPath, namespaceString, generateFiles);
                results.AppendLine($"Generated {entity.EntityName}.cs");
            }
            return results.ToString();
        }

        /// <summary>
        /// Creates POCO classes from datasource using entity names
        /// </summary>
        public string CreatePOCOClass(string datasourcename, string classname, List<string> entities, string usingheader, 
            string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true)
        {
            var results = new StringBuilder();
            var ds = _dmeEditor.GetDataSource(datasourcename);
            if (ds == null)
            {
                throw new ArgumentException($"Data source '{datasourcename}' not found");
            }

            foreach (var entityName in entities)
            {
                var entity = ds.GetEntityStructure(entityName, true);
                if (entity != null)
                {
                    var result = CreatePOCOClass(classname, entity, usingheader, implementations, 
                        extracode, outputpath, nameSpacestring, generateCSharpCodeFiles);
                    results.AppendLine($"Generated {entityName}.cs");
                }
                else
                {
                    results.AppendLine($"Entity '{entityName}' not found in datasource '{datasourcename}'");
                }
            }
            return results.ToString();
        }

        /// <summary>
        /// Creates INotifyPropertyChanged classes from datasource using entity names
        /// </summary>
        public string CreateINotifyClass(string datasourcename, List<string> entities, string usingheader, string implementations, 
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", 
            bool generateCSharpCodeFiles = true)
        {
            var results = new StringBuilder();
            var ds = _dmeEditor.GetDataSource(datasourcename);
            if (ds == null)
            {
                throw new ArgumentException($"Data source '{datasourcename}' not found");
            }

            foreach (var entityName in entities)
            {
                var entity = ds.GetEntityStructure(entityName, true);
                if (entity != null)
                {
                    var result = CreateINotifyClass(entity, usingheader, implementations, 
                        extracode, outputpath, nameSpacestring, generateCSharpCodeFiles);
                    results.AppendLine($"Generated {entityName}.cs");
                }
                else
                {
                    results.AppendLine($"Entity '{entityName}' not found in datasource '{datasourcename}'");
                }
            }
            return results.ToString();
        }

        /// <summary>
        /// Creates Entity classes from datasource using entity names
        /// </summary>
        public string CreateEntityClass(string datasourcename, List<string> entities, string usingHeader, string extraCode, 
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true)
        {
            var results = new StringBuilder();
            var ds = _dmeEditor.GetDataSource(datasourcename);
            if (ds == null)
            {
                throw new ArgumentException($"Data source '{datasourcename}' not found");
            }

            foreach (var entityName in entities)
            {
                var entity = ds.GetEntityStructure(entityName, true);
                if (entity != null)
                {
                    var result = CreateEntityClass(entity, usingHeader, extraCode, 
                        outputPath, namespaceString, generateFiles);
                    results.AppendLine($"Generated {entityName}.cs");
                }
                else
                {
                    results.AppendLine($"Entity '{entityName}' not found in datasource '{datasourcename}'");
                }
            }
            return results.ToString();
        }

        /// <summary>
        /// Creates a class from a template with field substitution
        /// </summary>
        private string CreateClassFromTemplate(string classname, EntityStructure entity, string template, 
            string usingheader, string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true)
        {
            var cls = string.IsNullOrWhiteSpace(classname) ? entity.EntityName : classname;
            var sb = new StringBuilder();

            // Add using statements
            if (!string.IsNullOrWhiteSpace(usingheader))
            {
                sb.AppendLine(usingheader);
            }
            else
            {
                sb.AppendLine(_helper.GenerateStandardUsings("using TheTechIdea.Beep.DataBase;"));
            }
            
            sb.AppendLine();
            sb.AppendLine($"namespace {nameSpacestring}");
            sb.AppendLine("{");

            // Class declaration
            var inherit = string.IsNullOrWhiteSpace(implementations) ? "" : $" : {implementations}";
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Represents the {cls} entity");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {cls}{inherit}");
            sb.AppendLine("    {");
            
            // Constructor
            sb.AppendLine($"        public {cls}() {{ }}");
            sb.AppendLine();

            // Generate properties for each field
            for (int i = 0; i < entity.Fields.Count; i++)
            {
                var field = entity.Fields[i];
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname, i);
                var backingFieldName = _helper.GenerateBackingFieldName(safePropertyName);

                var fieldCode = template
                    .Replace(":FIELDTYPE", field.fieldtype)
                    .Replace(":FIELDNAME", safePropertyName)
                    .Replace(":BACKINGFIELD", backingFieldName);

                sb.AppendLine($"        {fieldCode}");
                sb.AppendLine();
            }

            // Add extra code if provided
            if (!string.IsNullOrWhiteSpace(extracode))
            {
                var lines = extracode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    sb.AppendLine($"        {line.TrimEnd()}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();

            // Write to file if requested
            if (generateCSharpCodeFiles)
            {
                outputpath = _helper.EnsureOutputDirectory(outputpath);
                var filePath = Path.Combine(outputpath, $"{cls}.cs");
                _helper.WriteToFile(filePath, result, cls);
            }

            return result;
        }

        /// <summary>
        /// Generates the template for INotifyPropertyChanged properties
        /// </summary>
        private string GenerateNotifyPropertyTemplate()
        {
            return @"
private :FIELDTYPE? :BACKINGFIELD;

public :FIELDTYPE? :FIELDNAME
{
    get => :BACKINGFIELD;
    set
    {
        if (!EqualityComparer<:FIELDTYPE>.Default.Equals(:BACKINGFIELD, value))
        {
            :BACKINGFIELD = value;
            NotifyPropertyChanged();
        }
    }
}";
        }
    }
}