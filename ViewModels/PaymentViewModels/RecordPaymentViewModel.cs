using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Services.Payment;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels.PaymentViewModels
{
    public partial class RecordPaymentViewModel : ObservableObject
    {
        // =========================================================
        // SERVICES
        // =========================================================

        private readonly PaymentService _paymentService;

        private readonly Func<Task>? _navigateBackCallback;


        // =========================================================
        // STUDENT
        // =========================================================

        [ObservableProperty]
        private Student? _student;


        // =========================================================
        // FINANCIAL
        // =========================================================

        [ObservableProperty]
        private decimal _outstandingBalance;

        [ObservableProperty]
        private decimal _paymentAmount;


        // =========================================================
        // PAYMENT INFORMATION
        // =========================================================

        [ObservableProperty]
        private PaymentMethod _selectedPaymentMethod =
            PaymentMethod.Cash;

        [ObservableProperty]
        private string _receiptNumber =
            string.Empty;

        [ObservableProperty]
        private DateTime _paymentDate =
            DateTime.Now;

        [ObservableProperty]
        private string _notes =
            string.Empty;


        // =========================================================
        // STATE
        // =========================================================

        [ObservableProperty]
        private string _errorMessage =
            string.Empty;

        [ObservableProperty]
        private bool _isLoading;


        // =========================================================
        // PAYMENT METHODS
        // =========================================================

        public ObservableCollection<PaymentMethod> PaymentMethods { get; } =
            new(Enum.GetValues<PaymentMethod>());


        // =========================================================
        // UI STATE
        // =========================================================

        public bool HasStudent =>
            Student != null;

        public bool HasError =>
            !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool CanSubmit =>
            Student != null &&
            PaymentAmount > 0 &&
            PaymentAmount <= OutstandingBalance &&
            !string.IsNullOrWhiteSpace(ReceiptNumber) &&
            !IsLoading;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RecordPaymentViewModel(
            Student student,
            Func<Task>? navigateBackCallback = null)
        {
            _paymentService =
                new PaymentService();

            _navigateBackCallback =
                navigateBackCallback;

            Student =
                student;

            _ = InitializeAsync();
        }


        // =========================================================
        // PROPERTY CHANGE HANDLERS
        // =========================================================

        partial void OnStudentChanged(
            Student? value)
        {
            OnPropertyChanged(
                nameof(HasStudent));

            OnPropertyChanged(
                nameof(CanSubmit));
        }


        partial void OnPaymentAmountChanged(
            decimal value)
        {
            ErrorMessage =
                string.Empty;

            OnPropertyChanged(
                nameof(CanSubmit));
        }


        partial void OnSelectedPaymentMethodChanged(
            PaymentMethod value)
        {
            OnPropertyChanged(
                nameof(CanSubmit));
        }


        partial void OnReceiptNumberChanged(
            string value)
        {
            OnPropertyChanged(
                nameof(CanSubmit));
        }


        partial void OnPaymentDateChanged(
            DateTime value)
        {
            ErrorMessage =
                string.Empty;
        }


        partial void OnIsLoadingChanged(
            bool value)
        {
            OnPropertyChanged(
                nameof(CanSubmit));
        }


        partial void OnErrorMessageChanged(
            string value)
        {
            OnPropertyChanged(
                nameof(HasError));
        }


        // =========================================================
        // INITIALIZATION
        // =========================================================

        private async Task InitializeAsync()
        {
            if (Student == null)
            {
                ErrorMessage =
                    "ကျောင်းသားအချက်အလက် မတွေ့ရှိပါ။";

                return;
            }

            try
            {
                IsLoading = true;

                ErrorMessage =
                    string.Empty;


                // -------------------------------------------------
                // LOAD EXISTING OUTSTANDING BALANCE
                // -------------------------------------------------

                await ReloadExistingOutstandingBalanceAsync();


                if (OutstandingBalance <= 0)
                {
                    OutstandingBalance = 0m;

                    ErrorMessage =
                        "ဤကျောင်းသားတွင် ပေးရန်ကျန်ရှိသော အခကြေးငွေ မရှိပါ။";

                    return;
                }


                // -------------------------------------------------
                // RESET PREVIEW
                // -------------------------------------------------

                OutstandingBalance =
                    await _paymentService.GetOutstandingBalanceAsync(Student.Id);

                // -------------------------------------------------
                // RECEIPT
                // -------------------------------------------------

                GenerateReceiptNumber();


                // Default to complete current outstanding balance.
                PaymentAmount =
                    OutstandingBalance;
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"ငွေပေးချေမှုအချက်အလက်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }


        // =========================================================
        // RELOAD OUTSTANDING BALANCE
        // =========================================================

        private async Task ReloadExistingOutstandingBalanceAsync()
        {
            if (Student == null)
            {
                return;
            }

            OutstandingBalance =
                await _paymentService
                    .GetOutstandingBalanceAsync(
                        Student.Id);
        }


        // =========================================================
        // RECEIPT NUMBER
        // =========================================================

        private void GenerateReceiptNumber()
        {
            ReceiptNumber =
                $"RCP-{DateTime.Now:yyyyMMdd-HHmmssfff}";
        }


        // =========================================================
        // SET FULL AMOUNT
        // =========================================================

        [RelayCommand]
        private void SetFullAmount()
        {
            if (OutstandingBalance <= 0)
                return;

            PaymentAmount =
                OutstandingBalance;
        }


        // =========================================================
        // CLEAR AMOUNT
        // =========================================================

        [RelayCommand]
        private void ClearAmount()
        {
            PaymentAmount =
                0m;
        }


        // =========================================================
        // RECORD PAYMENT
        // =========================================================

        [RelayCommand]
        private async Task RecordPaymentAsync()
        {
            if (Student == null)
            {
                ErrorMessage =
                    "ကျေးဇူးပြု၍ ကျောင်းသားကို ရွေးချယ်ပါ။";

                return;
            }

            ErrorMessage =
                string.Empty;


            // -----------------------------------------------------
            // AMOUNT VALIDATION
            // -----------------------------------------------------

            if (PaymentAmount <= 0)
            {
                ErrorMessage =
                    "ငွေပေးချေမည့်ပမာဏသည် ၀ ထက်ကြီးရပါမည်။";

                return;
            }


            if (PaymentAmount > OutstandingBalance)
            {
                ErrorMessage =
                    $"ငွေပေးချေမည့်ပမာဏသည် လက်ကျန်ငွေ {OutstandingBalance:N0} MMK ထက် မကျော်ရပါ။";

                return;
            }


            // -----------------------------------------------------
            // RECEIPT
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    ReceiptNumber))
            {
                ErrorMessage =
                    "ပြေစာနံပါတ် ထည့်သွင်းပေးပါ။";

                return;
            }


            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (!Enum.IsDefined(
                    SelectedPaymentMethod))
            {
                ErrorMessage =
                    "ငွေပေးချေမှုနည်းလမ်း မမှန်ကန်ပါ။";

                return;
            }


            // -----------------------------------------------------
            // DATE
            // -----------------------------------------------------

            if (PaymentDate > DateTime.Now)
            {
                ErrorMessage =
                    "ငွေပေးချေသည့်ရက်သည် လက်ရှိအချိန်ထက် နောက်ကျ၍ မရပါ။";

                return;
            }


            // -----------------------------------------------------
            // CONFIRMATION
            // -----------------------------------------------------

            var confirmation =
                MessageBox.Show(
                    $"ကျောင်းသား: {Student.Name}\n\n" +
                    $"ငွေပေးချေမည့်ပမာဏ: {PaymentAmount:N0} MMK\n" +
                    $"ငွေပေးချေမှုနည်းလမ်း: {GetPaymentMethodText(SelectedPaymentMethod)}\n" +
                    $"ပြေစာနံပါတ်: {ReceiptNumber}" +
                    "\n\n" +
                    "ဤငွေပေးချေမှုကို မှတ်တမ်းတင်မည်မှာ သေချာပါသလား?",
                    "ငွေပေးချေမှု အတည်ပြုရန်",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);


            if (confirmation !=
                MessageBoxResult.Yes)
            {
                return;
            }


                // -----------------------------------------------------
                // SAVE PAYMENT
                // -----------------------------------------------------

            try
            {
                IsLoading = true;

                ErrorMessage =
                    string.Empty;


                await _paymentService
                    .RecordPaymentAsync(
                        studentId:
                            Student.Id,

                        amount:
                            PaymentAmount,

                        paymentMethod:
                            SelectedPaymentMethod,

                        receiptNumber:
                            ReceiptNumber.Trim(),

                        notes:
                            string.IsNullOrWhiteSpace(
                                Notes)
                                ? null
                                : Notes.Trim(),

                        paymentDate:
                            PaymentDate);


                var recordedAmount =
                    PaymentAmount;

                var recordedReceipt =
                    ReceiptNumber;


                // -------------------------------------------------
                // SUCCESS
                // -------------------------------------------------

                MessageBox.Show(
                    $"ငွေပေးချေမှု အောင်မြင်စွာ မှတ်တမ်းတင်ပြီးပါပြီ။\n\n" +
                    $"ပမာဏ: {recordedAmount:N0} MMK\n" +
                    $"ပြေစာနံပါတ်: {recordedReceipt}",
                    "ငွေပေးချေမှု အောင်မြင်သည်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);


                // -------------------------------------------------
                // RETURN
                // -------------------------------------------------

                if (_navigateBackCallback != null)
                {
                    await _navigateBackCallback();
                }
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage =
                    ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"ငွေပေးချေမှု မှတ်တမ်းတင်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }


        // =========================================================
        // CANCEL
        //
        // Nothing is saved here.
        // =========================================================

        [RelayCommand]
        private async Task CancelAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback();
            }
        }


        // =========================================================
        // PAYMENT METHOD TEXT
        // =========================================================

        private static string GetPaymentMethodText(
            PaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.Cash =>
                    "Cash",

                PaymentMethod.KPay =>
                    "KPay",

                PaymentMethod.Wave =>
                    "Wave",

                PaymentMethod.Other =>
                    "Other",

                _ =>
                    paymentMethod.ToString()
            };
        }
    }
}
