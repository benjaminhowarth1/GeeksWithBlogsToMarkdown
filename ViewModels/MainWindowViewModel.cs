using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GeeksWithBlogsToMarkdown.Commands.Base;
using GeeksWithBlogsToMarkdown.ViewModels.Base;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;
using BlogML.Xml;
using GeeksWithBlogsToMarkdown.Common;
using GeeksWithBlogsToMarkdown.Extensions;
using GeeksWithBlogsToMarkdown.Service;
using Html2Markdown;

namespace GeeksWithBlogsToMarkdown.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private IDialogCoordinator _dialogCoordinator;
        private ICommand _getPostsCommand;
        private ObservableCollection<BlogMLPost> _blogPosts;
        private string _blogTitle;
        private string _blogUrl;
        private DelegateCommand<object> _selectionChangedCommand;

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
            SelectionChangedCommand = new DelegateCommand<object>(OnSelectionChanged);
        }

        private void OnSelectionChanged(object o)
        {
            System.Collections.IList items = (System.Collections.IList)o;
            var post = items[0] as BlogMLPost;

            if (post != null)
            {
                HtmlMarkup = post.Content.Text;

                var converter = new Converter();
                Markdown = converter.Convert(HtmlMarkup);
            }
        }


        private string _markdown;

        public string Markdown
        {
            get { return _markdown; }
            set
            {
                _markdown = value;
                NotifyPropertyChanged();
            }
        }
        private string _htmlMarkup;

        public string HtmlMarkup
        {
            get { return _htmlMarkup; }
            set
            {
                _htmlMarkup = value;
                NotifyPropertyChanged();
            }
        }

        public DelegateCommand<object> SelectionChangedCommand
        {
            get { return _selectionChangedCommand; }
            set
            {
                _selectionChangedCommand = value;
                NotifyPropertyChanged();
            }
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
                return;
            }

            if (result == null)
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
            ProgressDialogController progressController = await _dialogCoordinator.ShowProgressAsync(this, AppContext.Instance.ApplicationName, "Getting blog posts...");
            progressController.SetIndeterminate();
            //Connect to GWB
            var response = await GWBService.Instance.GetAllBlogPostsAsync(progressController);

            await progressController.CloseAsync();

            if (response.Exception != null)
            {
                await _dialogCoordinator.ShowMessageAsync(this, AppContext.Instance.ApplicationName, response.Exception.Message);
            }
            else
            {
                var blog = response.Data;
                BlogTitle = blog.Title.ToUpper();
                BlogUrl = blog.RootUrl;
                BlogPosts = blog.Posts.ToObservableCollection();
            }

            
        }

        public string BlogUrl
        {
            get { return _blogUrl; }
            set
            {
                _blogUrl = value;
                NotifyPropertyChanged();
            }
        }

        public string BlogTitle
        {
            get { return _blogTitle; }
            set
            {
                _blogTitle = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<BlogMLPost> BlogPosts
        {
            get { return _blogPosts; }
            set
            {
                _blogPosts = value;
                NotifyPropertyChanged();
            }
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