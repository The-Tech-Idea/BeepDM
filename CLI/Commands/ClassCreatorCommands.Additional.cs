using System;
using System.CommandLine;
using TheTechIdea.Beep.CLI.Commands.Helpers;
using TheTechIdea.Beep.CLI.Infrastructure;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Additional ClassCreator commands using helper methods to work around System.CommandLine parameter limitations
    /// </summary>
    public static partial class ClassCreatorCommands
    {
        /// <summary>
        /// Builds additional commands that use helper methods for complex parameter scenarios
        /// </summary>
        public static Command[] BuildAdditionalCommands()
        {
            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ GENERATE POCO CLASS (DATASOURCE + ENTITY NAMES) ============
            
            var generatePocoDsCommand = new Command("generate-poco-ds", "Generate POCO classes from datasource using entity names");
            var pocoDsDsArg = new Argument<string>("datasource", "Data source name");
            var pocoDsClassNameArg = new Argument<string>("classname", "Base class name");
            var pocoDsEntitiesArg = new Argument<string[]>("entities", "Entity names to generate");
            var pocoDsOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var pocoDsNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");
            
            generatePocoDsCommand.AddArgument(pocoDsDsArg);
            generatePocoDsCommand.AddArgument(pocoDsClassNameArg);
            generatePocoDsCommand.AddArgument(pocoDsEntitiesArg);
            generatePocoDsCommand.AddOption(pocoDsOutputOpt);
            generatePocoDsCommand.AddOption(pocoDsNsOpt);
            generatePocoDsCommand.AddOption(profileOption);
            
            generatePocoDsCommand.SetHandler(async (string dsName, string className, string[] entityNames, string output, string ns, string profile) =>
            {
                await ClassCreatorCommandHelper.GeneratePocoFromDataSource(profile, dsName, className, entityNames, output, ns);
            }, pocoDsDsArg, pocoDsClassNameArg, pocoDsEntitiesArg, pocoDsOutputOpt, pocoDsNsOpt, profileOption);

            // ============ GENERATE INOTIFY CLASS (DATASOURCE + ENTITY NAMES) ============
            
            var generateINotifyDsCommand = new Command("generate-inotify-ds", "Generate INotifyPropertyChanged classes from datasource using entity names");
            var notDsDsArg = new Argument<string>("datasource", "Data source name");
            var notDsEntitiesArg = new Argument<string[]>("entities", "Entity names to generate");
            var notDsOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var notDsNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");
            
            generateINotifyDsCommand.AddArgument(notDsDsArg);
            generateINotifyDsCommand.AddArgument(notDsEntitiesArg);
            generateINotifyDsCommand.AddOption(notDsOutputOpt);
            generateINotifyDsCommand.AddOption(notDsNsOpt);
            generateINotifyDsCommand.AddOption(profileOption);
            
            generateINotifyDsCommand.SetHandler(async (string dsName, string[] entityNames, string output, string ns, string profile) =>
            {
                await ClassCreatorCommandHelper.GenerateINotifyFromDataSource(profile, dsName, entityNames, output, ns);
            }, notDsDsArg, notDsEntitiesArg, notDsOutputOpt, notDsNsOpt, profileOption);

            // ============ GENERATE ENTITY CLASS (DATASOURCE + ENTITY NAMES) ============
            
            var generateEntityDsCommand = new Command("generate-entity-ds", "Generate entity classes from datasource using entity names");
            var entDsDsArg = new Argument<string>("datasource", "Data source name");
            var entDsEntitiesArg = new Argument<string[]>("entities", "Entity names to generate");
            var entDsOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var entDsNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");
            
            generateEntityDsCommand.AddArgument(entDsDsArg);
            generateEntityDsCommand.AddArgument(entDsEntitiesArg);
            generateEntityDsCommand.AddOption(entDsOutputOpt);
            generateEntityDsCommand.AddOption(entDsNsOpt);
            generateEntityDsCommand.AddOption(profileOption);
            
            generateEntityDsCommand.SetHandler(async (string dsName, string[] entityNames, string output, string ns, string profile) =>
            {
                await ClassCreatorCommandHelper.GenerateEntityFromDataSource(profile, dsName, entityNames, output, ns);
            }, entDsDsArg, entDsEntitiesArg, entDsOutputOpt, entDsNsOpt, profileOption);

            return new Command[] 
            { 
                generatePocoDsCommand, 
                generateINotifyDsCommand, 
                generateEntityDsCommand 
            };
        }
    }
}
