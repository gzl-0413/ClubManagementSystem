using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementSystem.Controllers
{
    public class FacilityBookingController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public FacilityBookingController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // Admin Booking Page
        public IActionResult CreateAdminBooking()
        {
            ViewBag.Facilities = db.Facility.Where(f => f.IsActive)
                                             .Select(f => new { f.Id, f.Name })
                                             .ToList();
            ViewBag.PaymentMethods = new[] { "Cash", "Card", "E-Wallet" };
            

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

        // Calculate the booking fee based on user role
        // Controller action for calculating the fee
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

            // Get the user based on the email to check their role
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return Json(new { error = "User not found." });
            }

            // Calculate the duration in hours
            var duration = (end.ToTimeSpan() - start.ToTimeSpan()).TotalHours;

            // Base fee calculation
            var calculatedFee = (decimal)duration * facility.Price;

            // Apply role-based fee adjustments
            if (user is Admin)
            {
                calculatedFee *= 0.8m; // 20% discount for Admin
            }
            else if (db.Users.Any(m => m.Email == user.Email && m.Role == "Premium"))
            {
                calculatedFee = 0; // Free for premium members and coaches
            }

            return Json(new { result = new { fee = calculatedFee } });
        }


        // CreateAdminBooking Post
        //[HttpPost]
        //public async Task<IActionResult> CreateAdminBooking(FacBooking booking)
        //{
        //    var facility = await db.Facility.FindAsync(booking.FacilityId);
        //    var slot = await db.FacilityBookingCapacity
        //        .FirstOrDefaultAsync(s => s.FacilityId == booking.FacilityId
        //            && s.BookingDate == booking.BookingDate
        //            && s.StartTime == booking.StartTime);

        //    // Restrict booking to future slots only
        //    var now = DateTime.Now;
        //    if (booking.BookingDate < DateOnly.FromDateTime(now) ||
        //        (booking.BookingDate == DateOnly.FromDateTime(now) && booking.StartTime <= TimeOnly.FromDateTime(now)))
        //    {
        //        ModelState.AddModelError("Slot", "Cannot book past or current slots.");
        //        return View("CreateAdminBooking", booking);
        //    }

        //    if (slot == null || slot.RemainingCapacity <= 0)
        //    {
        //        ModelState.AddModelError("Slot", "Slot is fully booked.");
        //        return View("CreateAdminBooking", booking);
        //    }

        //    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == booking.Email);

        //    if (existingUser != null)
        //    {
        //        booking.Name = existingUser.Name;

        //        // Calculate fee based on role
        //        var duration = (booking.EndTime - booking.StartTime).Hours;
        //        var calculatedFee = duration * facility.Price;

        //        if (existingUser is Admin)
        //        {
        //            calculatedFee *= 0.8m; // 20% discount for Admin
        //        }
        //        else if (db.Members.Any(m => m.Email == existingUser.Email && m.Role == "Premium"))
        //        {
        //            calculatedFee = 0; // Free for premium members and coaches
        //        }

        //        if (booking.FeePaid != calculatedFee)
        //        {
        //            ModelState.AddModelError("FeePaid", $"Incorrect fee. Expected {calculatedFee:C}.");
        //            return View("CreateAdminBooking", booking);
        //        }
        //    }
        //    else
        //    {
        //        booking.Name = booking.Name;
        //    }

        //    booking.CreatedAt = DateTime.Now;
        //    booking.CreatedBy = User.Identity.Name;
        //    booking.PayBy = booking.PayBy ?? "Cash";

        //    slot.RemainingCapacity -= 1;
        //    db.FacBooking.Add(booking);
        //    await db.SaveChangesAsync();

        //    return RedirectToAction("CreateAdminBooking");
        //}
        [HttpPost]
        public async Task<IActionResult> CreateAdminBooking(FacilityBookingViewModel booking)
        {
            if (!ModelState.IsValid)
            {
                // If model is not valid, return the view with current data
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive)
                                                 .Select(f => new { f.Id, f.Name })
                                                 .ToList();
                ViewBag.PaymentMethods = new[] { "Cash", "Card", "E-Wallet" };
                return View(booking);
            }

            // Server-side time validation
            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                return View(booking);
            }

            if (booking.StartTime.Minute != 0 || booking.EndTime.Minute != 0)
            {
                ModelState.AddModelError("StartTime", "Start and end times must align with hourly slots.");
                return View(booking);
            }// Server-side time validation
            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                return View(booking);
            }

            if (booking.StartTime.Minute != 0 || booking.EndTime.Minute != 0)
            {
                ModelState.AddModelError("StartTime", "Start and end times must align with hourly slots.");
                return View(booking);
            }

            var facility = await db.Facility.FindAsync(booking.FacilityId);
            var slot = await db.FacilityBookingCapacity
                .FirstOrDefaultAsync(s => s.FacilityId == booking.FacilityId
                    && s.BookingDate == booking.BookingDate
                    && s.StartTime == booking.StartTime);

            // Restrict booking to future slots only
            var now = DateTime.Now;
            if (booking.BookingDate < DateOnly.FromDateTime(now) ||
                (booking.BookingDate == DateOnly.FromDateTime(now) && booking.StartTime <= TimeOnly.FromDateTime(now)))
            {
                ModelState.AddModelError("Slot", "Cannot book past or current slots.");
                return View(booking);
            }

            if (slot == null || slot.RemainingCapacity <= 0)
            {
                ModelState.AddModelError("Slot", "Slot is fully booked.");
                return View(booking);
            }

            // Calculate fee based on role and other conditions
            var calculatedFee = booking.FeePaid;
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == booking.Email);

            if (user != null)
            {
                if (user.Role == "Admin")
                {
                    calculatedFee *= 0.8m; // 20% discount for Admin
                }
                else if (db.Members.Any(m => m.Email == user.Email && m.Role == "Premium"))
                {
                    calculatedFee = 0; // Free for premium members and coaches
                }
            }

            if (booking.FeePaid != calculatedFee)
            {
                ModelState.AddModelError("FeePaid", $"Incorrect fee. Expected {calculatedFee:C}.");
                return View(booking);
            }

            
            booking.PayBy = booking.PayBy ?? "Cash";  // Default payment method if not selected

            slot.RemainingCapacity -= 1;
            db.FacBooking.Add(new FacBooking
            {
                FacilityId = booking.FacilityId,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                FeePaid = booking.FeePaid,
                PayBy = booking.PayBy,
                Name = booking.Name,
                PhoneNumber = booking.PhoneNumber,
                Email = booking.Email,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name
            });

            await db.SaveChangesAsync();

            return RedirectToAction("CreateAdminBooking");
        }

        // Coach Booking Page
        public IActionResult CreateCoachBooking()
        {
            ViewBag.Facilities = db.Facility.Where(f => f.IsActive)
                                             .Select(f => new { f.Id, f.Name })
                                             .ToList();
            return View(new FacilityBookingViewModel
            {
                Email = User.Identity.Name, // Autofill logged-in coach's email
                Name = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name)?.Name,
                FeePaid = 0, // Fee is always 0 for coaches
                PayBy = "None" // No payment method required
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoachBooking(FacilityBookingViewModel booking, string recurrenceDuration)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive)
                                                 .Select(f => new { f.Id, f.Name })
                                                 .ToList();
                ViewBag.RecurrenceDurations = new List<string> { "None", "1 Month", "3 Months", "6 Months", "1 Year" };
                return View(booking);
            }

            // Validate that End Time > Start Time and aligns with hourly slots
            if (booking.EndTime <= booking.StartTime || booking.StartTime.Minute != 0 || booking.EndTime.Minute != 0)
            {
                ModelState.AddModelError("Time", "Start and end times must be on the hour, and end time must be after start time.");
                return View(booking);
            }

            // Restrict booking to future slots only
            var now = DateTime.Now;
            if (booking.BookingDate < DateOnly.FromDateTime(now) ||
                (booking.BookingDate == DateOnly.FromDateTime(now) && booking.StartTime <= TimeOnly.FromDateTime(now)))
            {
                ModelState.AddModelError("Slot", "Cannot book past or current slots.");
                return View(booking);
            }

            // Determine the number of weeks to repeat based on selected duration
            int repeatWeeks = recurrenceDuration switch
            {
                "1 Month" => 4,
                "3 Months" => 12,
                "6 Months" => 26,
                "1 Year" => 52,
                _ => 0 // Default is no recurrence
            };

            // Loop to handle recurring bookings
            for (int i = 0; i <= repeatWeeks; i++)
            {
                var bookingDate = booking.BookingDate.AddDays(i * 7); // Next Tuesday
                if (bookingDate.DayOfWeek != DayOfWeek.Tuesday)
                    continue;

                var slot = await db.FacilityBookingCapacity
                    .FirstOrDefaultAsync(s => s.FacilityId == booking.FacilityId
                        && s.BookingDate == bookingDate
                        && s.StartTime == booking.StartTime);

                if (slot == null || slot.RemainingCapacity <= 0)
                {
                    continue; // Skip fully booked slots
                }

                slot.RemainingCapacity -= 1;
                db.FacBooking.Add(new FacBooking
                {
                    FacilityId = booking.FacilityId,
                    BookingDate = bookingDate,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    FeePaid = 0,
                    PayBy = "None",
                    Name = booking.Name,
                    PhoneNumber = booking.PhoneNumber,
                    Email = booking.Email,
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity.Name
                });
            }

            await db.SaveChangesAsync();
            return RedirectToAction("CreateCoachBooking");
        }
    }
}
