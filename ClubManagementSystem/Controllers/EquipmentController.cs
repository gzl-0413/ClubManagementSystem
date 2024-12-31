using ClubManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementSystem.Controllers;

[Authorize(Roles = "Admin,Superadmin")]
public class EquipmentController : Controller
{
    private readonly DB db;

    public EquipmentController(DB db)
    {
        this.db = db;
    }
    public IActionResult CreateEquipment()
    {
        return View(new EquipmentViewModel());
    }

    [HttpPost]
    public IActionResult CreateEquipment(EquipmentViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var equipment = new Equipment
            {
                Name = vm.Name.Trim(),
                Stock = vm.Stock,
                DepositAmount = vm.DepositAmount,
                isDeleted = false,
            };

            db.Equipment.Add(equipment);
            db.SaveChanges();

            TempData["Info"] = "Equipment created successfully.";
            return RedirectToAction("ViewAllEquipment");
        }

        return View(vm);
    }

    public IActionResult ViewAllEquipment()
    {
        var equipmentViewModel = db.Equipment
            .Where(e => !e.isDeleted)
            .Select(e => new EquipmentViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Stock = e.Stock,
                DepositAmount = e.DepositAmount,
            })
            .ToList();

        return View(equipmentViewModel);
    }

    public IActionResult EditEquipment(int id)
    {
        var equipment = db.Equipment.FirstOrDefault(e => e.Id == id && !e.isDeleted);
        if (equipment == null)
        {
            return NotFound();
        }

        var vm = new EquipmentViewModel
        {
            Id = equipment.Id,
            Name = equipment.Name,
            Stock = equipment.Stock,
            DepositAmount = equipment.DepositAmount
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditEquipment(EquipmentViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var equipment = db.Equipment.FirstOrDefault(e => e.Id == vm.Id);
            if (equipment == null)
            {
                return NotFound();
            }

            equipment.Name = vm.Name.Trim();
            equipment.Stock = vm.Stock;
            equipment.DepositAmount = vm.DepositAmount;
            equipment.isDeleted = false;

            db.SaveChanges();

            TempData["Info"] = "Equipment updated successfully.";
            return RedirectToAction("ViewAllEquipment");
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult DeleteEquipment(int id)
    {
        var equipment = db.Equipment.FirstOrDefault(e => e.Id == id);
        if (equipment == null)
        {
            TempData["Error"] = "Equipment not found.";
            return RedirectToAction("ViewAllEquipment");
        }

        equipment.isDeleted = true;
        db.SaveChanges();

        TempData["Info"] = "Equipment deleted successfully.";
        return RedirectToAction("ViewAllEquipment");
    }

    public List<EquipmentViewModel> GetLowStockEquipment()
    {
        return ViewBag.LowStockItems = db.Equipment
        .Where(e => e.Stock < 5)
        .Select(e => new EquipmentViewModel
        {
            Id = e.Id,
            Name = e.Name,
            Stock = e.Stock,
            DepositAmount = e.DepositAmount
        })
        .ToList();
    }
}
