using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace HostelManagementSystem.ViewModels.Base
{
    public abstract partial class PaginationViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalCount;

        public ObservableCollection<int> PageSizeOptions { get; } =
            new()
            {
        10,
        20,
        50,
      
            };

        protected abstract Task RefreshPageAsync();

        protected void UpdatePagination(int totalCount)
        {
            TotalCount = totalCount;

            TotalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)PageSize);

            if (CurrentPage < 1)
                CurrentPage = 1;

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
        }

        protected int GetSkipCount()
        {
            return (CurrentPage - 1) * PageSize;
        }

        protected void ResetPagination()
        {
            CurrentPage = 1;
            TotalPages = 1;
            TotalCount = 0;
        }

        [RelayCommand]
        private async Task FirstPageAsync()
        {
            if (CurrentPage <= 1)
                return;

            CurrentPage = 1;

            await RefreshPageAsync();
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage <= 1)
                return;

            CurrentPage--;

            await RefreshPageAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages)
                return;

            CurrentPage++;

            await RefreshPageAsync();
        }

        [RelayCommand]
        private async Task LastPageAsync()
        {
            if (CurrentPage >= TotalPages)
                return;

            CurrentPage = TotalPages;

            await RefreshPageAsync();
        }

        partial void OnPageSizeChanged(int value)
        {
            if (value <= 0)
                return;

            CurrentPage = 1;

            // Important:
            // Do not use Task.Run here.
            //
            // RefreshPageAsync() must be allowed to start from
            // the WPF UI thread so that derived ViewModels can
            // safely update ObservableCollections after awaits.
            _ = RefreshPageAsync();
        }
    }
}