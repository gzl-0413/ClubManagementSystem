using ClubManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementSystem.Controllers;

[Authorize(Roles = "Admin,Superadmin")]
public class EquipmentRentalController : Controller
{
    private readonly DB db;

    public EquipmentRentalController(DB db)
    {
        this.db = db;
    }

    public IActionResult ListRentals()
    {
        var rentals = db.EquipmentRental
            .Include(r => r.FacBooking) 
            .Include(r => r.Equipment)  
            .ToList();

        var rentalViewModels = rentals.Select(r => new EquipmentRentalViewModel
        {
            Id = r.Id,
            EquipmentId = r.EquipmentId,
            Quantity = r.Quantity,
            DepositPaid = r.DepositPaid,
            RentedAt = r.RentedAt,
            FacBookingId = r.FacBookingId,
         
        }).ToList();

        return View(rentalViewModels);
    }
    public IActionResult CreateRental()
    {
        ViewBag.FacilityBookings = new SelectList(
            db.FacBooking.Where(f => !f.isDeleted && f.BookingDate >= DateOnly.FromDateTime(DateTime.Now)),
            "Id", "Name"
        );

        ViewBag.Equipment = db.Equipment
        .Where(e => e.Stock > 0)
        .Select(e => new EquipmentViewModel
        {
            Id = e.Id,
            Name = e.Name,
            DepositAmount = e.DepositAmount
        }).ToList();

        return View();
    }

    [HttpPost]
    public IActionResult CreateRental(EquipmentRentalViewModel vm)
    {
        var facilityBooking = db.FacBooking
    .FirstOrDefault(fb => fb.Id == vm.FacBookingId && fb.BookingDate == DateOnly.FromDateTime(DateTime.Now));

        if (facilityBooking == null)
        {
            ModelState.AddModelError("", "You must have a valid facility booking on the selected date.");
            return View(vm);
        }

        var equipment = db.Equipment.FirstOrDefault(e => e.Id == vm.EquipmentId);

        if (equipment == null)
        {
            ModelState.AddModelError("", "Selected equipment does not exist.");
        }
        else if (equipment.Stock < vm.Quantity)
        {
            ModelState.AddModelError("", "Insufficient stock available for the selected equipment.");
        }

        if (ModelState.IsValid)
        {
            var rental = new EquipmentRental
            {
                FacBookingId = vm.FacBookingId,
                EquipmentId = vm.EquipmentId,
                Quantity = vm.Quantity,
                DepositPaid = vm.DepositPaid,
                RentedAt = DateTime.Now
            };
            equipment.Stock -= vm.Quantity;
            db.Equipment.Update(equipment);
            db.EquipmentRental.Add(rental);
            db.SaveChanges();

            TempData["Info"] = "Rental created successfully.";
            return RedirectToAction("ListRentals");
        }

        ViewBag.FacilityBookings = new SelectList(
            db.FacBooking.Where(f => !f.isDeleted),
            "Id", "FacilityName"
        );

        ViewBag.Equipments = new SelectList(
            db.Equipment.Where(e => e.Stock > 0),
            "Id", "Name", "DepositAmount"
        );

        return View(vm);
    }

    [HttpGet]
    public IActionResult GetDepositAmount(int equipmentId)
    {
        var equipment = db.Equipment.FirstOrDefault(e => e.Id == equipmentId);

        if (equipment != null)
        {
            return Json(equipment.DepositAmount);
        }

        return Json(0); // Default to 0 if the equipment is not found
    }

    public IActionResult EditRental(int id)
    {
        var rental = db.EquipmentRental
            .Include(r => r.FacBooking)
            .Include(r => r.Equipment)
            .FirstOrDefault(r => r.Id == id);

        if (rental == null)
        {
            return NotFound();
        }

        var vm = new EquipmentRentalViewModel
        {
            Id = rental.Id,
            EquipmentId = rental.EquipmentId,
            Quantity = rental.Quantity,
            DepositPaid = rental.DepositPaid,
            RentedAt = rental.RentedAt,
            FacBookingId = rental.FacBookingId,
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditRental(EquipmentRentalViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var rental = db.EquipmentRental.FirstOrDefault(r => r.Id == vm.Id);

            if (rental == null)
            {
                return NotFound();
            }

            // Update rental details
            rental.Quantity = vm.Quantity;
            rental.DepositPaid = vm.DepositPaid;

            db.SaveChanges();

            TempData["Info"] = "Rental updated successfully.";
            return RedirectToAction("ListRentals");
        }

        // If validation fails, return the same view with validation errors
        return View(vm);
    }

    [HttpPost]
    public IActionResult CancelRental(int id)
    {
        var rental = db.EquipmentRental.Find(id);

        if (rental == null)
        {
            TempData["Info"] = "Rental not found.";
        }

        // Update equipment stock
        var equipment = db.Equipment.Find(rental.EquipmentId);
        if (equipment != null)
        {
            equipment.Stock += rental.Quantity;
            db.SaveChanges();
        }

        db.EquipmentRental.Remove(rental);
        db.SaveChanges();

        TempData["Info"] = "Rental cancelled successfully.";
        return RedirectToAction("ListRentals");
    }

}
