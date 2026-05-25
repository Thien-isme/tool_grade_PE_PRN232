using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Q1_API.Models
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options) { }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<CourseAssignment> CourseAssignments { get; set; }
        public DbSet<ClassSection> ClassSections { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassSection>()
                .HasKey(cs => cs.SectionID);

            modelBuilder.Entity<CourseAssignment>()
                .HasKey(ca => new { ca.CourseID, ca.InstructorID });

            modelBuilder.Entity<CourseAssignment>()
                .HasOne(ca => ca.Course)
                .WithMany(c => c.CourseAssignments)
                .HasForeignKey(ca => ca.CourseID);

            modelBuilder.Entity<CourseAssignment>()
                .HasOne(ca => ca.Instructor)
                .WithMany(i => i.CourseAssignments)
                .HasForeignKey(ca => ca.InstructorID);

            modelBuilder.Entity<ClassSection>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.ClassSections)
                .HasForeignKey(cs => cs.CourseID);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentID);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.ClassSection)
                .WithMany(cs => cs.Enrollments)
                .HasForeignKey(e => e.SectionID);
        }
    }

    public class Course
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; }
        public int Credits { get; set; }
        public string Department { get; set; }

        public ICollection<CourseAssignment> CourseAssignments { get; set; } = new List<CourseAssignment>();
        public ICollection<ClassSection> ClassSections { get; set; } = new List<ClassSection>();
    }

    public class Instructor
    {
        public int InstructorID { get; set; }
        public string FullName { get; set; }
        public string Expertise { get; set; }
        public DateTime? HireDate { get; set; }

        public ICollection<CourseAssignment> CourseAssignments { get; set; } = new List<CourseAssignment>();
    }

    public class CourseAssignment
    {
        public int CourseID { get; set; }
        public Course Course { get; set; }

        public int InstructorID { get; set; }
        public Instructor Instructor { get; set; }

        public DateTime? AssignmentDate { get; set; }
    }

    public class ClassSection
    {
        public int SectionID { get; set; }
        public int? CourseID { get; set; }
        public Course Course { get; set; }
        public string RoomNumber { get; set; }
        public string Semester { get; set; }
        public int? MaxCapacity { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }

    public class Student
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }

    public class Enrollment
    {
        public int EnrollmentID { get; set; }
        public int? StudentID { get; set; }
        public Student Student { get; set; }
        public int? SectionID { get; set; }
        public ClassSection ClassSection { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public double? Grade { get; set; }
    }
}
