using System;
using System.Collections.Generic;
using System.Text;

namespace HostelManagementSystem.DTOs
{
    class CreateStudentDto
    {
        public int RoomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime Birthday { get; set; }
        public string NRC { get; set; } = string.Empty;
        public string Student_ph { get; set; } = string.Empty;
        public string Parent_ph { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string Father_name { get; set; } = string.Empty;
        public string Mother_name { get; set; } = string.Empty;
        public string Father_NRC { get; set; } = string.Empty;
        public string Mother_NRC { get; set; } = string.Empty;

        public string Major { get; set; } = string.Empty;

        public string High_school_roll_no { get; set; } = string.Empty;
        public string High_school_name { get; set; } = string.Empty;
        public string High_school_city { get; set; } = string.Empty;

        public string Img { get; set; } = string.Empty;
        public string High_school_certificate_img { get; set; } = string.Empty;
        public string NRC_img_front { get; set; } = string.Empty;
        public string NRC_img_back { get; set; } = string.Empty;

        public List<CreateStudentAcademicRecordDto> AcademicRecords { get; set; } = new();
    }
}
