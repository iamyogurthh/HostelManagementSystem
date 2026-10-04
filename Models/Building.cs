using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HostelManagementSystem.Models
{
    public class Building
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // BUILDING DETAILS
        // =========================================================
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Img { get; set; } = "pack://application:,,,/Image/DefaultImages/default_building.png";

        public bool IsActive { get; set; } = true;

        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================
        public List<Floor> Floors { get; set; } = new();
    }
}