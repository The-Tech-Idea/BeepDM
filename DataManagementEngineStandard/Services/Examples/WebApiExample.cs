//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using TheTechIdea.Beep.ConfigUtil;
//using TheTechIdea.Beep.Container;
//using TheTechIdea.Beep.DataBase;
//using TheTechIdea.Beep.Report;
//using TheTechIdea.Beep.Services;
//using TheTechIdea.Beep.Utilities;

//namespace BeepExamples.Web
//{
//    /// <summary>
//    /// Minimal Web API example using the new BeepService API.
//    /// This is the simplest possible setup for an ASP.NET Core Web API.
//    /// </summary>
//    public class WebApiMinimalExample
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Step 1: Register BeepService for web
//            builder.Services.AddBeepForWeb(opts =>
//            {
//                opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep");
//                opts.AppRepoName = "MyWebApi";
//            });

//            // Step 2: Add controllers
//            builder.Services.AddControllers();

//            var app = builder.Build();

//            // Step 3: Use BeepService middleware
//            app.UseBeepForWeb(); // Adds connection cleanup

//            app.UseRouting();
//            app.MapControllers();

//            app.Run();
//        }
//    }

//    /// <summary>
//    /// Web API with fluent builder configuration.
//    /// Shows advanced options like connection pooling and API discovery.
//    /// </summary>
//    public class WebApiFluentExample
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Fluent builder API
//            builder.Services.AddBeepForWeb()
//                .InDirectory(Path.Combine(builder.Environment.ContentRootPath, "Beep"))
//                .WithAppRepo("MyWebApi")
//                .WithConnectionPooling()
//                .WithRequestIsolation()
//                .WithApiDiscovery()
//                .WithMapping()
//                .Build();

//            builder.Services.AddControllers();

//            var app = builder.Build();
//            app.UseBeepForWeb();
//            app.UseRouting();
//            app.MapControllers();
//            app.Run();
//        }
//    }

//    /// <summary>
//    /// Sample controller demonstrating BeepService usage in Web API.
//    /// </summary>
//    [ApiController]
//    [Route("api/[controller]")]
//    public class DataSourceController : ControllerBase
//    {
//        private readonly IBeepService _beepService;

//        public DataSourceController(IBeepService beepService)
//        {
//            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
//        }

//        /// <summary>
//        /// Gets all available data sources.
//        /// </summary>
//        [HttpGet("list")]
//        public IActionResult GetDataSources()
//        {
//            try
//            {
//                var dataSources = _beepService.DMEEditor.DataSources
//                    .Select(ds => new
//                    {
//                        Name = ds.DatasourceName,
//                        Type = ds.DatasourceType.ToString(),
//                        Category = ds.Category.ToString(),
//                        Status = ds.ConnectionStatus.ToString()
//                    });

//                return Ok(dataSources);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { error = ex.Message });
//            }
//        }

//        /// <summary>
//        /// Gets entities for a specific data source.
//        /// </summary>
//        [HttpGet("{connectionName}/entities")]
//        public async Task<IActionResult> GetEntities(string connectionName)
//        {
//            try
//            {
//                var ds = _beepService.DMEEditor.GetDataSource(connectionName);
//                if (ds == null)
//                    return NotFound(new { error = $"Data source '{connectionName}' not found" });

//                var entities = await ds.GetEntitiesAsync();
//                return Ok(entities.Select(e => new
//                {
//                    Name = e.EntityName,
//                    Type = e.EntityType
//                }));
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { error = ex.Message });
//            }
//        }

//        /// <summary>
//        /// Creates a new data connection.
//        /// </summary>
//        [HttpPost("connections")]
//        public IActionResult CreateConnection([FromBody] ConnectionRequest request)
//        {
//            try
//            {
//                var connProps = new ConnectionProperties
//                {
//                    ConnectionName = request.Name,
//                    DriverName = request.Driver,
//                    ConnectionString = request.ConnectionString,
//                    DatabaseType = request.DatabaseType,
//                    Category = DatasourceCategory.RDBMS
//                };

//                _beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
//                var ds = _beepService.DMEEditor.GetDataSource(request.Name);

//                return Ok(new
//                {
//                    success = true,
//                    message = "Connection created successfully",
//                    connectionName = request.Name
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { error = ex.Message });
//            }
//        }

//        /// <summary>
//        /// Gets data from an entity with optional filtering.
//        /// </summary>
//        [HttpGet("{connectionName}/data/{entityName}")]
//        public async Task<IActionResult> GetData(string connectionName, string entityName, 
//            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
//        {
//            try
//            {
//                var ds = _beepService.DMEEditor.GetDataSource(connectionName);
//                if (ds == null)
//                    return NotFound(new { error = $"Data source '{connectionName}' not found" });

//                var entity = ds.Entities.FirstOrDefault(e => 
//                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    
//                if (entity == null)
//                    return NotFound(new { error = $"Entity '{entityName}' not found" });

//                var result = ds.GetEntity(entityName, new List<AppFilter>(), pageNumber, pageSize);

//                return Ok(new
//                {
//                    pageNumber,
//                    pageSize,
//                    totalRecords = result.TotalRecords,
//                    data = result.Data
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { error = ex.Message });
//            }
//        }
//    }

//    /// <summary>
//    /// Request model for creating connections.
//    /// </summary>
//    public class ConnectionRequest
//    {
//        public string Name { get; set; }
//        public string Driver { get; set; }
//        public string ConnectionString { get; set; }
//        public DataSourceType DatabaseType { get; set; }
//    }
//}
