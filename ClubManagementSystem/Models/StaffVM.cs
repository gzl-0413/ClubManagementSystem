using System.ComponentModel.DataAnnotations;

namespace ClubManagementSystem.Models
{
    public class StaffVM
    {
        // ViewModel for Adding Staff

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

            [Required(ErrorMessage = "Type is required.")]
            [MaxLength(20, ErrorMessage = "Type cannot exceed 20 characters.")]
            public string Type { get; set; } // FullTimer or Part-Timer

            public string? WorkTime { get; set; } // Optional: Work schedule for part-timers
        }

        // ViewModel for Editing Staff
        public class Edit
        {
            [Required(ErrorMessage = "Staff ID is required.")]
            public string Id { get; set; }

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

            [Required(ErrorMessage = "Type is required.")]
            [MaxLength(20, ErrorMessage = "Type cannot exceed 20 characters.")]
            public string Type { get; set; } // FullTimer or Part-Timer

            public string? WorkTime { get; set; } // Optional: Work schedule for part-timers
        }


        // ViewModel for Deleting Staff
        public class Delete
        {
            public string Id { get; set; }


            public string Name { get; set; }


            public string Email { get; set; }

            public string? PhotoURL { get; set; }

            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
            public string? Password { get; set; }

            public string Type { get; set; } // FullTimer or Part-Timer
            public string? WorkTime { get; set; } // Optional: Work schedule for part-timers
        }

        // ViewModel for Attendance
        public class WorkTimeViewModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public List<string> WorkTime { get; set; } // Holds the selected work times
        }
    }
}
