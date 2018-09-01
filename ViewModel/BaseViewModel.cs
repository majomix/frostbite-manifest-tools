using FrostbiteManifestSystemTools.Model;
using System;
using System.ComponentModel;
using System.IO;

namespace FrostbiteManifestSystemTools.ViewModel
{
    internal abstract class BaseViewModel : INotifyPropertyChanged
    {
        private int myCurrentProgress = 100;
        private string myInputDirectoryPath;
        private string myCurrentFile;
        private bool myHasError;

        public FrostbiteManifestEditor Model { get; protected set; }
        public string InputDirectoryPath
        {
            get { return myInputDirectoryPath ?? Directory.GetCurrentDirectory(); }
            set
            {
                if (myInputDirectoryPath != value)
                {
                    myInputDirectoryPath = value;
                    OnPropertyChanged("InputDirectoryPath");
                }
            }
        }
        public string CurrentFile
        {
            get { return myCurrentFile; }
            protected set
            {
                if (myCurrentFile != value)
                {
                    myCurrentFile = value;
                    OnPropertyChanged("CurrentFile");
                }
            }
        }
        public int CurrentProgress
        {
            get { return myCurrentProgress; }
            protected set
            {
                if (myCurrentProgress != value)
                {
                    myCurrentProgress = value;
                    OnPropertyChanged("CurrentProgress");
                }
            }
        }
        public bool HasError
        {
            get { return myHasError; }
            set
            {
                if (myHasError != value)
                {
                    myHasError = value;
                    OnPropertyChanged("HasError");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestClose;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void OnRequestClose(EventArgs e)
        {
            RequestClose(this, e);
        }

        public void LoadStructure()
        {
            Model.LoadFileStructure();
        }

        public void ExtractTranslationFiles(string targetDirectory)
        {
            Model.ExtractTextFile(targetDirectory);
            Model.ExtractFontFiles(targetDirectory);
        }

        public void ImportTranslationFiles(string inputDirectory)
        {
            Model.ImportTextFile(inputDirectory);
            Model.ImportFontFiles(inputDirectory);
        }
    }
}