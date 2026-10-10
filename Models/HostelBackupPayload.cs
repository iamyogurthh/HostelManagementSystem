using HostelManagementSystem.Models;

public class HostelBackupPayload
{
    public DateTime ExportedAt { get; set; }

    public string MachineName { get; set; } = string.Empty;


    // Infrastructure
    public List<Building> Buildings { get; set; } = new();
    public List<Floor> Floors { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<RoomType> RoomTypes { get; set; } = new();


    // Students
    public List<StudentCategory> StudentCategories { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public List<StudentAcademicRecord> AcademicRecords { get; set; } = new();
    public List<StudentAccommodation> StudentAccommodations { get; set; } = new();


    // Financial
    public List<FeeRule> FeeRules { get; set; } = new();
    public List<FeeCharge> FeeCharges { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<PaymentAllocation> PaymentAllocations { get; set; } = new();
}