using HostelManagementSystem.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Markup;

namespace HostelManagementSystem.Services
{
    public class ExportPDFService
    {
        // =========================================================
        // EXPORT STUDENT PDF
        // =========================================================

        public async Task ExportStudentAsPdfAsync(
            Student student,
            string? profileImagePath,
            Building? building,
            Floor? floor,
            Room? room)
        {
            if (student == null)
                throw new ArgumentNullException(nameof(student));

            // Save dialog must run on the UI thread.
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Student_{student.Name}_{DateTime.Now:yyyyMMdd}.pdf",
                Title = "Save Student Record as PDF"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            string targetPath = saveFileDialog.FileName;

            // Generate the PDF on a background thread
            // to prevent freezing the UI.
            await Task.Run(() =>
                GenerateStudentPdf(
                    student,
                    profileImagePath,
                    targetPath,
                    building?.Name,
                    floor?.Name,
                    room?.Name));
        }

        // =========================================================
        // GENERATE STUDENT PDF
        // =========================================================

        private void GenerateStudentPdf(
            Student student,
            string? profileImagePath,
            string targetPath,
            string? buildingName,
            string? floorName,
            string? roomName)
        {
            // Safely handle a student who does not currently
            // have a building, floor, or room assigned.

            string roomInfo;

            if (!string.IsNullOrWhiteSpace(buildingName) ||
                !string.IsNullOrWhiteSpace(floorName) ||
                !string.IsNullOrWhiteSpace(roomName))
            {
                roomInfo =
                    $"{buildingName ?? "-"} - " +
                    $"{floorName ?? "-"} - " +
                    $"{roomName ?? "-"}";
            }
            else
            {
                roomInfo = "အခန်းမသတ်မှတ်ရသေးပါ";
            }

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.PageColor(Colors.White);

                        page.DefaultTextStyle(x =>
                            x.FontSize(11)
                             .FontFamily("Pyidaungsu"));

                        // =================================================
                        // HEADER
                        // =================================================

                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(headerCol =>
                                {
                                    headerCol.Item()
                                        .Text("သူ့ထက်သာ အဆောင်")
                                        .FontSize(18)
                                        .Bold();

                                    headerCol.Item()
                                        .Text("ကျောင်းသား အချက်အလက် မှတ်တမ်း")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);
                                });

                                row.ConstantItem(120)
                                   .AlignRight()
                                   .Text($"ရက်စွဲ: {DateTime.Now:yyyy-MM-dd}")
                                   .FontSize(10);
                            });

                            col.Item()
                               .PaddingTop(10)
                               .LineHorizontal(1)
                               .LineColor(Colors.Grey.Lighten1);
                        });

                        // =================================================
                        // CONTENT
                        // =================================================

                        page.Content()
                            .PaddingVertical(15)
                            .Column(col =>
                            {
                                // =================================================
                                // PROFILE & OVERVIEW
                                // =================================================

                                col.Item().Row(row =>
                                {
                                    if (!string.IsNullOrWhiteSpace(profileImagePath) &&
                                        File.Exists(profileImagePath))
                                    {
                                        row.ConstantItem(100)
                                           .Height(120)
                                           .Image(profileImagePath)
                                           .FitArea();
                                    }
                                    else
                                    {
                                        row.ConstantItem(100)
                                           .Height(120)
                                           .Border(1)
                                           .BorderColor(Colors.Grey.Lighten2)
                                           .AlignCenter()
                                           .AlignMiddle()
                                           .Text("No Photo")
                                           .FontSize(10);
                                    }

                                    row.RelativeItem()
                                       .PaddingLeft(20)
                                       .Column(infoCol =>
                                       {
                                           infoCol.Item()
                                               .Text(t =>
                                               {
                                                   t.Span("အမည်: ").Bold();
                                                   t.Span(student.Name ?? "");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(4)
                                               .Text(t =>
                                               {
                                                   t.Span("ကျား/မ: ").Bold();
                                                   t.Span(student.Gender ?? "");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(4)
                                               .Text(t =>
                                               {
                                                   t.Span("မှတ်ပုံတင်အမှတ်: ").Bold();
                                                   t.Span(student.NRC ?? "");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(4)
                                               .Text(t =>
                                               {
                                                   t.Span("မွေးသက္ကရာဇ်: ").Bold();
                                                   t.Span($"{student.Birthday:yyyy-MM-dd}");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(4)
                                               .Text(t =>
                                               {
                                                   t.Span("အဓိကဘာသာ: ").Bold();
                                                   t.Span(student.Major ?? "");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(4)
                                               .Text(t =>
                                               {
                                                   t.Span("ဖုန်းနံပါတ်: ").Bold();
                                                   t.Span(student.Student_ph ?? "");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(4)
                                               .Text(t =>
                                               {
                                                   t.Span("အဆောင် / အခန်း: ").Bold();
                                                   t.Span(roomInfo);
                                               });
                                       });
                                });

                                col.Item()
                                   .PaddingVertical(10)
                                   .LineHorizontal(0.5f)
                                   .LineColor(Colors.Grey.Lighten2);

                                // =================================================
                                // PARENT INFORMATION
                                // =================================================

                                col.Item()
                                   .Text("မိဘ အချက်အလက်များ")
                                   .FontSize(13)
                                   .Bold();

                                col.Item()
                                   .PaddingTop(5)
                                   .Row(row =>
                                   {
                                       row.RelativeItem().Column(c =>
                                       {
                                           c.Item()
                                            .Text(t =>
                                            {
                                                t.Span("ဖခင်အမည်: ").Bold();
                                                t.Span(student.Father_name ?? "");
                                            });

                                           c.Item()
                                            .PaddingTop(4)
                                            .Text(t =>
                                            {
                                                t.Span("မိခင်အမည်: ").Bold();
                                                t.Span(student.Mother_name ?? "");
                                            });

                                           c.Item()
                                            .PaddingTop(4)
                                            .Text(t =>
                                            {
                                                t.Span("မိဘ ဖုန်းနံပါတ်: ").Bold();
                                                t.Span(student.Parent_ph ?? "");
                                            });
                                       });

                                       row.RelativeItem().Column(c =>
                                       {
                                           c.Item()
                                            .Text(t =>
                                            {
                                                t.Span("ဖခင် NRC: ").Bold();
                                                t.Span(student.Father_NRC ?? "");
                                            });

                                           c.Item()
                                            .PaddingTop(4)
                                            .Text(t =>
                                            {
                                                t.Span("မိခင် NRC: ").Bold();
                                                t.Span(student.Mother_NRC ?? "");
                                            });

                                           c.Item()
                                            .PaddingTop(4)
                                            .Text(t =>
                                            {
                                                t.Span("နေရပ်လိပ်စာ: ").Bold();
                                                t.Span(student.Address ?? "");
                                            });
                                       });
                                   });

                                col.Item()
                                   .PaddingVertical(10)
                                   .LineHorizontal(0.5f)
                                   .LineColor(Colors.Grey.Lighten2);

                                // =================================================
                                // GRADE 12 INFORMATION
                                // =================================================

                                col.Item()
                                   .Text("Grade 12 အချက်အလက်များ")
                                   .FontSize(12)
                                   .Bold();

                                col.Item()
                                   .PaddingTop(8)
                                   .Row(row =>
                                   {
                                       row.RelativeItem(2)
                                          .Text(t =>
                                          {
                                              t.Span("Grade 12 ခုံနံပါတ်: ").Bold();
                                              t.Span(student.High_school_roll_no ?? "");
                                          });

                                       row.RelativeItem(3)
                                          .Text(t =>
                                          {
                                              t.Span("တက်ခဲ့သည့် ကျောင်း: ").Bold();
                                              t.Span(student.High_school_name ?? "");
                                          });

                                       row.RelativeItem(2)
                                          .Text(t =>
                                          {
                                              t.Span("မြို့: ").Bold();
                                              t.Span(student.High_school_city ?? "");
                                          });
                                   });

                                col.Item()
                                   .PaddingVertical(12)
                                   .LineHorizontal(0.5f)
                                   .LineColor(Colors.Grey.Lighten2);

                                // =================================================
                                // ACADEMIC RECORDS
                                // =================================================

                                col.Item()
                                   .Text("ပညာအရည်အချင်း မှတ်တမ်းများ")
                                   .FontSize(13)
                                   .Bold();

                                col.Item()
                                   .PaddingTop(8)
                                   .Table(table =>
                                   {
                                       table.ColumnsDefinition(columns =>
                                       {
                                           columns.RelativeColumn(2);
                                           columns.RelativeColumn(2);
                                           columns.RelativeColumn(2);
                                           columns.RelativeColumn(1);
                                       });

                                       table.Header(header =>
                                       {
                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("သင်တန်းနှစ်")
                                                 .Bold();

                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("ခုံအမှတ်")
                                                 .Bold();

                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("တက်ရောက်သည့်နှစ်")
                                                 .Bold();

                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("ကြိမ်ရေ")
                                                 .Bold();
                                       });

                                       if (student.AcademicRecords != null)
                                       {
                                           foreach (var record in student.AcademicRecords)
                                           {
                                               table.Cell()
                                                    .BorderBottom(0.5f)
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Padding(5)
                                                    .Text(record.AcademicYearLevel ?? "");

                                               table.Cell()
                                                    .BorderBottom(0.5f)
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Padding(5)
                                                    .Text(record.RollNumber ?? "");

                                               table.Cell()
                                                    .BorderBottom(0.5f)
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Padding(5)
                                                    .Text(record.AttendedYear ?? "");

                                               table.Cell()
                                                    .BorderBottom(0.5f)
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Padding(5)
                                                    .Text(record.AttemptNumber.ToString());
                                           }
                                       }
                                   });
                            });

                        // =================================================
                        // FOOTER / SIGNATURES
                        // =================================================

                        page.Footer().Column(footerCol =>
                        {
                            footerCol.Item().Row(row =>
                            {
                                row.RelativeItem().Column(sign =>
                                {
                                    sign.Item()
                                        .PaddingTop(30)
                                        .LineHorizontal(1)
                                        .LineColor(Colors.Grey.Darken1);

                                    sign.Item()
                                        .AlignCenter()
                                        .Text("ကျောင်းသား လက်မှတ်")
                                        .FontSize(10);
                                });

                                row.ConstantItem(100);

                                row.RelativeItem().Column(sign =>
                                {
                                    sign.Item()
                                        .PaddingTop(30)
                                        .LineHorizontal(1)
                                        .LineColor(Colors.Grey.Darken1);

                                    sign.Item()
                                        .AlignCenter()
                                        .Text("အဆောင်မှူး လက်မှတ်")
                                        .FontSize(10);
                                });
                            });
                        });
                    });
                }).GeneratePdf(targetPath);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"Unable to save the PDF. Please close the file which is open in another program and try again.\n\nError: {ex.Message}",
                    "File Access Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"PDF သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "PDF Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // EXPORT BLANK FORM TEMPLATE
        // =========================================================

        public async Task ExportFormTemplateAsPdfAsync()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = "Student_Registration_Form_Template.pdf",
                Title = "Save Blank Registration Form Template as PDF"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            string targetPath = saveFileDialog.FileName;

            // Generate PDF in background thread.
            await Task.Run(() =>
                GenerateFormTemplatePdf(targetPath));
        }

        // =========================================================
        // GENERATE BLANK FORM TEMPLATE
        // =========================================================

        private void GenerateFormTemplatePdf(string targetPath)
        {
            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.PageColor(Colors.White);

                        page.DefaultTextStyle(x =>
                            x.FontSize(8)
                             .FontFamily("Pyidaungsu"));

                        // =================================================
                        // HEADER
                        // =================================================

                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(headerCol =>
                                {
                                    headerCol.Item()
                                             .Text("သူ့ထက်သာ အဆောင်")
                                             .FontSize(18)
                                             .Bold();

                                    headerCol.Item()
                                             .Text("ကျောင်းသား အချက်အလက် ဖြည့်စွက်ရန် ပုံစံ")
                                             .FontSize(13)
                                             .Bold()
                                             .FontColor(Colors.Grey.Darken2);
                                });

                                row.ConstantItem(120)
                                   .AlignRight()
                                   .Text("ရက်စွဲ: ___________")
                                   .FontSize(10);
                            });

                            col.Item()
                               .PaddingTop(8)
                               .LineHorizontal(1)
                               .LineColor(Colors.Grey.Lighten1);
                        });

                        // =================================================
                        // CONTENT
                        // =================================================

                        page.Content()
                            .PaddingVertical(15)
                            .Column(col =>
                            {
                                // =================================================
                                // PERSONAL INFORMATION
                                // =================================================

                                col.Item().Row(row =>
                                {
                                    row.ConstantItem(100)
                                       .Height(120)
                                       .Border(1)
                                       .BorderColor(Colors.Grey.Medium)
                                       .AlignCenter()
                                       .AlignMiddle()
                                       .Text("ဓာတ်ပုံ")
                                       .FontSize(9)
                                       .FontColor(Colors.Grey.Darken1)
                                       .LineHeight(1.3f);

                                    row.RelativeItem()
                                       .PaddingLeft(20)
                                       .Column(infoCol =>
                                       {
                                           infoCol.Item()
                                               .Text(t =>
                                               {
                                                   t.Span("အမည်: ").Bold();
                                                   t.Span("____________________________________");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(8)
                                               .Row(r =>
                                               {
                                                   r.AutoItem()
                                                    .Text("ကျား/မ: ")
                                                    .Bold();

                                                   r.AutoItem()
                                                    .PaddingLeft(10)
                                                    .Text("[  ] ကျား");

                                                   r.AutoItem()
                                                    .PaddingLeft(20)
                                                    .Text("[  ] မ");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(8)
                                               .Text(t =>
                                               {
                                                   t.Span("မှတ်ပုံတင်အမှတ်: ").Bold();
                                                   t.Span("_____________________________");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(8)
                                               .Text(t =>
                                               {
                                                   t.Span("မွေးသက္ကရာဇ်: ").Bold();
                                                   t.Span("_________________________________");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(8)
                                               .Text(t =>
                                               {
                                                   t.Span("အဓိကဘာသာ: ").Bold();
                                                   t.Span("__________________________________");
                                               });

                                           infoCol.Item()
                                               .PaddingTop(8)
                                               .Text(t =>
                                               {
                                                   t.Span("ဖုန်းနံပါတ်: ").Bold();
                                                   t.Span("___________________________________");
                                               });
                                       });
                                });

                                col.Item()
                                   .PaddingTop(8)
                                   .Text(t =>
                                   {
                                       t.Span("အဆောင် / အခန်း: ").Bold();
                                       t.Span("____________________________________________________________________");
                                   });

                                col.Item()
                                   .PaddingVertical(12)
                                   .LineHorizontal(0.5f)
                                   .LineColor(Colors.Grey.Lighten2);

                                // =================================================
                                // PARENT INFORMATION
                                // =================================================

                                col.Item()
                                   .Text("မိဘ အချက်အလက်များ")
                                   .FontSize(12)
                                   .Bold();

                                col.Item()
                                   .PaddingTop(8)
                                   .Row(row =>
                                   {
                                       row.RelativeItem().Column(c =>
                                       {
                                           c.Item()
                                            .Text(t =>
                                            {
                                                t.Span("ဖခင်အမည်: ").Bold();
                                                t.Span("_____________________");
                                            });

                                           c.Item()
                                            .PaddingTop(8)
                                            .Text(t =>
                                            {
                                                t.Span("မိခင်အမည်: ").Bold();
                                                t.Span("_____________________");
                                            });

                                           c.Item()
                                            .PaddingTop(8)
                                            .Text(t =>
                                            {
                                                t.Span("မိဘ ဖုန်းနံပါတ်: ").Bold();
                                                t.Span("_________________");
                                            });
                                       });

                                       row.RelativeItem().Column(c =>
                                       {
                                           c.Item()
                                            .Text(t =>
                                            {
                                                t.Span("ဖခင် NRC: ").Bold();
                                                t.Span("_____________________");
                                            });

                                           c.Item()
                                            .PaddingTop(8)
                                            .Text(t =>
                                            {
                                                t.Span("မိခင် NRC: ").Bold();
                                                t.Span("_____________________");
                                            });
                                       });
                                   });

                                col.Item()
                                   .PaddingTop(8)
                                   .Text(t =>
                                   {
                                       t.Span("နေရပ်လိပ်စာ: ").Bold();
                                       t.Span("____________________________________________________________________");
                                   });

                                col.Item()
                                   .PaddingVertical(12)
                                   .LineHorizontal(0.5f)
                                   .LineColor(Colors.Grey.Lighten2);

                                // =================================================
                                // GRADE 12 INFORMATION
                                // =================================================

                                col.Item()
                                   .Text("Grade 12 အချက်အလက်များ")
                                   .FontSize(12)
                                   .Bold();

                                col.Item()
                                   .PaddingTop(8)
                                   .Row(row =>
                                   {
                                       row.RelativeItem(2)
                                          .Text(t =>
                                          {
                                              t.Span("Grade 12 ခုံနံပါတ်: ").Bold();
                                              t.Span("_________________");
                                          });

                                       row.RelativeItem(3)
                                          .Text(t =>
                                          {
                                              t.Span("တက်ခဲ့သည့် ကျောင်း: ").Bold();
                                              t.Span("_______________________");
                                          });

                                       row.RelativeItem(2)
                                          .Text(t =>
                                          {
                                              t.Span("မြို့: ").Bold();
                                              t.Span("_______________");
                                          });
                                   });

                                col.Item()
                                   .PaddingVertical(12)
                                   .LineHorizontal(0.5f)
                                   .LineColor(Colors.Grey.Lighten2);

                                // =================================================
                                // ACADEMIC RECORDS TABLE
                                // =================================================

                                col.Item()
                                   .Text("ပညာအရည်အချင်း မှတ်တမ်းများ")
                                   .FontSize(12)
                                   .Bold();

                                col.Item()
                                   .PaddingTop(8)
                                   .Table(table =>
                                   {
                                       table.ColumnsDefinition(columns =>
                                       {
                                           columns.RelativeColumn(2);
                                           columns.RelativeColumn(2);
                                           columns.RelativeColumn(2);
                                           columns.RelativeColumn(1);
                                       });

                                       table.Header(header =>
                                       {
                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("သင်တန်းနှစ်")
                                                 .Bold();

                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("ခုံအမှတ်")
                                                 .Bold();

                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("တက်ရောက်သည့်နှစ်")
                                                 .Bold();

                                           header.Cell()
                                                 .Background(Colors.Grey.Lighten3)
                                                 .Padding(5)
                                                 .Text("ကြိမ်ရေ")
                                                 .Bold();
                                       });

                                       // Four blank rows
                                       for (int i = 0; i < 4; i++)
                                       {
                                           table.Cell()
                                                .BorderBottom(0.5f)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .PaddingVertical(8)
                                                .PaddingHorizontal(5)
                                                .Text("");

                                           table.Cell()
                                                .BorderBottom(0.5f)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .PaddingVertical(8)
                                                .PaddingHorizontal(5)
                                                .Text("");

                                           table.Cell()
                                                .BorderBottom(0.5f)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .PaddingVertical(8)
                                                .PaddingHorizontal(5)
                                                .Text("");

                                           table.Cell()
                                                .BorderBottom(0.5f)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .PaddingVertical(8)
                                                .PaddingHorizontal(5)
                                                .Text("");
                                       }
                                   });
                            });

                        // =================================================
                        // FOOTER / SIGNATURES
                        // =================================================

                        page.Footer().Column(footerCol =>
                        {
                            footerCol.Item().Row(row =>
                            {
                                row.RelativeItem().Column(sign =>
                                {
                                    sign.Item()
                                        .PaddingTop(35)
                                        .LineHorizontal(1)
                                        .LineColor(Colors.Grey.Darken1);

                                    sign.Item()
                                        .AlignCenter()
                                        .Text("ကျောင်းသား လက်မှတ်")
                                        .FontSize(10);
                                });

                                row.ConstantItem(100);

                                row.RelativeItem().Column(sign =>
                                {
                                    sign.Item()
                                        .PaddingTop(35)
                                        .LineHorizontal(1)
                                        .LineColor(Colors.Grey.Darken1);

                                    sign.Item()
                                        .AlignCenter()
                                        .Text("အဆောင်မှူး လက်မှတ်")
                                        .FontSize(10);
                                });
                            });
                        });
                    });
                }).GeneratePdf(targetPath);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"Unable to save the PDF. Please close the file which is open in another program and try again.\n\nError: {ex.Message}",
                    "File Access Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"PDF သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "PDF Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
