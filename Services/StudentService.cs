using HostelManagementSystem.Database;
using HostelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace HostelManagementSystem.Services
{
    public class StudentService
    {
        private readonly StudentAccommodationService _accommodationService;

        public StudentService()
        {
            _accommodationService = new StudentAccommodationService();
        }

        /// <summary>
        /// Soft-deletes a student.
        ///
        /// The student and all historical records remain in the
        /// database. The current accommodation is closed first.
        /// </summary>
        public async Task DeleteStudentWithRelatedDataAsync(
            int studentId,
            DateOnly endDate)
        {
            await using var db = new AppDbContext();

            // Make sure the student exists.
            var studentExists = await db.Students
                .AnyAsync(s => s.Id == studentId);

            if (!studentExists)
            {
                return;
            }

            // ---------------------------------------------------------
            // Close current accommodation + deactivate student.
            //
            // Passing the same DbContext means both operations are
            // saved together.
            // ---------------------------------------------------------

            await _accommodationService.DeactivateStudentAsync(
                studentId,
                endDate,
                db);

            await db.SaveChangesAsync();
        }
    }
}
