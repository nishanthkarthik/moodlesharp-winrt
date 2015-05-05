using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Headers;
using HtmlAgilityPack;
using Microsoft.VisualBasic.CompilerServices;
using RestSharp;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MoodleSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IRestResponse _loginResponse;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private async void DownloadAllFilesLocal(Dictionary<string, string> downloadList, IRestResponse loginResponse)
        {
            FolderPicker picker = new FolderPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            StorageFolder folder = await picker.PickSingleFolderAsync();

            foreach (KeyValuePair<string, string> keyValuePair in downloadList)
            {
                RestClient client = new RestClient(keyValuePair.Value);
                RestRequest request = new RestRequest(Method.GET);
                request.AddCookie(loginResponse.Cookies[0].Name, loginResponse.Cookies[0].Value);
                IRestResponse response = await client.ExecuteAsync(request);
                Uri uri = response.ResponseUri;
                var tempFilePath = uri.Segments.Last();
                tempFilePath = Uri.UnescapeDataString(tempFilePath);

                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Cookie.Add(new HttpCookiePairHeaderValue(loginResponse.Cookies.First().Name, loginResponse.Cookies.First().Value));
                HttpResponseMessage fileResponse = await httpClient.GetAsync(uri);

                Utils.SaveFileToStorage(folder, tempFilePath, fileResponse.Content);
            }   
        }

        private static Dictionary<string, string> GetDownloadUrl(IRestResponse loginResponse, Dictionary<string, string> courseDictionary, int courseKeyValuePairIndex)
        {
            Dictionary<string, string> urlList = new Dictionary<string, string>();
            RestClient client = new RestClient(courseDictionary.ElementAt(courseKeyValuePairIndex).Value);
            RestRequest request = new RestRequest(Method.GET);
            request.AddCookie(loginResponse.Cookies[0].Name, loginResponse.Cookies[0].Value);
            IRestResponse response = client.ExecuteAsync(request);
            HtmlDocument document = new HtmlDocument();
            document.Load(Utils.GenerateStreamFromString(response.Content));
            foreach (HtmlNode htmlNode in document.DocumentNode.SelectNodes(Reference.CourseContentParseXpath))
            {
                if (!urlList.ContainsKey(htmlNode.InnerText))
                    urlList.Add(htmlNode.InnerText, htmlNode.ChildNodes[0].GetAttributeValue("href", ""));
            }
            return urlList;
        }

        private static Dictionary<string, string> ParseCourses(HtmlDocument htmlDocument)
        {
            Dictionary<string, string> courseList = new Dictionary<string, string>();
            HtmlNodeCollection registeredCoursesCollection = htmlDocument.DocumentNode.SelectNodes(Reference.EnrolledCourseXPath);
            HtmlDocument document = new HtmlDocument();
            document.Load(Utils.GenerateStreamFromString(registeredCoursesCollection[0].InnerHtml));
            foreach (HtmlNode htmlNode in document.DocumentNode.SelectNodes(Reference.CourseXPath))
            {
                if (!courseList.ContainsKey(htmlNode.InnerText))
                    courseList.Add(htmlNode.InnerText, htmlNode.ChildNodes[0].GetAttributeValue("href", ""));
            }
            return courseList;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _loginResponse = (IRestResponse)e.Parameter;

        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loginResponse == null)
                Frame.Navigate(typeof(Login));
        }
    }
}
