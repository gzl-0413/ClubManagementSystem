using Microsoft.AspNetCore.Mvc;
using ClubManagementSystem.Models;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace ClubManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public StaffController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // GET: Staff/Home
        public IActionResult StaffHome()
        {
            var staffList = db.Staffs.ToList();
            return View(staffList);
        }

        public IActionResult StaffAdd()
        {
            var model = new StaffVM.Create();
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffAdd(StaffVM.Create vm)
        {
            if (!ModelState.IsValid)
            {
                // Debugging: Check if the model is valid
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);  // You can log this or inspect in debug output
                }

                // Set an error message if there are validation issues
                TempData["ErrorMessage"] = "There are validation errors. Please check the form.";
                return View(vm);
            }

            try
            {
                // Generate a unique ID for the staff member, e.g., ST1234
                var staffCount = db.Staffs.Count();
                var staffId = "ST" + (staffCount + 1).ToString("D4");
                var now = DateTime.UtcNow;
                string? photoUrl = null;

                var staff = new Staff
                {
                    Id = staffId,
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = photoUrl
                };

                // Check if a photo was uploaded
                if (vm.Photo != null)
                {
                    // Generate a unique file name for the uploaded photo
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.Photo.FileName);

                    // Set the path to save the file
                    var path = Path.Combine(en.WebRootPath, "StaffImg", fileName);

                    // Save the file to the StaffImg folder
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        vm.Photo.CopyTo(stream);
                    }

                    // Store the file name in the database
                    staff.PhotoURL = "" + fileName;
                }

                // Add the staff member to the database
                db.Staffs.Add(staff);
                db.SaveChanges();

                // Set a success message to TempData
                TempData["SuccessMessage"] = "Staff member added successfully with ID: " + staffId;

                // Redirect to the StaffHome view
                return RedirectToAction(nameof(StaffHome));
            }
            catch (Exception ex)
            {
                // Set an error message to TempData in case of an exception
                TempData["ErrorMessage"] = "An error occurred while adding the staff member: " + ex.Message;
            }

            // If an exception occurred, return the view with the model and error message
            return View(vm);
        }

        // GET: Staff/Edit/{id}
        public IActionResult StaffEdit(string id)
        {
            var staff = db.Staffs.Find(id);
            if (staff == null)
            {
                return NotFound();
            }

            var model = new StaffVM.Edit
            {
                Id = staff.Id,
                Name = staff.Name,
                Email = staff.Email,
                PhotoURL = staff.PhotoURL
            };

            return View(model);
        }

        // POST: Staff/Edit
        // POST: Staff/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffEdit(StaffVM.Edit model)
        {
            if (ModelState.IsValid)
            {
                var staff = db.Staffs.Find(model.Id);
                if (staff == null)
                {
                    return NotFound();
                }

                // Update staff details except for the photo
                staff.Name = model.Name;
                staff.Email = model.Email;

                // Do not update the photo if no new photo is uploaded (as we disabled it in the form)
                // staff.PhotoURL is not changed if no new file is provided

                db.Staffs.Update(staff);
                db.SaveChanges();

                return RedirectToAction(nameof(StaffHome));
            }

            return View(model);
        }


        // GET: Staff/Delete/{id}
        public IActionResult StaffDelete(string id)
        {
            var staff = db.Staffs.Find(id);
            if (staff == null)
            {
                return NotFound();
            }

            var model = new StaffVM.Delete
            {
                Id = staff.Id,
                Name = staff.Name,
                Email = staff.Email
            };

            return View(model);
        }

        // POST: Staff/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffDeleteConfirmed(string id)
        {
            var staff = db.Staffs.Find(id);
            if (staff == null)
            {
                return NotFound();
            }

            db.Staffs.Remove(staff);
            db.SaveChanges();

            return RedirectToAction(nameof(StaffHome));
        }
    }
}
