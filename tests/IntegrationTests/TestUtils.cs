using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_helpers.IntegrationTests
{
    internal static class TestUtils
    {
        public static string CreateTempDirectory(string prefix = "integration_test_")
        {
            var temp = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            return temp;
        }

        public static async Task<string> PackClassLibraryAsync(string packageId, string version, string code, string outFeed)
        {
            // Create new temp project
            var tempdir = CreateTempDirectory("nugetproj_");
            var projectDir = Path.Combine(tempdir, packageId);
            Directory.CreateDirectory(projectDir);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new classlib --no-restore --language C#",
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var r = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet new");
            await r.WaitForExitAsync();

            // Write code to Class1.cs
            var codeFile = Path.Combine(projectDir, "Class1.cs");
            await File.WriteAllTextAsync(codeFile, code);

            // Edit csproj to set PackageId and Version and generate package on build
            var csproj = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault()!
                ?? throw new InvalidOperationException("csproj not found");

            var csprojText = await File.ReadAllTextAsync(csproj);
            if (!csprojText.Contains("<PackageId>"))
            {
                csprojText = csprojText.Replace("</PropertyGroup>", $"  <PackageId>{packageId}</PackageId>\n  <PackageVersion>{version}</PackageVersion>\n  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>\n</PropertyGroup>");
                await File.WriteAllTextAsync(csproj, csprojText);
            }

            // Pack the project to feed dir
            Directory.CreateDirectory(outFeed);
            var packInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"pack -c Release -o \"{outFeed}\"",
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var packProc = Process.Start(packInfo) ?? throw new InvalidOperationException("Failed to start dotnet pack");
            var output = await packProc.StandardOutput.ReadToEndAsync();
            var error = await packProc.StandardError.ReadToEndAsync();
            await packProc.WaitForExitAsync();
            if (packProc.ExitCode != 0)
            {
                throw new InvalidOperationException($"dotnet pack failed: {output}\n{error}");
            }

            // find nupkg
            var nupkg = Directory.GetFiles(outFeed, "*.nupkg", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (nupkg == null) throw new InvalidOperationException("nupkg not found");
            return nupkg;
        }

        public static async Task<(string nupkg, string projectDir)> PackDependentClassLibrariesAsync(string commonId, string commonVersion, string pluginId, string pluginVersion, string commonCode, string pluginCode, string feedDir)
        {
            var commonNupkg = await PackClassLibraryAsync(commonId, commonVersion, commonCode, feedDir);

            // Now create plugin which references the common package by package id & version
            var tempdir = CreateTempDirectory("nugetproj_");
            var projectDir = Path.Combine(tempdir, pluginId);
            Directory.CreateDirectory(projectDir);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new classlib --no-restore --language C#",
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var r = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet new");
            await r.WaitForExitAsync();

            var csproj = Directory.GetFiles(projectDir, "*.csproj").First();
            var csprojText = await File.ReadAllTextAsync(csproj);
            // insert PackageId + PackageVersion
            if (!csprojText.Contains("<PackageId>"))
            {
                csprojText = csprojText.Replace("</PropertyGroup>", $"  <PackageId>{pluginId}</PackageId>\n  <PackageVersion>{pluginVersion}</PackageVersion>\n  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>\n</PropertyGroup>");
            }
            // add package source to restore from feed
            // add PackageReference to common package
            // Add restore source via RestoreSources property
            csprojText = csprojText.Replace("</PropertyGroup>", $"  <RestoreSources>{feedDir}</RestoreSources>\n</PropertyGroup>");
            csprojText = csprojText.Replace("</ItemGroup>", $"  <PackageReference Include=\"{commonId}\" Version=\"{commonVersion}\" />\n</ItemGroup>");

            await File.WriteAllTextAsync(csproj, csprojText);

            // Write code
            var codeFile = Path.Combine(projectDir, "Class1.cs");
            await File.WriteAllTextAsync(codeFile, pluginCode);

            // Pack
            var packInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"pack -c Release -o \"{feedDir}\"",
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var packProc = Process.Start(packInfo) ?? throw new InvalidOperationException("Failed to start dotnet pack");
            var output = await packProc.StandardOutput.ReadToEndAsync();
            var error = await packProc.StandardError.ReadToEndAsync();
            await packProc.WaitForExitAsync();
            if (packProc.ExitCode != 0)
            {
                throw new InvalidOperationException($"dotnet pack failed: {output}\n{error}");
            }

            var pluginNupkg = Directory.GetFiles(feedDir, "*.nupkg", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault(p => p.Contains(pluginId.ToLower()));
            if (pluginNupkg == null) throw new InvalidOperationException("plugin nupkg not found");

            return (pluginNupkg, projectDir);
        }

        public static async Task<string[]> MakeLocalFeedWithPackagesAsync(params (string id, string version, string code)[] packages)
        {
            var feed = CreateTempDirectory("nugetfeed_");
            var results = new List<string>();
            foreach (var pkg in packages)
            {
                var nupkg = await PackClassLibraryAsync(pkg.id, pkg.version, pkg.code, feed);
                results.Add(nupkg);
            }
            return results.ToArray();
        }
    }
}
