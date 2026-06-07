using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp.Steps
{
    public class DataImportStepOptions
    {
        public List<string> EntityNames { get; set; } = new();
        public bool SkipIfTargetHasData { get; set; } = true;
    }

    public class DataImportStep : ISetupStep
    {
        private readonly DataImportStepOptions _options;

        public DataImportStep(DataImportStepOptions options = null)
        {
            _options = options ?? new DataImportStepOptions();
        }

        public string StepId => "data-import";
        public string StepName => "Import Initial Data";
        public string Description => "Verifies that key entities exist and reports their record counts. Use DataImportManager separately for actual data import.";
        public IReadOnlyList<string> DependsOn => new[] { "defaults-setup", "seeding" };

        public bool CanSkip(SetupContext context)
        {
            return _options.EntityNames == null || _options.EntityNames.Count == 0;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Editor is not available." };
            return new ErrorsInfo { Flag = Errors.Ok };
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            try
            {
                var ds = context.DataSource;
                if (ds == null)
                    return new ErrorsInfo { Flag = Errors.Ok, Message = "No datasource available for import verification." };

                var total = _options.EntityNames.Count;
                var verified = 0;
                var messages = new List<string>();

                foreach (var entityName in _options.EntityNames)
                {
                    var pct = total > 0 ? (int)(verified * 100.0 / total) : 0;
                    progress?.Report(new PassedArgs { Messege = $"Verifying '{entityName}'...", ParameterInt1 = pct });

                    var entity = ds.GetEntityStructure(entityName, false);
                    if (entity == null)
                    {
                        messages.Add($"Entity '{entityName}' does not exist yet.");
                    }
                    else if (_options.SkipIfTargetHasData)
                    {
                        var rows = ds.GetEntity(entityName, new List<AppFilter>());
                        int count = (rows as System.Collections.IList)?.Count ?? 0;
                        messages.Add($"Entity '{entityName}' exists with {count} records.");
                    }
                    else
                    {
                        messages.Add($"Entity '{entityName}' verified.");
                    }

                    verified++;
                }

                progress?.Report(new PassedArgs { Messege = $"Verified {verified} entities.", ParameterInt1 = 100 });
                return new ErrorsInfo { Flag = Errors.Ok, Message = string.Join("; ", messages) };
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = $"Data import verification failed: {ex.Message}" };
            }
        }
    }
}
