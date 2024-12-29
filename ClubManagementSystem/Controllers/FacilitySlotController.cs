using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementSystem.Controllers;

//[Authorize(Roles = "Admin")]
public class FacilitySlotController : Controller
{
    private readonly DB db;

    public FacilitySlotController(DB context)
    {
        db = context;
    }

    [HttpGet]
    public IActionResult CreateSlots()
    {
        ViewBag.Facilities = db.Facility
            .Where(f => f.IsActive)
            .ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateSlots(int facilityId, int months, int capacity)
    {
        var facility = await db.Facility.FindAsync(facilityId);

        if (facility == null || !facility.IsActive)
        {
            ModelState.AddModelError("", "Facility not found or inactive.");
            ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
            return View();
        }

        // Find the latest slot date for the facility
        var latestSlot = db.FacilityBookingCapacity
            .Where(s => s.FacilityId == facilityId)
            .OrderByDescending(s => s.BookingDate)
            .FirstOrDefault();

        var startDate = latestSlot != null
            ? latestSlot.BookingDate.AddDays(1)  // Start from the next day
            : DateOnly.FromDateTime(DateTime.Today);  // Start from today if no slots exist

        var endDate = startDate.AddMonths(months);

        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            for (int hour = 8; hour <= 22; hour++)
            {
                var existingSlot = db.FacilityBookingCapacity.Any(s =>
                    s.FacilityId == facilityId &&
                    s.BookingDate == day &&
                    s.StartTime == TimeOnly.FromTimeSpan(TimeSpan.FromHours(hour))
                );

                // Only add if the slot doesn't already exist
                if (!existingSlot)
                {
                    var slot = new FacilityBookingCapacity
                    {
                        FacilityId = facilityId,
                        BookingDate = day,
                        StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(hour)),
                        EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(hour + 1)),
                        RemainingCapacity = capacity,
                        isClass = false
                    };
                    db.FacilityBookingCapacity.Add(slot);
                }
            }
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Slots created successfully!";
        return RedirectToAction("ViewSlots", "FacilityBooking");
    }

    public IActionResult CreateClassSlots()
    {
        ViewBag.Facilities = db.Facility
                    .Where(f => f.IsActive)
                    .ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateClassSlots(int facilityId, int months, int capacity, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var facility = await db.Facility.FindAsync(facilityId);

        if (facility == null || !facility.IsActive)
        {
            ModelState.AddModelError("", "Facility not found or inactive.");
            ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
            return View();
        }

        var latestSlot = db.FacilityBookingCapacity
            .Where(s => s.FacilityId == facilityId)
            .OrderByDescending(s => s.BookingDate)
            .FirstOrDefault();

        var startDate = latestSlot != null
            ? latestSlot.BookingDate.AddDays(1)
            : DateOnly.FromDateTime(DateTime.Today);

        var endDate = startDate.AddMonths(months);

        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            if (day.DayOfWeek == dayOfWeek)
            {
                // Check for overlapping slots
                var overlappingSlot = db.FacilityBookingCapacity
                    .Where(s => s.FacilityId == facilityId &&
                                s.BookingDate == day &&
                                s.StartTime < TimeOnly.FromTimeSpan(endTime) &&
                                s.EndTime > TimeOnly.FromTimeSpan(startTime))
                    .FirstOrDefault();

                if (overlappingSlot != null)
                {
                    // Check if overlapping slot is already booked
                    var isBooked = db.FacBooking.Any(b =>
                        b.FacilityId == facilityId &&
                        b.BookingDate == day &&
                        b.StartTime == overlappingSlot.StartTime &&
                        b.EndTime == overlappingSlot.EndTime);

                    if (isBooked)
                    {
                        TempData["Error"] = $"Cannot create class on {day} from {startTime} to {endTime}. Slot is already booked.";
                        continue;
                    }
                    else
                    {
                        // Update existing slot to class type
                        overlappingSlot.RemainingCapacity = capacity;
                        db.FacilityBookingCapacity.Update(overlappingSlot);
                    }
                }
                else
                {
                    // Create new slot if no overlap exists
                    var slot = new FacilityBookingCapacity
                    {
                        FacilityId = facilityId,
                        BookingDate = day,
                        StartTime = TimeOnly.FromTimeSpan(startTime),
                        EndTime = TimeOnly.FromTimeSpan(endTime),
                        RemainingCapacity = capacity,
                        isClass = true
                    };
                    db.FacilityBookingCapacity.Add(slot);
                }
            }
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Class slots created successfully!";
        return RedirectToAction("ViewSlots", "FacilityBooking");
    }
}
