#nullable disable warnings

using System.ComponentModel.DataAnnotations;

namespace ClubManagementSystem.Models;

public class FacilityViewModel
{
    // Facility Details
    public int Id { get; set; }
    [Required(ErrorMessage = "Facility Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }
    public string? Image { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Capacity must be a positive number.")]
    public int? Capacity { get; set; }
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]

    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Related Data
    [Required(ErrorMessage = "Please select a facility category.")]
    public int FacilityCategoriesId { get; set; }
    //public string FacilityCategoryName { get; set; }
    //public FacilityCategories FacilityCategories { get; set; } // Facility Category Name
    public List<TimeSlotViewModel> FacilityTimeSlots { get; set; } = new();
}

public class TimeSlotViewModel
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; }
}

public class FacilityScheduleViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; }
    public DateOnly SelectedDate { get; set; }
    public List<DateOnly> AvailableDates { get; set; } = [];
    public List<TimeSlotViewModel> TimeSlots { get; set; } = [];
}

public class FacilityCategoryViewModel
{
    public int Id { get; set; }
    public string FacCategoryName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int FacilityCount { get; set; } 
}

