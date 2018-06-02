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
        private string myTargetDirectory;
        public bool? Export { get; set; }
        public ICommand ExtractByParameterCommand { get; private set; }
        public ICommand ImportByParameterCommand { get; private set; }

        public OneTimeRunViewModel()
        {
            ParseCommandLine();
            Model = new FrostbiteManifestEditor();

            ImportByParameterCommand = new ImportByParameterCommand();
            ExtractByParameterCommand = new ExtractByParameterCommand();
        }

        public void ParseCommandLine()
        {
            OptionSet options = new OptionSet()
                .Add("export", value => Export = true)
                .Add("import", value => Export = false)
                .Add("index=", value => LoadedFilePath = CreateFullPath(value, false))
                .Add("dir=", value => myTargetDirectory = CreateFullPath(value, true));

            options.Parse(Environment.GetCommandLineArgs());
        }

        public void Extract()
        {
            if (myTargetDirectory != null && LoadedFilePath != null)
            {
                LoadStructure();
            }
        }

        public void Import()
        {
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
