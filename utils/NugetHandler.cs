using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace TheTechIdea.Tools
{
    public class NugetHandler
    {
        public  NugetHandler()
        {

        }
        public IEnumerable<IPackageSearchMetadata> results { get; set; }
        public IEnumerable<IPackageSearchMetadata> packageMetaData { get; set; }
        public async Task SearchPackagesAsync(string searchstring)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: true);

            results = await resource.SearchAsync(
                searchstring,
                searchFilter,
                skip: 0,
                take: 20,
                logger,
                cancellationToken);

            foreach (IPackageSearchMetadata result in results)
            {
                Console.WriteLine($"Found package {result.Identity.Id} {result.Identity.Version}");
            }
        }
        public async Task GetPackageMetaDataAsync(string packagename)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            packageMetaData = await resource.GetMetadataAsync(
                packagename,
                includePrerelease: true,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            foreach (IPackageSearchMetadata package in packageMetaData)
            {
                Console.WriteLine($"Version: {package.Identity.Version}");
                Console.WriteLine($"Listed: {package.IsListed}");
                Console.WriteLine($"Tags: {package.Tags}");
                Console.WriteLine($"Description: {package.Description}");
            }
        }
        public async Task DownloadPackageAsync(string packagename,string version)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            string packageId = packagename;
            NuGetVersion packageVersion = new NuGetVersion(version);
             MemoryStream packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                packageId,
                packageVersion,
                packageStream,
                cache,
                logger,
                cancellationToken);

            Console.WriteLine($"Downloaded package {packageId} {packageVersion}");

            PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
            NuspecReader nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);

            Console.WriteLine($"Tags: {nuspecReader.GetTags()}");
            Console.WriteLine($"Description: {nuspecReader.GetDescription()}");
        }
       
    }
     
}
