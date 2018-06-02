using System;
using System.ComponentModel;
using System.Windows.Input;

namespace FrostbiteManifestSystemTools.ViewModel.Commands
{
    internal abstract class AbstractWorkerCommand : ICommand
    {
        public BackgroundWorker Worker { get; private set; }

        public AbstractWorkerCommand()
        {
            Worker = new BackgroundWorker();
            Worker.DoWork += DoWork;
        }

        public bool CanExecute(object parameter)
        {
            BaseViewModel viewModel = parameter as BaseViewModel;
            if (viewModel == null)
            {
                object[] parameters = parameter as object[];
                if (parameters != null && parameters.Length == 2)
                {
                    if (parameters[1] is bool) viewModel = parameters[0] as BaseViewModel;
                }
            }
            return viewModel != null && viewModel.LoadedFilePath != null && Worker.IsBusy == false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public abstract void Execute(object parameter);
        protected abstract void DoWork(object sender, DoWorkEventArgs e);
    }
}
