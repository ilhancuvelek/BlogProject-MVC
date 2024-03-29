﻿using BlogProject.EmailServices;
using BlogProject.Extensions;
using BlogProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BlogProject.Identity;
using System.Threading.Tasks;

namespace BlogProject.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;
        private IEmailSender _emailSender;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login(string ReturnUrl=null)
        {
            return View(new LoginModel() { ReturnUrl=ReturnUrl});
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                return View(loginModel);
            }
            var user = await _userManager.FindByEmailAsync(loginModel.Email);
            if (user==null)
            {
                ModelState.AddModelError("", "Bu email ile daha önce hesap açılmamış.");
                return View(loginModel);
            }
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "Lütfen email hasabınıza gelen link ile hesabınızı onaylayın.");
                return View(loginModel);

            }
            var result = await _signInManager.PasswordSignInAsync(user, loginModel.Password, true, false);
            if (result.Succeeded)
            {
                return Redirect(loginModel.ReturnUrl??"~/");
            }
            ModelState.AddModelError("", "Girilen email veya parola yanlış.");
            return View(loginModel);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel registerModel)
        {
            if (!ModelState.IsValid)
            {
                return View(registerModel);
            }
            var user = new User()
            {
                Email = registerModel.Email,
                UserName=registerModel.UserName,
                FirstName=registerModel.FirstName,
                LastName=registerModel.LastName
            };
            var result = await _userManager.CreateAsync(user, registerModel.Password);
            if (result.Succeeded)
            {
                //generate code
                var code=await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var url = Url.Action("ConfirmEmail", "Account", new
                {
                    userId=user.Id,
                    token=code
                });
                //email
                await _emailSender.SendEmailAsync(registerModel.Email, "Hesabınızı onaylayınız.", $"Lütfen email hesabınızı onaylamak için linke <a href='https://localhost:5001{url}'>tıklayınız.</a>");
                return RedirectToAction("Login", "Account");
            }
            ModelState.AddModelError("", "Bilinmeyen bir hata oluştu.Lütfen tekrar deneyiniz.");
            return View(registerModel);
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("~/");
        }

        public async Task<IActionResult> ConfirmEmail(string userId,string token)
        {
            if (userId == null || token == null)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Geçersiz token",
                    Message = "Geçersiz token.",
                    AlertType = "danger"
                });
                return View();
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    TempData.Put("message", new AlertMessage()
                    {
                        Title= "Hesabınız Onaylandı",
                        Message= "Hesabınız Onaylandı.",
                        AlertType= "success"
                    });
                    return View();
                }
            }
            TempData.Put("message", new AlertMessage()
            {
                Title = "Hesabınız Onaylanmadı",
                Message = "Hesabınız Onaylanmadı.",
                AlertType = "warning"
            });
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                return View(); 
            }
            var user = await _userManager.FindByEmailAsync(Email);
            if (user==null)
            {
                return View();
            }
            //generate code
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action("ResetPassword", "Account", new
            {
                token=code,
                userId=user.Id
            });
            //email
            await _emailSender.SendEmailAsync(Email, "Reset Password", $"Parolanızı Yenilemek için linke <a href='https://localhost:5001{url}'>tıklayınız.</a>");
            return View();
        }
        public IActionResult ResetPassword(string userId, string token)
        {
            if (userId==null||token==null)
            {
                return RedirectToAction("Index", "Home");
            }
            var model = new ResetPasswordModel() { Token = token };
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel resetPasswordModel)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordModel);
            }
            var user = await _userManager.FindByEmailAsync(resetPasswordModel.Email);
            if (user==null)
            {
                return RedirectToAction("Index", "Home");
            }
            var result = await _userManager.ResetPasswordAsync(user, resetPasswordModel.Token, resetPasswordModel.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("Login", "Account");
            }
            return View(resetPasswordModel);
        }


        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
