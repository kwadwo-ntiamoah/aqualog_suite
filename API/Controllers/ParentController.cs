using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    public class ParentController: ControllerBase
    {
        protected IActionResult Problem(List<Error> errors)
        {
            if (errors.Count is 0) return Problem();
            if (errors.All(error => error.Type == ErrorType.Validation)) return ValidationProblem(errors);

            var error = errors.First();
            return Problem(error);
        }

        private ObjectResult Problem(Error error)
        {
            var statusCode = error.Type switch
            {
              ErrorType.Conflict => StatusCodes.Status409Conflict,
              ErrorType.Validation => StatusCodes.Status400BadRequest,
              ErrorType.NotFound => StatusCodes.Status404NotFound,
              ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
              _ => StatusCodes.Status500InternalServerError  
            };

            return Problem(statusCode: statusCode, title: error.Description);
        }

        private ActionResult ValidationProblem(List<Error> errors)
        {
            var modelStateDictionary = new ModelStateDictionary();

            foreach (var err in errors) modelStateDictionary.AddModelError(err.Code, err.Description);
            return ValidationProblem(modelStateDictionary);
        }

        protected string GetUserId()
        {
            return HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }
    }
}