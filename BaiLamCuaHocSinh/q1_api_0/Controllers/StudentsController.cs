using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q1_API.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Q1_API.Controllers
{
    [Route("api/[controller]")]
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
            var students = await _context.Students
                .Include(s => s.Enrollments)
                .Select(s => new StudentDTO
                {
                    StudentId = s.StudentID,
                    StudentName = s.StudentName,
                    Email = s.Email,
                    Gpa = s.Enrollments.Where(e => e.Grade.HasValue).Any() 
                          ? s.Enrollments.Where(e => e.Grade.HasValue).Average(e => e.Grade.Value) 
                          : 0
                })
                .ToListAsync();

            return Ok(students);
        }
    }

    [Route("api/student-performance")]
    [ApiController]
    public class StudentPerformanceController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public StudentPerformanceController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentPerformance([FromQuery] double? minGpa, [FromQuery] string studentName, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest("Invalid pagination parameters.");
            }

            var query = _context.Students
                .Include(s => s.Enrollments)
                .Select(s => new StudentDTO
                {
                    StudentId = s.StudentID,
                    StudentName = s.StudentName,
                    Email = s.Email,
                    Gpa = s.Enrollments.Where(e => e.Grade.HasValue).Any() 
                          ? s.Enrollments.Where(e => e.Grade.HasValue).Average(e => e.Grade.Value) 
                          : 0
                }).AsQueryable();

            if (minGpa.HasValue)
            {
                query = query.Where(s => s.Gpa >= minGpa.Value);
            }

            if (!string.IsNullOrEmpty(studentName))
            {
                query = query.Where(s => s.StudentName.Contains(studentName));
            }

            var totalStudents = await query.CountAsync();
            var totalPages = (int)System.Math.Ceiling(totalStudents / (double)pageSize);

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var response = new StudentPerformanceResponse
            {
                Data = data,
                TotalStudents = totalStudents,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };

            return Ok(response);
        }
    }

    public class StudentDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public double Gpa { get; set; }
    }

    public class StudentPerformanceResponse
    {
        public List<StudentDTO> Data { get; set; }
        public int TotalStudents { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
