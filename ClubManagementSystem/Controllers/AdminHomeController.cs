using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult AdminHome()
    {
        return View();
    }
}
