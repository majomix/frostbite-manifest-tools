using FrostbiteManifestSystemTools.Model;
using System;
using System.ComponentModel;
using System.IO;

namespace FrostbiteManifestSystemTools.ViewModel
{
    internal abstract class BaseViewModel : INotifyPropertyChanged
    {
        private int myCurrentProgress = 100;
        private string myLoadedFilePath;
        private string myCurrentFile;
        private bool myHasError;

        public FrostbiteManifestEditor Model { get; protected set; }
        public string LoadedFilePath
        {
            get { return myLoadedFilePath; }
            set
            {
                if (myLoadedFilePath != value)
                {
                    myLoadedFilePath = value;
                    OnPropertyChanged("LoadedFilePath");
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
            using (ManifestBinaryReader reader = new ManifestBinaryReader(File.Open(LoadedFilePath, FileMode.Open)))
            {
                Model.LoadFileStructure(reader);
                OnPropertyChanged("Model");
            }
        }

        public void ExtractFile(string directory)
        {
            using (ManifestBinaryReader reader = new ManifestBinaryReader(File.Open(LoadedFilePath, FileMode.Open)))
            {
                //foreach (Entry entry in entryCollection)
                //{
                Model.ExtractFile(directory, reader);
                //CurrentProgress = (int)(currentSize * 100.0 / totalSize);
                //}
            }
        }

        public void ResolveNewFiles(string directory)
        {
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                string[] tokens = file.Split(new string[] { directory + @"\" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (!String.IsNullOrWhiteSpace(token))
                    {
                    }
                }
            }
        }

        public void SaveStructure(string path)
        {
            using (ManifestBinaryReader reader = new ManifestBinaryReader(File.Open(LoadedFilePath, FileMode.Open)))
            {
                using (ManifestBinaryWriter writer = new ManifestBinaryWriter(File.Open(path, FileMode.Create)))
                {
                    //foreach (Entry entry in entries)
                    //{
                    Model.SaveDataEntry(reader, writer);
                    //CurrentProgress = (int)(currentSize * 100.0 / totalSize);
                    //CurrentFile = entry.Name;
                    //}

                    Model.SaveIndex(writer);
                }
            }

            OnPropertyChanged("Model");
        }

        public string GenerateRandomName()
        {
            Random generator = new Random();
            return Path.ChangeExtension(LoadedFilePath, @".tmp_" + generator.Next().ToString());
        }
    }
}