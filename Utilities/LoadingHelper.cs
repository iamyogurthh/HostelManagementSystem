using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Messages;
using System;
using System.Threading.Tasks;

namespace HostelManagementSystem.Utilities
{
    public static class LoadingHelper
    {
        public static async Task ExecuteWithLoadingAsync(Func<Task> action, string busyText = "ခဏစောင့်ပါ...")
        {
            try
            {
                // Turn Spinner ON
                WeakReferenceMessenger.Default.Send(new BusyMessage(true, busyText));

                await action();
            }
            finally
            {
                // Turn Spinner OFF (always executes even if errors occur)
                WeakReferenceMessenger.Default.Send(new BusyMessage(false));
            }
        }
    }
}