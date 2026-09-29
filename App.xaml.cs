using HostelManagementSystem.Database;
using HostelManagementSystem.Helpers;
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
            }
        }
    }
}