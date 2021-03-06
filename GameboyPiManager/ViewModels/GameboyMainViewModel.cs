﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFUtilities.MVVM;
using GameboyPiManager.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Specialized;
using System.Windows.Shapes;
using System.Windows.Media;
using GameboyPiManager.ViewModels.Interfaces;
using System.ComponentModel;

namespace GameboyPiManager.ViewModels
{
    public class GameboyMainViewModel : ViewModel<Gameboy>, IGameboyMain
    {
        public VideogameUploaderViewModel VideogameUploaderVM { get; private set; }
        public ICommand DownloadBackupCmd { get; private set; }
        public ICommand UploadBackupCmd { get; private set; }
        public ICommand ReloadCmd { get; private set; }

        private BackgroundWorker downloadWorker;
        private BackgroundWorker uploadWorker;

        private string statusMessage;
        public string StatusMessage
        {
            get { return statusMessage; }
            set
            {
                if (StatusMessage != value)
                {
                    statusMessage = value;
                    this.OnPropertyChanged("StatusMessage");
                }
            }
        }

        public bool IsConnected
        {
            get { return Model.IsConnected; }
            set
            {
                if (IsConnected != value)
                {
                    Model.IsConnected = value;
                    this.OnPropertyChanged(() => IsConnected);
                }
            }
        }

        private ObservableCollection<VideogameConsoleViewModel> consolesVMs;
        public ObservableCollection<VideogameConsoleViewModel> ConsolesVMs
        {
            get { return consolesVMs; }
            private set
            {
                if (ConsolesVMs != value)
                {
                    consolesVMs = value;
                    this.OnPropertyChanged("ConsolesVMs");
                }
            }
        }
       
        #region Contructor
        public GameboyMainViewModel(Gameboy model) 
            : base(model)
        {
            VideogameUploaderVM = new VideogameUploaderViewModel(model, this);
            VideogameUploaderVM.DoReload += new ReloadViewModelHandler(reload);

            createBackgroundWorker();

            DownloadBackupCmd = new Command(selectedPath => downloadWorker.RunWorkerAsync(selectedPath));
            UploadBackupCmd = new Command(selectedPath => uploadWorker.RunWorkerAsync(selectedPath));
            
            ReloadCmd = new Command(p => reload());
            ConsolesVMs = new ObservableCollection<VideogameConsoleViewModel>();
            createConsolesVMs();
            ConsolesVMs.CollectionChanged += (s, e) => OnPropertyChanged(() => ConsolesVMs);
            Model.SetOnConnectionChanged(SetIsConnection);
        }

        private void createBackgroundWorker()
        {
            downloadWorker = new BackgroundWorker();
            downloadWorker.DoWork += DownloadWorker_DoWork;
            downloadWorker.RunWorkerCompleted += DownloadWorker_RunWorkerCompleted;

            uploadWorker = new BackgroundWorker();
            uploadWorker.DoWork += UploadWorker_DoWork;
            uploadWorker.RunWorkerCompleted += UploadWorker_RunWorkerCompleted;
        }

        private void DownloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Model.DownloadBackup(e.Argument as string, isCompleted => e.Result = isCompleted);
        }

        private void DownloadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool successfulDownload = (bool)e.Result;
            if (successfulDownload)
            {
                StatusMessage = "Download completed!";
            }
            else
            {
                StatusMessage = "Download was not completed. Please check logging.";
            }
        }

        private void UploadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Model.UploadBackup(e.Argument as string, isCompleted => e.Result = isCompleted);
        }

        private void UploadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool successfulDownload = (bool)e.Result;
            if (successfulDownload)
            {
                StatusMessage = "Upload completed!";
            }
            else
            {
                StatusMessage = "Upload was not completed. Please check logging.";
            }
        }

        private void createConsolesVMs()
        {
            ConsolesVMs.Clear();
            foreach (VideogameConsole consoleModel in Model.Consoles.Values)
            {
                ConsolesVMs.Add(new VideogameConsoleViewModel(consoleModel));
            }
        }
        #endregion

        private void SetIsConnection(bool isConnected)
        {
            this.IsConnected = isConnected;
        }

        private void OnDownloadFinished()
        {
            StatusMessage = "Download completed!";
        }

        private void reload()
        {
            Model.Reload();
            createConsolesVMs();
        }
    }
}
