using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class ReviewsPage : ContentPage
{
    // Servicios necesarios
    private readonly ReviewService _reviewService;
    private readonly DatabaseService _databaseService;

    // Datos de la novela
    private readonly int _novelId;
    private readonly string _novelTitle;

    // Reseña del usuario actual (si existe)
    private Review _userReview;

    /// <summary>
    /// Constructor que recibe el ID y título de la novela
    /// </summary>
    /// <param name="novelId">ID de la novela</param>
    /// <param name="novelTitle">Título de la novela para mostrar</param>
    public ReviewsPage(int novelId, string novelTitle)
    {
        InitializeComponent();

        _novelId = novelId;
        _novelTitle = novelTitle;
        Title = $"Reseñas - {novelTitle}";

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _reviewService = new ReviewService(_databaseService);

        // Cargar datos
        LoadReviews();
    }

    /// <summary>
    /// Se ejecuta cada vez que la página aparece
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Recargar por si el usuario escribió una reseña
        LoadReviews();
    }

    /// <summary>
    /// Carga todas las reseñas y estadísticas
    /// </summary>
    private async void LoadReviews()
    {
        try
        {
            // Obtener estadísticas
            var stats = await _reviewService.GetReviewStatsAsync(_novelId);
            UpdateStatistics(stats);

            // Obtener todas las reseñas
            var reviews = await _reviewService.GetNovelReviewsAsync(_novelId);

            // Si hay usuario logueado, buscar su reseña
            if (AuthService.CurrentUser != null)
            {
                _userReview = await _reviewService.GetUserReviewAsync(
                    AuthService.CurrentUser.Id, _novelId);

                // Actualizar texto del botón
                WriteReviewButton.Text = _userReview != null ?
                    "Editar mi reseña" : "Escribir reseña";
            }
            else
            {
                WriteReviewButton.IsVisible = false;
            }

            // Mostrar las reseñas
            DisplayReviews(reviews);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar reseñas: " + ex.Message, "OK");
        }
    }

    /// <summary>
    /// Actualiza las estadísticas en la UI
    /// </summary>
    private void UpdateStatistics(ReviewStats stats)
    {
        // Actualizar promedio
        AverageRatingLabel.Text = stats.AverageRating.ToString("F1");

        // Actualizar estrellas visuales
        int fullStars = (int)Math.Floor(stats.AverageRating);
        bool hasHalfStar = (stats.AverageRating - fullStars) >= 0.5m;

        string stars = new string('★', fullStars);
        if (hasHalfStar && fullStars < 5) stars += "☆";
        stars += new string('☆', 5 - stars.Length);
        StarsLabel.Text = stars;

        // Total de reseñas
        TotalReviewsLabel.Text = $"{stats.TotalReviews} reseña{(stats.TotalReviews != 1 ? "s" : "")}";

        // Actualizar barras de progreso
        Stars5Bar.Progress = stats.GetPercentage(5) / 100;
        Stars5Count.Text = stats.RatingDistribution[5].ToString();

        Stars4Bar.Progress = stats.GetPercentage(4) / 100;
        Stars4Count.Text = stats.RatingDistribution[4].ToString();

        Stars3Bar.Progress = stats.GetPercentage(3) / 100;
        Stars3Count.Text = stats.RatingDistribution[3].ToString();

        Stars2Bar.Progress = stats.GetPercentage(2) / 100;
        Stars2Count.Text = stats.RatingDistribution[2].ToString();

        Stars1Bar.Progress = stats.GetPercentage(1) / 100;
        Stars1Count.Text = stats.RatingDistribution[1].ToString();
    }

    /// <summary>
    /// Muestra las reseñas en la interfaz
    /// </summary>
    private void DisplayReviews(List<Review> reviews)
    {
        // Limpiar contenedor
        ReviewsContainer.Children.Clear();

        // Si no hay reseñas, mostrar mensaje
        if (reviews.Count == 0)
        {
            NoReviewsLabel.IsVisible = true;
            return;
        }

        NoReviewsLabel.IsVisible = false;

        // Crear una tarjeta para cada reseña
        foreach (var review in reviews)
        {
            var reviewCard = CreateReviewCard(review);
            ReviewsContainer.Children.Add(reviewCard);
        }
    }

    /// <summary>
    /// Crea una tarjeta visual para una reseña
    /// </summary>
    private Frame CreateReviewCard(Review review)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 15,
            HasShadow = false
        };

        var mainStack = new StackLayout { Spacing = 10 };

        // Encabezado con nombre de usuario y fecha
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        // Nombre de usuario y estrellas
        var userStack = new StackLayout { Spacing = 5 };

        var nameLabel = new Label
        {
            Text = review.UserName,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16
        };
        userStack.Children.Add(nameLabel);

        var starsLabel = new Label
        {
            Text = review.GetStars(),
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = 18
        };
        userStack.Children.Add(starsLabel);

        headerGrid.Children.Add(userStack);
        Grid.SetColumn(userStack, 0);

        // Fecha
        var dateLabel = new Label
        {
            Text = review.CreatedAt.ToString("dd/MM/yyyy"),
            TextColor = Color.FromArgb("#808080"),
            FontSize = 12,
            VerticalOptions = LayoutOptions.Start
        };
        headerGrid.Children.Add(dateLabel);
        Grid.SetColumn(dateLabel, 1);

        mainStack.Children.Add(headerGrid);

        // Comentario (si existe)
        if (!string.IsNullOrWhiteSpace(review.Comment))
        {
            var commentLabel = new Label
            {
                Text = review.Comment,
                TextColor = Color.FromArgb("#CCCCCC"),
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            };
            mainStack.Children.Add(commentLabel);
        }

        // Si es la reseña del usuario actual, agregar botón de eliminar
        if (AuthService.CurrentUser != null && review.UserId == AuthService.CurrentUser.Id)
        {
            var deleteButton = new Button
            {
                Text = "Eliminar mi reseña",
                BackgroundColor = Color.FromArgb("#B71C1C"),
                TextColor = Colors.White,
                CornerRadius = 20,
                HeightRequest = 35,
                FontSize = 12,
                HorizontalOptions = LayoutOptions.End
            };

            deleteButton.Clicked += async (s, e) => await DeleteReview(review.Id);
            mainStack.Children.Add(deleteButton);
        }

        frame.Content = mainStack;
        return frame;
    }

    /// <summary>
    /// Maneja el clic en el botón de escribir/editar reseña
    /// </summary>
    private async void OnWriteReviewClicked(object sender, EventArgs e)
    {
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Iniciar sesión",
                "Debes iniciar sesión para escribir una reseña", "OK");
            return;
        }

        // Navegar a la página de escribir reseña
        await Navigation.PushAsync(new WriteReviewPage(_novelId, _novelTitle, _userReview));
    }

    /// <summary>
    /// Elimina la reseña del usuario
    /// </summary>
    private async Task DeleteReview(int reviewId)
    {
        bool confirm = await DisplayAlert("Confirmar",
            "¿Estás seguro de que quieres eliminar tu reseña?", "Sí", "No");

        if (!confirm) return;

        try
        {
            var success = await _reviewService.DeleteReviewAsync(
                AuthService.CurrentUser.Id, reviewId);

            if (success)
            {
                await DisplayAlert("Éxito", "Tu reseña ha sido eliminada", "OK");
                _userReview = null;
                WriteReviewButton.Text = "Escribir reseña";
                LoadReviews(); // Recargar
            }
            else
            {
                await DisplayAlert("Error", "No se pudo eliminar la reseña", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al eliminar: " + ex.Message, "OK");
        }
    }
}