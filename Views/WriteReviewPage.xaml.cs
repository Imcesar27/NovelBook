using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class WriteReviewPage : ContentPage
{
    // Servicios
    private readonly ReviewService _reviewService;
    private readonly DatabaseService _databaseService;

    // Datos
    private readonly int _novelId;
    private readonly Review _existingReview; // Si está editando
    private int _selectedRating = 0;

    // Referencias a los botones de estrellas
    private List<Button> _starButtons;

    /// <summary>
    /// Constructor para crear o editar una reseña
    /// </summary>
    /// <param name="novelId">ID de la novela</param>
    /// <param name="novelTitle">Título de la novela</param>
    /// <param name="existingReview">Reseña existente si está editando</param>
    public WriteReviewPage(int novelId, string novelTitle, Review existingReview = null)
    {
        InitializeComponent();

        _novelId = novelId;
        _existingReview = existingReview;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _reviewService = new ReviewService(_databaseService);

        // Configurar UI
        NovelTitleLabel.Text = novelTitle;
        Title = existingReview != null ? "Editar Reseña" : "Escribir Reseña";
        SaveButton.Text = existingReview != null ? "Actualizar reseña" : "Publicar reseña";

        // Lista de botones de estrellas para facilitar el manejo
        _starButtons = new List<Button> { Star1, Star2, Star3, Star4, Star5 };

        // Si está editando, cargar datos existentes
        if (_existingReview != null)
        {
            LoadExistingReview();
        }

        // Configurar el editor de comentarios
        CommentEditor.TextChanged += OnCommentTextChanged;
    }

    /// <summary>
    /// Carga los datos de una reseña existente
    /// </summary>
    private void LoadExistingReview()
    {
        // Establecer la calificación
        _selectedRating = _existingReview.Rating;
        UpdateStarDisplay();

        // Establecer el comentario
        CommentEditor.Text = _existingReview.Comment ?? "";
        UpdateCharCount();
    }

    /// <summary>
    /// Maneja el clic en una estrella
    /// </summary>
    private void OnStarClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string ratingStr)
        {
            if (int.TryParse(ratingStr, out int rating))
            {
                _selectedRating = rating;
                UpdateStarDisplay();
            }
        }
    }

    /// <summary>
    /// Actualiza la visualización de las estrellas
    /// </summary>
    private void UpdateStarDisplay()
    {
        // Actualizar las estrellas
        for (int i = 0; i < _starButtons.Count; i++)
        {
            _starButtons[i].Text = i < _selectedRating ? "★" : "☆";
        }

        // Actualizar descripción
        RatingDescriptionLabel.Text = _selectedRating switch
        {
            1 => "Muy mala",
            2 => "Mala",
            3 => "Regular",
            4 => "Buena",
            5 => "Excelente",
            _ => "Toca las estrellas para calificar"
        };

        // Cambiar color del texto según si ha calificado
        RatingDescriptionLabel.TextColor = _selectedRating > 0 ?
            Colors.White : Color.FromArgb("#808080");

        // Habilitar botón de guardar si ha seleccionado calificación
        SaveButton.IsEnabled = _selectedRating > 0;
    }

    /// <summary>
    /// Maneja cambios en el texto del comentario
    /// </summary>
    private void OnCommentTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCharCount();
    }

    /// <summary>
    /// Actualiza el contador de caracteres
    /// </summary>
    private void UpdateCharCount()
    {
        int charCount = CommentEditor.Text?.Length ?? 0;
        CharCountLabel.Text = $"{charCount} / 1000 caracteres";

        // Cambiar color si se acerca al límite
        if (charCount > 900)
        {
            CharCountLabel.TextColor = Color.FromArgb("#FF5252");
        }
        else
        {
            CharCountLabel.TextColor = Color.FromArgb("#808080");
        }
    }

    /// <summary>
    /// Maneja el clic en cancelar
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Cancelar",
            "¿Estás seguro de que quieres cancelar? Se perderán los cambios no guardados.",
            "Sí", "No");

        if (confirm)
        {
            await Navigation.PopAsync();
        }
    }

    /// <summary>
    /// Maneja el clic en guardar
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validar que haya calificación
        if (_selectedRating == 0)
        {
            await DisplayAlert("Error", "Debes seleccionar una calificación", "OK");
            return;
        }

        // Deshabilitar botón mientras guarda
        SaveButton.IsEnabled = false;
        SaveButton.Text = "Guardando...";

        try
        {
            // Obtener el comentario (puede ser vacío)
            string comment = CommentEditor.Text?.Trim() ?? "";

            // Guardar la reseña
            bool success = await _reviewService.SaveReviewAsync(
                AuthService.CurrentUser.Id,
                _novelId,
                _selectedRating,
                comment
            );

            if (success)
            {
                await DisplayAlert("Éxito",
                    _existingReview != null ? "Tu reseña ha sido actualizada" : "Tu reseña ha sido publicada",
                    "OK");

                // Volver a la página anterior
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo guardar la reseña", "OK");

                // Rehabilitar botón
                SaveButton.IsEnabled = true;
                SaveButton.Text = _existingReview != null ? "Actualizar reseña" : "Publicar reseña";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al guardar: " + ex.Message, "OK");

            // Rehabilitar botón
            SaveButton.IsEnabled = true;
            SaveButton.Text = _existingReview != null ? "Actualizar reseña" : "Publicar reseña";
        }
    }
}