using FrostbiteManifestSystemTools.Model;
using FrostbiteManifestSystemTools.ViewModel.Commands;
using NDesk.Options;
using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace FrostbiteManifestSystemTools.ViewModel
{
    internal class OneTimeRunViewModel : BaseViewModel
    {
        private string myUserDirectory;
        public bool? Export { get; set; }
        public ICommand ExtractByParameterCommand { get; private set; }
        public ICommand ImportByParameterCommand { get; private set; }

        public OneTimeRunViewModel()
        {
            ParseCommandLine();
            Model = new FrostbiteManifestEditor(InputDirectoryPath);

            ImportByParameterCommand = new ImportByParameterCommand();
            ExtractByParameterCommand = new ExtractByParameterCommand();
        }

        public void ParseCommandLine()
        {
            OptionSet options = new OptionSet()
                .Add("export", value => Export = true)
                .Add("import", value => Export = false)
                .Add("gamedir=", value => InputDirectoryPath = CreateFullPath(value, true))
                .Add("userdir=", value => myUserDirectory = CreateFullPath(value, true));

            options.Parse(Environment.GetCommandLineArgs());
        }

        public void Extract()
        {
            if (myUserDirectory != null && InputDirectoryPath != null)
            {
                LoadStructure();
                ExtractTranslationFiles(myUserDirectory);
            }
        }

        public void Import()
        {
            if (myUserDirectory != null && InputDirectoryPath != null)
            {
                LoadStructure();
                ImportTranslationFiles(myUserDirectory);
            }
        }

        private string CreateFullPath(string path, bool isDirectory)
        {
            if (!String.IsNullOrEmpty(path) && !path.Contains(':'))
            {
                path = Directory.GetCurrentDirectory() + @"\" + path.Replace('/', '\\');
            }

            return (isDirectory || File.Exists(path)) ? path : null;
        }
    }
}
