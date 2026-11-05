using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;

namespace HostelManagementSystem.ViewModels
{
    public partial class DataTransferViewModel : ObservableObject
    {
        // =========================================================
        // OPTIONS
        // =========================================================

        [ObservableProperty]
        private bool _includeStudents = true;

        [ObservableProperty]
        private bool _includeInfrastructure = true;

        [ObservableProperty]
        private bool _includeFinancialData = true;


        // =========================================================
        // STATUS
        // =========================================================

        [ObservableProperty]
        private string _statusMessage = string.Empty;


        // =========================================================
        // EXPORT
        // =========================================================

        [RelayCommand]
        private async Task ExportDataAsync()
        {
            if (!IncludeStudents &&
                !IncludeInfrastructure &&
                !IncludeFinancialData)
            {
                StatusMessage =
                    "အနည်းဆုံး ဒေတာ အမျိုးအစား တစ်ခု ရွေးချယ်ပါ။";

                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Hostel Data",

                Filter =
                    "Hostel Backup File (*.hmsb)|*.hmsb|" +
                    "JSON File (*.json)|*.json",

                FileName =
                    $"HMS_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.hmsb"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                WeakReferenceMessenger.Default.Send(
                    new BusyMessage(
                        true,
                        "ဒေတာများ ထုတ်ယူနေပါသည်..."));

                await Task.Run(async () =>
                {
                    await using var db =
                        new AppDbContext();

                    var payload =
                        new HostelBackupPayload
                        {
                            ExportedAt =
                                DateTime.Now,

                            MachineName =
                                Environment.MachineName
                        };


                    // =================================================
                    // INFRASTRUCTURE
                    // =================================================

                    if (IncludeInfrastructure)
                    {
                        payload.Buildings =
                            await db.Buildings
                                .AsNoTracking()
                                .ToListAsync();

                        payload.Floors =
                            await db.Floors
                                .AsNoTracking()
                                .ToListAsync();

                        payload.Rooms =
                            await db.Rooms
                                .AsNoTracking()
                                .ToListAsync();

                        payload.RoomTypes =
                            await db.RoomTypes
                                .AsNoTracking()
                                .ToListAsync();
                    }


                    // =================================================
                    // STUDENT DATA
                    // =================================================

                    if (IncludeStudents)
                    {
                        payload.StudentCategories =
                            await db.StudentCategories
                                .AsNoTracking()
                                .ToListAsync();

                        payload.Students =
                            await db.Students
                                .AsNoTracking()
                                .ToListAsync();

                        payload.AcademicRecords =
                            await db.StudentAcademicRecords
                                .AsNoTracking()
                                .ToListAsync();

                        payload.StudentAccommodations =
                            await db.StudentAccommodations
                                .AsNoTracking()
                                .ToListAsync();
                    }


                    // =================================================
                    // FINANCIAL DATA
                    // =================================================

                    if (IncludeFinancialData)
                    {
                        payload.FeeRules =
                            await db.FeeRules
                                .AsNoTracking()
                                .ToListAsync();

                        payload.FeeCharges =
                            await db.FeeCharges
                                .AsNoTracking()
                                .ToListAsync();

                        payload.Payments =
                            await db.Payments
                                .AsNoTracking()
                                .ToListAsync();

                        payload.PaymentAllocations =
                            await db.PaymentAllocations
                                .AsNoTracking()
                                .ToListAsync();
                    }


                    // =================================================
                    // JSON OPTIONS
                    // =================================================

                    var jsonOptions =
                        new JsonSerializerOptions
                        {
                            WriteIndented = true,

                            ReferenceHandler =
                                ReferenceHandler.IgnoreCycles
                        };


                    // =================================================
                    // WRITE DIRECTLY TO FILE
                    // =================================================

                    await using var stream =
                        File.Create(dialog.FileName);

                    await JsonSerializer.SerializeAsync(
                        stream,
                        payload,
                        jsonOptions);
                });


                
                StatusMessage =
                    "ဒေတာ သိမ်းဆည်းခြင်း အောင်မြင်ပါသည်။";
               
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Export အမှား ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";

            }
            finally
            {
                WeakReferenceMessenger.Default.Send(
                    new BusyMessage(
                        false,
                        string.Empty));
                await Task.Delay(TimeSpan.FromSeconds(5));
                StatusMessage = string.Empty;
            }
        }


        // =========================================================
        // IMPORT
        // =========================================================

        [RelayCommand]
        private async Task ImportDataAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Hostel Data",

                Filter =
                    "Hostel Backup File (*.hmsb;*.json)|*.hmsb;*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                WeakReferenceMessenger.Default.Send(
                    new BusyMessage(
                        true,
                        "ဒေတာများ ထည့်သွင်းနေပါသည်..."));

                var updatedEntityTypes =
                    new HashSet<string>();


                await Task.Run(async () =>
                {
                    // =================================================
                    // READ BACKUP FILE
                    // =================================================

                    await using var stream =
                        File.OpenRead(dialog.FileName);

                    var jsonOptions =
                        new JsonSerializerOptions
                        {
                            ReferenceHandler =
                                ReferenceHandler.IgnoreCycles
                        };

                    var payload =
                        await JsonSerializer
                            .DeserializeAsync<
                                HostelBackupPayload>(
                                stream,
                                jsonOptions);

                    if (payload == null)
                    {
                        throw new InvalidDataException(
                            "ဖိုင်အချက်အလက် မမှန်ကန်ပါ။");
                    }


                    // =================================================
                    // DATABASE
                    // =================================================

                    await using var db =
                        new AppDbContext();

                    db.ChangeTracker
                        .AutoDetectChangesEnabled = false;


                    await using var transaction =
                        await db.Database
                            .BeginTransactionAsync();

                    try
                    {
                        // =================================================
                        // 1. ROOM TYPES
                        // =================================================
                        //
                        // Rooms depend on RoomTypes.
                        // =================================================

                        if (payload.RoomTypes?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.RoomTypes,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "RoomType");
                        }


                        // =================================================
                        // 2. BUILDINGS
                        // =================================================

                        if (payload.Buildings?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.Buildings,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "Building");
                        }


                        // =================================================
                        // 3. FLOORS
                        // =================================================

                        if (payload.Floors?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.Floors,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "Floor");
                        }


                        // =================================================
                        // 4. ROOMS
                        // =================================================

                        if (payload.Rooms?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.Rooms,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "Room");
                        }


                        // =================================================
                        // 5. STUDENT CATEGORIES
                        // =================================================
                        //
                        // Students and FeeRules depend on categories.
                        // =================================================

                        if (payload.StudentCategories?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.StudentCategories,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "StudentCategory");
                        }


                        // =================================================
                        // 6. STUDENTS
                        // =================================================

                        if (payload.Students?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.Students,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "Student");
                        }


                        // =================================================
                        // 7. ACADEMIC RECORDS
                        // =================================================

                        if (payload.AcademicRecords?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.AcademicRecords,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "StudentAcademicRecord");
                        }


                        // =================================================
                        // 8. STUDENT ACCOMMODATIONS
                        // =================================================
                        //
                        // Depends on Students + Rooms.
                        // =================================================

                        if (payload.StudentAccommodations?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.StudentAccommodations,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "StudentAccommodation");
                        }


                        // =================================================
                        // 9. FEE RULES
                        // =================================================
                        //
                        // Depends on StudentCategory + RoomType.
                        // =================================================

                        if (payload.FeeRules?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.FeeRules,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "FeeRule");
                        }


                        // =================================================
                        // 10. FEE CHARGES
                        // =================================================
                        //
                        // Depends on Students,
                        // StudentAccommodation and FeeRule.
                        // =================================================

                        if (payload.FeeCharges?.Count > 0)
                        {
                            // Backward compatibility for backups created before
                            // manual billing fields were introduced. Older files
                            // may contain an unknown source value or no description.
                            foreach (var charge in payload.FeeCharges)
                            {
                                if (!Enum.IsDefined(charge.Source))
                                {
                                    charge.Source = FeeChargeSource.Manual;
                                }

                                if (charge.Source == FeeChargeSource.Manual &&
                                    string.IsNullOrWhiteSpace(charge.Description))
                                {
                                    charge.Description = "Imported fee charge";
                                }
                            }

                            await BatchUpsertAsync(
                                db,
                                payload.FeeCharges,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "FeeCharge");
                        }


                        // =================================================
                        // 11. PAYMENTS
                        // =================================================
                        //
                        // Depends on Students.
                        // =================================================

                        if (payload.Payments?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.Payments,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "Payment");
                        }


                        // =================================================
                        // 12. PAYMENT ALLOCATIONS
                        // =================================================
                        //
                        // Depends on Payment + FeeCharge.
                        // Must be last.
                        // =================================================

                        if (payload.PaymentAllocations?.Count > 0)
                        {
                            await BatchUpsertAsync(
                                db,
                                payload.PaymentAllocations,
                                x => x.Id);

                            updatedEntityTypes.Add(
                                "PaymentAllocation");
                        }


                        // =================================================
                        // COMMIT
                        // =================================================

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                    finally
                    {
                        db.ChangeTracker
                            .AutoDetectChangesEnabled = true;
                    }
                });


                // =====================================================
                // NOTIFY UI
                // =====================================================

                foreach (var entityName in
                         updatedEntityTypes)
                {
                    WeakReferenceMessenger.Default.Send(
                        new DatabaseUpdatedMessage(
                            entityName,
                            0,
                            DatabaseOperation.Inserted));
                }


                StatusMessage =
                    "ဒေတာ ထည့်သွင်းခြင်း အောင်မြင်ပါသည်။";
               
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Import အမှား ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(
                    new BusyMessage(
                        false,
                        string.Empty));
                await Task.Delay(TimeSpan.FromSeconds(5));
                StatusMessage = string.Empty;
            }
        }


        // =========================================================
        // BATCH UPSERT
        // =========================================================

        private static async Task BatchUpsertAsync<T>(
            AppDbContext db,
            List<T> items,
            Func<T, int> keySelector)
            where T : class
        {
            if (items == null ||
                items.Count == 0)
            {
                return;
            }


            // =====================================================
            // GET EXISTING IDS
            // =====================================================

            // Evaluate the key selector in memory. A Func<T, int> cannot be
            // translated by EF Core when used directly inside IQueryable.Select.
            var existingEntities =
                await db.Set<T>()
                    .AsNoTracking()
                    .ToListAsync();

            var existingIds =
                existingEntities
                    .Select(keySelector)
                    .ToHashSet();


            // =====================================================
            // SPLIT ADD / UPDATE
            // =====================================================

            var toAdd =
                items
                    .Where(x =>
                        !existingIds.Contains(
                            keySelector(x)))
                    .ToList();

            var toUpdate =
                items
                    .Where(x =>
                        existingIds.Contains(
                            keySelector(x)))
                    .ToList();


            const int batchSize = 1000;


            // =====================================================
            // INSERT
            // =====================================================

            for (int i = 0;
                 i < toAdd.Count;
                 i += batchSize)
            {
                var batch =
                    toAdd
                        .Skip(i)
                        .Take(batchSize)
                        .ToList();

                await db.Set<T>()
                    .AddRangeAsync(batch);

                await db.SaveChangesAsync();

                db.ChangeTracker.Clear();
            }


            // =====================================================
            // UPDATE
            // =====================================================

            for (int i = 0;
                 i < toUpdate.Count;
                 i += batchSize)
            {
                var batch =
                    toUpdate
                        .Skip(i)
                        .Take(batchSize)
                        .ToList();

                db.Set<T>()
                    .UpdateRange(batch);

                await db.SaveChangesAsync();

                db.ChangeTracker.Clear();
            }
        }
    }
}
