using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        public int FloorId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Img { get; set; } = "pack://application:,,,/Images/DefaultImages/default_room.jpg";

        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey(nameof(FloorId))]
        public Floor Floor { get; set; } = null!;

        // Computed helper for WPF availability logic (Not saved to DB)
        [NotMapped]
        public bool IsAvailableForBooking =>
            IsActive &&
            (Floor?.IsActive ?? true) &&
            (Floor?.Building?.IsActive ?? true);
    }
}