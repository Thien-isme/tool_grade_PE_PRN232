using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q1_API.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Q1_API.Controllers
{
    [Route("api/student")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public StudentsController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students.ToListAsync();
            return Ok(students);
        }
    }

    [Route("api/performance")]
    [ApiController]
    public class StudentPerformanceController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetStudentPerformance()
        {
            return NotFound();
        }
    }
}
