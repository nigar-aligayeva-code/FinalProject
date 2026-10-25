using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Reviews;

public class CreateReviewDto
{
    [Required(ErrorMessage = "Confirmation code is required.")]
    public string ConfirmationCode { get; set; } = string.Empty;

    // Rezervasiyanın sahibi olduğunu təsdiqləmək üçün
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Please write a comment.")]
    [StringLength(1000, MinimumLength = 10,
        ErrorMessage = "Comment must be between 10 and 1000 characters.")]
    public string Comment { get; set; } = string.Empty;

    // Otelə qiymət (istəyə bağlıdır)
    [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
    public int? HotelRating { get; set; }

    [StringLength(1000, ErrorMessage = "Comment must be between 10 and 1000 characters.")]
    public string? HotelComment { get; set; }
}
