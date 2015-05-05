using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using RestSharp;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MoodleSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IRestResponse _loginResponse;
        private bool _onNavigatedToCalled = false;
        private List<CourseListModel> _courseList;
        private List<CourseContentModel> _courseContent;
        private Dictionary<string, string> _courseDictionary;
        private Dictionary<string, string> _downloadUrlDictionary;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            _courseList = new List<CourseListModel>();
            _courseContent = new List<CourseContentModel>();
        }

        private static async void DownloadAllFilesLocal(Dictionary<string, string> downloadList, IRestResponse loginResponse)
        {
            FolderPicker picker = new FolderPicker { ViewMode = PickerViewMode.List };
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = await picker.PickSingleFolderAsync();

            foreach (KeyValuePair<string, string> keyValuePair in downloadList)
            {
                //RestClient client = new RestClient(keyValuePair.Value);
                //RestRequest request = new RestRequest(Method.GET);
                //request.CookieContainer = new CookieContainer();
                //request.AddCookie(loginResponse.Cookies[0].Name, loginResponse.Cookies[0].Value);
                //request.CookieContainer.Add(new Uri("https://courses.iitm.ac.in", UriKind.Absolute), new Cookie(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
                //IRestResponse response = await client.ExecuteAsync(request);

                var webRequest = WebRequest.CreateHttp(keyValuePair.Value);
                webRequest.Proxy = null;
                webRequest.CookieContainer = new CookieContainer();
                webRequest.CookieContainer.Add(new Uri("https://courses.iitm.ac.in", UriKind.Absolute),
                    new Cookie(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
                webRequest.Method = "HEAD";
                using (WebResponse webResponse = await webRequest.GetResponseAsync())
                {
                    Uri uri = webResponse.ResponseUri;
                    var tempFilePath = uri.Segments.Last();
                    tempFilePath = Uri.UnescapeDataString(tempFilePath);

                    StorageFile storageFile =
                        await folder.CreateFileAsync(tempFilePath, CreationCollisionOption.ReplaceExisting);

                    BackgroundDownloader backgroundDownloader = new BackgroundDownloader { Method = "GET" };
                    backgroundDownloader.SetRequestHeader("Cookie",
                        loginResponse.Cookies.First().Name + "=" + loginResponse.Cookies.First().Value);
                    backgroundDownloader.CostPolicy = BackgroundTransferCostPolicy.Always;
                    DownloadOperation downloadOperation = backgroundDownloader.CreateDownload(uri, storageFile);
                    await downloadOperation.StartAsync();
                }
                //HttpClient httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Cookie.Add(new HttpCookiePairHeaderValue(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
                //HttpResponseMessage fileResponse = await httpClient.GetAsync(uri);

                //await new Utils().SaveFileToStorage(folder, tempFilePath, fileResponse.Content);
            }
        }

        private async Task<Dictionary<string, string>> GetDownloadUrl(IRestResponse loginResponse, Dictionary<string, string> courseDictionary, int courseKeyValuePairIndex)
        {
            Dictionary<string, string> urlList = new Dictionary<string, string>();
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Cookie.Add(new HttpCookiePairHeaderValue(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
            string httpContent =
                await httpClient.GetStringAsync(
                new Uri(courseDictionary.ElementAt(courseKeyValuePairIndex).Value, UriKind.Absolute));

            IHtmlDocument primaryParsed = await new HtmlParser(httpContent).ParseAsync();
            IEnumerable<IElement> primaryParsedResult = primaryParsed.QuerySelectorAll("div")
                .Where(x => x.ClassName == "activityinstance");
            _courseContent.Clear();

            foreach (IElement element in primaryParsedResult)
            {
                if (!urlList.ContainsKey(Regex.Replace(element.Children.First().InnerHtml, "<.*?>", String.Empty).Replace(" File", "")))
                {
                    _courseContent.Add(new CourseContentModel() { FileName = Regex.Replace(element.Children.First().InnerHtml, "<.*?>", String.Empty).Replace(" File", ""), UriString = element.Children.First().GetAttribute("href") });
                    urlList.Add(Regex.Replace(element.Children.First().InnerHtml, "<.*?>", String.Empty).Replace(" File", ""), element.Children.First().GetAttribute("href"));
                }
            }

            return urlList;
        }

        private async Task<Dictionary<string, string>> ParseCourses(HtmlParser htmlDocument)
        {
            Dictionary<string, string> courseList = new Dictionary<string, string>();

            IHtmlDocument parsedHtml = await htmlDocument.ParseAsync();
            var registeredCoursesCollection =
                parsedHtml.QuerySelectorAll("div").Where(x => x.ClassName == "courses frontpage-course-list-enrolled");

            var courseParser = await new HtmlParser(registeredCoursesCollection.First().InnerHtml).ParseAsync();
            IEnumerable<IElement> courseCollection = courseParser.QuerySelectorAll("h3").Where(x => x.ClassName == "coursename");

            foreach (IElement element in courseCollection)
            {
                if (!courseList.ContainsKey(element.Children.First().InnerHtml))
                {
                    _courseList.Add(new CourseListModel() { CourseName = element.Children.First().InnerHtml });
                    courseList.Add(element.Children.First().InnerHtml, element.Children.First().GetAttribute("href"));
                }
            }

            return courseList;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ReferenceEquals(e.Parameter, ""))
                return;
            _loginResponse = (IRestResponse)e.Parameter;
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loginResponse == null)
            {
                Frame.Navigate(typeof(Login));
                return;
            }

            if (_onNavigatedToCalled)
                return;
            _onNavigatedToCalled = true;

            if (_loginResponse != null)
            {
                HtmlParser htmlParser = new HtmlParser(Utils.GenerateStreamFromString(_loginResponse.Content));
                _courseDictionary = await ParseCourses(htmlParser);
                _downloadUrlDictionary = await GetDownloadUrl(_loginResponse, _courseDictionary, 0);
                CourseView.ItemsSource = _courseList;
                ContentView.ItemsSource = _courseContent;
                //DownloadAllFilesLocal(_downloadUrlDictionary, _loginResponse);
            }
        }

        private async void DownloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ContentView.SelectedItems.Count <= 0)
            {
                MessageDialog messageDialog = new MessageDialog("Please select one or more items to download", "No files selected");
                await messageDialog.ShowAsync();
                return;
            }
            Dictionary<string, string> dictionary = ContentView.SelectedItems.Cast<CourseContentModel>().ToDictionary(contentModel => contentModel.FileName, contentModel => contentModel.UriString);
            DownloadAllFilesLocal(dictionary, _loginResponse);
            MessageDialog completeDialog = new MessageDialog("All your files have been downloaded.", "Complete!");
            await completeDialog.ShowAsync();
        }

        private async void DownloadAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadAllFilesLocal(_downloadUrlDictionary, _loginResponse);
            MessageDialog completeDialog = new MessageDialog("All your files have been downloaded.", "Complete!");
            await completeDialog.ShowAsync();
        }

        private async void CourseView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CourseView.SelectedIndex < 0)
                return;
            _downloadUrlDictionary = await GetDownloadUrl(_loginResponse, _courseDictionary, CourseView.SelectedIndex);
            ContentView.ItemsSource = null;
            ContentView.ItemsSource = _courseContent;
        }
    }
}
