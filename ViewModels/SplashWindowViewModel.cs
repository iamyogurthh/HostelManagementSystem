using HostelManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HostelManagementSystem.ViewModels
{
    public partial class SplashWindowViewModel: ObservableObject
    {
        public string CurrentVersion => AppVersionInfo.CurrentVersion;
    }
}
