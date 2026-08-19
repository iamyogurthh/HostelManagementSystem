namespace HostelManagementSystem.Messages
{
    public class BusyMessage
    {
        public bool IsBusy { get; }
        public string Message { get; }

        public BusyMessage(bool isBusy, string message = "ခဏစောင့်ပါ...")
        {
            IsBusy = isBusy;
            Message = message;
        }
    }
}