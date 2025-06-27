using NovelBook.Views;

namespace NovelBook
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                BarTextColor = Colors.White
            };
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