using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HostelManagementSystem.Services
{
    /// <summary>
    /// Handles all student accommodation operations:
    /// - Get current accommodation
    /// - Assign a room
    /// - Transfer between rooms
    /// - Release a room
    /// - Deactivate a student
    /// - Validate room capacity
    /// - Close previous accommodation
    /// - Create new accommodation
    ///
    /// StudentAccommodation is the source of truth for
    /// the student's current room.
    /// </summary>
    public class StudentAccommodationService
    {
        // =========================================================
        // GET CURRENT ACCOMMODATION
        // =========================================================

        /// <summary>
        /// Gets the student's current accommodation.
        ///
        /// Current accommodation means:
        /// IsActive == true AND EndDate == null.
        /// </summary>
        public async Task<StudentAccommodation?> GetCurrentAsync(
            int studentId,
            AppDbContext? db = null)
        {
            if (db != null)
            {
                return await GetCurrentInternalAsync(db, studentId);
            }

            await using var context = new AppDbContext();

            return await GetCurrentInternalAsync(context, studentId);
        }

        private static async Task<StudentAccommodation?> GetCurrentInternalAsync(
            AppDbContext db,
            int studentId)
        {
            return await db.StudentAccommodations
                .Include(a => a.Room)
                    .ThenInclude(r => r.Floor)
                        .ThenInclude(f => f.Building)
                .FirstOrDefaultAsync(a =>
                    a.StudentId == studentId &&
                    a.IsActive &&
                    a.EndDate == null);
        }

        // =========================================================
        // ASSIGN / TRANSFER
        // =========================================================

        /// <summary>
        /// Assigns a student to a room.
        ///
        /// If the student has no current accommodation:
        ///     Creates a new accommodation.
        ///
        /// If the student is already in another room:
        ///     Closes the current accommodation and creates a new one.
        ///
        /// If the student is already in the selected room:
        ///     Does nothing and returns the existing accommodation.
        ///
        /// When an AppDbContext is supplied, the caller owns the
        /// transaction and SaveChanges lifecycle.
        /// </summary>
        public async Task<StudentAccommodation> AssignAsync(
            int studentId,
            int roomId,
            DateOnly startDate,
            AppDbContext? db = null)
        {
            if (db != null)
            {
                return await AssignInternalAsync(
                    db,
                    studentId,
                    roomId,
                    startDate);
            }

            await using var context = new AppDbContext();

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            try
            {
                var accommodation = await AssignInternalAsync(
                    context,
                    studentId,
                    roomId,
                    startDate);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return accommodation;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task<StudentAccommodation> AssignInternalAsync(
            AppDbContext db,
            int studentId,
            int roomId,
            DateOnly startDate)
        {
            // ---------------------------------------------------------
            // 1. Validate student
            // ---------------------------------------------------------
            var student = await db.Students
                .FirstOrDefaultAsync(s =>
                    s.Id == studentId &&
                    s.IsActive);

            if (student == null)
            {
                throw new InvalidOperationException(
                    "Student does not exist or is inactive.");
            }

            // ---------------------------------------------------------
            // 2. Validate destination room
            // ---------------------------------------------------------

            var room = await db.Rooms
                .Include(r => r.Floor)
                    .ThenInclude(f => f.Building)
                .FirstOrDefaultAsync(r =>
                    r.Id == roomId &&
                    r.IsActive);

            if (room == null)
            {
                throw new InvalidOperationException(
                    "The selected room does not exist or is inactive.");
            }

            if (!room.Floor.IsActive ||
                !room.Floor.Building.IsActive)
            {
                throw new InvalidOperationException(
                    "The selected room is not available because its floor or building is inactive.");
            }

            // 3. Validate fee rule before assigning the room
            var assignmentDate = startDate.ToDateTime(TimeOnly.MinValue);

            var matchingFeeRule = await db.FeeRules
                .AnyAsync(fr =>
                    fr.StudentCategoryId == student.StudentCategoryId &&
                    fr.RoomTypeId == room.RoomTypeId &&
                    fr.IsActive &&
                    fr.EffectiveFrom <= assignmentDate &&
                    (fr.EffectiveTo == null ||
                     fr.EffectiveTo >= assignmentDate));

            if (!matchingFeeRule)
            {
                throw new InvalidOperationException(
                    "No matching fee rule exists for this student category " +
                    "and room type on the selected assignment date. " +
                    "Please create an applicable fee rule before assigning this room.");
            }

            // ---------------------------------------------------------
            // 4. Get current accommodation
            // ---------------------------------------------------------

            var currentAccommodation =
                await db.StudentAccommodations
                    .FirstOrDefaultAsync(a =>
                        a.StudentId == studentId &&
                        a.IsActive &&
                        a.EndDate == null);

            // ---------------------------------------------------------
            // 5. Already in selected room
            // ---------------------------------------------------------

            if (currentAccommodation != null &&
                currentAccommodation.RoomId == roomId)
            {
                return currentAccommodation;
            }

            // ---------------------------------------------------------
            // 6. Validate destination room capacity
            // ---------------------------------------------------------

            var currentResidentCount =
                await db.StudentAccommodations
                    .CountAsync(a =>
                        a.RoomId == roomId &&
                        a.IsActive &&
                        a.EndDate == null);

            if (currentResidentCount >= room.Capacity)
            {
                throw new InvalidOperationException(
                    $"Room '{room.Name}' is already full.");
            }

            // ---------------------------------------------------------
            // 7. Close previous accommodation
            // ---------------------------------------------------------

            if (currentAccommodation != null)
            {
                if (startDate < currentAccommodation.StartDate)
                {
                    throw new InvalidOperationException(
                        "The new accommodation start date cannot be earlier than the current accommodation start date.");
                }

                currentAccommodation.EndDate = startDate;
            }

            // ---------------------------------------------------------
            // 8. Create new accommodation
            // ---------------------------------------------------------

            var newAccommodation = new StudentAccommodation
            {
                StudentId = studentId,
                RoomId = roomId,
                StartDate = startDate,
                EndDate = null,
                IsActive = true
            };

            await db.StudentAccommodations.AddAsync(
                newAccommodation);

            return newAccommodation;
        }

        // =========================================================
        // RELEASE
        // =========================================================

        /// <summary>
        /// Releases the student's current room.
        ///
        /// The accommodation record is preserved as historical data.
        /// Only EndDate is populated.
        ///
        /// IsActive remains true because the accommodation record
        /// itself is valid historical data.
        /// </summary>
        public async Task ReleaseAsync(
            int studentId,
            DateOnly endDate,
            AppDbContext? db = null)
        {
            if (db != null)
            {
                await ReleaseInternalAsync(
                    db,
                    studentId,
                    endDate);

                return;
            }

            await using var context = new AppDbContext();

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            try
            {
                await ReleaseInternalAsync(
                    context,
                    studentId,
                    endDate);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task ReleaseInternalAsync(
            AppDbContext db,
            int studentId,
            DateOnly endDate)
        {
            var accommodation =
                await db.StudentAccommodations
                    .FirstOrDefaultAsync(a =>
                        a.StudentId == studentId &&
                        a.IsActive &&
                        a.EndDate == null);

            // Student has no current room.
            if (accommodation == null)
            {
                return;
            }

            if (endDate < accommodation.StartDate)
            {
                throw new InvalidOperationException(
                    "The release date cannot be earlier than the accommodation start date.");
            }

            accommodation.EndDate = endDate;
        }

        // =========================================================
        // DEACTIVATE STUDENT
        // =========================================================

        /// <summary>
        /// Deactivates a student without physically deleting
        /// any database records.
        ///
        /// The student's current accommodation is closed first.
        ///
        /// Preserved:
        /// - Student record
        /// - Academic records
        /// - Accommodation history
        /// - Fee charges
        /// - Payments
        /// - Payment allocations
        ///
        /// Only Student.IsActive is changed to false and the
        /// current accommodation receives an EndDate.
        /// </summary>
        public async Task DeactivateStudentAsync(
            int studentId,
            DateOnly endDate,
            AppDbContext? db = null)
        {
            if (db != null)
            {
                await DeactivateStudentInternalAsync(
                    db,
                    studentId,
                    endDate);

                return;
            }

            await using var context = new AppDbContext();

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            try
            {
                await DeactivateStudentInternalAsync(
                    context,
                    studentId,
                    endDate);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task DeactivateStudentInternalAsync(
            AppDbContext db,
            int studentId,
            DateOnly endDate)
        {
            // ---------------------------------------------------------
            // 1. Find student
            // ---------------------------------------------------------

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                throw new InvalidOperationException(
                    "Student does not exist.");
            }

            // Already inactive.
            if (!student.IsActive)
            {
                return;
            }

            // ---------------------------------------------------------
            // 2. Find current accommodation
            // ---------------------------------------------------------

            var currentAccommodation =
                await db.StudentAccommodations
                    .FirstOrDefaultAsync(a =>
                        a.StudentId == studentId &&
                        a.IsActive &&
                        a.EndDate == null);

            // ---------------------------------------------------------
            // 3. Close current accommodation
            // ---------------------------------------------------------

            if (currentAccommodation != null)
            {
                if (endDate < currentAccommodation.StartDate)
                {
                    throw new InvalidOperationException(
                        "The deactivation date cannot be earlier than the accommodation start date.");
                }

                currentAccommodation.EndDate = endDate;
            }

            // ---------------------------------------------------------
            // 4. Soft delete student
            // ---------------------------------------------------------

            student.IsActive = false;
            student.UpdatedAt = DateTime.Now;
        }
    }
}

