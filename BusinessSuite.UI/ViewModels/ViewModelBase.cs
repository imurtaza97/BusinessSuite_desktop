using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BusinessSuite.UI.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _statusMessageColor = "#111827";

        protected void SetStatusMessage(string message, string color = "#111827")
        {
            StatusMessage = message;
            StatusMessageColor = color;
        }

        protected void ClearStatusMessage()
        {
            StatusMessage = string.Empty;
        }
    }
}
