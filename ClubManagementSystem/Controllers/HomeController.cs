using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;

    public HomeController(DB db, IWebHostEnvironment en)
    {
        this.db = db;
        this.en = en;
    }

    // GET: Home/Index
    public IActionResult Index()
    {
        return View();
    }

    // GET: Home/ViewFacilityCategory
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

    // GET: Home/CreateFacilityCategory
    public IActionResult CreateFacilityCategory()
    {
        return View();
    }

    // POST: Home/CreateFacilityCategory
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

    // GET: Home/Update
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


    // POST: Home/Update
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

    // POST: Home/Delete
    [HttpPost]
    public IActionResult DeleteFacilityCategories(int id)
    {
        var c = db.FacilityCategories.Find(id);

        if (c != null)
        {
            // TODO
            db.FacilityCategories.Remove(c);
            db.SaveChanges();

            TempData["Info"] = "Record deleted.";
        }

        return Redirect(Request.Headers.Referer.ToString());
    }

    // POST: Home/DeleteMany
    [HttpPost]
    public IActionResult DeleteManyFacilityCategories(int[] ids)
    {
        try
        {
            // Execute delete
            int n = db.FacilityCategories
                .Where(c => ids.Contains(c.Id))
                .ExecuteDelete();

            TempData["Info"] = $"{n} record(s) deleted.";
        }
        catch (Exception ex)
        {
            // Handle any errors
            TempData["Info"] = $"Error occurred: {ex.Message}";
        }
        return RedirectToAction("FacilityCategories");
    }
}
