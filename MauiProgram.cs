using Microsoft.Extensions.Logging;
using NovelBook.Services;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace NovelBook;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registrar servicios
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<NovelService>();
        builder.Services.AddSingleton<LibraryService>();

        // Registrar el servicio de biometría
        builder.Services.AddSingleton(typeof(IFingerprint), CrossFingerprint.Current);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}