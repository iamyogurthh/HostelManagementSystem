using System;
using System.Collections.Generic;
using System.Text;

namespace HostelManagementSystem.DTOs
{
    class UpdateStudentAcademicRecordDto
    {
        public int Id { get; set; } // 0 if new record
        public string AcademicYearLevel { get; set; } = string.Empty;
        public int AttemptNumber { get; set; } = 1;
        public string RollNumber { get; set; } = string.Empty;
        public string AttendedYear { get; set; } = string.Empty;
        public bool IsHonours { get; set; }
    }
}
