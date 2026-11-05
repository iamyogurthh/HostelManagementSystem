using HostelManagementSystem.Database;
using HostelManagementSystem.Helpers;
using HostelManagementSystem.Services;
using HostelManagementSystem.ViewModels;
using HostelManagementSystem.Views;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Appearance;

namespace HostelManagementSystem
{
    public partial class App : Application
    {
        private LicenseService _licenseService = null!;

        private Window? _licenseWindow;
        private MainWindow? _mainWindow;
        private SplashWindow? _splashWindow;

        protected override async void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            // =====================================================
            // Application Shutdown Mode
            // =====================================================

            ShutdownMode =
                ShutdownMode.OnExplicitShutdown;

            _splashWindow =
                new SplashWindow();

            _splashWindow.Show();


            // =====================================================
            // Theme
            // =====================================================

            ThemeManager.ApplySavedTheme();


            // =====================================================
            // QuestPDF
            // =====================================================

            QuestPDF.Settings.License =
                LicenseType.Community;

            string fontPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    "Fonts",
                    "Pyidaungsu.ttf"
                );

            if (File.Exists(fontPath))
            {
                using var fontStream =
                    File.OpenRead(fontPath);

                FontManager.RegisterFontFromStream(
                    fontStream
                );
            }


            // =====================================================
            // Application Accent
            // =====================================================

            ApplicationAccentColorManager.Apply(
                System.Windows.Media.Color.FromRgb(
                    0x05,
                    0x50,
                    0x35
                ),
                ApplicationTheme.Dark
            );


            // =====================================================
            // Database
            // =====================================================

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }


            // =====================================================
            // License
            // =====================================================

            _licenseService =
                new LicenseService();


            LicenseState licenseState =
                _licenseService.GetLicenseState();


            // =====================================================
            // No Valid Local License
            // =====================================================

            if (licenseState != LicenseState.Active)
            {
                OpenLicenseWindow();
                return;
            }


            // =====================================================
            // Local License Is Active
            // =====================================================

            if (!_licenseService.ShouldValidateOnline())
            {
                OpenMainWindow();
                return;
            }


            // =====================================================
            // Periodic Online Validation
            // =====================================================

            LicenseValidationResult validationResult =
                await _licenseService.ValidateOnlineAsync();


            // =====================================================
            // Server Confirmed License
            // =====================================================

            if (validationResult.IsSuccess)
            {
                OpenMainWindow();
                return;
            }


            // =====================================================
            // Server Rejected License
            // =====================================================

            if (validationResult.IsServerRejected)
            {
                OpenLicenseWindow();
                return;
            }


            // =====================================================
            // Offline / Network Unavailable
            // =====================================================

            if (validationResult.IsOffline)
            {
                OpenMainWindow();
                return;
            }


            // =====================================================
            // Unexpected Validation Failure
            // =====================================================

            OpenMainWindow();
        }


        // =========================================================
        // License Window
        // =========================================================

        private void OpenLicenseWindow()
        {
            CloseSplashWindow();

            var licenseView =
                new LicenseActivationView();

            licenseView.ActivationSucceeded +=
                LicenseView_ActivationSucceeded;

            if (licenseView.DataContext
                is LicenseActivationViewModel viewModel)
            {
                viewModel.CloseRequested +=
                    LicenseView_CloseRequested;
            }

            _licenseWindow =
                new Window
                {
                    Title =
                        "Hostel Management System - License Activation",

                    Content =
                        licenseView,

                    Width =
                        1000,

                    Height =
                        700,

                    MinWidth =
                        800,

                    MinHeight =
                        600,

                    WindowStartupLocation =
                        WindowStartupLocation.CenterScreen,

                    Background =
                        System.Windows.Media.Brushes.Transparent,

                    AllowsTransparency =
                        true,

                    WindowStyle =
                        WindowStyle.None,

                    ResizeMode =
                        ResizeMode.CanResize
                };

            _licenseWindow.Closed +=
                LicenseWindow_Closed;

            _licenseWindow.Show();
        }


        // =========================================================
        // License Close Requested
        // =========================================================

        private void LicenseView_CloseRequested(
            object? sender,
            EventArgs e)
        {
            _licenseWindow?.Close();
        }


        // =========================================================
        // License Window Closed
        // =========================================================

        private void LicenseWindow_Closed(
            object? sender,
            EventArgs e)
        {
            if (_mainWindow == null)
            {
                Shutdown();
            }

            _licenseWindow = null;
        }


        // =========================================================
        // License Activated
        // =========================================================

        private void LicenseView_ActivationSucceeded(
            object? sender,
            EventArgs e)
        {
            OpenMainWindow();

            if (_licenseWindow != null)
            {
                _licenseWindow.Close();
                _licenseWindow = null;
            }
        }


        // =========================================================
        // Main HMS Window
        // =========================================================

        private void OpenMainWindow()
        {
            CloseSplashWindow();

            _mainWindow =
                new MainWindow();

            MainWindow =
                _mainWindow;

            _mainWindow.Closed +=
                MainWindow_Closed;

            _mainWindow.Show();

            _mainWindow.Activate();
        }

        private void CloseSplashWindow()
        {
            if (_splashWindow == null)
            {
                return;
            }

            _splashWindow.Close();
            _splashWindow = null;
        }


        // =========================================================
        // MainWindow Closed
        // =========================================================

        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            Shutdown();
        }
    }
}