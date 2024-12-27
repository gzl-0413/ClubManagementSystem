using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClubManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using X.PagedList.Extensions;

namespace ClubManagementSystem.Controllers
{
    public class CoachController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public CoachController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // CoachAdminHome: Dashboard for coaches
        [Authorize(Roles = "Admin","SuperAdmin")]
        public IActionResult CoachAdminHome(string name)
        {
            // Fetch all coaches
            IQueryable<Coach> coaches = db.Coaches.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                coaches = coaches.Where(c => c.Name.Contains(name) || c.Email.Contains(name));
            }

            // Return all coaches without pagination
            var coachList = coaches.ToList();
            ViewBag.Name = name;

            return View(coachList);
        }




        // CoachPage: List all coaches with search, sort, and pagination
        public IActionResult CoachPage(string name, string sort, string dir, int page = 1)
        {
            IQueryable<Coach> coaches = db.Coaches.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                coaches = coaches.Where(c => c.Name.Contains(name) || c.Email.Contains(name));
            }

            switch (sort)
            {
                case "Name":
                    coaches = dir == "asc" ? coaches.OrderBy(c => c.Name) : coaches.OrderByDescending(c => c.Name);
                    break;
                case "CreatedAt":
                    coaches = dir == "asc" ? coaches.OrderBy(c => c.CreatedAt) : coaches.OrderByDescending(c => c.CreatedAt);
                    break;
                default:
                    coaches = dir == "asc" ? coaches.OrderBy(c => c.Email) : coaches.OrderByDescending(c => c.Email);
                    break;
            }

            var pagedCoaches = coaches.ToPagedList(page, 10);

            ViewBag.Name = name;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            return View(pagedCoaches);
        }

        // CreateCoach: Display form to create a new coach
        public IActionResult CreateCoach()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoach(CoachCreateVM vm, IFormFile Photo)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (db.Coaches.Any(c => c.Email == vm.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(vm);  // Return to the view with error
                }

                // Validate photo before saving
                string photoPath = null;
                if (Photo != null)
                {
                    var err = hp.ValidatePhoto(Photo);  // Validate the photo
                    if (!string.IsNullOrEmpty(err))
                    {
                        ModelState.AddModelError("Photo", err);  // Add error if photo validation fails
                        return View(vm);
                    }

                    // Save photo using helper method
                    photoPath = hp.SavePhoto(Photo, "coaches");
                }

                // Create and save new coach
                var coach = new Coach
                {
                    CoachID = "CH" + Guid.NewGuid().ToString().Substring(0, 6),  // Unique Coach ID like CH1234
                    Name = vm.Name,
                    Email = vm.Email,
                    PhoneNumber = vm.PhoneNumber,
                    Password = vm.Password, // Add password field
                    Photo = photoPath,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now,
                };

                db.Coaches.Add(coach);
                await db.SaveChangesAsync();

                TempData["Info"] = "Coach account created successfully.";
                return RedirectToAction("CoachAdminHome");  // Redirect to Coach Page after creation
            }

            // If the model is not valid, return the form with errors
            return View(vm);
        }


        // Display the Edit Coach form
        public IActionResult EditCoach(string id)
        {
            // Fetch the coach using the ID
            var coach = db.Coaches.FirstOrDefault(c => c.CoachID == id);
            if (coach == null)
            {
                TempData["Error"] = "Coach not found.";
                return RedirectToAction("CoachAdminHome"); // Redirect if coach not found
            }

            // Populate the ViewModel with coach details
            var vm = new UpdateCoachProfileVM
            {
                Id = coach.CoachID,
                Name = coach.Name,
                PhoneNumber = coach.PhoneNumber,
                Email = coach.Email,
                Photo = coach.Photo // Include current photo for display
            };

            return View(vm);
        }

        // Handle the submission of the Edit Coach form
        [HttpPost]
        public async Task<IActionResult> EditCoach(UpdateCoachProfileVM vm, IFormFile Photo)
        {
            if (!ModelState.IsValid)
            {
                return View(vm); // Return form with validation errors
            }

            // Fetch the coach record
            var coach = db.Coaches.FirstOrDefault(c => c.CoachID == vm.Id);
            if (coach == null)
            {
                TempData["Error"] = "Coach not found.";
                return RedirectToAction("CoachAdminHome"); // Redirect if coach not found
            }

            string photoPath = coach.Photo; // Retain existing photo path

            // Handle photo upload
            if (Photo != null)
            {
                var err = hp.ValidatePhoto(Photo); // Validate the uploaded photo
                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError("Photo", err); // Add validation error
                    return View(vm);
                }

                // Save the new photo and update the path
                photoPath = hp.SavePhoto(Photo, "coaches");
            }

            // Update coach properties
            coach.Name = vm.Name;
            coach.PhoneNumber = vm.PhoneNumber;
            coach.Email = vm.Email;
            coach.Photo = photoPath; // Update the photo path
            coach.ModifiedAt = DateTime.Now; // Update modification timestamp

            // Save changes to the database
            await db.SaveChangesAsync();

            TempData["Info"] = "Coach profile updated successfully.";
            return RedirectToAction("CoachAdminHome"); // Redirect to Admin Home after update
        }

        // GET method to show the confirmation page
        public IActionResult DeleteCoach(string id)
        {
            var coach = db.Coaches.Find(id);
            if (coach != null)
            {
                return View(coach); // Pass the coach data to the confirmation view
            }
            else
            {
                TempData["Error"] = "Coach not found.";
                return RedirectToAction("CoachAdminHome"); // Redirect back to the main page if coach not found
            }
        }

        // POST method to handle the deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCoachConfirmed(string id)
        {
            var coach = db.Coaches.Find(id);
            if (coach != null)
            {
                db.Coaches.Remove(coach);
                db.SaveChanges();
                TempData["Success"] = "Coach deleted successfully.";
                return RedirectToAction("CoachAdminHome"); // Redirect to the admin dashboard after deletion
            }
            else
            {
                TempData["Error"] = "Coach not found.";
                return RedirectToAction("CoachAdminHome");
            }
        }
    }
    }
