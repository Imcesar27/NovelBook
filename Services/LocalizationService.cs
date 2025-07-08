using System.Globalization;

namespace NovelBook.Services
{
    /// <summary>
    /// Servicio para manejar la localización y traducciones de la aplicación
    /// </summary>
    public static class LocalizationService
    {
        // Diccionario de traducciones
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            // Español
            ["es"] = new Dictionary<string, string>
            {
                // LoginPage
                ["Welcome"] = "Bienvenido a NovelBook",
                ["WelcomeSubtitle"] = "Tu biblioteca de novelas ligeras",
                ["Email"] = "Correo electrónico",
                ["Password"] = "Contraseña",
                ["Login"] = "Entrar",
                ["Register"] = "Regístrate",
                ["Guest"] = "Invitado",
                ["ForgotPassword"] = "¿Olvidaste tu contraseña?",
                ["LoginWithSocial"] = "Iniciar sesión con:",
                ["NoAccount"] = "¿No tienes cuenta?",

                // LibraryPage
                ["MyLibrary"] = "Mi Biblioteca",
                ["Reading"] = "Leyendo",
                ["Completed"] = "Completadas",
                ["Favorites"] = "Favoritas",
                ["NoNovels"] = "No hay novelas en tu biblioteca",
                ["SearchLibrary"] = "Buscar en biblioteca...",

                // ExplorePage
                ["ExploreNovels"] = "Explorar Novelas",
                ["PopularGenres"] = "Géneros Populares",
                ["AllNovels"] = "Todas las Novelas",
                ["FilterByGenre"] = "Filtrar por género",
                ["FilterByStatus"] = "Filtrar por estado",
                ["InProgress"] = "En progreso",
                ["SearchNovels"] = "Buscar novelas...",

                // SettingsPage
                ["Settings"] = "Configuración",
                ["Appearance"] = "Apariencia",
                ["AppTheme"] = "Tema de la aplicación",
                ["System"] = "Sistema",
                ["Light"] = "Claro",
                ["Dark"] = "Oscuro",
                ["Language"] = "Idioma",
                ["Spanish"] = "Español",
                ["English"] = "English",
                ["Account"] = "Cuenta",
                ["AccountInfo"] = "Información de cuenta",
                ["Logout"] = "Cerrar sesión",
                ["BiometricAuth"] = "Autenticación biométrica",
                ["ChangePassword"] = "Cambiar contraseña",
                ["Reading"] = "Lectura",
                ["FontSize"] = "Tamaño de letra",
                ["NightMode"] = "Modo nocturno automático",
                ["KeepScreenOn"] = "Mantener pantalla encendida",
                ["Notifications"] = "Notificaciones",
                ["NewChapters"] = "Nuevos capítulos",
                ["NotifyNewChapters"] = "Notificar cuando haya nuevos capítulos",
                ["RecommendedNovels"] = "Novelas recomendadas",
                ["NotifyRecommended"] = "Recibir recomendaciones personalizadas",
                ["Downloads"] = "Descargas",
                ["WifiOnly"] = "Solo Wi-Fi",
                ["DownloadOnlyWifi"] = "Descargar solo con Wi-Fi",
                ["ChapterLimit"] = "Límite de capítulos",
                ["MaxChaptersPerNovel"] = "Máximo de capítulos por novela",
                ["Cache"] = "Caché",
                ["ClearCache"] = "Limpiar caché",
                ["CacheSize"] = "Tamaño del caché",

                // Common
                ["OK"] = "OK",
                ["Cancel"] = "Cancelar",
                ["Yes"] = "Sí",
                ["No"] = "No",
                ["Save"] = "Guardar",
                ["Delete"] = "Eliminar",
                ["Edit"] = "Editar",
                ["Close"] = "Cerrar",
                ["Loading"] = "Cargando...",
                ["Error"] = "Error",
                ["Success"] = "Éxito",
                ["Warning"] = "Advertencia",
                ["Info"] = "Información",
                ["GuestMode"] = "Modo invitado"
            },

            // English
            ["en"] = new Dictionary<string, string>
            {
                // LoginPage
                ["Welcome"] = "Welcome to NovelBook",
                ["WelcomeSubtitle"] = "Your light novel library",
                ["Email"] = "Email",
                ["Password"] = "Password",
                ["Login"] = "Login",
                ["Register"] = "Register",
                ["Guest"] = "Guest",
                ["ForgotPassword"] = "Forgot password?",
                ["LoginWithSocial"] = "Login with:",
                ["NoAccount"] = "Don't have an account?",

                // LibraryPage
                ["MyLibrary"] = "My Library",
                ["Reading"] = "Reading",
                ["Completed"] = "Completed",
                ["Favorites"] = "Favorites",
                ["NoNovels"] = "No novels in your library",
                ["SearchLibrary"] = "Search library...",

                // ExplorePage
                ["ExploreNovels"] = "Explore Novels",
                ["PopularGenres"] = "Popular Genres",
                ["AllNovels"] = "All Novels",
                ["FilterByGenre"] = "Filter by genre",
                ["FilterByStatus"] = "Filter by status",
                ["InProgress"] = "In progress",
                ["SearchNovels"] = "Search novels...",

                // SettingsPage
                ["Settings"] = "Settings",
                ["Appearance"] = "Appearance",
                ["AppTheme"] = "App theme",
                ["System"] = "System",
                ["Light"] = "Light",
                ["Dark"] = "Dark",
                ["Language"] = "Language",
                ["Spanish"] = "Español",
                ["English"] = "English",
                ["Account"] = "Account",
                ["AccountInfo"] = "Account information",
                ["Logout"] = "Logout",
                ["BiometricAuth"] = "Biometric authentication",
                ["ChangePassword"] = "Change password",
                ["Reading"] = "Reading",
                ["FontSize"] = "Font size",
                ["NightMode"] = "Automatic night mode",
                ["KeepScreenOn"] = "Keep screen on",
                ["Notifications"] = "Notifications",
                ["NewChapters"] = "New chapters",
                ["NotifyNewChapters"] = "Notify when new chapters available",
                ["RecommendedNovels"] = "Recommended novels",
                ["NotifyRecommended"] = "Receive personalized recommendations",
                ["Downloads"] = "Downloads",
                ["WifiOnly"] = "Wi-Fi only",
                ["DownloadOnlyWifi"] = "Download only on Wi-Fi",
                ["ChapterLimit"] = "Chapter limit",
                ["MaxChaptersPerNovel"] = "Maximum chapters per novel",
                ["Cache"] = "Cache",
                ["ClearCache"] = "Clear cache",
                ["CacheSize"] = "Cache size",

                // Common
                ["OK"] = "OK",
                ["Cancel"] = "Cancel",
                ["Yes"] = "Yes",
                ["No"] = "No",
                ["Save"] = "Save",
                ["Delete"] = "Delete",
                ["Edit"] = "Edit",
                ["Close"] = "Close",
                ["Loading"] = "Loading...",
                ["Error"] = "Error",
                ["Success"] = "Success",
                ["Warning"] = "Warning",
                ["Info"] = "Information",
                ["GuestMode"] = "Guest mode"
            }
        };

        /// <summary>
        /// Obtiene el idioma actual de la aplicación
        /// </summary>
        public static string CurrentLanguage => Preferences.Get("AppLanguage", "es");

        /// <summary>
        /// Obtiene una traducción para la clave especificada
        /// </summary>
        public static string GetString(string key)
        {
            var language = CurrentLanguage;

            if (Translations.ContainsKey(language) && Translations[language].ContainsKey(key))
            {
                return Translations[language][key];
            }

            // Si no se encuentra la traducción, devolver la clave como fallback
            return key;
        }

        /// <summary>
        /// Obtiene una traducción con formato
        /// </summary>
        public static string GetString(string key, params object[] args)
        {
            var translation = GetString(key);
            return string.Format(translation, args);
        }

        /// <summary>
        /// Cambia el idioma de la aplicación
        /// </summary>
        public static void SetLanguage(string languageCode)
        {
            Preferences.Set("AppLanguage", languageCode);

            var culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}