using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HostelManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        public int FloorId { get; set; }

        // =========================================================
        // ROOM TYPE RELATIONSHIP
        // =========================================================
        public int RoomTypeId { get; set; }

        [ForeignKey(nameof(RoomTypeId))]
        public RoomType RoomType { get; set; } = null!;

        // =========================================================
        // ROOM DETAILS
        // =========================================================
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Img { get; set; } =
            "pack://application:,,,/Image/DefaultImages/default_room.png";

        public int Capacity { get; set; }

        // Soft delete / active room status.
        public bool IsActive { get; set; } = true;

        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================
        [ForeignKey(nameof(FloorId))]
        public Floor Floor { get; set; } = null!;

        // =========================================================
        // ACCOMMODATION HISTORY
        // =========================================================
        public List<StudentAccommodation> Accommodations { get; set; } = new();

        // =========================================================
        // COMPUTED PROPERTIES
        // =========================================================

        /// <summary>
        /// Number of students currently staying in this room.
        ///
        /// IsActive means the accommodation record has not been
        /// soft-deleted.
        ///
        /// EndDate == null means the accommodation is currently active.
        /// </summary>
        [NotMapped]
        public int CurrentResidentsCount =>
            Accommodations?
                .Count(a => a.IsActive && a.EndDate == null) ?? 0;

        [NotMapped]
        public bool IsAvailableForBooking =>
            IsActive &&
            (Floor?.IsActive ?? true) &&
            (Floor?.Building?.IsActive ?? true);
    }
}