using System;
using System.Collections.Generic;
using System.Text;

namespace HostelManagementSystem.DTOs
{
    public class CreateStudentAcademicRecordDto
    {
        public string AcademicYearLevel { get; set; } = string.Empty;
        public int AttemptNumber { get; set; } = 1;
        public string RollNumber { get; set; } = string.Empty;
        public string AttendedYear { get; set; } = string.Empty;
        public bool IsHonours { get; set; }
    }
}
