using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClubManagementSystem.Models;
using X.PagedList.Extensions;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using System.Security.Claims;

namespace ClubManagementSystem.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly DB _context;
    private readonly Helper _helper;

    public AdminController(DB context, Helper helper)
    {
        _context = context;
        _helper = helper;
    }

    // AdminHome: Dashboard for admins
    public IActionResult AdminHome()
    {
        // Get the list of admins
        var admins = _context.Admins.ToList();

        // Calculate member statistics
        var totalMembers = _context.Members.Count();
        var activeMembers = _context.Members.Count(m => m.IsActivated);
        var inactiveMembers = totalMembers - activeMembers;

        // Pass data to the view using ViewBag
        ViewBag.TotalMembers = totalMembers;
        ViewBag.ActiveMembers = activeMembers;
        ViewBag.InactiveMembers = inactiveMembers;

        return View(admins);
    }


    // AdminPage: List all admins
    public IActionResult AdminPage(string name, string sort, string dir, int page = 1)
    {

        // Query admins and apply search filtering if necessary
        IQueryable<Admin> admins = _context.Admins.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            admins = admins.Where(a => a.Email.Contains(name) || a.Name.Contains(name));
        }

        // Paginate the results
        var pagedAdmins = admins.ToPagedList(page, 10); // Show 10 results per page

        // Pass parameters to the view for sorting and searching
        ViewBag.Name = name;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        return View(pagedAdmins);
    }



    // AdminCreate: Display form to create a new admin
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult AdminCreate()
    {
        return View();
    }


    [HttpPost]
    public IActionResult AdminCreate(AdminCreateVM vm)
    {
        if (ModelState.IsValid)
        {
            // Check if the logged-in user is a SuperAdmin
            var loggedInUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (loggedInUserRole != "SuperAdmin")
            {
                TempData["Error"] = "You do not have permission to create an admin account.";
                return RedirectToAction("AdminPage");
            }

            // Check if email already exists
            if (_context.Admins.Any(a => a.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(vm);
            }

            // Create admin entity without PhotoURL
            var admin = new Admin
            {
                Email = vm.Email,
                Hash = _helper.HashPassword(vm.Password),
                Name = vm.Name,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "System",
                ModifiedAt = DateTime.Now,
                ModifiedBy = User.Identity?.Name ?? "System",
                IsActivated = true,
                ActivationCode = string.Empty // Default value instead of null
            };

            _context.Admins.Add(admin);
            _context.SaveChanges();

            TempData["Info"] = "Admin account created successfully.";
            return RedirectToAction("AdminPage");
        }
        return View(vm);
    }


    // AdminEdit: Display form to edit an existing admin
    public IActionResult AdminEdit(string email)
    {
        var admin = _context.Admins.Find(email);
        if (admin == null)
        {
            return NotFound();
        }

        // Populate the Edit form with the current admin's details
        var vm = new UpdateProfileVM
        {
            Email = admin.Email,
            Name = admin.Name
        };

        return View(vm);
    }
    [HttpPost]
    public IActionResult AdminEdit(UpdateProfileVM vm)
    {
        if (ModelState.IsValid)
        {
            var admin = _context.Admins.Find(vm.Email);
            if (admin != null)
            {
                // Update the admin's name
                admin.Name = vm.Name;
                admin.ModifiedAt = DateTime.Now;
                admin.ModifiedBy = User.Identity.Name;

                // Save changes to the database
                _context.SaveChanges();

                TempData["Info"] = "Admin profile updated successfully.";
                return RedirectToAction("AdminPage");
            }
            else
            {
                TempData["Error"] = "Admin not found.";
            }
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult DeleteAdmin(string email)
    {
        // Get the currently logged-in admin's email
        var loggedInAdminEmail = User.Identity.Name;

        // Check if the logged-in admin is trying to delete their own account
        if (email == loggedInAdminEmail)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction("AdminPage");
        }

        var admin = _context.Admins.Find(email);
        if (admin != null)
        {
            _context.Admins.Remove(admin);
            _context.SaveChanges();
            TempData["Info"] = "Admin deleted successfully.";
        }
        else
        {
            TempData["Error"] = "Admin not found.";
        }

        return RedirectToAction("AdminPage");
    }

    public async Task<IActionResult> MemberPage(string name = null, string sort = "Email", string dir = "asc", int page = 1)
    {
        var members = _context.Members.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            members = members.Where(m => m.Name.Contains(name) || m.Email.Contains(name));
        }

        // Sorting logic
        if (sort == "Name")
        {
            members = dir == "asc" ? members.OrderBy(m => m.Name) : members.OrderByDescending(m => m.Name);
        }
        else if (sort == "CreatedAt")
        {
            members = dir == "asc" ? members.OrderBy(m => m.CreatedAt) : members.OrderByDescending(m => m.CreatedAt);
        }
        else
        {
            members = dir == "asc" ? members.OrderBy(m => m.Email) : members.OrderByDescending(m => m.Email);
        }

        // Pagination
        var pageSize = 10;
        var totalMembers = await members.CountAsync();
        var pagedMembers = await members.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Name = name;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        return View(new StaticPagedList<Member>(pagedMembers, page, pageSize, totalMembers));
    }

    [HttpGet]
    public IActionResult MemberEdit(string email)
    {
        // Find the member by email
        var member = _context.Members.FirstOrDefault(m => m.Email == email);

        if (member == null)
        {
            return NotFound();
        }

        // Create a ViewModel for the member
        var viewModel = new EditMemberVM
        {
            Email = member.Email,
            Name = member.Name,
            IsActivated = member.IsActivated
        };

        return View(viewModel); // Return the MemberEdit view with the member data
    }

    // POST: MemberEdit - Update the member details after the form is submitted
    [HttpPost]
    public IActionResult MemberEdit(EditMemberVM model)
    {
        if (ModelState.IsValid)
        {
            // Find the member by email
            var member = _context.Members.FirstOrDefault(m => m.Email == model.Email);

            if (member == null)
            {
                return NotFound();
            }

            // Update the member's information
            member.Name = model.Name;
            member.IsActivated = model.IsActivated;

            // Save changes to the database
            _context.SaveChanges();

            // Redirect to the member list or another page after update
            TempData["Info"] = "Member updated successfully!";
            return RedirectToAction("MemberPage");
        }

        // If validation fails, return the form with errors
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteMember(string email)
    {
        var member = _context.Members.FirstOrDefault(m => m.Email == email);

        if (member == null)
        {
            TempData["Error"] = "Member not found.";
            return RedirectToAction("MemberPage");
        }

        _context.Members.Remove(member);
        _context.SaveChanges();

        TempData["Info"] = "Member has been successfully deleted.";
        return RedirectToAction("MemberPage");
    }

}
