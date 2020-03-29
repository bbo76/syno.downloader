namespace SynoDownloader.ViewModel
{
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using MaterialDesignThemes.Wpf;

    public class AddSubFolderViewModel : ViewModelBase
    {
        private string _newFolderName;
        public string NewFolderName
        {
            get { return _newFolderName; }
            set
            {
                Set(ref _newFolderName, value);
                OkCommand.RaiseCanExecuteChanged();
            }
        }

        private RelayCommand _okCommand;
        public RelayCommand OkCommand
        {
            get { return _okCommand ?? (_okCommand = new RelayCommand(Validate, () => !string.IsNullOrEmpty(NewFolderName))); }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand
        {
            get { return _cancelCommand ?? (_cancelCommand = new RelayCommand(() => DialogHost.CloseDialogCommand.Execute(false, null))); }
        }

        private void Validate()
        {
            DialogHost.CloseDialogCommand.Execute(true, null);
        }
    }
}
