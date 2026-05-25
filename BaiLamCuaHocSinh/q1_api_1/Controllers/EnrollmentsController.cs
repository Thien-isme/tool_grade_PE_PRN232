using Microsoft.AspNetCore.Mvc;
using Q1_API.Models;

namespace Q1_API.Controllers
{
    [Route("api/enrollment")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        [HttpPut("{id}/grade")]
        public IActionResult UpdateGrade(int id)
        {
            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult CancelEnrollment(int id)
        {
            return NotFound();
        }
    }
}
