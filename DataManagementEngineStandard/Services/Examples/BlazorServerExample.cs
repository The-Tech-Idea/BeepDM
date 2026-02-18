//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Components;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using TheTechIdea.Beep.Container;
//using TheTechIdea.Beep.DataBase;
//using TheTechIdea.Beep.Services;

//namespace BeepExamples.Blazor
//{
//    /// <summary>
//    /// Blazor Server minimal example using the new BeepService API.
//    /// </summary>
//    public class BlazorServerMinimalExample
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Step 1: Register BeepService for Blazor Server
//            builder.Services.AddBeepForBlazorServer(opts =>
//            {
//                opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep");
//                opts.AppRepoName = "MyBlazorApp";
//                opts.EnableSignalRProgress = true;
//            });

//            // Step 2: Add Blazor services
//            builder.Services.AddRazorPages();
//            builder.Services.AddServerSideBlazor();

//            var app = builder.Build();

//            app.UseStaticFiles();
//            app.UseRouting();
//            app.MapBlazorHub();
//            app.MapFallbackToPage("/_Host");

//            app.Run();
//        }
//    }

//    /// <summary>
//    /// Blazor Server with fluent builder configuration.
//    /// </summary>
//    public class BlazorServerFluentExample
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Fluent builder API for Blazor Server
//            builder.Services.AddBeepForBlazorServer()
//                .InDirectory(Path.Combine(builder.Environment.ContentRootPath, "Beep"))
//                .WithAppRepo("MyBlazorApp")
//                .WithSignalR()
//                .WithCircuitHandlers()
//                .WithMapping()
//                .Build();

//            builder.Services.AddRazorPages();
//            builder.Services.AddServerSideBlazor();

//            var app = builder.Build();

//            app.UseStaticFiles();
//            app.UseRouting();
//            app.MapBlazorHub();
//            app.MapFallbackToPage("/_Host");

//            app.Run();
//        }
//    }

//    /// <summary>
//    /// Blazor WebAssembly minimal example.
//    /// </summary>
//    public class BlazorWasmMinimalExample
//    {
//        public static async Task Main(string[] args)
//        {
//            var builder = WebAssemblyHostBuilder.CreateDefault(args);
//            builder.RootComponents.Add<App>("#app");

//            // Register BeepService for Blazor WASM
//            builder.Services.AddBeepForBlazorWasm(opts =>
//            {
//                opts.DirectoryPath = "Beep"; // Browser storage path
//                opts.AppRepoName = "MyBlazorWasmApp";
//                opts.UseBrowserStorage = true;
//            });

//            await builder.Build().RunAsync();
//        }
//    }

//    /// <summary>
//    /// Blazor component example showing BeepService usage.
//    /// </summary>
//    public class DataSourcesComponent : ComponentBase
//    {
//        [Inject]
//        public IBeepService BeepService { get; set; }

//        protected List<DataSourceInfo> DataSources { get; set; } = new();
//        protected bool IsLoading { get; set; } = true;
//        protected string ErrorMessage { get; set; }

//        protected override async Task OnInitializedAsync()
//        {
//            try
//            {
//                await LoadDataSources();
//            }
//            catch (Exception ex)
//            {
//                ErrorMessage = $"Error loading data sources: {ex.Message}";
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        private async Task LoadDataSources()
//        {
//            DataSources = BeepService.DMEEditor.DataSources
//                .Select(ds => new DataSourceInfo
//                {
//                    Name = ds.DatasourceName,
//                    Type = ds.DatasourceType.ToString(),
//                    Category = ds.Category.ToString(),
//                    Status = ds.ConnectionStatus.ToString()
//                })
//                .ToList();

//            await Task.CompletedTask;
//        }

//        protected async Task RefreshDataSources()
//        {
//            IsLoading = true;
//            ErrorMessage = null;
//            try
//            {
//                await LoadDataSources();
//            }
//            catch (Exception ex)
//            {
//                ErrorMessage = $"Error refreshing data sources: {ex.Message}";
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }
//    }

//    /// <summary>
//    /// Blazor component for entity browsing.
//    /// </summary>
//    public class EntityBrowserComponent : ComponentBase
//    {
//        [Inject]
//        public IBeepService BeepService { get; set; }

//        [Parameter]
//        public string ConnectionName { get; set; }

//        protected List<EntityInfo> Entities { get; set; } = new();
//        protected bool IsLoading { get; set; } = true;
//        protected string ErrorMessage { get; set; }

//        protected override async Task OnParametersSetAsync()
//        {
//            if (!string.IsNullOrEmpty(ConnectionName))
//            {
//                await LoadEntities();
//            }
//        }

//        private async Task LoadEntities()
//        {
//            IsLoading = true;
//            ErrorMessage = null;

//            try
//            {
//                var ds = BeepService.DMEEditor.GetDataSource(ConnectionName);
//                if (ds == null)
//                {
//                    ErrorMessage = $"Data source '{ConnectionName}' not found";
//                    return;
//                }

//                var entities = await ds.GetEntitiesAsync();
//                Entities = entities.Select(e => new EntityInfo
//                {
//                    Name = e.EntityName,
//                    Type = e.EntityType,
//                    FieldCount = e.Fields?.Count ?? 0
//                }).ToList();
//            }
//            catch (Exception ex)
//            {
//                ErrorMessage = $"Error loading entities: {ex.Message}";
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }
//    }

//    /// <summary>
//    /// Data transfer objects for Blazor components.
//    /// </summary>
//    public class DataSourceInfo
//    {
//        public string Name { get; set; }
//        public string Type { get; set; }
//        public string Category { get; set; }
//        public string Status { get; set; }
//    }

//    public class EntityInfo
//    {
//        public string Name { get; set; }
//        public string Type { get; set; }
//        public int FieldCount { get; set; }
//    }

//    /// <summary>
//    /// Sample WebAssemblyHostBuilder (for WASM example).
//    /// This is a simplified version - actual implementation would use Microsoft.AspNetCore.Components.WebAssembly.Hosting.
//    /// </summary>
//    public class WebAssemblyHostBuilder
//    {
//        public List<object> RootComponents { get; } = new();
//        public IServiceCollection Services { get; } = new ServiceCollection();

//        public static WebAssemblyHostBuilder CreateDefault(string[] args) => new();

//        public void Add<T>(string selector) => RootComponents.Add(new { Type = typeof(T), Selector = selector });

//        public object Build() => this;
//        public async Task RunAsync() => await Task.CompletedTask;
//    }

//    public class App { }
//}
