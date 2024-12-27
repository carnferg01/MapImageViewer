using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MapImageViewer
{
    public sealed partial class CreditsContentDialog : ContentDialog
    {
        public CreditsContentDialog() //StorageFile imageFile, StorageFile inkFile)
        {
            this.InitializeComponent();
            //if (imageFile != null)
            //{
            //    tboxImageSource.Text = imageFile.Path;
            //}
            //if (inkFile != null)
            //{
            //    tboxInkSource.Text = inkFile.Path;
            //}
        }
    }
}


