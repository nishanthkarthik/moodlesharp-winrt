using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MoodleSharp.UI;
using RestSharp;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MoodleSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Login : Page
    {
        public Login()
        {
            this.InitializeComponent();
        }

        private async void LoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            BusyIndicator busyIndicator = new BusyIndicator(Dictionary.LoggingIn);
            busyIndicator.Start(Dictionary.LoggingIn);
            if (String.IsNullOrEmpty(RollBox.Text) || String.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageDialog messageDialog = new MessageDialog(Dictionary.LoginFieldEmpty,Dictionary.LoginFieldEmptyTitle)
                {
                    Options = MessageDialogOptions.AcceptUserInputAfterDelay
                };
                messageDialog.ShowAsync();
                return;
            }
            IRestResponse response = await LoginToMoodle(RollBox.Text, PasswordBox.Password);
            if (response.Content.Contains(Dictionary.InvalidLoginIdentifier))
            {
                MessageDialog messageDialog = new MessageDialog(Dictionary.LoginFailed, Dictionary.LoginFailedTitle)
                {
                    Options = MessageDialogOptions.AcceptUserInputAfterDelay
                };
                messageDialog.ShowAsync();
                return;
            }
            busyIndicator.TitleText = Dictionary.LoggedIn;
            Frame.Navigate(typeof (MainPage), response);
        }

        private async Task<IRestResponse> LoginToMoodle(string userName, string password)
        {
            RestClient client = new RestClient("https://courses.iitm.ac.in/login/index.php");
            client.CookieContainer = new CookieContainer();
            RestRequest request = new RestRequest(Method.POST);
            request.AddParameter("username", userName, ParameterType.GetOrPost);
            request.AddParameter("password", password, ParameterType.GetOrPost);
            request.AddHeader("HTTPonly", "true");
            IRestResponse response = await client.ExecuteAsync(request);
            string xCookie = client.CookieContainer.GetCookieHeader(new Uri("http://courses.iitm.ac.in"));
            string[] parsedStrings = xCookie.Split('=');
            response.Cookies.Add(new RestResponseCookie() { Name = parsedStrings[0], Value = parsedStrings[1] });
            return response;
        }
    }
}
