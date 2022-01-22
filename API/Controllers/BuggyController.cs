using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
        private readonly DataContext _context;

        public BuggyController(DataContext context)
        {
            this._context = context;
        }

        // api/buggy/auth
        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
            return "secret test";
        }

        // api/buggy/not-found
        [HttpGet("not-found")]
        public ActionResult<AppUser> GetNotFound()
        {
            var thing = this._context.Users.Find(-1);
            if (thing==null)
            {
                return NotFound();
            }
            return Ok(thing);
        }

        // api/buggy/server-error
        [HttpGet("server-error")]
        public ActionResult<string> GetServerError()
        {
            
                var thing = this._context.Users.Find(-1);
                var thingToReturn = thing.ToString();
                return thingToReturn;
           
              
            
            
        }

        // api/buggy/server-error
        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
            return BadRequest();
        }
    }
}
