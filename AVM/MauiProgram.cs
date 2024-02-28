using AVM.Services;
using AVM.ViewModels.CleanUp;
using AVM.ViewModels.Dashboard;
using AVM.ViewModels.Launcher;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using System.Reflection;

namespace AVM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("AVM.appsettings.json");

            if(File.Exists("appsettings.json"))
            {
                using var fileStream = File.OpenRead("appsettings.json");
                builder.Configuration.AddJsonStream(fileStream);
            }

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            builder.Configuration.AddConfiguration(config);

            builder.Services.AddSingleton<IConfiguration>(config);

            builder.Services.AddSingleton<VmManager>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddSingleton<CleanUpResource>();
            builder.Services.AddSingleton<CleanUpResourcesViewModel>();

            builder.Services.AddSingleton<LaunchDeployment>();
            builder.Services.AddSingleton<LaunchDeploymentViewModel>();

            builder.Services.AddSingleton<LaunchProgress>();
            builder.Services.AddSingleton<LaunchProgressViewModel>();

            builder.Services.AddSingleton<ResourceGroup>();
            builder.Services.AddSingleton<ResourceGroupViewModel>();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitMarkup()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 6 Free-Solid-900.otf", "FontAwesomeSolid");
                    fonts.AddFont("Font Awesome 6 Free-Regular-400.otf", "FontAwesomeRegular");
                    fonts.AddFont("Font Awesome 6 Brands-Regular-400.otf", "FontAwesomeBrandsRegular");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
