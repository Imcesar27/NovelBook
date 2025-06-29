using NovelBook.Services;
using System.Collections.ObjectModel;

namespace NovelBook.Views;

/// <summary>
/// Página para gestionar los géneros literarios
/// </summary>
public partial class ManageGenresPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private ObservableCollection<GenreDisplayItem> _genres;

    public ManageGenresPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _genres = new ObservableCollection<GenreDisplayItem>();

        GenresCollection.ItemsSource = _genres;
    }

    /// <summary>
    /// Se ejecuta cuando aparece la página
    /// </summary>
    /// <summary>
    /// Se ejecuta cuando aparece la página
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Deshabilitar navegación de Shell
        Shell.SetNavBarIsVisible(this, false);

        // Verificar permisos de administrador
        if (AuthService.CurrentUser == null || !AuthService.CurrentUser.IsAdmin)
        {
            await DisplayAlert("Acceso Denegado", "No tienes permisos para acceder a esta sección", "OK");
            await Navigation.PopAsync();
            return;
        }

        await LoadGenresAsync();
    }

    /// <summary>
    /// Carga todos los géneros de la base de datos
    /// </summary>
    private async Task LoadGenresAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            EmptyState.IsVisible = false;

            // Obtener géneros con contador de novelas
            var genres = await GetGenresWithCountAsync();

            _genres.Clear();
            foreach (var genre in genres)
            {
                _genres.Add(genre);
            }

            // Actualizar estadísticas
            StatsLabel.Text = $"Total de géneros: {genres.Count}";

            // Mostrar estado vacío si no hay géneros
            if (genres.Count == 0)
            {
                EmptyState.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar géneros: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Obtiene los géneros con el conteo de novelas asociadas
    /// </summary>
    private async Task<List<GenreDisplayItem>> GetGenresWithCountAsync()
    {
        var genres = new List<GenreDisplayItem>();

        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT g.id, g.name, COUNT(ng.novel_id) as novel_count
                         FROM genres g
                         LEFT JOIN novel_genres ng ON g.id = ng.genre_id
                         GROUP BY g.id, g.name
                         ORDER BY g.name";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                genres.Add(new GenreDisplayItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    NovelCount = reader.GetInt32(2),
                    NovelCountText = $"{reader.GetInt32(2)} novelas"
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo géneros: {ex.Message}");
        }

        return genres;
    }

    /// <summary>
    /// Agrega un nuevo género
    /// </summary>
    private async void OnAddGenreClicked(object sender, EventArgs e)
    {
        var genreName = NewGenreEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(genreName))
        {
            await DisplayAlert("Error", "Ingresa un nombre para el género", "OK");
            return;
        }

        // Validar longitud
        if (genreName.Length > 50)
        {
            await DisplayAlert("Error", "El nombre del género no puede exceder 50 caracteres", "OK");
            return;
        }

        try
        {
            // Verificar si ya existe
            if (await GenreExistsAsync(genreName))
            {
                await DisplayAlert("Error", "Ya existe un género con ese nombre", "OK");
                return;
            }

            // Agregar el género
            bool success = await AddGenreAsync(genreName);

            if (success)
            {
                NewGenreEntry.Text = "";
                await LoadGenresAsync();

                // Hacer scroll al nuevo género
                if (_genres.Count > 0)
                {
                    var newGenre = _genres.FirstOrDefault(g => g.Name == genreName);
                    if (newGenre != null)
                    {
                        GenresCollection.ScrollTo(newGenre, ScrollToPosition.End);
                    }
                }
            }
            else
            {
                await DisplayAlert("Error", "No se pudo agregar el género", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Verifica si un género ya existe
    /// </summary>
    private async Task<bool> GenreExistsAsync(string name)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM genres WHERE LOWER(name) = LOWER(@name)";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@name", name);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Agrega un género a la base de datos
    /// </summary>
    private async Task<bool> AddGenreAsync(string name)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = "INSERT INTO genres (name) VALUES (@name)";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@name", name);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error agregando género: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina un género
    /// </summary>
    private async void OnDeleteGenreClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int genreId)
        {
            var genre = _genres.FirstOrDefault(g => g.Id == genreId);
            if (genre == null) return;

            // Verificar si tiene novelas asociadas
            if (genre.NovelCount > 0)
            {
                bool confirm = await DisplayAlert(
                    "Advertencia",
                    $"El género '{genre.Name}' está asociado a {genre.NovelCount} novela(s).\n\n" +
                    "Si lo eliminas, las novelas perderán este género.\n\n" +
                    "¿Deseas continuar?",
                    "Eliminar",
                    "Cancelar");

                if (!confirm) return;
            }
            else
            {
                bool confirm = await DisplayAlert(
                    "Confirmar",
                    $"¿Eliminar el género '{genre.Name}'?",
                    "Eliminar",
                    "Cancelar");

                if (!confirm) return;
            }

            try
            {
                button.IsEnabled = false;

                bool success = await DeleteGenreAsync(genreId);

                if (success)
                {
                    await LoadGenresAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar el género", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar: {ex.Message}", "OK");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// Elimina un género de la base de datos
    /// </summary>
    private async Task<bool> DeleteGenreAsync(int genreId)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // Primero eliminar las relaciones con novelas
            var deleteRelationsQuery = "DELETE FROM novel_genres WHERE genre_id = @id";
            using var deleteRelationsCommand = new Microsoft.Data.SqlClient.SqlCommand(deleteRelationsQuery, connection);
            deleteRelationsCommand.Parameters.AddWithValue("@id", genreId);
            await deleteRelationsCommand.ExecuteNonQueryAsync();

            // Luego eliminar el género
            var deleteGenreQuery = "DELETE FROM genres WHERE id = @id";
            using var deleteGenreCommand = new Microsoft.Data.SqlClient.SqlCommand(deleteGenreQuery, connection);
            deleteGenreCommand.Parameters.AddWithValue("@id", genreId);

            var rowsAffected = await deleteGenreCommand.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando género: {ex.Message}");
            return false;
        }
    }

    // En MorePage.xaml.cs, actualizar el método OnOptionTapped para evitar duplicados:

    private async void OnOptionTapped(object sender, EventArgs e)
    {
        if (sender is Grid grid && grid.GestureRecognizers[0] is TapGestureRecognizer tap)
        {
            string option = tap.CommandParameter as string;

            // Prevenir múltiples taps
            grid.IsEnabled = false;

            try
            {
                switch (option)
                {
                    case "ManageNovels":
                        // Verificar si ya estamos en esa página
                        var currentPage = Navigation.NavigationStack.LastOrDefault();
                        if (currentPage?.GetType() != typeof(ManageNovelsPage))
                        {
                            await Navigation.PushAsync(new ManageNovelsPage());
                        }
                        break;

                        // ... resto de los casos ...
                }
            }
            finally
            {
                grid.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// Maneja el botón de volver personalizado
    /// </summary>
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

/// <summary>
/// Clase para mostrar géneros con información adicional
/// </summary>
public class GenreDisplayItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int NovelCount { get; set; }
    public string NovelCountText { get; set; }
}