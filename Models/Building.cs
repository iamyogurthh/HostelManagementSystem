using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace HostelManagementSystem.Models
{
    public class Building
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = "pack://application:,,,/Images/DefaultImages/default_building.jpg";
        public bool IsActive { get; set; } = true;

        //Navigating Property
        public List<Floor> Floors { get; set; } = new();

    }
}
