using HostelManagementSystem.Database;
using HostelManagementSystem.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HostelManagementSystem.Services
{
    public class StudentService
    {
        public async Task DeleteStudentWithRelatedDataAsync(int studentId)
        {
            await using var db = new AppDbContext();

            var studentRecord = await db.Students
                .Include(s => s.AcademicRecords)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (studentRecord != null)
            {
                // 1. Delete local image files
                var imageManager = new ImageManager();
                await Task.Run(() =>
                {
                    if (!string.IsNullOrWhiteSpace(studentRecord.Img)) imageManager.DeleteImage(studentRecord.Img);
                    if (!string.IsNullOrWhiteSpace(studentRecord.High_school_certificate_img)) imageManager.DeleteImage(studentRecord.High_school_certificate_img);
                    if (!string.IsNullOrWhiteSpace(studentRecord.NRC_img_front)) imageManager.DeleteImage(studentRecord.NRC_img_front);
                    if (!string.IsNullOrWhiteSpace(studentRecord.NRC_img_back)) imageManager.DeleteImage(studentRecord.NRC_img_back);
                });

                // 2. Remove Academic Records
                if (studentRecord.AcademicRecords != null && studentRecord.AcademicRecords.Any())
                {
                    db.StudentAcademicRecords.RemoveRange(studentRecord.AcademicRecords);
                }

                // 3. Remove Student
                db.Students.Remove(studentRecord);
                await db.SaveChangesAsync();
            }
        }
    }
}
