namespace SynoDownloader.Models
{
    using GalaSoft.MvvmLight;
    using Renci.SshNet;

    public class DownloadFile : ObservableObject
    {
        public DownloadFile(string url, string outputDir)
        {
            Url = url;
            OutputDir = outputDir;
        }

        public string Url { get; set; }
        public string OutputDir { get; set; }

        private int _percentage;
        public int Percentage
        {
            get { return _percentage; }

            set { Set(ref _percentage, value); }
        }

        private string _downloadSpeed;
        public string DownloadSpeed
        {
            get { return _downloadSpeed; }
            set { Set(ref _downloadSpeed, value); }
        }

        private string _remainingTime;
        public string RemainingTime
        {
            get { return _remainingTime; }
            set { Set(ref _remainingTime, value); }
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get { return _isDownloading; }
            set { Set(ref _isDownloading, value); }
        }

        public SshClient DownloadSshCommand { get; set; }
    }
}
