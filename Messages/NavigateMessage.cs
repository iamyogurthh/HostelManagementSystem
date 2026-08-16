using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace HostelManagementSystem.Messages
{
    public class NavigateMessage
    {
        public ObservableObject TargetViewModel { get; }
        public NavigateMessage(ObservableObject targetViewModel) 
        {
            TargetViewModel = targetViewModel;
        }
    }
}
