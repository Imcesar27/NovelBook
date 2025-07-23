using NovelBook.Services;
using System.ComponentModel;
using System.Globalization;

namespace NovelBook.Extensions
{
    /// <summary>
    /// Extensión de marcado XAML para facilitar las traducciones reactivas.
    /// Uso:
    /// - Text="{extensions:Translate Welcome}"
    /// - StringFormat="{extensions:Translate ViewReviews}"
    /// </summary>
    [ContentProperty(nameof(Key))]
    public class TranslateExtension : IMarkupExtension, INotifyPropertyChanged
    {
        public string Key { get; set; }

        public TranslateExtension()
        {
            // Escuchar el evento de cambio de idioma
            LocalizationService.LanguageChanged += OnLanguageChanged;
        }

        ~TranslateExtension()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            // Devuelve directamente el string traducido
            return LocalizationService.GetString(Key ?? string.Empty);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Key)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
