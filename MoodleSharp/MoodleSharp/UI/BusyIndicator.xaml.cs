using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoodleSharp.UI
{
    /// <summary> 
    /// Popup to lock the user input without locking the UI thread, indicating the app is busy. 
    /// </summary> 
    public sealed partial class BusyIndicator : UserControl
    {
        #region Private Constructors

        /// <summary> 
        /// Initializes a new instance of <see cref="BusyIndicator"/> with a title. 
        /// </summary> 
        /// <param name="title">The title to be set.</param> 
        public BusyIndicator(string title)
        {
            this.InitializeComponent();

            // Set the title 
            TitleTextBlock.Text = title;
        }

        #endregion Private Constructors

        public string TitleText
        {
            get { return TitleTextBlock.Text; }
            set { this.TitleTextBlock.Text = value; }
        }

        #region Public Methods

        /// <summary> 
        /// Closes the BusyIndicator. 
        /// </summary> 
        public void Close()
        {
            // Close the parent; closes the dialog too. 
            ((Popup)Parent).IsOpen = false;
        }

        #endregion Public Methods


        #region Public Static Methods

        /// <summary> 
        /// Locks the screen ans starts the BusyIndicator by creating a popup. 
        /// </summary> 
        /// <param name="title">The title to be displayed by the BusyIndicator.</param> 
        /// <returns>The BusyIndicator.</returns> 
        public BusyIndicator Start(string title)
        {
            // Create a popup with the size of the app's window. 
            Popup popup = new Popup()
            {
                Height = Window.Current.Bounds.Height,
                IsLightDismissEnabled = false,
                Width = Window.Current.Bounds.Width
            };

            // Create the BusyIndicator as a child, having the same size as the app. 
            BusyIndicator busyIndicator = new BusyIndicator(title)
            {
                Height = popup.Height,
                Width = popup.Width
            };

            // Set the child of the popop 
            popup.Child = busyIndicator;

            // Postion the popup to the upper left corner 
            popup.SetValue(Canvas.LeftProperty, 0);
            popup.SetValue(Canvas.TopProperty, 0);

            // Open it. 
            popup.IsOpen = true;

            // Return the BusyIndicator 
            return (busyIndicator);
        }

        #endregion Public Static Methods
    } 
}
