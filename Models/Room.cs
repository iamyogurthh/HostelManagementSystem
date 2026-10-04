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

        public string Img { get; set; } = "pack://application:,,,/Image/DefaultImages/default_room.png";

        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================
        [ForeignKey(nameof(FloorId))]
        public Floor Floor { get; set; } = null!;

        // Assigned Students
        public List<Student> Students { get; set; } = new();

        // Accommodation History & Billing Tracking
        public List<StudentAccommodation> Accommodations { get; set; } = new();

        // =========================================================
        // COMPUTED PROPERTIES (UI & WPF LOGIC)
        // =========================================================
        [NotMapped]
        public int CurrentResidentsCount => Students?.Count(s => s.IsActive) ?? 0;

        [NotMapped]
        public bool IsAvailableForBooking =>
            IsActive &&
            (Floor?.IsActive ?? true) &&
            (Floor?.Building?.IsActive ?? true);
    }
}