using NovelBook.Views;
using System.Globalization;

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
            var savedLanguage = Preferences.Get("AppLanguage", "es");

            var culture = new CultureInfo(savedLanguage);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
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