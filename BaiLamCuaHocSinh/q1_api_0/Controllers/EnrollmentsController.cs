using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q1_API.Models;
using System.Threading.Tasks;

namespace Q1_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public EnrollmentsController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpPut("{enrollmentId}/grade")]
        public async Task<IActionResult> UpdateGrade(int enrollmentId, [FromBody] GradeUpdateModel model)
        {
            if (model.Grade < 0 || model.Grade > 10)
            {
                return BadRequest("Grade must be between 0 and 10");
            }

            var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.Grade = model.Grade;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                enrollmentId = enrollment.EnrollmentID,
                studentId = enrollment.StudentID,
                grade = enrollment.Grade
            });
        }

        [HttpDelete("{enrollmentId}")]
        public async Task<IActionResult> CancelEnrollment(int enrollmentId)
        {
            var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
            if (enrollment == null)
            {
                return NotFound("No enrollment found with provided EnrollmentId");
            }

            if (enrollment.Grade != null)
            {
                return BadRequest("Cannot cancel an enrollment that has already been graded");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class GradeUpdateModel
    {
        public double Grade { get; set; }
    }
}
