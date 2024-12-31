using System.ComponentModel.DataAnnotations;

namespace ClubManagementSystem.Models
{
    public class CoachVM
    {
        [Key]
        public string Id { get; set; } // Updated to string type for Coach ID

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Specialty is required.")]
        [MaxLength(200, ErrorMessage = "Specialty cannot exceed 200 characters.")]
        public string Specialty { get; set; }

        [MaxLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }

        [MaxLength(255, ErrorMessage = "Photo URL cannot exceed 255 characters.")]
        public string? Photo { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public bool IsActivated { get; set; }

        public string? CreatedBy { get; set; }

        public string? ModifiedBy { get; set; }
    }

    // ViewModel for creating or updating coach
    public class CoachCreateVM
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [MaxLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }

        // Remove Photo URL max length validation, since it is no longer necessary
        [Required(ErrorMessage = "Profile photo is required.")]
        public IFormFile Photo { get; set; }
    }

    public class UpdateCoachProfileVM
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [RegularExpression(@"^(\+60|0)[1-9]\d-\d{7,8}$", ErrorMessage = "Phone number must be in the format 012-3456789 or 03-12345678.")]
        public string? PhoneNumber { get; set; }

        [MaxLength(255, ErrorMessage = "Photo URL cannot exceed 255 characters.")]
        public string? Photo { get; set; }
    }

    public class DeleteCoachVM
    {
        [Required(ErrorMessage = "Coach ID is required.")]
        public string Id { get; set; }
    }


}
