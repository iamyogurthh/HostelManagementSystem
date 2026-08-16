using HostelManagementSystem.Database;
using HostelManagementSystem.Helpers;
using HostelManagementSystem.Models;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace HostelManagementSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.ApplySavedTheme();

            using (var db = new AppDbContext())
            {
                // Ensures database and tables exist
                db.Database.EnsureCreated();

                // Seed sample data if the Buildings table is empty
                if (!db.Buildings.Any())
                {
                    // 1. Create Buildings
                    var boysBuilding = new Building
                    {
                        Name = "Boys Building",
                        Img = "pack://application:,,,/Images/DefaultImages/building.jpg",
                        IsActive = true
                    };

                    var girlsBuilding = new Building
                    {
                        Name = "Girls Building",
                        Img = "pack://application:,,,/Images/DefaultImages/building.jpg",
                        IsActive = true
                    };

                    db.Buildings.AddRange(boysBuilding, girlsBuilding);
                    db.SaveChanges(); // Saves to generate IDs for Foreign Keys

                    // 2. Create Floors
                    var floor1 = new Floor
                    {
                        BuildingId = boysBuilding.Id,
                        Name = "1st Floor",
                        Img = "pack://application:,,,/Images/DefaultImages/floor.jpg",
                        IsActive = true
                    };

                    var floor2 = new Floor
                    {
                        BuildingId = girlsBuilding.Id,
                        Name = "1st Floor",
                        Img = "pack://application:,,,/Images/DefaultImages/floor.jpg",
                        IsActive = true
                    };

                    var floor3 = new Floor
                    {
                        BuildingId = girlsBuilding.Id,
                        Name = "2nd Floor",
                        Img = "pack://application:,,,/Images/DefaultImages/floor.jpg",
                        IsActive = true
                    };

                    db.Floors.AddRange(floor1, floor2, floor3);
                    db.SaveChanges(); // Saves to generate IDs for Foreign Keys

                    // 3. Create Rooms
                    db.Rooms.AddRange(
                        new Room
                        {
                            FloorId = floor1.Id,
                            Name = "Fancy Room",
                            Capacity = 3,
                            Img = "pack://application:,,,/Images/DefaultImages/room.jpg",
                            IsActive = true
                        },
                        new Room
                        {
                            FloorId = floor1.Id,
                            Name = "Normal Room",
                            Capacity = 5,
                            Img = "pack://application:,,,/Images/DefaultImages/room.jpg",
                            IsActive = true
                        },
                        new Room
                        {
                            FloorId = floor2.Id,
                            Name = "Classic Room",
                            Capacity = 4,
                            Img = "pack://application:,,,/Images/DefaultImages/room.jpg",
                            IsActive = true
                        },
                        new Room
                        {
                            FloorId = floor3.Id,
                            Name = "Full Room",
                            Capacity = 3,
                            Img = "pack://application:,,,/Images/DefaultImages/room.jpg",
                            IsActive = true
                        }
                    );
                    db.SaveChanges();
                }
            }
        }
    }
}