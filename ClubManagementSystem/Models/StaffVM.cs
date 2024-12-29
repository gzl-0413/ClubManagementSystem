using System.ComponentModel.DataAnnotations;

namespace ClubManagementSystem.Models
{
    public class StaffVM
    {
        // Create ViewModel for Adding Staff
        public class Create
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
            public string Password { get; set; }


            [Required(ErrorMessage = "Name is required.")]
            [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            public string Name { get; set; }

            [MaxLength(255, ErrorMessage = "Photo URL cannot exceed 255 characters.")]
            public string? PhotoURL { get; set; }

            // Optional: Allow photo upload in the form
            public IFormFile? Photo { get; set; }

        }

        // Edit ViewModel for Editing Staff
        public class Edit
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
            public string Email { get; set; }

            public string Password { get; set; }


            [Required(ErrorMessage = "Name is required.")]
            [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            public string Name { get; set; }

            [MaxLength(255, ErrorMessage = "Photo URL cannot exceed 255 characters.")]
            public string? PhotoURL { get; set; }

            // Optional: Allow photo upload in the form
            public IFormFile? Photo { get; set; }


            public string Id { get; set; }
        }
        public class Delete
        {
            public string Id { get; set; }

            [Required(ErrorMessage = "Name is required.")]
            [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
            public string Email { get; set; }
            public string? PhotoURL { get; set; }
        }
    }
}
