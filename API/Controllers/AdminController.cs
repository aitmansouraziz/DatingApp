using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class AdminController:BaseApiController
    {
       
        private readonly UserManager<AppUser> _userManager;


        public AdminController(UserManager<AppUser> userManager)
        {
            this._userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            List<AppUserRole> usersRole = new List<AppUserRole>(); 
            var users = await  _userManager.Users
                .Include(r=>r.UserRoles)
                .ThenInclude(r=>r.Role)
                .OrderBy(u=>u.UserName)
                .Select(u=> new
                {
                    Id=u.Id,
                    Username =u.UserName,
                    Roles=u.UserRoles.Select(r=>r.Role.Name).ToList()
                })
                .ToListAsync();
            
            
            return Ok(users);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModeration()
        {
            return Ok(" admins or moderators  can see this");
        }

        
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string userName,[FromQuery]string roles)
        {
            var selectedRoles = roles.Split(",").ToArray();
            var user = await _userManager.FindByNameAsync(userName);
            if (user is null) return NotFound("Couldnt find user");
            var userRoles = await _userManager.GetRolesAsync(user);
            
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded) return BadRequest("Failed to add to roles");
            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded) return BadRequest("Faild to remove from roles");
            return Ok(await _userManager.GetRolesAsync(user));
        }
    }
}
