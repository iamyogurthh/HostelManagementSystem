using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HostelManagementSystem.Models
{
    public class RoomType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // =========================================================
        // RELATIONSHIPS
        // =========================================================
        public List<Room> Rooms { get; set; } = new();

        public List<FeeRule> FeeRules { get; set; } = new();
    }
}