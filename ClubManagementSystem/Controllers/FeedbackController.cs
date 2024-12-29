using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;

namespace ClubManagementSystem.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public FeedbackController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // GET : Feedback/FeedbackList
        public IActionResult FeedbackList(string? name, string? sort, string? dir, int page = 1)
        {
            // Searching
            ViewBag.Name = name = name?.Trim() ?? "";
            var searched = db.Feedbacks.Where(f => f.Content.Contains(name));

            // Sort
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            Func<Feedback, object> fn = sort switch
            {
                "Content" => f => f.Content,
                "Photo" => f => f.Photo,
                "CreateDateTime" => f => f.CreateDateTime,
                "ReplyContent" => f => f.ReplyContent,
                "AdminEmail" => f => f.AdminEmail,
                "UserEmail" => f => f.UserEmail,
                "ReplyStatus" => f => f.ReplyStatus,
                _ => f => f.Id,
            };

            var sorted = dir == "des" ?
                         searched.OrderByDescending(fn) :
                         searched.OrderBy(fn);

            // Fetch UserName for UserEmail
            var userEmails = sorted.Select(f => f.UserEmail).Distinct().ToList();
            var users = db.Users.Where(u => userEmails.Contains(u.Email))
                                .ToDictionary(u => u.Email, u => u.Name);

            // Fetch AdminName for AdminEmail
            var adminEmails = sorted.Select(f => f.AdminEmail).Distinct().ToList();
            var admins = db.Users.Where(u => adminEmails.Contains(u.Email))
                                 .ToDictionary(u => u.Email, u => u.Name);

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

            // Pass UserName and AdminName to the view
            ViewBag.Users = users;
            ViewBag.Admins = admins;

            return View(m);
        }

        // GET : Feedback/FeedbackReply
        public IActionResult FeedbackReply(string id)
        {
            var feedback = db.Feedbacks.FirstOrDefault(f => f.Id == id);

            if (feedback == null)
            {
                TempData["Error"] = "Feedback not found.";
                return RedirectToAction("FeedbackList");
            }

            // Get UserName based on UserEmail
            var user = db.Users.FirstOrDefault(u => u.Email == feedback.UserEmail);
            var userName = user?.Name ?? "Unknown User"; // Default to "Unknown User" if no user is found

            var viewModel = new FeedBackViewModel
            {
                Id = feedback.Id,
                Content = feedback.Content,
                Photo = feedback.Photo,
                CreateDateTime = feedback.CreateDateTime,
                ReplyContent = feedback.ReplyContent,
                ReplyPhoto = feedback.ReplyPhoto,
                ReplyStatus = feedback.ReplyStatus,
                AdminEmail = feedback.AdminEmail,
                UserEmail = feedback.UserEmail,
                UserName = userName  // Add UserName to the view model
            };

            return View(viewModel);
        }

        // POST : Feedback/FeedbackReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FeedbackReply(string id, FeedBackViewModel model, IEnumerable<IFormFile> ReplyPhoto)
        {
            // Find the feedback record to update
            var feedback = db.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback == null)
            {
                TempData["Error"] = "Feedback not found.";
                return RedirectToAction("FeedbackList");
            }

            // Retrieve admin email
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(adminEmail))
            {
                TempData["Error"] = "Unable to determine the logged-in admin.";
                return RedirectToAction("FeedbackList");
            }

            // Handle file upload for reply photos
            if (ReplyPhoto != null && ReplyPhoto.Any())
            {
                try
                {
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos");

                    var newPhotoPaths = new List<string>();

                    foreach (var photo in ReplyPhoto.Take(3)) // Limit to 3 photos
                    {
                        if (photo?.Length > 0) // Ensure the file is not null
                        {
                            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
                            var filePath = Path.Combine(uploadPath, uniqueName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                photo.CopyTo(stream);
                            }

                            newPhotoPaths.Add(uniqueName);
                        }
                    }

                    // Update the ReplyPhoto field with the new photo names
                    feedback.ReplyPhoto = string.Join(",", newPhotoPaths);
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error uploading photos: {ex.Message}";
                    return View(model); // Return view with the current model
                }
            }

            // Update feedback fields
            feedback.ReadStatus = "Unread";
            feedback.ReplyContent = model.ReplyContent;
            feedback.ReplyDateTime = DateTime.Now;
            feedback.ReplyStatus = "Replied";
            feedback.AdminEmail = adminEmail;

            // Save changes to the database
            try
            {
                db.SaveChanges();
                TempData["Info"] = "Feedback replied successfully!";
                return RedirectToAction("FeedbackList");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving the feedback reply. Please try again.");
                Console.WriteLine($"Error: {ex.Message}");
                return View(model);
            }
        }

        // GET : Feedback/FeedbackDetail
        public IActionResult FeedbackDetail(string id)
        {
            var feedback = db.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback == null)
            {
                TempData["Error"] = "Feedback not found.";
                return RedirectToAction("FeedbackList");
            }

            // Fetch the user's name from the Users table using the email
            var user = db.Users.FirstOrDefault(u => u.Email == feedback.UserEmail);
            var userName = user != null ? user.Name : "Unknown User";

            // Fetch the admin's name from the Users table using the admin email
            var admin = db.Users.FirstOrDefault(u => u.Email == feedback.AdminEmail);
            var adminName = admin != null ? admin.Name : "Unknown Admin";

            var viewModel = new FeedBackViewModel
            {
                Id = feedback.Id,
                Content = feedback.Content,
                Photo = feedback.Photo,
                ReadStatus = feedback.ReadStatus,
                CreateDateTime = feedback.CreateDateTime,
                ReplyContent = feedback.ReplyContent,
                ReplyPhoto = feedback.ReplyPhoto,
                ReplyDateTime = feedback.ReplyDateTime,
                ReplyStatus = feedback.ReplyStatus,
                AdminName = adminName, // Include the admin's name
                UserName = userName // Include the user's name
            };
            return View(viewModel);
        }

        // GET : Feedback/FeedbackView
        public IActionResult FeedbackView()
        {
            // Retrieve the logged-in user's email
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            // Fetch feedbacks associated with the logged-in user and order by CreateDateTime descending
            var feedbacks = db.Feedbacks
                .Where(f => f.UserEmail == userEmail)
                .OrderByDescending(f => f.CreateDateTime) // Order by latest feedback first
                .ToList();

            // Create an IEnumerable<FeedBackViewModel> for the view
            IEnumerable<FeedBackViewModel> viewModelList = feedbacks.Select(f => new FeedBackViewModel
            {
                Id = f.Id,
                Content = f.Content,
                Photo = f.Photo,
                ReadStatus = f.ReadStatus,
                CreateDateTime = f.CreateDateTime,
                ReplyContent = f.ReplyContent,
                ReplyPhoto = f.ReplyPhoto,
                ReplyDateTime = f.ReplyDateTime,
                ReplyStatus = f.ReplyStatus,
                AdminName = db.Users.FirstOrDefault(u => u.Email == f.AdminEmail)?.Name ?? "Unknown Admin",
                UserName = db.Users.FirstOrDefault(u => u.Email == f.UserEmail)?.Name ?? "Unknown User"
            });

            // Pass the IEnumerable to the view
            return View(viewModelList);
        }

        // GET : Feedback/FeedbackViewDetail
        public IActionResult FeedbackViewDetail(string id)
        {
            // Find the feedback by ID
            var feedback = db.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback == null)
            {
                TempData["Error"] = "Feedback not found.";
                return RedirectToAction("FeedbackView");
            }

            // Update the status from "Unread" to "Read"
            if (feedback.ReadStatus == "Unread")
            {
                feedback.ReadStatus = "Read";
                db.SaveChanges(); // Save changes to the database
            }

            // Map feedback to ViewModel
            var viewModel = new FeedBackViewModel
            {
                Id = feedback.Id,
                Content = feedback.Content,
                Photo = feedback.Photo,
                CreateDateTime = feedback.CreateDateTime,
                ReplyContent = feedback.ReplyContent,
                ReplyPhoto = feedback.ReplyPhoto,
                ReplyDateTime = feedback.ReplyDateTime,
                ReadStatus = feedback.ReadStatus,
                ReplyStatus = feedback.ReplyStatus
            };


            return View("FeedbackViewDetail", viewModel);
        }

        // POST : Feedback/UpdateFeedbackStatus
        [HttpPost]
        public IActionResult UpdateFeedbackStatus(string id)
        {
            // Find the feedback by ID
            var feedback = db.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback == null)
            {
                return NotFound();
            }

            // Update the status
            feedback.ReadStatus = "Read";
            db.SaveChanges();

            return Ok(new { message = "Feedback status updated successfully" });
        }

        // GET : Feedback/FeedbackCreate
        [HttpGet]
        public IActionResult FeedbackCreate()
        {
            return View();
        }

        // POST : Feedback/FeedbackCreate
        [HttpPost]
        public IActionResult FeedbackCreate(string content, List<IFormFile> photos)
        {
            // Check if the directory for saving photos exists, and create it if not
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Retrieve the current user's email
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            // Generate a new ID based on the last ID in the database
            var lastFeedback = db.Feedbacks.OrderByDescending(f => f.Id).FirstOrDefault();
            var newId = lastFeedback != null
                ? "K" + (int.Parse(lastFeedback.Id.Substring(1)) + 1).ToString("D4")
                : "K0001";

            // Handle photo uploads
            var photoNames = new List<string>();
            if (photos != null && photos.Any())
            {
                foreach (var photo in photos.Take(3)) // Limit to 3 photos
                {
                    var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
                    var filePath = Path.Combine(uploadPath, uniqueName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        photo.CopyTo(stream);
                    }

                    photoNames.Add(uniqueName);
                }
            }

            // Map feedback data
            var feedback = new Feedback
            {
                Id = newId,
                Content = content,
                Photo = string.Join(",", photoNames), // Save photo names as a comma-separated string
                ReadStatus = "Read",
                CreateDateTime = DateTime.Now,
                ReplyContent = "-",
                ReplyPhoto = "-",
                ReplyDateTime = new DateTime(1900, 1, 1, 0, 0, 0),
                ReplyStatus = "Pending",
                AdminEmail = "-",
                UserEmail = userEmail
            };

            // Save feedback to the database
            db.Feedbacks.Add(feedback);
            db.SaveChanges();
            TempData["Info"] = "Feedback created successfully!";
            return RedirectToAction("FeedbackView");
        }

    }
}
