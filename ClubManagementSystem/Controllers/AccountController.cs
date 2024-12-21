using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace ClubManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public AccountController(DB db, IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        var u = db.Users.Find(vm.Email);

        // Validate user credentials
        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
        }

        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";

            // Sign in the user
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            // Redirect based on the role
            if (u.Role == "Admin")
            {
                return RedirectToAction("AdminHome", "Admin");
            }

            // If no specific role, use returnURL or default to Home/Index
            if (!string.IsNullOrEmpty(returnURL))
            {
                return Redirect(returnURL);
            }

            return RedirectToAction("Index", "Home");
        }

        return View(vm);
    }


    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        hp.SignOut();

        return RedirectToAction("Login");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        // Check for duplicate email
        if (ModelState.IsValid("Email") &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        // Validate photo (but do not save it yet)
        if (ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "")
            {
                ModelState.AddModelError("Photo", err);
            }
        }

        // Only proceed if the entire ModelState is valid
        if (ModelState.IsValid)
        {
            var now = DateTime.UtcNow;
            string? photoUrl = null;

            // Save the photo only after all validations pass
            if (vm.Photo != null)
            {
                photoUrl = hp.SavePhoto(vm.Photo, "photos");
            }

            // Add the new member to the database
            db.Members.Add(new()
            {
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                Name = vm.Name,
                PhotoURL = photoUrl,
                CreatedAt = now,
                CreatedBy = "System",
                ModifiedAt = now,
                ModifiedBy = "System",
            });

            db.SaveChanges();

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        // If there are validation errors, return to the form
        return View(vm);
    }



    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }

        if (ModelState.IsValid)
        {
            u.Hash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View();
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var m = db.Members.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Name = m.Name,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Members.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            m.Name = vm.Name;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photos");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction();
        }

        vm.Email = m.Email;
        vm.PhotoURL = m.PhotoURL;
        return View(vm);
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.Find(vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            string password = hp.RandomPassword();

            u!.Hash = hp.HashPassword(password);
            db.SaveChanges();

            // Send reset password email
            SendResetPasswordEmail(u, password);

            TempData["Info"] = $"Password reset. Check your email.";
            return RedirectToAction();
        }

        return View();
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        // TODO
        var url = Url.Action("Login", "Account", null, "https");

        var path = u switch 
            {
                Admin => Path.Combine (en. WebRootPath, "photos", "admin.jpg"), 
                Member m => Path.Combine (en. WebRootPath, "photos", m. PhotoURL), 
               _  =>"" };

        var att = new Attachment(path);
        mail.Attachments.Add(att);
        // TODO
        att.ContentId = "photo";

        mail.Body = $@"
            <img src='cid:photo' style='width: 200px; height: 200px;
                                        border: 1px solid #333'>
            <p>Dear {u.Name},<p>
            <p>Your password has been reset to:</p>
            <h1 style='color: red'>{password}</h1>
            <p>
                Please <a href='{url}'>login</a>
                with your new password.
            </p>
            <p>From, 🐱 Super Admin</p>
        ";

        // TODO
        hp.SendEmail(mail);
    }
}
