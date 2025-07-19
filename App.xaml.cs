using NovelBook.Views;
using System.Globalization;
using NovelBook.Services;


namespace NovelBook
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Aplicar tema guardado
            ApplySavedTheme();

            // Aplicar idioma guardado
            ApplySavedLanguage();

            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                BarTextColor = Colors.White
            };
        }

        /// <summary>
        /// Aplica el tema guardado en las preferencias
        /// </summary>
        private void ApplySavedTheme()
        {
            var savedTheme = Preferences.Get("AppTheme", "System");

            switch (savedTheme)
            {
                case "Light":
                    UserAppTheme = AppTheme.Light;
                    break;
                case "Dark":
                    UserAppTheme = AppTheme.Dark;
                    break;
                default:
                    UserAppTheme = AppTheme.Unspecified;
                    break;
            }
        }

        /// <summary>
        /// Aplica el idioma guardado en las preferencias
        /// </summary>
        private void ApplySavedLanguage()
        {
            try
            {
                var savedLanguage = Preferences.Get("AppLanguage", "system");
                string languageToApply;

                if (savedLanguage == "system")
                {
                    // Obtener el idioma del sistema
                    var culture = CultureInfo.CurrentUICulture;
                    var systemLanguage = culture.TwoLetterISOLanguageName.ToLower();

                    // Si el sistema está en español o inglés, usar ese idioma
                    // De lo contrario, usar español como predeterminado
                    languageToApply = (systemLanguage == "es" || systemLanguage == "en") ? systemLanguage : "es";
                }
                else
                {
                    // Usar el idioma guardado específicamente
                    languageToApply = savedLanguage;
                }

                // Aplicar el idioma
                LocalizationService.SetLanguage(languageToApply);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando idioma: {ex.Message}");
                // En caso de error, usar español como predeterminado
                LocalizationService.SetLanguage("es");
            }
        }

        // Método para cambiar a la app principal después del login
        public static void SetMainPageToShell()
        {
            Application.Current.MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            // Configurar tamaño de ventana
            window.Width = 500;
            window.Height = 1000;

            // tamaño mínimo
            window.MinimumWidth = 350;
            window.MinimumHeight = 600;

            // Tamaño máximo
            // window.MaximumWidth = 500;
            // window.MaximumHeight = 900;

            return window;
        }
    }
}