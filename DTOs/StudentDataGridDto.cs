namespace HostelManagementSystem.DTOs
{
    public class StudentDataGridDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NRC { get; set; } = string.Empty;
        public string Student_ph { get; set; } = string.Empty;
        public string Father_name { get; set; } = string.Empty;
        public string LatestRollNumber { get; set; } = string.Empty;
    }
}