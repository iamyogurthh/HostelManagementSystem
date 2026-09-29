using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;

namespace HostelManagementSystem.ViewModels
{
    public partial class DataTransferViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _includeStudents = true;

        [ObservableProperty]
        private bool _includeInfrastructure = true; // Buildings, Floors, Rooms

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        #region EXPORT LOGIC
        [RelayCommand]
        private async Task ExportDataAsync()
        {
            if (!IncludeStudents && !IncludeInfrastructure)
            {
                StatusMessage = "အနည်းဆုံး ဒေတာ အမျိုးအစား တစ်ခု ရွေးချယ်ပါ။";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Hostel Data",
                Filter = "Hostel Backup File (*.hmsb)|*.hmsb|JSON File (*.json)|*.json",
                FileName = $"HMS_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.hmsb"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    WeakReferenceMessenger.Default.Send(new BusyMessage(true, "ဒေတာများ ထုတ်ယူနေပါသည်..."));

                    await Task.Run(async () =>
                    {
                        using var db = new AppDbContext();
                        var payload = new HostelBackupPayload
                        {
                            ExportedAt = DateTime.Now,
                            MachineName = Environment.MachineName
                        };

                        if (IncludeInfrastructure)
                        {
                            payload.Buildings = await db.Buildings.AsNoTracking().ToListAsync();
                            payload.Floors = await db.Floors.AsNoTracking().ToListAsync();
                            payload.Rooms = await db.Rooms.AsNoTracking().ToListAsync();
                        }

                        if (IncludeStudents)
                        {
                            payload.Students = await db.Students.AsNoTracking().ToListAsync();
                            payload.AcademicRecords = await db.StudentAcademicRecords.AsNoTracking().ToListAsync();
                        }

                        var jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            ReferenceHandler = ReferenceHandler.IgnoreCycles
                        };

                        // Stream directly to file stream to save memory allocations
                        await using var stream = File.Create(dialog.FileName);
                        await JsonSerializer.SerializeAsync(stream, payload, jsonOptions);
                    });

                    StatusMessage = "ဒေတာ သိမ်းဆည်းခြင်း အောင်မြင်ပါသည်။";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Export အမှား ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
                finally
                {
                    WeakReferenceMessenger.Default.Send(new BusyMessage(false, string.Empty));
                }
            }
        }
        #endregion

        #region IMPORT LOGIC (UPSERT BATCHING)
        [RelayCommand]
        private async Task ImportDataAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Hostel Data",
                Filter = "Hostel Backup File (*.hmsb;*.json)|*.hmsb;*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    WeakReferenceMessenger.Default.Send(new BusyMessage(true, "ဒေတာများ ထည့်သွင်းနေပါသည်..."));

                    var updatedEntityTypes = new HashSet<string>();

                    await Task.Run(async () =>
                    {
                        // 1. Stream JSON file directly from disk
                        await using var stream = File.OpenRead(dialog.FileName);
                        var jsonOptions = new JsonSerializerOptions
                        {
                            ReferenceHandler = ReferenceHandler.IgnoreCycles
                        };

                        var payload = await JsonSerializer.DeserializeAsync<HostelBackupPayload>(stream, jsonOptions);

                        if (payload == null)
                        {
                            throw new InvalidDataException("ဖိုင်အချက်အလက် မမှန်ကန်ပါ။");
                        }

                        using var db = new AppDbContext();

                        // Disable AutoDetectChanges for high performance imports
                        db.ChangeTracker.AutoDetectChangesEnabled = false;

                        using var transaction = await db.Database.BeginTransactionAsync();

                        try
                        {
                            // 2. Upsert Infrastructure
                            if (payload.Buildings?.Count > 0)
                            {
                                await BatchUpsertAsync(db, payload.Buildings, b => b.Id);
                                updatedEntityTypes.Add("Building");
                            }

                            if (payload.Floors?.Count > 0)
                            {
                                await BatchUpsertAsync(db, payload.Floors, f => f.Id);
                                updatedEntityTypes.Add("Floor");
                            }

                            if (payload.Rooms?.Count > 0)
                            {
                                await BatchUpsertAsync(db, payload.Rooms, r => r.Id);
                                updatedEntityTypes.Add("Room");
                            }

                            // 3. Upsert Students (10,000+ records handled efficiently)
                            if (payload.Students?.Count > 0)
                            {
                                await BatchUpsertAsync(db, payload.Students, s => s.Id);
                                updatedEntityTypes.Add("Student");
                            }

                            // 4. Upsert Academic Records
                            if (payload.AcademicRecords?.Count > 0)
                            {
                                await BatchUpsertAsync(db, payload.AcademicRecords, ar => ar.Id);
                                updatedEntityTypes.Add("StudentAcademicRecord");
                            }

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    });

                    // 5. Notify UI using BULK messages per entity type (Prevents Dispatcher lockup)
                    foreach (var entityName in updatedEntityTypes)
                    {
                        WeakReferenceMessenger.Default.Send(
                            new DatabaseUpdatedMessage(entityName, 0, DatabaseOperation.Inserted)
                        );
                    }

                    StatusMessage = "ဒေတာ ထည့်သွင်းခြင်း အောင်မြင်ပါသည်။";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Import အမှား ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
                finally
                {
                    WeakReferenceMessenger.Default.Send(new BusyMessage(false, string.Empty));
                }
            }
        }

        /// <summary>
        /// Generic helper method to perform high-speed bulk inserts and updates in batches.
        /// </summary>
        private async Task BatchUpsertAsync<T>(AppDbContext db, List<T> items, Func<T, int> keySelector) where T : class
        {
            if (items == null || items.Count == 0) return;

            // Single query to fetch all existing Primary Keys into a HashSet for O(1) lookups
            var existingIds = (await db.Set<T>()
                .AsNoTracking()
                .Select(x => keySelector(x))
                .ToListAsync())
                .ToHashSet();

            var toAdd = items.Where(x => !existingIds.Contains(keySelector(x))).ToList();
            var toUpdate = items.Where(x => existingIds.Contains(keySelector(x))).ToList();

            const int batchSize = 1000;

            // Batch Add
            for (int i = 0; i < toAdd.Count; i += batchSize)
            {
                var batch = toAdd.Skip(i).Take(batchSize);
                await db.Set<T>().AddRangeAsync(batch);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }

            // Batch Update
            for (int i = 0; i < toUpdate.Count; i += batchSize)
            {
                var batch = toUpdate.Skip(i).Take(batchSize);
                db.Set<T>().UpdateRange(batch);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        }
        #endregion
    }
}