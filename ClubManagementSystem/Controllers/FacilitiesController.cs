using ClubManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace ClubManagementSystem.Controllers
{
    public class FacilitiesController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;

        public FacilitiesController(DB db, IWebHostEnvironment en)
        {
            this.db = db;
            this.en = en;
        }

        // GET: Facilities/Facilities
        public IActionResult Facilities()
        {
            // Fetch all facilities along with related categories and timeslots
            var facilities = db.Facility
                .Select(f => new FacilityViewModel
                {
                    Id = f.Id,
                    Name = f.Name,
                    Image = f.Image,
                    Capacity = f.Capacity,
                    Description = f.Description,
                    IsActive = f.IsActive,
                    FacilityCategoriesId = f.FacilityCategoriesId,
                    CreatedAt = f.CreatedAt,
                    ModifiedAt = f.ModifiedAt,
                })
                .ToList();

            return View(facilities);
        }

        // GET: Facilities/CreateFacility
        public IActionResult CreateFacility()
        {
            ViewBag.FacilityCategories = new SelectList(db.FacilityCategories, "Id", "FacCategoryName");
            return View();
        }

        // POST: Facilities/CreateFacility
        [HttpPost]
        public IActionResult CreateFacility(FacilityViewModel vm, List<IFormFile> Image)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    List<string> uploadedImagePaths = new List<string>();

                    // Process images
                    if (Image != null && Image.Any())
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                        foreach (var image in Image)
                        {
                            if (image.Length > 0)
                            {
                                var fileExtension = Path.GetExtension(image.FileName).ToLower();

                                // Validate file format
                                if (!allowedExtensions.Contains(fileExtension))
                                {
                                    ModelState.AddModelError("Image", $"Invalid format for {image.FileName}. Only JPG, JPEG, and PNG are allowed.");
                                    ViewBag.FacilityCategories = new SelectList(db.FacilityCategories, "Id", "FacCategoryName");
                                    return View(vm);
                                }

                                // Upload image
                                string uploadsFolder = Path.Combine(en.WebRootPath, "images", "facilities");
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    image.CopyTo(fileStream);
                                }

                                uploadedImagePaths.Add(uniqueFileName);
                            }
                        }
                    }
                    // Save facility details
                    var facility = new Facility
                    {
                        Name = vm.Name.Trim(),
                        Capacity = vm.Capacity,
                        Description = vm.Description?.Trim(),
                        IsActive = vm.IsActive,
                        FacilityCategoriesId = vm.FacilityCategoriesId,
                        Image = string.Join(",", uploadedImagePaths), // Store paths as comma-separated string
                        CreatedAt = DateTime.Now
                    };

                    db.Facility.Add(facility);
                    db.SaveChanges();

                    TempData["Info"] = "Facility created successfully.";
                    return RedirectToAction("Facilities");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error occurred: {ex.Message}");
                }
            }

            ViewBag.FacilityCategories = new SelectList(db.FacilityCategories, "Id", "FacCategoryName");
            return View(vm);
        }


        // GET: Facilities/UpdateFacilities
        public IActionResult UpdateFacilities(int id)
        {
            var facility = db.Facility
                .Include(f => f.FacilityCategories)
                .FirstOrDefault(f => f.Id == id);

            if (facility == null)
            {
                return NotFound();
            }

            var vm = new FacilityViewModel
            {
                Id = facility.Id,
                Name = facility.Name,
                Image = facility.Image,
                Capacity = facility.Capacity,
                Description = facility.Description,
                IsActive = facility.IsActive,
                FacilityCategoriesId = facility.FacilityCategoriesId
            };

            ViewBag.FacilityCategories = new SelectList(db.FacilityCategories, "Id", "FacCategoryName", facility.FacilityCategoriesId);

            // Pass existing images to ViewBag
            ViewBag.ExistingImages = facility.Image?.Split(',') ?? [];

            return View(vm);
        }

        // POST: Facilities/UpdateFacilities
        [HttpPost]
        public IActionResult UpdateFacilities(FacilityViewModel vm, List<IFormFile> Image, string[] DeleteImages)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var facility = db.Facility.FirstOrDefault(f => f.Id == vm.Id);
                    if (facility == null)
                    {
                        return NotFound();
                    }

                    // Update basic details
                    facility.Name = vm.Name.Trim();
                    facility.Capacity = vm.Capacity;
                    facility.Description = vm.Description?.Trim();
                    facility.IsActive = vm.IsActive;
                    facility.FacilityCategoriesId = vm.FacilityCategoriesId;
                    facility.ModifiedAt = DateTime.Now;

                    string uploadsFolder = Path.Combine(en.WebRootPath, "images", "facilities");
                    List<string> updatedImagePaths = facility.Image?.Split(',').ToList() ?? new List<string>();

                    // Handle new image uploads
                    if (Image != null && Image.Any())
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                        foreach (var image in Image)
                        {
                            if (image.Length > 0)
                            {
                                var fileExtension = Path.GetExtension(image.FileName).ToLower();

                                if (!allowedExtensions.Contains(fileExtension))
                                {
                                    ModelState.AddModelError("Image", $"Invalid format for {image.FileName}. Only JPG, JPEG, and PNG are allowed.");
                                    ViewBag.FacilityCategories = new SelectList(db.FacilityCategories, "Id", "FacCategoryName", vm.FacilityCategoriesId);
                                    return View(vm);
                                }

                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    image.CopyTo(fileStream);
                                }

                                updatedImagePaths.Add(uniqueFileName);
                            }
                        }
                    }

                    // Handle image deletions
                    if (DeleteImages != null && DeleteImages.Any())
                    {
                        foreach (var imageName in DeleteImages)
                        {
                            updatedImagePaths.Remove(imageName);

                            string imagePath = Path.Combine(uploadsFolder, imageName);
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                    }

                    // Update image paths in database
                    facility.Image = string.Join(",", updatedImagePaths);

                    db.SaveChanges();
                    TempData["Info"] = "Facility updated successfully.";
                    return RedirectToAction("Facilities");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error occurred: {ex.Message}");
                }
            }

            ViewBag.FacilityCategories = new SelectList(db.FacilityCategories, "Id", "FacCategoryName", vm.FacilityCategoriesId);
            return View(vm);
        }

        // POST: Facilities/DeleteFacilities
        [HttpPost]
        public IActionResult DeleteFacilities(int id)
        {
            var f = db.Facility.Find(id);

            if (f != null)
            {
                try
                {
                    // Check if the facility has an image
                    if (!string.IsNullOrEmpty(f.Image))
                    {
                        // Split in case of multiple images (comma-separated)
                        var imagePaths = f.Image.Split(',');

                        foreach (var imagePath in imagePaths)
                        {
                            // Generate the full path
                            string filePath = Path.Combine(en.WebRootPath, "images", "facilities", imagePath);

                            // Delete the file if it exists
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }

                    // Delete the facility from the database
                    db.Facility.Remove(f);
                    db.SaveChanges();

                    TempData["Info"] = "Facility and associated images deleted.";
                }
                catch (Exception ex)
                {
                    TempData["Info"] = $"Error occurred: {ex.Message}";
                }
            }

            return Redirect(Request.Headers.Referer.ToString());
        }

        // POST: FacilityCategories/DeleteManyFacilityCategories
        [HttpPost]
        public IActionResult DeleteManyFacilities(int[] ids)
        {
            try
            {
                // Fetch facilities to get their image paths
                var facilities = db.Facility
                    .Where(f => ids.Contains(f.Id))
                    .ToList();

                foreach (var facility in facilities)
                {
                    if (!string.IsNullOrEmpty(facility.Image))
                    {
                        var imagePaths = facility.Image.Split(',');

                        foreach (var imagePath in imagePaths)
                        {
                            string filePath = Path.Combine(en.WebRootPath, "images", "facilities", imagePath);

                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }
                }

                // Execute delete after removing images
                int n = db.Facility
                    .Where(f => ids.Contains(f.Id))
                    .ExecuteDelete();

                TempData["Info"] = $"{n} facility record(s) and images deleted.";
            }
            catch (Exception ex)
            {
                TempData["Info"] = $"Error occurred: {ex.Message}";
            }
            return RedirectToAction("Facilities");
        }

    }
}
