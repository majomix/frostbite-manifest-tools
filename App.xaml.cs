using FrostbiteManifestSystemTools.View;
using System;
using System.Windows;

namespace FrostbiteManifestSystemTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                OneTimeRunWindow oneTimeRunWindow = new OneTimeRunWindow();
                MainWindow = oneTimeRunWindow;
                MainWindow.Show();
            }
            else
            {
                Shutdown(1);
            }
        }
    }
}