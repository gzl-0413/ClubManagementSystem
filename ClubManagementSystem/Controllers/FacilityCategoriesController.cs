using Microsoft.AspNetCore.Mvc;

namespace ClubManagementSystem.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class FacilityCategoriesController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;

    public FacilityCategoriesController(DB db, IWebHostEnvironment en)
    {
        this.db = db;
        this.en = en;
    }

    // GET: FacilityCategories/FacilityCategories
    public IActionResult FacilityCategories()
    {
        var categories = db.FacilityCategories
            .Include(fc => fc.Facility) // Include facilities for count
            .ToList();

        var viewModel = categories.Select(c => new FacilityCategoryViewModel
        {
            Id = c.Id,
            FacCategoryName = c.FacCategoryName,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            ModifiedAt = c.ModifiedAt,
            FacilityCount = c.Facility.Count // Calculate the number of facilities
        }).ToList();

        return View(viewModel);
    }

    // GET: FacilityCategories/CreateFacilityCategory
    public IActionResult CreateFacilityCategory()
    {
        return View();
    }

    // POST: FacilityCategories/CreateFacilityCategory
    [HttpPost]
    public IActionResult CreateFacilityCategory(FacilityCategories model)
    {
        if (ModelState.IsValid)
        {
            model.CreatedAt = DateTime.Now;
            db.FacilityCategories.Add(model);
            db.SaveChanges();
            TempData["Success"] = "Facility Category created successfully!";
            return RedirectToAction("FacilityCategories");
        }

        return View(model);
    }

    // GET: FacilityCategories/UpdateFacilityCategories
    [HttpGet]
    public IActionResult UpdateFacilityCategories(int id)
    {
        // Find the existing category by Id
        var category = db.FacilityCategories.FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        var vm = new FacilityCategories
        {
            Id = category.Id,
            FacCategoryName = category.FacCategoryName,
            Description = category.Description,
        };

        return View(category);
    }


    // POST: FacilityCategories/UpdateFacilityCategories
    [HttpPost]
    public IActionResult UpdateFacilityCategories(FacilityCategories model)
    {
        // Fetch the existing record
        var existingCategory = db.FacilityCategories.FirstOrDefault(c => c.Id == model.Id);

        if (existingCategory == null)
        {
            return NotFound();
        }

        // Update only if the new value is provided
        if (!string.IsNullOrEmpty(model.FacCategoryName) && model.FacCategoryName != existingCategory.FacCategoryName)
        {
            existingCategory.FacCategoryName = model.FacCategoryName;
        }

        if (!string.IsNullOrEmpty(model.Description) && model.Description != existingCategory.Description)
        {
            existingCategory.Description = model.Description;
        }

        // Always update the ModifiedAt timestamp
        existingCategory.ModifiedAt = DateTime.Now;

        // Save the changes to the database
        db.SaveChanges();

        TempData["Success"] = "Facility Category updated successfully!";
        return RedirectToAction("FacilityCategories"); // Redirect back to the listing
    }

    // POST: FacilityCategories/DeleteFacilityCategories
    [HttpPost]
    public IActionResult DeleteFacilityCategories(int id)
    {
        var category = db.FacilityCategories.Find(id);

        if (category != null)
        {
            bool hasRelatedFacilities = db.Facility.Any(f => f.FacilityCategoriesId == id);

            if (hasRelatedFacilities)
            {
                TempData["Info"] = "Cannot delete category. Facilities are linked to it.";
            }
            else
            {
                db.FacilityCategories.Remove(category);
                db.SaveChanges();
                TempData["Info"] = "Record deleted.";
            }
        }

        return Redirect(Request.Headers.Referer.ToString());
    }

    // POST: FacilityCategories/DeleteManyFacilityCategories
    [HttpPost]
    public IActionResult DeleteManyFacilityCategories(int[] ids)
    {
        try
        {
            // Check for categories linked to facilities
            var linkedFacilities = db.Facility
                .Where(f => ids.Contains(f.FacilityCategoriesId))
                .Select(f => f.FacilityCategoriesId)
                .Distinct()
                .ToList();

            if (linkedFacilities.Any())
            {
                TempData["Info"] = "Cannot delete. Some categories have linked facilities.";
            }
            else
            {
                int n = db.FacilityCategories
                    .Where(c => ids.Contains(c.Id))
                    .ExecuteDelete();

                TempData["Info"] = $"{n} record(s) deleted.";
            }
        }
        catch (Exception ex)
        {
            TempData["Info"] = $"Error occurred: {ex.Message}";
        }

        return RedirectToAction("FacilityCategories");
    }
}
