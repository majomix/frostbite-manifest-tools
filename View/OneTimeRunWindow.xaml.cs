using FrostbiteManifestSystemTools.ViewModel;
using System;
using System.Windows;

namespace FrostbiteManifestSystemTools.View
{
    /// <summary>
    /// Interaction logic for OneTimeRunWindow.xaml
    /// </summary>
    public partial class OneTimeRunWindow : Window
    {
        public OneTimeRunWindow()
        {
            InitializeComponent();

            OneTimeRunViewModel oneTimeRunViewModel = new OneTimeRunViewModel();
            oneTimeRunViewModel.RequestClose += (s, e) => this.Dispatcher.Invoke(new Action(() => Close())); // violates MVVM
            DataContext = oneTimeRunViewModel;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            OneTimeRunViewModel oneTimeRunViewModel = DataContext as OneTimeRunViewModel;
            if (oneTimeRunViewModel != null)
            {
                if (oneTimeRunViewModel.Export == null)
                {
                    Close();
                }
                else if (oneTimeRunViewModel.Export == true)
                {
                    oneTimeRunViewModel.ExtractByParameterCommand.Execute(oneTimeRunViewModel);
                }
                else
                {
                    oneTimeRunViewModel.ImportByParameterCommand.Execute(oneTimeRunViewModel);
                }
            }
        }
    }
}