using HostelManagementSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HostelManagementSystem.Views
{
    /// <summary>
    /// Interaction logic for SettingView.xaml
    /// </summary>
    public partial class SettingView : UserControl
    {
        public SettingView()
        {
            InitializeComponent();

            ThemeCheckBox.IsChecked = ThemeManager.IsDarkMode;
        }

        private void ThemeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = ThemeCheckBox.IsChecked ?? false;
            ThemeManager.SetTheme(isDark);
        }
    }
}
