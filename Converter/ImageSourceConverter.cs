using System.Globalization;
using NovelBook.Services;

namespace NovelBook.Converters;

public class ImageSourceConverter : IValueConverter
{
    private static NovelService _novelService;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string imageUrl && !string.IsNullOrEmpty(imageUrl))
        {
            // Si es una URL de base de datos
            if (imageUrl.StartsWith("db://covers/"))
            {
                var novelIdStr = imageUrl.Replace("db://covers/", "");
                if (int.TryParse(novelIdStr, out int novelId))
                {
                    // Cargar imagen de la base de datos de forma asíncrona, osea cargar la imagen sin detener procesos
                    Task.Run(async () =>
                    {
                        if (_novelService == null)
                        {
                            _novelService = new NovelService(new DatabaseService());
                        }

                        var (data, type) = await _novelService.GetNovelCoverAsync(novelId);
                        if (data != null)
                        {
                            return ImageSource.FromStream(() => new MemoryStream(data));
                        }
                        return null;
                    });
                }
            }
            // Si es una URL normal
            else
            {
                return imageUrl;
            }
        }

        // Imagen por defecto
        return "novel_placeholder.jpg";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}