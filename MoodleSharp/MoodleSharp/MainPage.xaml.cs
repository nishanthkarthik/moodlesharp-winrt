using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
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

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private static async void DownloadAllFilesLocal(Dictionary<string, string> downloadList, IRestResponse loginResponse)
        {
            FolderPicker picker = new FolderPicker();
            picker.ViewMode = PickerViewMode.List;
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

                HttpWebRequest webRequest = WebRequest.CreateHttp(keyValuePair.Value);
                webRequest.CookieContainer = new CookieContainer();
                webRequest.CookieContainer.Add(new Uri("https://courses.iitm.ac.in", UriKind.Absolute), 
                    new Cookie(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
                webRequest.Method = "GET";
                WebResponse webResponse = await webRequest.GetResponseAsync();

                Uri uri = webResponse.ResponseUri;
                var tempFilePath = uri.Segments.Last();
                tempFilePath = Uri.UnescapeDataString(tempFilePath);

                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Cookie.Add(new HttpCookiePairHeaderValue(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
                HttpResponseMessage fileResponse = await httpClient.GetAsync(uri);

                Utils.SaveFileToStorage(folder, tempFilePath, fileResponse.Content);
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

            foreach (IElement element in primaryParsedResult)
            {
                if (!urlList.ContainsKey(Regex.Replace(element.Children.First().InnerHtml, "<.*?>", String.Empty)))
                    urlList.Add(Regex.Replace(element.Children.First().InnerHtml, "<.*?>", String.Empty), element.Children.First().GetAttribute("href"));
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
                    courseList.Add(element.Children.First().InnerHtml, element.Children.First().GetAttribute("href"));
            }

            return courseList;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ReferenceEquals(e.Parameter, ""))
                return;
            _loginResponse = (IRestResponse) e.Parameter;
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
                Dictionary<string, string> courseDictionary = await ParseCourses(htmlParser);
                Dictionary<string, string> downloadUrlDictionary = await GetDownloadUrl(_loginResponse, courseDictionary, 0);
                DownloadAllFilesLocal(downloadUrlDictionary, _loginResponse);
            }
        }
    }
}
