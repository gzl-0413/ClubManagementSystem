#nullable disable warnings

namespace ClubManagementSystem.Models;

public class EventViewModel
{
    // Event Details
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Image { get; set; }
    public string Venue { get; set; }
    public string Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Related Data
    public string EventCategory { get; set; } // Event Category Name
    public List<EventPricingViewModel> EventPricings { get; set; } = new();
}

public class EventPricingViewModel
{
    public string Tier { get; set; }
    public decimal Price { get; set; }
}
