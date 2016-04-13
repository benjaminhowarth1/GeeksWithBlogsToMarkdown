using System;
using System.Threading.Tasks;
using GeeksWithBlogsToMarkdown.Commands.Base;
using GeeksWithBlogsToMarkdown.ViewModels.Base;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;
using GeeksWithBlogsToMarkdown.Extensions;

namespace GeeksWithBlogsToMarkdown.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private IDialogCoordinator _dialogCoordinator;
        private ICommand _getPostsCommand;

        public ICommand GetPostsCommand
        {
            get { return _getPostsCommand; }
            set
            {
                _getPostsCommand = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand ShowSettingsCommand { get; set; }

        public MainWindowViewModel(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
            ShowSettingsCommand = new DelegateCommand<MetroWindow>(ShowSettings, (window) => true);
            GetPostsCommand = new DelegateCommand<MetroWindow>(OnGetPosts, (window) => true);
        }

        private async void OnGetPosts(MetroWindow metroWindow)
        {
            LoginDialogData result = null;
            bool canConnect = true;
            var userName = string.Empty;
            var password = string.Empty;
            Settings.Instance.ReadSettings();
            //check if username and password already available

            if (string.IsNullOrWhiteSpace(Settings.Instance.GWBUserName))
            {
                //prompt is username is empty
                result = await PromptForCredentials(metroWindow);
                canConnect = result != null;
            }
           
            if (result == null && !canConnect)
            {
                //User pressed cancel
            }
            else if (result == null)
            {
                //if everything okay in settings, continue
                userName = Settings.Instance.GWBUserName;
                password = Settings.Instance.GWBPassword.DecryptString().ToInsecureString();
            }
            else
            {
                userName = result.Username;
                password = result.Password;
                //user clicked connect in prompt
                if (result.ShouldRemember)
                {
                    Settings.Instance.GWBUserName = userName;
                    Settings.Instance.GWBPassword = password.ToSecureString().EncryptString();
                    Settings.Instance.WriteOrUpdateSettings();
                }
            }

            //Connect to GWB
            await _dialogCoordinator.ShowMessageAsync(this, "Details", $"User:{userName}, Password:{password}");
        }

        private async Task<LoginDialogData> PromptForCredentials(MetroWindow metroWindow)
        {
            var result = await metroWindow.ShowLoginAsync("Login to Geekswithblogs", "Enter your Geekswithblogs credentials",
                new LoginDialogSettings
                {
                    ShouldHideUsername = false,
                    EnablePasswordPreview = true,
                    ColorScheme = MetroDialogColorScheme.Theme,
                    RememberCheckBoxVisibility = System.Windows.Visibility.Visible,
                    AffirmativeButtonText = "Connect"

                });
            return result;
        }

        private void ShowSettings(MetroWindow metroWindow)
        {
            var settingsFlyout = metroWindow.Flyouts.Items[0] as Flyout;
            if (settingsFlyout == null)
            {
                return;
            }
            settingsFlyout.DataContext = new SettingsViewModel();
            settingsFlyout.IsOpen = true;
        }
    }
}