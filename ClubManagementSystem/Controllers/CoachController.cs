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
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult CoachAdminHome(string name, int page = 1)
        {
            IQueryable<Coach> coaches = db.Coaches.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                coaches = coaches.Where(c => c.Name.Contains(name) || c.Email.Contains(name));
            }

            var pagedCoaches = coaches.ToPagedList(page, 10);  // 10 coaches per page
            ViewBag.Name = name;

            return View(pagedCoaches);
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


        // EditCoach: Display form to edit an existing coach
        public IActionResult EditCoach(string id)
        {
            var coach = db.Coaches.FirstOrDefault(c => c.CoachID == id);
            if (coach == null)
            {
                return NotFound();
            }

            var vm = new UpdateCoachProfileVM
            {
                Id = coach.CoachID,
                Name = coach.Name,
                PhoneNumber = coach.PhoneNumber,
                Email = coach.Email,  // Include Email for editing
                Photo = coach.Photo   // Include Photo URL (for display/editing)
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> EditCoach(UpdateCoachProfileVM vm, IFormFile Photo)
        {
            if (ModelState.IsValid)
            {
                var coach = db.Coaches.FirstOrDefault(c => c.CoachID == vm.Id);
                if (coach != null)
                {
                    string photoPath = coach.Photo;

                    // Validate and handle photo upload
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

                    // Update coach's details, including Email and Photo
                    coach.Name = vm.Name;
                    coach.PhoneNumber = vm.PhoneNumber;
                    coach.Email = vm.Email;  // Email is now editable
                    coach.Photo = photoPath;
                    coach.ModifiedAt = DateTime.Now;

                    // Save the changes
                    db.SaveChanges();

                    TempData["Info"] = "Coach profile updated successfully.";
                    return RedirectToAction("CoachPage");
                }
                else
                {
                    TempData["Error"] = "Coach not found.";
                }
            }
            return View(vm);
        }



        [HttpPost]
        public IActionResult DeleteCoach(string id)
        {
            var coach = db.Coaches.FirstOrDefault(c => c.CoachID == id);
            if (coach != null)
            {
                db.Coaches.Remove(coach);
                db.SaveChanges();
                TempData["Info"] = "Coach deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Coach not found.";
            }

            return RedirectToAction("CoachPage");
        }

        // CoachProfile: Display a specific coach's profile
        public async Task<IActionResult> CoachProfile(string id)
        {
            var coach = await db.Coaches.FirstOrDefaultAsync(c => c.CoachID == id);
            if (coach == null)
            {
                return NotFound();
            }

            return View(coach);
        }
    }
}
