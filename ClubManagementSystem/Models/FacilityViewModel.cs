#nullable disable warnings

using System.ComponentModel.DataAnnotations;

namespace ClubManagementSystem.Models;

public class FacilityBookingHistoryViewModel
{
    public List<FacBooking> Bookings { get; set; }
    public DateTime SelectedDate { get; set; }
}

public class MemberBookingViewModel
{
    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(11, ErrorMessage = "Phone number cannot exceed 11 characters.")]
    public string PhoneNumber { get; set; }
    public int FacilityId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class BookingConfirmationViewModel
{
    public Facility Facility { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal? Fee { get; set; }
    public string Email { get; set; }
}

public class FacilityViewModel
{
    // Facility Details
    public int Id { get; set; }
    [Required(ErrorMessage = "Facility Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }
    public string? Image { get; set; }
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Related Data
    [Required(ErrorMessage = "Please select a facility category.")]
    public int FacilityCategoriesId { get; set; }
}

public class FacilityBookingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Facility selection is required.")]
    public int FacilityId { get; set; } // FK to Facility
    [Required(ErrorMessage = "Booking name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(11, ErrorMessage = "Phone number cannot exceed 11 characters.")]
    public string PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Booking date is required.")]
    public DateOnly BookingDate { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    [CustomValidation(typeof(TimeValidation), nameof(TimeValidation.ValidateTimeRange))]
    public TimeOnly EndTime { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Fee paid must be a positive value.")]
    public decimal FeePaid { get; set; }
    public string PayBy {  get; set; }
    public bool isPaid { get; set; }
}

public class FacilityBookingCapacityViewModel
{
    public int Id { get; set; }
    public int FacilityId { get; set; } 
    public string FacilityName { get; set; } 

    public DateOnly BookingDate { get; set; } 

    public TimeOnly StartTime { get; set; } 

    public TimeOnly EndTime { get; set; }

    public int RemainingCapacity { get; set; } 

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
public class FacilityAvailabilityViewModel
{
    public List<Facility> Facilities { get; set; }
    public List<FacBooking> Bookings { get; set; }
    public List<FacilityCategories> Categories { get; set; }
    public DateTime SelectedDate { get; set; }
}

public class EquipmentViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Stock { get; set; }
    public decimal DepositAmount { get; set; }
}

public class EquipmentRentalViewModel
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public int FacBookingId { get; set; }
    public int Quantity { get; set; }

    [Display(Name = "Deposit Paid")]
    [Range(0, double.MaxValue, ErrorMessage = "Deposit must be a positive value.")]
    public decimal DepositPaid { get; set; }

    [Display(Name = "Rented At")]
    [DataType(DataType.Date)]
    public DateTime RentedAt { get; set; }

    [Display(Name = "Returned At")]
    [DataType(DataType.Date)]
    public DateTime? ReturnedAt { get; set; }

    public bool IsReturned => ReturnedAt.HasValue; // Helper property to check if returned
}

public static class TimeValidation
{
    public static ValidationResult ValidateTimeRange(TimeOnly endTime, ValidationContext context)
    {
        var instance = context.ObjectInstance as FacilityBookingViewModel;
        if (instance != null && instance.StartTime >= endTime)
        {
            return new ValidationResult("End time must be later than start time.");
        }

        return ValidationResult.Success;
    }
}

