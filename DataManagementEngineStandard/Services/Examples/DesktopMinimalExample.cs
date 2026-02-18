//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Windows.Forms;
//using TheTechIdea.Beep.Container;
//using TheTechIdea.Beep.Services;
//using TheTechIdea.Beep.Utilities;

//namespace BeepExamples.Desktop
//{
//    /// <summary>
//    /// Minimal desktop application example using the new BeepService API.
//    /// This is the simplest possible setup for a WinForms application.
//    /// </summary>
//    public class DesktopMinimalExample
//    {
//        [STAThread]
//        static void Main(string[] args)
//        {
//            // Step 1: Create IHost with BeepService registration
//            var host = Host.CreateDefaultBuilder(args)
//                .ConfigureServices((context, services) =>
//                {
//                    // Single line registration for desktop apps
//                    services.AddBeepForDesktop(opts => 
//                        opts.DirectoryPath = AppContext.BaseDirectory);
//                })
//                .Build();

//            // Step 2: Initialize BeepService and load assemblies
//            var beepService = host.UseBeepForDesktop();

//            // Step 3: Run your application
//            Application.Run(new MainForm(beepService));
//        }
//    }

//    /// <summary>
//    /// Desktop application with progress reporting during assembly loading.
//    /// Shows how to provide user feedback during initialization.
//    /// </summary>
//    public class DesktopWithProgressExample
//    {
//        [STAThread]
//        static void Main(string[] args)
//        {
//            var host = Host.CreateDefaultBuilder(args)
//                .ConfigureServices((context, services) =>
//                {
//                    services.AddBeepForDesktop(opts =>
//                    {
//                        opts.DirectoryPath = AppContext.BaseDirectory;
//                        opts.AppRepoName = "MyDesktopApp";
//                        opts.EnableProgressReporting = true;
//                        opts.EnableAutoMapping = true;
//                        opts.EnableAssemblyLoading = true;
//                    });
//                })
//                .Build();

//            // Create progress reporter
//            var progress = new Progress<PassedArgs>(args =>
//            {
//                Console.WriteLine($"[{args.EventType}] {args.Messege}");
//                if (args.ParameterInt1 > 0)
//                    Console.WriteLine($"Progress: {args.ParameterInt1}%");
//            });

//            // Initialize with progress reporting
//            var beepService = host.UseBeepForDesktop(progress);

//            Application.Run(new MainForm(beepService));
//        }
//    }

//    /// <summary>
//    /// Desktop application using fluent builder API.
//    /// Demonstrates method chaining for configuration.
//    /// </summary>
//    public class DesktopFluentExample
//    {
//        [STAThread]
//        static void Main(string[] args)
//        {
//            var host = Host.CreateDefaultBuilder(args)
//                .ConfigureServices((context, services) =>
//                {
//                    // Fluent builder API with method chaining
//                    services.AddBeepForDesktop()
//                        .InDirectory(AppContext.BaseDirectory)
//                        .WithAppRepo("MyDesktopApp")
//                        .WithProgressUI()
//                        .WithDesignTimeSupport()
//                        .WithMapping()
//                        .WithAssemblyLoading()
//                        .Build();
//                })
//                .Build();

//            var beepService = host.UseBeepForDesktop();
//            Application.Run(new MainForm(beepService));
//        }
//    }

//    /// <summary>
//    /// Sample MainForm using injected BeepService.
//    /// Shows proper dependency usage in WinForms.
//    /// </summary>
//    public class MainForm : Form
//    {
//        private readonly IBeepService _beepService;

//        public MainForm(IBeepService beepService)
//        {
//            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
//            InitializeComponent();
//            LoadData();
//        }

//        private void InitializeComponent()
//        {
//            this.Text = "Beep Desktop Example";
//            this.Size = new System.Drawing.Size(800, 600);
//        }

//        private async void LoadData()
//        {
//            try
//            {
//                // Access DMEEditor through BeepService
//                var dataSources = _beepService.DMEEditor.DataSources;
//                Console.WriteLine($"Loaded {dataSources.Count} data sources");

//                // Example: Get a data source
//                if (dataSources.Count > 0)
//                {
//                    var ds = dataSources[0];
//                    var entities = await ds.GetEntitiesAsync();
//                    Console.WriteLine($"Data source has {entities?.Count()} entities");
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }
//    }
//}
