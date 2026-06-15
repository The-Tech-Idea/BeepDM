using System;
using System.Linq;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Static heuristics for detecting project roles from .csproj metadata.
    /// Returns (role, confidence). First match wins in priority order.
    /// </summary>
    public static class RoleDetectionHeuristics
    {
        /// <summary>
        /// Detect the most likely role for a project. Returns (role, confidence, heuristicName).
        /// </summary>
        public static (ProjectRole Role, float Confidence, string? Heuristic) Detect(ProjectInfo project)
        {
            // 1. Test projects — definitive
            if (project.IsTestProject)
                return (ProjectRole.Test, 1.0f, "TestFrameworkReference");

            // 2. IdentityServer — name or package
            if (project.Name.Contains("IdentityServer", StringComparison.OrdinalIgnoreCase))
                return (ProjectRole.IdentityServer, 1.0f, "NameContainsIdentityServer");
            if (HasPackage(project, "Duende.IdentityServer") || HasPackage(project, "IdentityServer4"))
                return (ProjectRole.IdentityServer, 0.95f, "IdentityServerPackageRef");

            // 3. Data — DbContext in Data folders
            if (project.DataFolders.Any(d => d.HasDbContext))
                return (ProjectRole.Data, 0.95f, "DataFolderWithDbContext");
            if (HasPackage(project, "Microsoft.EntityFrameworkCore"))
                return (ProjectRole.Data, 0.8f, "EFCorePackageRef");
            if (project.Name.EndsWith(".Data", StringComparison.OrdinalIgnoreCase))
                return (ProjectRole.Data, 0.7f, "NameEndsWithData");

            // 4. API — Controllers or ASP.NET Core API markers
            if (project.Sdk != null && project.Sdk.Contains("Web", StringComparison.OrdinalIgnoreCase) &&
                !HasPackage(project, "MudBlazor") && !HasPackage(project, "Microsoft.AspNetCore.Components"))
                return (ProjectRole.Api, 0.85f, "WebSdkNoBlazor");
            if (HasPackage(project, "Microsoft.AspNetCore.Mvc") || HasPackage(project, "Swashbuckle"))
                return (ProjectRole.Api, 0.8f, "MvcOrSwaggerPackage");

            // 5. Web — Blazor/MVC/Razor
            if (HasPackage(project, "MudBlazor") || HasPackage(project, "Microsoft.AspNetCore.Components.Web"))
                return (ProjectRole.Web, 0.9f, "BlazorPackageRef");
            if (project.Sdk != null && project.Sdk.Contains("Web", StringComparison.OrdinalIgnoreCase))
                return (ProjectRole.Web, 0.7f, "WebSdk");

            // 6. Service — Aspire, BackgroundService
            if (HasPackage(project, "Aspire.Hosting"))
                return (ProjectRole.Service, 0.95f, "AspireHostingPackage");
            if (HasPackage(project, "Microsoft.Extensions.Hosting"))
                return (ProjectRole.Service, 0.6f, "HostingPackage");

            // 7. Console — Exe output type
            if (project.OutputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
                return (ProjectRole.Console, 0.8f, "OutputTypeExe");

            // 8. Library — fallback
            if (project.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase))
                return (ProjectRole.Library, 0.5f, "OutputTypeLibrary");

            // 9. Unknown
            return (ProjectRole.Unknown, 0.0f, null);
        }

        private static bool HasPackage(ProjectInfo project, string packagePrefix)
        {
            return project.PackageReferences.Keys.Any(k =>
                k.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
