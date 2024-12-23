using System.Linq;
using System.Security.Claims;
using ClubManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace ClubManagementSystem.Controllers
{
    public class AnnouncementController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public AnnouncementController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // GET: Announcement/AnnList
        public IActionResult AnnList(string? name, string? sort, string? dir, int page = 1)
        {
            // Searching
            ViewBag.Name = name = name?.Trim() ?? "";
            var searched = db.Announcements.Where(s => s.Title.Contains(name));

            // Sort
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            Func<Announcement, object> fn = sort switch
            {
                "Title" => s => s.Title,
                "Content" => s => s.Content,
                "Photo" => s => s.Photo,
                "DateTime" => s => s.DateTime,
                "AdminEmail" => s => s.AdminEmail,
                _ => s => s.Id,
            };

            var sorted = dir == "des" ?
                         searched.OrderByDescending(fn) :
                         searched.OrderBy(fn);

            // Pagination
            if (page < 1)
            {
                return RedirectToAction(null, new { name, sort, dir, page = 1 });
            }

            var m = sorted.ToPagedList(page, 10);

            if (page > m.PageCount && m.PageCount > 0)
            {
                return RedirectToAction(null, new { name, sort, dir, page = m.PageCount });
            }

            return View(m);
        }

        // POST: Announcement/AnnList
        [HttpPost]
        [Route("Announcement/ToggleStatus/{id}")]
        public IActionResult ToggleStatus(string id)
        {
            var announcement = db.Announcements.Find(id);
            if (announcement == null)
            {
                return NotFound();
            }

            // Toggle the status between "Posted" and "Blocked"
            announcement.Status = (announcement.Status == "Posted") ? "Blocked" : "Posted";
            db.SaveChanges();

            var response = new ToggleStatusResponse
            {
                Success = true,
                Message = "Status updated successfully."
            };

            return Ok(response);
        }

        // GET: Announcement/AnnAdd
        public IActionResult AnnAdd()
        {
            return View(new Announcement());
        }

        // POST: Announcement/AnnAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnnAdd(Announcement announcement, IEnumerable<IFormFile> Photos, string PostOption, DateTime? PostDate)
        {
            // Retrieve the currently logged-in admin's email from the claims
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(adminEmail))
            {
                TempData["Error"] = "Unable to retrieve admin email. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Validate the PostDate using the helper method
            if (PostOption == "later" && !IsPostDateValid(PostDate, out string errorMessage))
            {
                TempData["Error"] = errorMessage;
                return View(announcement); // Return to the AnnAdd view with the error message
            }

            // Retrieve the latest ID from the database
            var lastAnnouncement = db.Announcements
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();

            if (lastAnnouncement != null && lastAnnouncement.Id.StartsWith("A"))
            {
                var numericPart = lastAnnouncement.Id.Substring(1);
                if (int.TryParse(numericPart, out int number))
                {
                    announcement.Id = $"A{(number + 1):D3}";
                }
                else
                {
                    announcement.Id = "A001"; // Handle unexpected formats
                }
            }
            else
            {
                announcement.Id = "A001"; // Default ID if no records
            }

            // Assign other properties programmatically
            announcement.Status = PostOption == "now" ? "Posted" : "Pending"; // Set status based on "Post Now"
            announcement.AdminEmail = adminEmail;
            announcement.LikeUsers = "-";

            // Set DateTime based on the user's choice
            if (PostOption == "now")
            {
                announcement.DateTime = DateTime.Now; // Post immediately
            }
            else
            {
                announcement.DateTime = PostDate ?? DateTime.Now; // Post later (use the selected date or now as fallback)
            }

            if (Photos != null && Photos.Any())
            {
                var photoNames = new List<string>();
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos");

                foreach (var photo in Photos)
                {
                    var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
                    var filePath = Path.Combine(uploadPath, uniqueName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        photo.CopyTo(stream);
                    }

                    photoNames.Add(uniqueName);
                }

                announcement.Photo = string.Join(",", photoNames);
            }

            // Clear ModelState errors for programmatically assigned fields
            ModelState.Remove("Id");
            ModelState.Remove("Photo");
            ModelState.Remove("Status");
            ModelState.Remove("AdminEmail");

            if (ModelState.IsValid)
            {
                db.Announcements.Add(announcement);
                db.SaveChanges();

                TempData["Message"] = "Announcement added successfully!";
                return RedirectToAction("AnnList");
            }

            return View(announcement);
        }

        // GET: Announcement/AnnUpdate
        public IActionResult AnnUpdate(string id)
        {
            var announcement = db.Announcements.FirstOrDefault(a => a.Id == id);

            if (announcement == null)
            {
                TempData["Error"] = "Announcement not found.";
                return RedirectToAction("AnnList");
            }

            // Pass the existing photos to the view as a model
            var photoUrls = announcement.Photo?.Split(',').ToList() ?? new List<string>();

            ViewBag.ExistingPhotos = photoUrls;

            return View(announcement);
        }


        // POST: Announcement/AnnUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnnUpdate(Announcement announcement, IEnumerable<IFormFile> Photos, string PostTime, DateTime? PostDate)
        {
            // Find the existing announcement to update
            var existingAnnouncement = db.Announcements.FirstOrDefault(a => a.Id == announcement.Id);
            if (existingAnnouncement == null)
            {
                TempData["Error"] = "Announcement not found.";
                return RedirectToAction("AnnList");
            }

            // Update the announcement properties
            existingAnnouncement.Title = announcement.Title;
            existingAnnouncement.Content = announcement.Content;

            // Set Status and DateTime based on selected radio button
            if (PostTime == "PostNow")
            {
                existingAnnouncement.Status = "Posted";
                existingAnnouncement.DateTime = DateTime.Now;
            }
            else if (PostTime == "PostLater")
            {
                existingAnnouncement.Status = "Pending";

                // Validate PostDate using the helper method
                if (!IsPostDateValid(PostDate, out string errorMessage))
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("AnnUpdate", new { id = announcement.Id });
                }

                // If PostDate is valid, use the provided PostDate, or default to now if not provided
                existingAnnouncement.DateTime = PostDate ?? DateTime.Now;
            }

            // Handle file upload for new photos
            if (Photos != null && Photos.Any())
            {
                // Get the old photo names
                var oldPhotoNames = string.IsNullOrEmpty(existingAnnouncement.Photo)
                    ? new List<string>()
                    : existingAnnouncement.Photo.Split(',').ToList();

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos");

                // Delete old photos from the server if there are any
                foreach (var oldPhotoName in oldPhotoNames)
                {
                    var oldFilePath = Path.Combine(uploadPath, oldPhotoName);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var newPhotoNames = new List<string>();

                // Upload new photos and add to the list
                foreach (var photo in Photos)
                {
                    var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
                    var filePath = Path.Combine(uploadPath, uniqueName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        photo.CopyTo(stream);
                    }

                    // Add the new photo to the list
                    newPhotoNames.Add(uniqueName);
                }

                // Update the photo field with the new list of photos, limit to 3
                existingAnnouncement.Photo = string.Join(",", newPhotoNames.Take(3));  // Only keep the first 3 photos
            }

            // Save changes to the database
            db.SaveChanges();

            TempData["Message"] = "Announcement updated successfully!";
            return RedirectToAction("AnnList");
        }

        public bool IsPostDateValid(DateTime? postDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            // If PostDate is provided and is in the past, return false with error message
            if (postDate.HasValue && postDate.Value < DateTime.Now)
            {
                errorMessage = "The selected post time cannot be in the past.";
                return false;
            }

            // If PostDate is valid or not provided, return true
            return true;
        }

        // GET: Announcement/AnnView
        public IActionResult AnnView()
        {
            // Retrieve the currently logged-in user's email
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            // Fetch announcements and related admin details where Status is "Posted"
            var announcements = db.Announcements
                .Where(a => a.Status == "Posted")  // Filter only "Posted" announcements
                .Select(a => new AnnouncementViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    Photo = a.Photo,
                    DateTime = a.DateTime,
                    AdminEmail = a.AdminEmail,
                    AdminName = db.Users
                        .Where(u => u.Email == a.AdminEmail)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "Unknown Admin",
                    AdminProfilePicture = db.Users
                        .Where(u => u.Email == a.AdminEmail)
                        .Select(u => u.Name) // Assuming ProfilePicture is the correct field for profile image
                        .FirstOrDefault() ?? "default-profile.png",
                    LikeUsers = a.LikeUsers, // Comma-separated list of email addresses of users who liked the post
                })
                .OrderByDescending(a => a.DateTime) // Order by DateTime
                .ToList(); // Fetch all "Posted" announcements into memory

            // After the data is retrieved, calculate the LikeCount and check if the current user liked the post
            foreach (var announcement in announcements)
            {
                // Calculate like count
                announcement.LikeCount = string.IsNullOrEmpty(announcement.LikeUsers)
                    ? 0
                    : announcement.LikeUsers.Split(',').Length; // Calculate like count in memory

                // Check if the current user has liked the post
                announcement.IsLikedByCurrentUser = !string.IsNullOrEmpty(announcement.LikeUsers) &&
                                                     announcement.LikeUsers.Split(',').Contains(userEmail);
            }

            return View(announcements);
        }

        // POST: Announcement/LikeAnn
        [HttpPost]
        public IActionResult LikeAnn(String announcementId)
        {
            // Retrieve the currently logged-in user's email from the claims
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var announcement = db.Announcements
                                 .FirstOrDefault(a => a.Id == announcementId);

            if (announcement != null)
            {
                var likeUsers = string.IsNullOrEmpty(announcement.LikeUsers) ? new List<string>() : announcement.LikeUsers.Split(',').ToList();

                if (likeUsers.Contains(userEmail))
                {
                    likeUsers.Remove(userEmail); // Unliking
                }
                else
                {
                    likeUsers.Add(userEmail); // Liking
                }

                announcement.LikeUsers = string.Join(",", likeUsers);

                db.SaveChanges();
            }

            int likeCount = announcement?.LikeUsers?.Split(',').Length ?? 0;

            return Json(new { success = true, likeCount });
        }


    }
}
