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

        // MEMBER PART------------------------------------------------------------------
        // GET: FacilityBooking/ViewMemberSlots
        [Authorize(Roles = "Member,Premium")]
        public async Task<IActionResult> ViewMemberSlots(DateTime? date, int? categoryId)
        {
            DateTime selectedDate = date.HasValue ? date.Value
            : DateTime.Today;

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

        // GET: FacilityBooking/ViewBookingHistory
        [Authorize(Roles = "Member,Premium")]
        public async Task<IActionResult> ViewBookingHistory()
        {
            var email = User.Identity.Name;  // Get the logged-in user's email
            var bookings = await db.FacBooking
                .Include(b => b.Facility)
                .Where(b => b.Email == email && !b.isDeleted)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var viewModel = new FacilityBookingHistoryViewModel
            {
                Bookings = bookings,
                SelectedDate = DateTime.Now
            };

            return View(viewModel);
        }

        // GET: FacilityBooking/CreateMemberBooking
        [Authorize(Roles = "Member,Premium")]
        public IActionResult CreateMemberBooking()
        {
            ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
            return View();
        }

        // POST: FacilityBooking/CreateMemberBooking
        [HttpPost]
        [Authorize(Roles = "Member,Premium")]
        [HttpPost]
        public async Task<IActionResult> CreateMemberBooking(MemberBookingViewModel booking)
        {
            if (!ModelState.IsValid)
            {
                // Reload facilities for the view
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
                return View(booking);
            }

            var facility = await db.Facility.FindAsync(booking.FacilityId);

            if (booking.BookingDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
                return View(booking);
            }

            if (booking.StartTime.Minute != 0 || booking.EndTime.Minute != 0)
            {
                ModelState.AddModelError("StartTime", "Bookings must be made in hourly intervals (e.g., 12:00 PM).");
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
                return View(booking);
            }

            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
                return View(booking);
            }

            var overlappingBooking = await db.FacBooking
                .Where(b => b.FacilityId == booking.FacilityId &&
                            b.BookingDate == booking.BookingDate &&
                            (b.StartTime < booking.EndTime && b.EndTime > booking.StartTime))
                .FirstOrDefaultAsync();

            if (overlappingBooking != null)
            {
                ModelState.AddModelError("StartTime", "The selected time slot is already booked.");
                ViewBag.Facilities = db.Facility.Where(f => f.IsActive).ToList();
                return View(booking);
            }

            var memberEmail = User.Identity.Name;
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == memberEmail);

            // Calculate fee
            decimal fee = user.Role == "premium" ? 0 : (decimal)(booking.EndTime - booking.StartTime).TotalHours * facility.Price;

            var newBooking = new FacBooking
            {
                FacilityId = booking.FacilityId,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Email = memberEmail,
                Name = user.Name,
                PhoneNumber = booking.PhoneNumber,
                FeePaid = fee,
                isPaid = false,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name
            };

            db.FacBooking.Add(newBooking);
            await db.SaveChangesAsync();

            return RedirectToAction("BookingConfirmation", new { id = newBooking.Id });
        }

        // GET: FacilityBooking/BookingConfirmation
        [Authorize(Roles = "Member,Premium")]
        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var booking = await db.FacBooking
                .Include(b => b.Facility)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            var viewModel = new BookingConfirmationViewModel
            {
                Facility = booking.Facility,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Fee = booking.FeePaid,
                Email = booking.Email
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Member,Premium")]
        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var booking = await db.FacBooking.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            if (booking.FeePaid > 0)
            {
                // Redirect to payment page
                return RedirectToAction("Payment", new { id });
                // need to put isPaid = true
            }

            TempData["Info"] = "Booking confirmed successfully.";
            return RedirectToAction("ViewBookingHistory");
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
                        // Fee is zero for premium and coach
                        return Json(new { fee = 0 });

                    case "staff":
                    case "admin":
                    case "superadmin":
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

        // ADMIN PART----------------------------------------------------------------------------------------------------------------
        [Authorize(Roles = "Admin, Superadmin")]
        // GET: FacilityBooking/ViewSlots
        public async Task<IActionResult> ViewSlots(DateTime? date, int? categoryId, string viewMode = "chart")
        {
            DateTime selectedDate = date ?? DateTime.Today;
            var facilities = await db.Facility
                .Include(f => f.FacilityCategories)
                .Include(f => f.FacBookings)
                .Where(f => !categoryId.HasValue || f.FacilityCategoriesId == categoryId)
                .ToListAsync();

            var bookings = await db.FacBooking
                .Where(b => b.BookingDate == DateOnly.FromDateTime(selectedDate) && !b.isDeleted)
                .ToListAsync();

            var viewModel = new FacilityAvailabilityViewModel
            {
                Facilities = facilities,
                Bookings = bookings,
                Categories = db.FacilityCategories.ToList(),
                SelectedDate = selectedDate
            };

            ViewBag.ViewMode = viewMode;
            return View(viewModel);
        }

        [Authorize(Roles = "Admin, Superadmin")]
        // GET: FacilityBooking/CreateAdminBooking
        public IActionResult CreateAdminBooking()
        {
            ViewBag.Facilities = db.Facility
                                    .Where(f => f.IsActive)
                                    .Select(f => new { f.Id, f.Name })
                                    .ToList();

            ViewBag.PaymentMethods = new[] { "None", "Cash", "Card", "E-Wallet" };
            return View();
        }

        [Authorize(Roles = "Admin, Superadmin")]
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

        [Authorize(Roles = "Admin, Superadmin")]
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

            // **Check for overlapping bookings**
            var overlappingBooking = await db.FacBooking
                .Where(b => b.FacilityId == booking.FacilityId &&
                            b.BookingDate == booking.BookingDate &&
                            ((b.StartTime < booking.EndTime && b.EndTime > booking.StartTime) ||  // Overlapping logic
                             (b.StartTime == booking.StartTime && b.EndTime == booking.EndTime)))
                .FirstOrDefaultAsync();

            if (overlappingBooking != null)
            {
                ModelState.AddModelError("StartTime", "The selected time slot is already booked.");
                return View(booking);
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
                CreatedBy = User.Identity.Name,
                isDeleted = false
            });

            await db.SaveChangesAsync();
            TempData["Info"] = "Facility booked successfully.";

            return RedirectToAction("ViewSlots");
        }

        [Authorize(Roles = "Admin, Superadmin")]
        // GET: FacilityBooking/EditBooking/{id}
        public async Task<IActionResult> EditBooking(int id)
        {
            var booking = await db.FacBooking
                .Include(b => b.Facility)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Facilities = db.Facility
                .Where(f => f.IsActive)
                .Select(f => new { f.Id, f.Name })
                .ToList();

            var vm = new FacilityBookingViewModel
            {
                Id = booking.Id,
                Name = booking.Name,
                PhoneNumber = booking.PhoneNumber,
                Email = booking.Email,
                FacilityId = booking.FacilityId,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                PayBy = booking.PayBy
            };

            return View(booking);
        }

        [Authorize(Roles = "Admin, Superadmin")]
        // POST: FacilityBooking/EditBooking/{id}
        [HttpPost]
        public async Task<IActionResult> EditBooking(int id, FacilityBookingViewModel updatedBooking)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedBooking);
            }

            var existingBooking = await db.FacBooking.FindAsync(id);
            if (existingBooking == null)
            {
                return NotFound();
            }

            // Check if the booking date is in the past
            if (updatedBooking.BookingDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
                return View(updatedBooking);
            }

            // Calculate original duration
            var originalDuration = (existingBooking.EndTime - existingBooking.StartTime).TotalHours;
            var newDuration = (updatedBooking.EndTime - updatedBooking.StartTime).TotalHours;

            if (originalDuration != newDuration)
            {
                ModelState.AddModelError("EndTime", $"Duration must remain the same ({originalDuration} hours).");
                return View(updatedBooking);
            }

            // Check for overlapping bookings
            var overlappingBooking = await db.FacBooking
                .Where(b => b.FacilityId == updatedBooking.FacilityId &&
                            b.BookingDate == updatedBooking.BookingDate &&
                            b.Id != id && 
                            (b.StartTime < updatedBooking.EndTime && b.EndTime > updatedBooking.StartTime))
                .FirstOrDefaultAsync();

            if (overlappingBooking != null)
            {
                ModelState.AddModelError("StartTime", "The selected time slot is already booked.");
                return View(updatedBooking);
            }

            // Update booking details
            existingBooking.BookingDate = updatedBooking.BookingDate;
            existingBooking.StartTime = updatedBooking.StartTime;
            existingBooking.EndTime = updatedBooking.EndTime;
            existingBooking.FacilityId = updatedBooking.FacilityId;
            existingBooking.Name = updatedBooking.Name;
            existingBooking.PhoneNumber = updatedBooking.PhoneNumber;
            existingBooking.Email = updatedBooking.Email;
            existingBooking.PayBy = updatedBooking.PayBy;
            existingBooking.isDeleted = false;
            db.FacBooking.Update(existingBooking);
            await db.SaveChangesAsync();

            TempData["Info"] = "Booking updated successfully.";
            return RedirectToAction("ViewSlots");
        }

        [Authorize(Roles = "Admin, Superadmin")]
        // POST: FacilityBooking/CancelBooking/{id}
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await db.FacBooking
                .Include(b => b.Facility)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Prevent cancellation of past bookings
            if (booking.BookingDate < DateOnly.FromDateTime(DateTime.Now))
            {
                TempData["Error"] = "Cannot cancel past bookings.";
                return RedirectToAction("ViewSlots");
            }

            // TODO: PAYMENT MODULE - DELETE PAYMENT RECORD (if didn't do then remove this line)
            booking.isDeleted = true;
            db.FacBooking.Update(booking);
            await db.SaveChangesAsync();

            TempData["Info"] = "Booking cancelled successfully.";
            return RedirectToAction("ViewSlots");
        }
    }
}
