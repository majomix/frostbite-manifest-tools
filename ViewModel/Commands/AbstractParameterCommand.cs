using System;
using System.ComponentModel;

namespace FrostbiteManifestSystemTools.ViewModel.Commands
{
    abstract internal class AbstractParameterCommand : AbstractWorkerCommand
    {
        protected OneTimeRunViewModel myOneTimeRunViewModel;

        public override void Execute(object parameter)
        {
            myOneTimeRunViewModel = (OneTimeRunViewModel)parameter;
            Worker.RunWorkerAsync();
        }

        protected override void DoWork(object sender, DoWorkEventArgs e)
        {
            DoSpecificWork();
            if (!myOneTimeRunViewModel.HasError)
            {
                myOneTimeRunViewModel.OnRequestClose(new EventArgs());
            }
        }

        protected abstract void DoSpecificWork();
    }
}