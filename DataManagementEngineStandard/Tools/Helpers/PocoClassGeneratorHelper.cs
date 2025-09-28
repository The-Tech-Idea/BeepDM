using System;
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
        /// Creates an Entity class that inherits from Entity base class
        /// </summary>
        public string CreateEntityClass(EntityStructure entity, string usingHeader, string extraCode, 
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true)
        {
            const string implementations = "Entity, INotifyPropertyChanged";
            
            var notificationCode = @"
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }
            return false;
        }";

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