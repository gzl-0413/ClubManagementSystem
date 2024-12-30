using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using static ClubManagementSystem.Models.StaffVM;

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

        // GET: Staff/Add
        public IActionResult StaffAdd()
        {
            var model = new StaffVM.Create();
            return View(model);
        }

        // POST: Staff/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffAdd(StaffVM.Create vm, string[] WorkTime)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "There are validation errors. Please check the form.";
                return View(vm);
            }

            try
            {
                var staffCount = db.Staffs.Count();
                var staffId = "ST" + (staffCount + 1).ToString("D4");
                string? photoUrl = null;

                var selectedWorkTimes = string.Join(",", WorkTime);

                var staff = new Staff
                {
                    Id = staffId,
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    Type = vm.Type,
                    WorkTime = selectedWorkTimes,
                    PhotoURL = photoUrl
                };

                if (vm.Photo != null)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.Photo.FileName);
                    var path = Path.Combine(en.WebRootPath, "StaffImg", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        vm.Photo.CopyTo(stream);
                    }

                    staff.PhotoURL = fileName;
                }

                db.Staffs.Add(staff);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Staff member added successfully with ID: " + staffId;
                return RedirectToAction(nameof(StaffHome));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while adding the staff member: " + ex.Message;
                return View(vm);
            }
        }

        // GET: Staff/Edit/{id}
        public IActionResult StaffEdit(string id)
        {
            try
            {
                var staff = db.Staffs.Find(id);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = $"Staff member with ID {id} not found.";
                    return NotFound();
                }

                var model = new StaffVM.Edit
                {
                    Id = staff.Id,
                    Name = staff.Name,
                    Email = staff.Email,
                    Type = staff.Type,
                    WorkTime = staff.WorkTime,
                    PhotoURL = staff.PhotoURL
                };

                TempData["DebugMessage"] = $"Staff data for {staff.Name} (ID: {id}) successfully loaded for editing.";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while fetching staff data: {ex.Message}";
                return View("Error");
            }
        }

        // POST: Staff/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffEdit(StaffVM.Edit model, string[] WorkTime)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "There are validation errors. Please check the form.";
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        TempData["DebugMessage"] += $"Validation Error: {error.ErrorMessage}\n";
                    }
                    return View(model);
                }

                var staff = db.Staffs.Find(model.Id);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = $"Staff member with ID {model.Id} not found.";
                    return NotFound();
                }

                staff.Name = model.Name;
                staff.Email = model.Email;
                staff.Type = model.Type;
                staff.WorkTime = WorkTime != null && WorkTime.Length > 0 ? string.Join(",", WorkTime) : staff.WorkTime;

                if (model.Photo != null)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Photo.FileName);
                    var path = Path.Combine(en.WebRootPath, "StaffImg", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        model.Photo.CopyTo(stream);
                    }

                    staff.PhotoURL = fileName;
                }

                db.Staffs.Update(staff);
                db.SaveChanges();

                TempData["SuccessMessage"] = $"Staff member {staff.Name} updated successfully.";
                return RedirectToAction(nameof(StaffHome));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the staff member: {ex.Message}";
                return View(model);
            }
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
                Email = staff.Email,
                Type = staff.Type
            };

            return View(model);
        }

        // POST: Staff/DeleteConfirmed
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

            TempData["SuccessMessage"] = "Staff member deleted successfully.";
            return RedirectToAction(nameof(StaffHome));
        }

        // GET: Staff/WorkTime
        public IActionResult StaffWorkTime()
        {
            try
            {
                var staffList = db.Staffs.ToList();
                var model = staffList.Select(staff => new StaffVM.WorkTimeViewModel
                {
                    Id = staff.Id,
                    Name = staff.Name,
                    Type = staff.Type,
                    WorkTime = staff.WorkTime?.Split(',').ToList()
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while fetching staff data: {ex.Message}";
                return View("Error");
            }
        }

    }
}