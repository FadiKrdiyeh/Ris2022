﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ris2022.Data;
using Ris2022.Data.Models;
using Ris2022.Data.ViewModel;
using Ris2022.Pages.Shared;
using Ris2022.Pages.Account;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.ReportingServices.ReportProcessing.ReportObjectModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Security.Claims;
using System;

namespace Ris2022.Controllers
{
    public class AdministrationController : Controller
    {
        private readonly RisDBContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<RisAppUser> userManager;

        public AdministrationController(RisDBContext context,
                                        RoleManager<IdentityRole> roleManager,
                                        UserManager<RisAppUser> userManager)
        {
            _roleManager = roleManager;
            this.userManager = userManager;
            _context = context;
            
        }

        [HttpGet]
        [Authorize(Policy = "Index+DetailsUsersPolicy")]
        public IActionResult ListUsers()
        {
            var users = userManager.Users;
            return View(users);
        }

        [HttpGet]
        [Authorize(Policy = "EditUsersPolicy")]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if(user == null)
            {
                ViewBag.ErrorMessage = $"User With ID: {id} Cannot Be Found";
                return View("NotFound");
            }

            var userClaims = await userManager.GetClaimsAsync(user);
            var userRoles = await userManager.GetRolesAsync(user);

            var model = new EditUserViewModel()
            {
                Id = user.Id,
                //Email = user.Email,
                UserName = user.UserName,
                FirstName = user.Firstname,
                LastName = user.Lastname,
                Claims = userClaims.ToList(),
                Roles = userRoles
            };

            return View(model);
        }
        [HttpPost]
        [Authorize(Policy = "EditUsersPolicy")]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.Id);

            if(user == null)
            {
                ViewBag.ErrorMessage = $"User With ID: {model.Id} Cannot Be Found";
                return View("NotFound");
            }
            else
            {
                //user.Email = model.Email;
                user.UserName = model.UserName;
                user.Firstname = model.FirstName;
                user.Lastname = model.LastName;

                var result = await userManager.UpdateAsync(user);

                if(result.Succeeded)
                {
                    return RedirectToAction("ListUsers");
                }

                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Policy = "ManageClaimsUsersPolicy")]
        public async Task<IActionResult> ManageUserClaims(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                ViewBag.ErrorMessage = $"User With ID: {userId} Cannot Be Found";
                return View("NotFound");
            }

            var existingUserClaims = await userManager.GetClaimsAsync(user);

            var model = new UserClaimsViewModel
            {
                UserId = user.Id,
            };

            foreach (Claim claim in ClaimsStore.AllClaims)
            {
                //string[] strings = claim.Type.Split("-->");
                string claimTitle = claim.Type.Split("-->").Skip(1).First();

                UserClaim userClaim = new UserClaim
                {
                    ClaimType = claim.Type,
                    //ClaimTitle = claim.Type.Split("-->").Skip(1).First()
                    ClaimTitle = claimTitle
                };

                if (existingUserClaims.Any(c => c.Type == claim.Type && c.Value == "true"))
                {
                    userClaim.IsSelected = true;
                }
                model.Claims.Add(userClaim);
            }
            ViewBag.UserName = user.UserName;

            return View(model);
        }
        [HttpPost]
        [Authorize(Policy = "ManageClaimsUsersPolicy")]
        public async Task<IActionResult> ManageUserClaims(UserClaimsViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                ViewBag.ErrorMessage = $"User With ID: {model.UserId} Cannot Be Found";
                return View("NotFound");
            }

            var claims = await userManager.GetClaimsAsync(user);
            var result = await userManager.RemoveClaimsAsync(user, claims);

            if(!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Cannot remove user existing claims");
                return View(model);
            }

            result = await userManager.AddClaimsAsync(user, model.Claims.Select(c => new Claim(c.ClaimType, c.IsSelected ? "true" : "false")));

            if(!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Canot add selected roles to user");
                return View(model);
            }

            return RedirectToAction("EditUser", new { Id = model.UserId });
        }

        [HttpGet]
        [Authorize(Policy = "CreateRolesPolicy")]
        public IActionResult CreateRole()
        {
            return View();
        }
        [HttpPost]
        [Authorize(Policy = "CreateRolesPolicy")]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                IdentityRole identityRole = new IdentityRole() { Name = model.RoleName };
                IdentityResult result = await _roleManager.CreateAsync(identityRole);

                if (result.Succeeded)
                {
                    return RedirectToAction("ListRoles", "administration");
                }
                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError(String.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = "Index+DetailsRolesPolicy")]
        public IActionResult ListRoles()
        {
            var roles = _roleManager.Roles;
            return View(roles);
        }

        [HttpGet]
        [Authorize(Policy = "EditRolesPolicy")]
        public async Task<IActionResult> EditRole(string id)
        {
           var role=await _roleManager.FindByIdAsync(id);

            if (role==null)
            {
                ViewBag.ErrorMessage = $"Role with Id={id} cannot be Found";
                return RedirectToAction("NotFound","Administration");
            }

            var model = new EditRoleViewModel
            {
                id = role.Id,
                RoleName = role.Name
            };

            foreach (var user in userManager.Users)
            {
                if (await userManager.IsInRoleAsync(user, role.Name))
                    model.Users.Add(user.UserName);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "EditRolesPolicy")]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.id);

            if (role == null)
            {
                ViewBag.ErrorMessage = $"Role with Id={model.id} cannot be Found";
                return RedirectToAction("NotFound", "Administration");
            }
            else
            {
                role.Name = model.RoleName;
                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    return RedirectToAction("ListRoles");
                }

                foreach (var error in result.Errors )
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            
        }

        [HttpGet]
        [Authorize(Policy = "EditUserInRolesPolicy")]
        public async Task<IActionResult> EditUsersInRole(string RoleId)
        {
            ViewBag.roleId = RoleId;

            var role = await _roleManager.FindByIdAsync(RoleId);

            ViewBag.RoleName = role.Name;

            if (role == null)
            {
                ViewBag.ErrorMessage = $"Role with Id={RoleId} cannot be Found";
                return RedirectToAction("NotFound", "Administration");
            }

            var model = new List<UserRoleViewModel>();

            foreach (var user in userManager.Users)
            {
                var userRoleViewModel = new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName
                };

                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    userRoleViewModel.IsSelected = true;
                }
                else
                {
                    userRoleViewModel.IsSelected = false;

                }

                model.Add(userRoleViewModel);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "EditUserInRolesPolicy")]
        public async Task<IActionResult> EditUsersInRole(List<UserRoleViewModel> model,string RoleId)
        {

            var role = await _roleManager.FindByIdAsync(RoleId);

            if (role == null)
            {
                ViewBag.ErrorMessage = $"Role with Id={RoleId} cannot be Found";
                return RedirectToAction("NotFound", "Administration");
            }

            for (int i=0; i<model.Count; i++)
            {
                var user = await userManager.FindByIdAsync(model[i].UserId);

                IdentityResult result = null;

                if (model[i].IsSelected && !(await userManager.IsInRoleAsync(user,role.Name)))
                {
                    result = await userManager.AddToRoleAsync(user, role.Name);
                }

                else if (!model[i].IsSelected && await userManager.IsInRoleAsync(user, role.Name))
                {
                    result = await userManager.RemoveFromRoleAsync(user, role.Name);
                }

                else
                {
                    continue;
                }

                if (result.Succeeded)
                {
                    if (i < (model.Count - 1))
                        continue;
                    else 
                        return RedirectToAction("EditRole",new {id = RoleId });
                }
            }

            return RedirectToAction("EditRole", new { id = RoleId });
            return View();
        }

        [HttpGet]
        [Authorize(Policy = "ManageRolesUsersPolicy")]
        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            ViewBag.UserId = userId;
            ViewBag.UserName = user.UserName;

            if(user == null)
            {
                ViewBag.ErrorMessage = $"User With ID: {userId} Cannot Be Found";
                return View("NotFound");
            }

            var model = new List<UserRolesViewModel>();

            foreach(var role in _roleManager.Roles)
            {
                var userRolesViewModel = new UserRolesViewModel()
                {
                    RoleId= role.Id,
                    RoleName = role.Name,
                };

                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesViewModel.IsSelected = true;
                }
                else
                {
                    userRolesViewModel.IsSelected = false;
                }
                model.Add(userRolesViewModel);
            }
            return View(model);
        }
        [HttpPost]
        [Authorize(Policy = "ManageRolesUsersPolicy")]
        public async Task<IActionResult> ManageUserRoles(List<UserRolesViewModel> model, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                ViewBag.ErrorMessage = $"User With ID: {userId} Cannot Be Found";
                return View("NotFound");
            }

            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded) 
            {
                ModelState.AddModelError(string.Empty, "Cannot remove user existing roles");
                return View(model);
            }

            result = await userManager.AddToRolesAsync(user, model.Where(x => x.IsSelected).Select(y => y.RoleName));

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Canot add selected roles to user");
                return View(model);
            }

            return RedirectToAction("EditUser", new { Id = userId });
        }

        [HttpPost]
        [Authorize(Policy = "DeleteRolesPolicy")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Role With ID: {id} Cannot Be Found";
                return View("NotFound");
            }
            else
            {
                try
                {
                    var result = await _roleManager.DeleteAsync(role);
                    if(result.Succeeded) 
                    {
                        return RedirectToAction("ListRoles");
                    }
                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return RedirectToAction("ListRoles");
                }
                catch(DbUpdateException exp)
                {
                    ViewBag.ErrorTitle = $"{role.Name} role is in use";
                    ViewBag.ErrorMessage = $"{role.Name} role cannot be deleted as there  are users in this role. If you want to delete this role, please remove the users from the role and then try to delete";
                    return View("Error");
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
