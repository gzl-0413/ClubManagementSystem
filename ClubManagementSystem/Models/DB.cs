using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementSystem.Models;
#nullable disable warnings
public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options) { }

    // DbSet
    public DbSet<Facility> Facility { get; set; }
    public DbSet<FacilityCategories> FacilityCategories { get; set; }
    public DbSet<FacBooking> FacBooking { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<EquipmentRental> EquipmentRental { get; set; }
   
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<SuperAdmin> SuperAdmin { get; set; }

    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Coach> Coaches { get; set; }

    public DbSet<Announcement> Announcements { get; set; }
}

// Entity Classes
#nullable disable warnings

// --------------------------------------------------------Facility Module--------------------------------------------------------- //
public class Facility
{
    // Column
    [Key]
    public int Id { get; set; }
    [Required(ErrorMessage = "Facility Name is required.")]
    [MaxLength(100, ErrorMessage = "Facility Name cannot exceed 100 characters.")]
    public string Name { get; set; }
    public string? Image { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // FK
    public int FacilityCategoriesId { get; set; }

    // Navigation
    public FacilityCategories FacilityCategories { get; set; }
    public List<FacBooking> FacBookings { get; set; } = [];
}

public class FacilityCategories
{
    // Column
    [Key]
    public int Id { get; set; }
    [Required(ErrorMessage = "Category Name is required.")]
    [MaxLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
    public string FacCategoryName { get; set; }
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int FacilityCount { get; set; }

    // Navigation
    public List<Facility> Facility { get; set; } = []; 
}

public class FacBooking
{
    // Column
    [Key]
    public int Id { get; set; }
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
    public string PhoneNumber { get; set; }
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }
    [Required]
    public DateOnly BookingDate { get; set; }
    [Required]
    public TimeOnly StartTime { get; set; }
    [Required]
    public TimeOnly EndTime { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Fee paid must be a positive value.")]
    public decimal? FeePaid { get; set; }
    public bool isPaid { get; set; }
    public string? PayBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool isDeleted { get; set; }

    // Foreign Key: Link to Facility
    [Required]
    public int FacilityId { get; set; }

    // Navigation Property
    public Facility Facility { get; set; }
}

// -------------------------------------------------------Facility Module End--------------------------------------------------------- //

// ------------------------------------------------------Equipment Rental Module------------------------------------------------------ //
public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Stock { get; set; }
    public decimal DepositAmount { get; set; }
    public bool isDeleted { get; set; }
}


public class EquipmentRental
{
    public int Id { get; set; }
    public int FacBookingId { get; set; } 
    public int EquipmentId { get; set; } 
    public int Quantity { get; set; }
    public decimal DepositPaid { get; set; }
    public DateTime RentedAt { get; set; } 
    public DateTime? ReturnedAt { get; set; } 
    public FacBooking FacBooking { get; set; }
    public Equipment Equipment { get; set; }
}
// ---------------------------------------------------Equipment Rental Module End------------------------------------------------------ //

// -------------------------------------------------------User Module------------------------------------------------------------------ //
public abstract class User
{
    [Key, Required, MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [Required, MaxLength(255)]
    public string Hash { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    public string Role => GetType().Name;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }

    // New fields for account activation
    public bool IsActivated { get; set; } = false; // Default to false
    public string ActivationCode { get; set; }
}



public class Admin : User
{
    // Additional properties for Admin, if any
}

public class SuperAdmin : User
{ 

}


public class Staff : User
{
    [Required]
    [MaxLength(255)]
    public string Password { get; set; }

    [MaxLength(255)]
    public string PhotoURL { get; set; }
}


public class Member : User
{
    [MaxLength(255)]
    public string PhotoURL { get; set; }
}

// ------------------------------------------------------User Module End---------------------------------------------------------------- //

// -----------------------------------------------------Announcement Module------------------------------------------------------------- //

public class Announcement
{
    [Key, Required, MaxLength(100)]
    public string Id { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; }

    [Required, MaxLength(1000)]
    public string Content { get; set; }

    [Required, MaxLength(300)]
    public string Photo { get; set; } = string.Empty;

    [Required]
    public DateTime DateTime { get; set; }

    [Required]
    public string Status { get; set; } 

    public string LikeUsers { get; set; } = string.Empty;

    //FK
    public string AdminEmail { get; set; }
}

// -----------------------------------------------------Announcement Module End---------------------------------------------------------- //

public class Coach
{
    [Key]
    [Required]
    public string CoachID { get; set; } // Unique identifier for each coach in the format CH1234

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(255, ErrorMessage = "Password cannot exceed 255 characters.")]
    public string Password { get; set; }

    [MaxLength(255, ErrorMessage = "Photo URL cannot exceed 255 characters.")]
    public string? Photo { get; set; }

    [MaxLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ModifiedAt { get; set; }
}



