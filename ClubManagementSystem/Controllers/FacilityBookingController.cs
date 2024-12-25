using System.Security.Claims;
using ClubManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementSystem.Controllers
{
    public class FacilityBookingController : Controller
    {
        private readonly DB db;

        public FacilityBookingController(DB db)
        {
            this.db = db;
        }

        // GET: FacilityBooking/ViewSlots
        // View Availability (Chart & Table)
        public async Task<IActionResult> ViewSlots(DateTime? date, int? categoryId)
        {
            DateTime selectedDate = date ?? DateTime.Today;
            var facilities = await db.Facility
                .Include(f => f.FacilityCategories)
                .Include(f => f.FacBookings)
                .Where(f => !categoryId.HasValue || f.FacilityCategoriesId == categoryId)
                .ToListAsync();

            var bookings = await db.FacBooking
                .Where(b => b.BookingDate == DateOnly.FromDateTime(selectedDate))
                .ToListAsync();

            var viewModel = new FacilityAvailabilityViewModel
            {
                Facilities = facilities,
                Bookings = bookings,
                Categories = db.FacilityCategories.ToList(),
                SelectedDate = selectedDate
            };

            return View(viewModel);
        }

        // GET: FacilityBooking/CreateAdminBooking
        public IActionResult CreateAdminBooking()
        {
            ViewBag.Facilities = db.Facility
                                    .Where(f => f.IsActive)
                                    .Select(f => new { f.Id, f.Name })
                                    .ToList();

            var availableDates = db.FacilityBookingCapacity
                           .Where(fbc => fbc.RemainingCapacity > 0)
                           .Select(fbc => fbc.BookingDate) // Only select the BookingDate
                           .Distinct() // To ensure we only have unique dates
                           .ToList();

            // Pass the available dates to the view (as a list of strings in the format yyyy-MM-dd)
            ViewBag.AvailableDates = availableDates.Select(d => d.ToString("yyyy-MM-dd")).ToList();

            ViewBag.PaymentMethods = new[] { "None", "Cash", "Card", "E-Wallet" };
            return View();
        }

        // Autocomplete Email for Admin
        [HttpGet]
        public async Task<IActionResult> GetEmails(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var emails = await db.Users
                .Where(u => u.Email.Contains(term))
                .Select(u => new { u.Email, u.Name })
                .ToListAsync();

            return Json(emails);
        }

        [HttpGet]
        public async Task<IActionResult> CalculateFee(int facilityId, string bookingDate, string startTime, string endTime, string email)
        {
            var date = DateOnly.Parse(bookingDate);
            var start = TimeOnly.Parse(startTime);
            var end = TimeOnly.Parse(endTime);

            // Fetch the facility
            var facility = await db.Facility.FindAsync(facilityId);
            if (facility == null)
            {
                return Json(new { error = "Facility not found." });
            }

            // Calculate the duration in hours
            var duration = (end.ToTimeSpan() - start.ToTimeSpan()).TotalHours;

            if (duration <= 0)
            {
                return Json(new { error = "Invalid time range. End time must be after start time." });
            }

            // Check if the email exists and fetch the user role
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                switch (user.Role.ToLower())
                {
                    case "member":
                        // Normal fee calculation for members
                        var normalFee = (decimal)duration * facility.Price;
                        return Json(new { fee = normalFee });

                    case "premium":
                    case "coach":
                        // Fee is zero for premium and coach
                        return Json(new { fee = 0 });

                    case "staff":
                    case "admin":
                        // Staff and admin cannot book
                        return Json(new { error = "Booking is not allowed for staff or admin roles." });

                    default:
                        return Json(new { error = "Invalid role." });
                }
            }
            else
            {
                // Treat as guest if email is not found
                var guestFee = (decimal)duration * facility.Price;
                return Json(new { fee = guestFee });
            }
        }

        // POST: FacilityBooking/CreateAdminBooking
        [HttpPost]
        public async Task<IActionResult> CreateAdminBooking(FacilityBookingViewModel booking)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive).Select(f => new { f.Id, f.Name }).ToList();
                ViewBag.PaymentMethods = new[] { "None", "Cash", "Card", "E-Wallet" };
                return View(booking);
            }

            var facility = await db.Facility.FindAsync(booking.FacilityId);
            var slots = await db.FacilityBookingCapacity
        .Where(s => s.FacilityId == booking.FacilityId
            && s.BookingDate == booking.BookingDate
            && s.StartTime >= booking.StartTime
            && s.StartTime < booking.EndTime)  // Overlapping slots
        .ToListAsync();

            if (!slots.Any())
            {
                ModelState.AddModelError("Slot", "Selected time slots are not available for this facility.");
                return View(booking);
            }

            // Check if any slot is fully booked
            if (slots.Any(s => s.RemainingCapacity <= 0))
            {
                ModelState.AddModelError("Slot", "One or more slots are fully booked.");
                return View(booking);
            }

            if (booking.BookingDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
                return View(booking);
            }

            var now = DateTime.Now;
            var bookingDateTime = booking.BookingDate.ToDateTime(booking.StartTime);

            // Ensure the time slot aligns with exact hours
            if (booking.StartTime.Minute != 0 || booking.EndTime.Minute != 0)
            {
                ModelState.AddModelError("StartTime", "Bookings must be made in hourly intervals (e.g., 12:00 PM).");
                return View(booking);
            }

            // Ensure booking time is not in the past
            if (bookingDateTime <= now)
            {
                ModelState.AddModelError("StartTime", "Booking time has already passed.");
                return View(booking);
            }

            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                return View(booking);
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == booking.Email);
            if (user != null && user.Role == "Coach")
            {
                foreach (var slot in slots)
                {
                    slot.RemainingCapacity = 0; // Set capacity to 0 for coach users
                }
            } else
            {
                foreach (var slot in slots)
                {
                    slot.RemainingCapacity -= 1;
                }
            }

            // Final fee validation before saving to database
            var calculatedFee = booking.FeePaid;

            if (booking.FeePaid != calculatedFee)
            {
                ModelState.AddModelError("FeePaid", $"Incorrect fee. Expected {calculatedFee:C}.");
                return View(booking);
            }

            booking.PayBy = booking.PayBy ?? "None";
            
            db.FacBooking.Add(new FacBooking
            {
                FacilityId = booking.FacilityId,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                FeePaid = booking.FeePaid,
                PayBy = booking.PayBy,
                isPaid = true,
                Name = booking.Name,
                PhoneNumber = booking.PhoneNumber,
                Email = booking.Email,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name
            });

            await db.SaveChangesAsync();
            TempData["Info"] = "Facility booked successfully.";

            return RedirectToAction("ViewSlots");
        }
    }
}
