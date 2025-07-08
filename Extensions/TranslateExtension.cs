using NovelBook.Services;

namespace NovelBook.Extensions
{
    /// <summary>
    /// Extensión de marcado XAML para facilitar las traducciones
    /// Uso: Text="{extensions:Translate Welcome}"
    /// </summary>
    [ContentProperty(nameof(Key))]
    public class TranslateExtension : IMarkupExtension<string>
    {
        public string Key { get; set; }

        public string ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return string.Empty;

            return LocalizationService.GetString(Key);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }
    }
}