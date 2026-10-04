using System;
using System.Collections.Generic;

// not include in the database context, this is just a payload for backup and restore operations
namespace HostelManagementSystem.Models
{
    public class HostelBackupPayload
    {
        public DateTime ExportedAt { get; set; } = DateTime.Now;
        public string MachineName { get; set; } = Environment.MachineName;

        public List<Building>? Buildings { get; set; }
        public List<Floor>? Floors { get; set; }
        public List<Room>? Rooms { get; set; }
        public List<Student>? Students { get; set; }
        public List<StudentAcademicRecord>? AcademicRecords { get; set; }
    }
}