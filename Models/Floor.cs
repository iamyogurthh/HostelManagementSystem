using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class Floor
    {
        [Key]
        public int Id { get; set; }
        public int BuildingId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = "pack://application:,,,/Images/DefaultImages/default_floor.png";
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(BuildingId))]
        public Building Building { get; set; } = null!;

        public List<Room> Rooms { get; set; } = new();
    }
}
