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

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Img { get; set; } = "pack://application:,,,/Images/DefaultImages/default_room.png";

        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey(nameof(FloorId))]
        public Floor Floor { get; set; } = null!;

        // Navigation Property for assigned Students
        public List<Student> Students { get; set; } = new();

        // Computed property for UI resident count calculation
        [NotMapped]
        public int CurrentResidentsCount => Students?.Count(s => s.IsActive) ?? 0;

        // Computed helper for WPF availability logic
        [NotMapped]
        public bool IsAvailableForBooking =>
            IsActive &&
            (Floor?.IsActive ?? true) &&
            (Floor?.Building?.IsActive ?? true);
    }
}