using System;
using System.Collections.Generic;
using Windows.UI;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Windows.Storage.Streams;
using System.IO.Compression;
using System.Text;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Core;
using System.Diagnostics;
using System.Collections;
using System.Xml.Linq;
//using ColorMine.ColorSpaces;
//using ColorMine.ColorSpaces.Comparisons;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MapImageViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        WriteableBitmap writableBitmap;
        MatrixTransform previousTransform { get; set; }
        TransformGroup transforms { get; set; }
        MemoryStream memoryImage = null;


        public Stack<InkStroke> UndoStrokes = new Stack<InkStroke>();

        public MainPage()
        {
            this.InitializeComponent();
            InitManipulationTransforms();

            inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }



        public async Task<MemoryStream> CreateInMemoryCopyAsync(StorageFile storageFile)
        {
            // Open the StorageFile as a stream for reading
            using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
            {
                // Create a DataReader to read the stream
                using (DataReader dataReader = new DataReader(fileStream.GetInputStreamAt(0)))
                {
                    // Load the file into the DataReader
                    await dataReader.LoadAsync((uint)fileStream.Size);

                    // Read the entire file into a byte array
                    byte[] fileBytes = new byte[fileStream.Size];
                    dataReader.ReadBytes(fileBytes);

                    // Create a new MemoryStream and write the byte array to it
                    MemoryStream memoryStream = new MemoryStream(fileBytes);

                    // Optionally reset the position of the stream to the beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    return memoryStream;
                }
            }
        }

        //public async Task<WriteableBitmap> ConvertMemoryStreamToWritableBitmapAsync(MemoryStream memoryStream)
        //{
        //    // Convert MemoryStream to IRandomAccessStream
        //    memoryStream.Position = 0; // Ensure the memory stream position is at the beginning
        //    IRandomAccessStream randomAccessStream = memoryStream.AsRandomAccessStream();

        //    // Decode the image from the stream using BitmapDecoder
        //    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

        //    // Get the pixel data from the decoder
        //    PixelDataProvider pixelData = await decoder.GetPixelDataAsync();

        //    // Create a new WritableBitmap with the same dimensions as the image
        //    WriteableBitmap writableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);

        //    // Get the pixel buffer from the decoder
        //    byte[] pixels = pixelData.DetachPixelData();

        //    // Copy the pixel data into the WritableBitmap's pixel buffer
        //    using (Stream pixelStream = writableBitmap.PixelBuffer.AsStream())
        //    {
        //        pixelStream.Write(pixels, 0, pixels.Length);
        //    }

        //    return writableBitmap;
        //}


        public async Task<WriteableBitmap> ConvertMemoryStreamToWritableBitmapAsync(MemoryStream memoryStream)
        {
            // Convert MemoryStream to IRandomAccessStream
            memoryStream.Position = 0; // Ensure the memory stream position is at the beginning
            IRandomAccessStream randomAccessStream = memoryStream.AsRandomAccessStream();

            // Decode the image from the stream using BitmapDecoder
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

            // Create a new WriteableBitmap with the same dimensions as the image
            WriteableBitmap writableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);

            // Get the pixel data from the decoder
            PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8, // Ensure BGRA format
                BitmapAlphaMode.Premultiplied, // Handle transparency, if present
                new BitmapTransform(), // No transformation
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage // Avoid unnecessary color conversions
            );

            // Get the pixel bytes
            byte[] pixels = pixelData.DetachPixelData();

            // Write pixel data into the WriteableBitmap's pixel buffer
            using (Stream stream = writableBitmap.PixelBuffer.AsStream())
            {
                stream.Write(pixels, 0, pixels.Length);
            }

            return writableBitmap;
        }


        public async void LoadFile(StorageFile storageFile)
        {
            // Remove existing ink
            inkCanvas.InkPresenter.StrokeContainer.Clear();

            if (storageFile.FileType == ".inkedmap")
            {
                // Open the file as an IRandomAccessStream
                using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fileStream.AsStreamForRead(), Encoding.UTF8, leaveOpen: false))
                    {
                        // Read image data
                        int imageLength = reader.ReadInt32();
                        byte[] imageBytes = reader.ReadBytes(imageLength);

                        // Process imageBytes (e.g., create an Image from bytes)
                        // Create a MemoryStream from the byte array
                        memoryImage = new MemoryStream(imageBytes);

                        // Optional: Set the position to the beginning of the stream
                        memoryImage.Position = 0;





                        // Read ink data
                        int inkLength = reader.ReadInt32();
                        byte[] inkBytes = reader.ReadBytes(inkLength);

                        // Load the ink data into an InkStrokeContainer
                        using (var inkStream = new MemoryStream(inkBytes))
                        {
                            //var inkContainer = new InkStrokeContainer();
                            await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inkStream.AsRandomAccessStream());
                        }
                    }
                }
            }
            else
            {
                // Copy file in to memory 
                memoryImage = await CreateInMemoryCopyAsync(storageFile);
            }

            try {
                //if (storageFile.FileType == ".gif")
                //{
                //    // todo fix colours of giff


                //    using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
                //    {
                //        // Create a decoder for the GIF image
                //        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

                //        // Create a writable bitmap with the proper dimensions
                //        writableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);

                //        // Get the pixel data with the expected pixel format (BGRA8)
                //        var pixelData = await decoder.GetPixelDataAsync(
                //            BitmapPixelFormat.Bgra8, // Ensure BGRA format
                //            BitmapAlphaMode.Premultiplied, // Use premultiplied alpha
                //            new BitmapTransform(),
                //            ExifOrientationMode.IgnoreExifOrientation,
                //            ColorManagementMode.DoNotColorManage
                //        );

                //        // Get the pixel bytes
                //        byte[] pixels = pixelData.DetachPixelData();

                //        // Write pixel data into the WriteableBitmap
                //        using (Stream stream = writableBitmap.PixelBuffer.AsStream())
                //        {
                //            stream.Write(pixels, 0, pixels.Length);
                //        }
                //    }
                //}
                //else
                //{
                    // Copy memory image in to writableBitmap
                writableBitmap = await ConvertMemoryStreamToWritableBitmapAsync(memoryImage);
                //}
            } catch
            {
                throw new NotImplementedException();
            }

            //inkCanvas.Height = writableBitmap.PixelHeight;
            //inkCanvas.Width = writableBitmap.PixelWidth;
            Debug.Write(writableBitmap.GetHashCode());

            // Display image
            MapImage.Source = writableBitmap;
            MapImage.Height = writableBitmap.PixelHeight;
            MapImage.Width = writableBitmap.PixelWidth;


        }


        public async void SaveInkedImgFile()
        {
            // ensure somthing to save
            if (memoryImage == null)
            {
                throw new Exception("Image/map doesn't exist");
            }
            else
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("inkedmap", new List<string>() { ".inkedmap" });
                // savePicker.SuggestedFileName = "Inked map " + mapFile.DisplayName;

                StorageFile inkedmapFileTemp = await savePicker.PickSaveFileAsync();
                if (inkedmapFileTemp == null)
                {
                    throw new Exception("No save file seleted");
                }


                // Open the file as an IRandomAccessStream
                using (IRandomAccessStream fileStream = await inkedmapFileTemp.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream.AsStreamForWrite(), Encoding.UTF8, leaveOpen: false))
                    {
                        // Write metadata length and data
                        //PropertyMetadata = 
                        //byte[] metadataBytes = Encoding.UTF8.GetBytes(metadata);
                        //writer.Write(metadataBytes.Length);
                        //writer.Write(metadataBytes);

                        // Copy memoryImage to the file
                        byte[] imageBytes = memoryImage.ToArray();
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);



                        // Copy ink to file
                        using (var inkStream = new MemoryStream())
                        {
                            // Save the InkStrokeContainer to the stream
                            this.inkCanvas.InkPresenter.StrokeContainer.SaveAsync(inkStream.AsOutputStream()).AsTask().Wait();

                            // Convert the inkStream to a byte array and write it
                            byte[] inkBytes = inkStream.ToArray();
                            writer.Write(inkBytes.Length);  // Write the length of ink data
                            writer.Write(inkBytes);         // Write the ink data itself
                        }

                        writer.Flush();
                    }
                }


                //// Write data to file
                //using (var sss = await inkedmapFileTemp.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    var save = await this.inkCanvas.InkPresenter.StrokeContainer.SaveAsync(sss); //< -this line raise that exception
                //}
            }
        }


        //public async void SaveInkFile()
        //{
   
        //    // ensure somthing to save
        //    if (mapFile == null)
        //    {
        //        throw new Exception("Nothing to save");
        //    }


        //    // if not exists
        //        // Save new myink file
        //    var savePicker = new Windows.Storage.Pickers.FileSavePicker();
        //    savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        //    savePicker.FileTypeChoices.Add("mapink", new List<string>() { ".mapink" });
        //    savePicker.SuggestedFileName = "MapInked " + mapFile.DisplayName;

        //    var inkFileTemp = await savePicker.PickSaveFileAsync();

        //    if (inkFileTemp == null)
        //    {
        //        throw new Exception("No save file seleted");
        //    }

        //    // Write data to file
        //    using (var sss = await inkFileTemp.OpenAsync(FileAccessMode.ReadWrite))
        //    {
        //        var save = await this.inkCanvas.InkPresenter.StrokeContainer.SaveAsync(sss); //< -this line raise that exception
        //    }
        //    inkFile = inkFileTemp;
        //}

        public async void OpenFiles()
        {
            // Pick map file
            var pickerImage = new Windows.Storage.Pickers.FileOpenPicker();
            pickerImage.FileTypeFilter.Add(".inkedmap");
            pickerImage.FileTypeFilter.Add(".jpg");
            pickerImage.FileTypeFilter.Add(".jpeg");
            pickerImage.FileTypeFilter.Add(".png");
            pickerImage.FileTypeFilter.Add(".gif");
            pickerImage.FileTypeFilter.Add(".tiff");
            pickerImage.FileTypeFilter.Add(".tif");
            pickerImage.CommitButtonText = "Open Image";
            StorageFile mapFileTemp = await pickerImage.PickSingleFileAsync();

            if (mapFileTemp == null)
            {
                ContentDialog errorNoFilePicked = new ContentDialog();
                errorNoFilePicked.Content = "No file picked";
                errorNoFilePicked.PrimaryButtonText = "Ok";
                await errorNoFilePicked.ShowAsync();
            }
            else
            {
                LoadFile(mapFileTemp);
            }
        }

        


        private void InitManipulationTransforms()
        {
            transforms = new TransformGroup();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            deltaTransform.Rotation = 0;
            deltaTransform.ScaleX = 1;
            deltaTransform.ScaleY = 1;
            deltaTransform.SkewX = 0;
            deltaTransform.SkewY = 0;
            deltaTransform.TranslateX = 0;
            deltaTransform.TranslateY = 0;

            transforms.Children.Add(previousTransform);
            transforms.Children.Add(deltaTransform);

            // Set the render transform on the rect
            mainCanvas.RenderTransform = transforms;
        }

        private void resetRotation()
        {
            previousTransform.Matrix = transforms.Value;

            var pt = previousTransform.Matrix;
            var scalefactor = Math.Sqrt(pt.M11 * pt.M11 + pt.M12 * pt.M12);

            var rotationMatrix11 = (1.0 / scalefactor) * pt.M11;
            var rotationMatrix12 = (1.0 / scalefactor) * pt.M12;

            var angle = Math.Acos(rotationMatrix11) * 180 / Math.PI;
            var angle2 = Math.Asin(-rotationMatrix12) * 180 / Math.PI;

            double rotation;

            if (angle2 > 0)
            {
                rotation = -angle;
            }
            else if (angle2 < 0)
            {
                rotation = angle;
            }
            else
            {
                rotation = 0;
            }

            deltaTransform.CenterX = grid.ActualWidth / 2;
            deltaTransform.CenterY = grid.ActualHeight / 2;

            deltaTransform.Rotation = -rotation;
            deltaTransform.ScaleX = 1;
            deltaTransform.ScaleY = 1;
            deltaTransform.SkewX = 0;
            deltaTransform.SkewY = 0;
            deltaTransform.TranslateX = 0;
            deltaTransform.TranslateY = 0;

        }
        private void resetPosition()
        {
            previousTransform.Matrix = transforms.Value;

            var pt = previousTransform.Matrix;
            var scalefactor = Math.Sqrt(pt.M11 * pt.M11 + pt.M12 * pt.M12);

            deltaTransform.CenterX = grid.ActualWidth / 2;
            deltaTransform.CenterY = grid.ActualHeight / 2;
            deltaTransform.Rotation = 0;
            deltaTransform.ScaleX = 1;
            deltaTransform.ScaleY = 1;
            deltaTransform.SkewX = 0;
            deltaTransform.SkewY = 0;

            deltaTransform.TranslateX = -previousTransform.Matrix.OffsetX + grid.ActualWidth / 2 - MapImage.ActualWidth / 2 * scalefactor;
            deltaTransform.TranslateY = -previousTransform.Matrix.OffsetY + grid.ActualHeight / 2 - MapImage.ActualHeight / 2 * scalefactor;
        }
        private void resetScale()
        {
            previousTransform.Matrix = transforms.Value;

            var pt = previousTransform.Matrix;
            var scalefactor = Math.Sqrt(pt.M11 * pt.M11 + pt.M12 * pt.M12);

            var fitToFrameScale = grid.ActualHeight / MapImage.ActualHeight;

            deltaTransform.CenterX = grid.ActualWidth / 2;
            deltaTransform.CenterY = grid.ActualHeight / 2;
            deltaTransform.Rotation = 0;
            deltaTransform.ScaleX = 1 / scalefactor * fitToFrameScale;
            deltaTransform.ScaleY = 1 / scalefactor * fitToFrameScale;
            deltaTransform.SkewX = 0;
            deltaTransform.SkewY = 0;
            deltaTransform.TranslateX = 0;
            deltaTransform.TranslateY = 0;

            
        }
        private void resetAll()
        {
            resetRotation();
            resetScale();
            resetPosition();
        }

        private void OpenFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFiles();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ContentDialog startDialog = new ContentDialog();
            startDialog.Content = "Open files?";
            startDialog.PrimaryButtonText = "Open files";
            startDialog.SecondaryButtonText = "Skip";
            var startDialogResults = await startDialog.ShowAsync();
            if (startDialogResults == ContentDialogResult.Primary)
            {
                OpenFiles();
            }
        }

        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            previousTransform.Matrix = transforms.Value;

            // Get center point for rotation
            Point center = previousTransform.TransformPoint(new Point(e.Position.X, e.Position.Y));
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
            // the rotation, scale, X, and Y changes
            deltaTransform.Rotation = e.Delta.Rotation;
            deltaTransform.ScaleX = e.Delta.Scale;
            deltaTransform.ScaleY = e.Delta.Scale;
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
        }

        public Color GetPixelColor(WriteableBitmap bitmap, int x, int y)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            if (x < 0 || x >= bitmap.PixelWidth || y < 0 || y >= bitmap.PixelHeight)
                throw new ArgumentOutOfRangeException("Coordinates are out of bounds.");

            // Calculate the index of the pixel in the buffer
            int index = (y * bitmap.PixelWidth + x) * 4;

            using (var stream = bitmap.PixelBuffer.AsStream())
            {
                // Seek to the index position
                stream.Seek(index, SeekOrigin.Begin);

                // Read the pixel data (BGRA format)
                byte[] pixelData = new byte[4];
                stream.Read(pixelData, 0, 4);

                // Extract color components
                byte blue = pixelData[0];
                byte green = pixelData[1];
                byte red = pixelData[2];
                byte alpha = pixelData[3];

                // Create and return the Color object
                return Color.FromArgb(alpha, red, green, blue);
            }
        }

        private void mainCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //previousTransform.Matrix = transforms.Value;
            // Get center point
            Point position = (new Point(e.GetCurrentPoint(MapImage).Position.X, e.GetCurrentPoint(MapImage).Position.Y));

            if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Shift)
            {
                Debug.Write(writableBitmap.GetHashCode() + " pppppppppppppppppppppppppppppppppppppppppp");
                //using (writeableBitmap.GetBitmapContext())
                //{
                Stopwatch stopwatch = Stopwatch.StartNew();
                // Calculate the target pixel position
                int x = (int)(writableBitmap.PixelWidth / MapImage.ActualWidth * position.X);
                int y = (int)(writableBitmap.PixelHeight / MapImage.ActualHeight * position.Y);

                // Get the target color from the bitmap at the calculated position
                Color targetColor = GetPixelColor(writableBitmap, x, y);

                // Determine the threshold from the input or placeholder text
                int threshold = string.IsNullOrEmpty(colourPickerThreshold.Text)
                    ? int.Parse(colourPickerThreshold.PlaceholderText)
                    : int.Parse(colourPickerThreshold.Text);

                // Access the pixel buffer of the WriteableBitmap
                byte[] pixels = new byte[writableBitmap.PixelWidth * writableBitmap.PixelHeight * 4];
                var xxxxx = writableBitmap.PixelBuffer.ToArray();
                using (var stream = writableBitmap.PixelBuffer.AsStream())
                {
                    stream.Read(pixels, 0, pixels.Length);
                }
                stopwatch.Stop();
                Debug.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");





                stopwatch = Stopwatch.StartNew();
                unsafe
                {
                    fixed (byte* pPixels = pixels)
                    {
                        byte* endPixel = pPixels + pixels.Length;
                        byte* currentPixel = pPixels;

                        // Precompute target color values
                        byte targetAlpha = targetColor.A;
                        byte targetRed = targetColor.R;
                        byte targetGreen = targetColor.G;
                        byte targetBlue = targetColor.B;

                        Color backgroundcolour = (grid.Background as SolidColorBrush).Color;

                        // Processing 16 pixels (64 bytes) per iteration if possible
                        while (currentPixel + 63 < endPixel) // Ensure we don't exceed the buffer
                        {
                            // Load 16 pixels (64 bytes) into local variables
                            uint* pixelData = (uint*)currentPixel;
                            for (int i = 0; i < 16; i++)
                            {
                                // Extract the color (BGRA format)
                                byte blue = (byte)(pixelData[i] & 0xFF);
                                byte green = (byte)((pixelData[i] >> 8) & 0xFF);
                                byte red = (byte)((pixelData[i] >> 16) & 0xFF);
                                byte alpha = (byte)((pixelData[i] >> 24) & 0xFF);

                                // Check if the current color is close to the target color
                                if (!ColorsAreClose(new Color { A = alpha, R = red, G = green, B = blue }, targetColor, threshold))
                                {
                                    // Set the pixel to white (R = 255, G = 255, B = 255) and alpha = 0 (transparent)
                                    pixelData[i] = (uint)((backgroundcolour.R << 16) | (backgroundcolour.G << 8) | backgroundcolour.B); // BGRA: Set to white and transparent (0x00FFFFFF)
                                }
                            }
                            currentPixel += 64; // Move to the next block of pixels
                        }

                        // Process any remaining pixels (less than 16 pixels) individually
                        while (currentPixel < endPixel)
                        {
                            // Access the current color (BGRA format)
                            byte blue = currentPixel[0];
                            byte green = currentPixel[1];
                            byte red = currentPixel[2];
                            byte alpha = currentPixel[3];

                            // Check if the current color is close to the target color
                            if (!ColorsAreClose(new Color { A = alpha, R = red, G = green, B = blue }, targetColor, threshold))
                            {
                                // Set the pixel to white (R = 255, G = 255, B = 255) and alpha = 0 (transparent)
                                currentPixel[0] = 255; // Blue channel
                                currentPixel[1] = 255; // Green channel
                                currentPixel[2] = 255; // Red channel
                                currentPixel[3] = 0;   // Alpha channel (transparent)
                            }

                            currentPixel += 4; // Move to the next pixel
                        }
                    }
                }
                stopwatch.Stop();
                Debug.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");




                stopwatch = Stopwatch.StartNew();
                // Create a new WriteableBitmap to update the display
                writableBitmap = new WriteableBitmap(writableBitmap.PixelWidth, writableBitmap.PixelHeight);
                Debug.Write(writableBitmap.GetHashCode());
                using (var stream = writableBitmap.PixelBuffer.AsStream())
                {
                    //stream.Seek(0, SeekOrigin.Begin); // Ensure you start writing at the beginning
                    stream.Write(pixels, 0, pixels.Length);
                }
                stopwatch.Stop();
                Debug.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");


                var xxxxxx = writableBitmap.PixelBuffer.ToArray();

                Debug.Write(writableBitmap.GetHashCode());
                stopwatch = Stopwatch.StartNew();
                
                // Set the modified WriteableBitmap as the source for the image
                MapImage.Source = writableBitmap;
                stopwatch.Stop();
                Debug.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"\n--------------\n");

                var xxxxxxyy = (MapImage.Source as WriteableBitmap).PixelBuffer.ToArray();



                //}
            }
            if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control)
            {
                
                //Ellipse ellipse = new Ellipse();
                
                //InkStrokeBuilder isb = new InkStrokeBuilder();
                
                //List<Point> points = new List<Point>();
                //InkStroke inkStroke = isb.CreateStroke(points.AsEnumerable<Point>());

                //inkCanvas.InkPresenter.StrokeContainer.AddStroke(inkStroke);
            }

        }

        private void MapImage_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            previousTransform.Matrix = transforms.Value;

            // Get center point for rotation
            Point center = previousTransform.TransformPoint(new Point(e.GetCurrentPoint(mainCanvas).Position.X, e.GetCurrentPoint(MapImage).Position.Y));
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control)
            {
                double dblDelta_Scroll = e.GetCurrentPoint(mainCanvas).Properties.MouseWheelDelta;
                dblDelta_Scroll = (dblDelta_Scroll > 0) ? 5 : -5;
                deltaTransform.Rotation = dblDelta_Scroll;

                deltaTransform.ScaleX = 1;
                deltaTransform.ScaleY = 1;
                deltaTransform.TranslateX = 0;
                deltaTransform.TranslateY = 0;
            }
            else
            {
                double dblDelta_Scroll = e.GetCurrentPoint(mainCanvas).Properties.MouseWheelDelta;
                dblDelta_Scroll = (dblDelta_Scroll > 0) ? 1.2 : 0.8;
                deltaTransform.ScaleX = dblDelta_Scroll;
                deltaTransform.ScaleY = dblDelta_Scroll;

                deltaTransform.Rotation = 0;
                deltaTransform.TranslateX = 0;
                deltaTransform.TranslateY = 0;
            }
        }

  

        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            resetAll();
        }
        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            resetPosition();
        }
        private void ResetScale_Click(object sender, RoutedEventArgs e)
        {
            resetScale();
        }
        private void ResetRotation_Click(object sender, RoutedEventArgs e)
        {
            resetRotation();
        }

        private void MapVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (MapImage.Visibility == Visibility.Visible)
            {
                MapImage.Visibility = Visibility.Collapsed;
                (sender as MenuFlyoutItem).Text = "Show map";
            }
            else
            {
                MapImage.Visibility = Visibility.Visible;
                (sender as MenuFlyoutItem).Text = "Hide map";
            }
        }

        private void MouseCursor_Click(object sender, RoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor.Type == Windows.UI.Core.CoreCursorType.Arrow)
            {
                // Change to cross
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Cross, 0);
                //MouseCursor.Icon = new FontIcon() { Glyph = "\xE710" };
                (sender as MenuFlyoutItem).Text = "Set cursor as arrow";
            }
            else
            {
                // Change to arrow
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
                //MouseCursor.Icon = new FontIcon() { Glyph = "\xE8B0" };
                (sender as MenuFlyoutItem).Text = "Set cursor as cross";
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveInkedImgFile();
        }
        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            var view = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                (sender as AppBarButton).Icon = new SymbolIcon(Symbol.FullScreen);
            }
            else
            {
                var outcome = view.TryEnterFullScreenMode();
                if (outcome == true)
                {
                    (sender as AppBarButton).Icon = new SymbolIcon(Symbol.BackToWindow);
                }
            }
        }
        private async void Help_Click(object sender, RoutedEventArgs e)
        {
            HelpContentDialog helpDialog = new HelpContentDialog();
            await helpDialog.ShowAsync();
        }
        private async void Credits_Click(object sender, RoutedEventArgs e)
        {
            CreditsContentDialog creditsDialog = new CreditsContentDialog();
            await creditsDialog.ShowAsync();
        }


        private void inkToolbar_ActiveToolChanged(InkToolbar sender, object args)
        {
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= StartLine;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= ContinueLine;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= CompleteLine;

            if (sender.ActiveTool.Name != "movementButton")
            {
                inkCanvas.InkPresenter.InputDeviceTypes =
                  Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                  Windows.UI.Core.CoreInputDeviceTypes.Pen |
                  Windows.UI.Core.CoreInputDeviceTypes.Touch;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
            }
        }

        private void inkToolbar_InkDrawingAttributesChanged(InkToolbar sender, object args)
        {
            if (sender.ActiveTool.Name == "highlighterPen")
            {
                InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.PenTip = PenTipShape.Circle;
                drawingAttributes.Size = new Size(drawingAttributes.Size.Width, drawingAttributes.Size.Width);
                drawingAttributes.DrawAsHighlighter = true;
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            }
        }
        
        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            // mainstack -> undostack

            IReadOnlyList<InkStroke> strokes = args.Strokes;
            if (strokes.Count > 0)
            {
                foreach (var stroke in strokes)
                {
                    UndoStrokes.Push(stroke);
                }
            }
        }

        private void toShapesToggle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as InkToolbarCustomToggleButton).IsChecked == true)
            {
                inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            }
            else
            {
                inkCanvas.InkPresenter.StrokesCollected -= InkPresenter_StrokesCollected;
            }

        }
        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            InkStroke stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Last();

            // Action 1 = We use a function that we will implement just after to create the XAML Line
            InkStroke s = ConvertStrokeToLine(stroke);
            // Action 2 = We add the Line in the second Canvas
            inkCanvas.InkPresenter.StrokeContainer.AddStroke(s);
            //ShapesCanvas.Children.Add(line);

            // We delete the InkStroke from the InkCanvas
            stroke.Selected = true;
            inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }
        private InkStroke ConvertStrokeToLine(InkStroke stroke)
        {
            // The origin = (X1, Y1)
            var startX = stroke.GetInkPoints().First().Position.X;
            var startY = stroke.GetInkPoints().First().Position.Y;
            // The end = (X2, Y2)
            var endX = stroke.GetInkPoints().Last().Position.X;
            var endY = stroke.GetInkPoints().Last().Position.Y;

            List<Point> ips = new List<Point>();
            ips.Add(new Point(startX, startY));
            ips.Add(new Point(endX, endY));

            InkStrokeBuilder s = new InkStrokeBuilder();
            s.SetDefaultDrawingAttributes(stroke.DrawingAttributes);
            InkStroke newStroke = s.CreateStroke(ips);

            return newStroke;
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            // mainstack -> undostack

            IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count > 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                UndoStrokes.Push(strokes[strokes.Count - 1]);
                inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }
        }
        private void redoButton_Click(object sender, RoutedEventArgs e)
        {
            // undostack -> mainstack

            if (UndoStrokes.Count > 0)
            {
                var stroke = UndoStrokes.Pop();
                var strokeBuilder = new InkStrokeBuilder();
                strokeBuilder.SetDefaultDrawingAttributes(stroke.DrawingAttributes);
                System.Numerics.Matrix3x2 matr = stroke.PointTransform;
                IReadOnlyList<InkPoint> inkPoints = stroke.GetInkPoints();
                InkStroke stk = strokeBuilder.CreateStrokeFromInkPoints(inkPoints, matr);
                inkCanvas.InkPresenter.StrokeContainer.AddStroke(stk);
            }
        }



        bool ColorsAreClose(Color color1, Color color2, int threshold = 50)
        {
            int rDiff = color1.R - color2.R;
            int gDiff = color1.G - color2.G;
            int bDiff = color1.B - color2.B;

            int distanceSquared = rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;
            int thresholdSquared = threshold * threshold;

            return distanceSquared <= thresholdSquared;

        }

        //bool ColorsAreClose(Color color1, Color color2, float threshold = 2.0f)
        //{
        //    return ColorComparison.AreColorsClose(color1, color2, threshold);
        //}




        private async void reSetColourPickerButton_Click(object sender, RoutedEventArgs e)
        {



            if (memoryImage != null)
            {
                // Copy memory image in to writableBitmap
                writableBitmap = await ConvertMemoryStreamToWritableBitmapAsync(memoryImage);

                // Display image
                MapImage.Source = writableBitmap;
                MapImage.Height = writableBitmap.PixelHeight;
                MapImage.Width = writableBitmap.PixelWidth;

            }
            else
            {
                // Handle exceptions (e.g., file read errors, image decoding issues)
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"No image found",
                    CloseButtonText = "Ok"
                };
                await errorDialog.ShowAsync();
            }
        }


        public void ModifywritableBitmap(byte r, byte g, byte b)
        {
            // Get the pixel buffer
            IBuffer buffer = writableBitmap.PixelBuffer;
            byte[] pixels = buffer.ToArray();

            // Pixel format is BGRA, so each pixel is 4 bytes: Blue, Green, Red, Alpha
            const int bytesPerPixel = 4;

            for (int i = 0; i < pixels.Length; i += bytesPerPixel)
            {
                byte blue = pixels[i];
                byte green = pixels[i + 1];
                byte red = pixels[i + 2];
                byte alpha = pixels[i + 3];

                if (alpha == 0)
                {
                    // Set the RGB values to 255
                    pixels[i] = b;       // Blue
                    pixels[i + 1] = g;   // Green
                    pixels[i + 2] = r;   // Red
                                         // Alpha remains 0
                }
            }

            // Update the pixel buffer with modified data
            //pixels = data.AsBuffer();
            //bitmap.PixelBuffer = pixels;
            writableBitmap = new WriteableBitmap(writableBitmap.PixelWidth, writableBitmap.PixelHeight);
            //Debug.Write(writableBitmap.GetHashCode());
            using (var stream = writableBitmap.PixelBuffer.AsStream())
            {
                //stream.Seek(0, SeekOrigin.Begin); // Ensure you start writing at the beginning
                stream.Write(pixels, 0, pixels.Length);
            }
            MapImage.Source = writableBitmap;
        }



        private void CanvasColour_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuFlyoutItem).Text == "Set black background")
            {
                grid.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                //mainCanvas.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                (sender as MenuFlyoutItem).Text = "Set white background";
                //var xxxxxxyy = (MapImage.Source as WriteableBitmap).PixelBuffer.ToArray();
                //(MapImage.Source as WriteableBitmap).Invalidate();
                ModifywritableBitmap(0, 0, 0);
            }
            else
            {
                grid.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                //mainCanvas.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                (sender as MenuFlyoutItem).Text = "Set black background";
                ModifywritableBitmap(255, 255, 255);
            }
        }


        // Function to combine InkCanvas ink with WriteableBitmap
        private async Task<WriteableBitmap> CombineInkWithBitmap(InkCanvas inkCanvas, WriteableBitmap writeableBitmap)
        {
            // 1. Render InkCanvas to a WriteableBitmap
            var inkCanvasBitmap = await RenderInkCanvasToBitmap(inkCanvas);

            // 2. Crop the inkBitmap to the bounds of the writeableBitmap
            var croppedInkBitmap = CropInkToBitmapBounds(inkCanvasBitmap, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);

            // 3. Combine the ink with the original writeableBitmap
            var combinedBitmap = CombineBitmaps(writeableBitmap, croppedInkBitmap);

            return combinedBitmap;
        }

        // Render the InkCanvas content to a WriteableBitmap
        private async Task<WriteableBitmap> RenderInkCanvasToBitmap(InkCanvas inkCanvas)
        {
            var renderTargetBitmap = new RenderTargetBitmap();
            var pixelWidth = (int)inkCanvas.ActualWidth;
            var pixelHeight = (int)inkCanvas.ActualHeight;
            await renderTargetBitmap.RenderAsync(inkCanvas);

            var pixels = await renderTargetBitmap.GetPixelsAsync();
            var inkCanvasBitmap = new WriteableBitmap(pixelWidth, pixelHeight);      // take far too long
            pixels.CopyTo(inkCanvasBitmap.PixelBuffer);
            return inkCanvasBitmap;
        }

        // Crop the ink bitmap to the bounds of the writeableBitmap
        private WriteableBitmap CropInkToBitmapBounds(WriteableBitmap inkBitmap, int width, int height)
        {
            var croppedBitmap = new WriteableBitmap(width, height);
            var sourcePixels = inkBitmap.PixelBuffer.ToArray();
            var croppedPixels = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = (y * inkBitmap.PixelWidth + x) * 4;
                    int targetIndex = (y * width + x) * 4;
                    croppedPixels[targetIndex] = sourcePixels[sourceIndex];
                    croppedPixels[targetIndex + 1] = sourcePixels[sourceIndex + 1];
                    croppedPixels[targetIndex + 2] = sourcePixels[sourceIndex + 2];
                    croppedPixels[targetIndex + 3] = sourcePixels[sourceIndex + 3];
                }
            }

            // Create a new IBuffer from the byte array
            var buffer = croppedPixels.AsBuffer();

            // Set the PixelBuffer
            using (var pixelStream = buffer.AsStream())
            {
                // Open the pixel buffer stream for writing
                using (var outputStream = croppedBitmap.PixelBuffer.AsStream())
                {
                    pixelStream.CopyTo(outputStream);
                }
            }
            return croppedBitmap;
        }

        // Combine the ink bitmap with the original writeableBitmap
        private WriteableBitmap CombineBitmaps(WriteableBitmap baseBitmap, WriteableBitmap inkBitmap)
        {
            var combinedBitmap = new WriteableBitmap(baseBitmap.PixelWidth, baseBitmap.PixelHeight);
            var basePixels = baseBitmap.PixelBuffer.ToArray();
            var inkPixels = inkBitmap.PixelBuffer.ToArray();
            var combinedPixels = new byte[basePixels.Length];

            for (int i = 0; i < basePixels.Length; i += 4)
            {
                // Assuming ink is to be blended using alpha blending
                byte baseAlpha = basePixels[i + 3];
                byte inkAlpha = inkPixels[i + 3];

                byte blendedAlpha = (byte)Math.Max(baseAlpha, inkAlpha);
                byte blendedRed = (byte)((basePixels[i] * baseAlpha + inkPixels[i] * inkAlpha) / blendedAlpha);
                byte blendedGreen = (byte)((basePixels[i + 1] * baseAlpha + inkPixels[i + 1] * inkAlpha) / blendedAlpha);
                byte blendedBlue = (byte)((basePixels[i + 2] * baseAlpha + inkPixels[i + 2] * inkAlpha) / blendedAlpha);

                combinedPixels[i] = blendedRed;
                combinedPixels[i + 1] = blendedGreen;
                combinedPixels[i + 2] = blendedBlue;
                combinedPixels[i + 3] = blendedAlpha;
            }

            // Create a new IBuffer from the byte array
            var buffer = combinedPixels.AsBuffer();

            // Set the PixelBuffer
            using (var pixelStream = buffer.AsStream())
            {
                // Open the pixel buffer stream for writing
                using (var outputStream = combinedBitmap.PixelBuffer.AsStream())
                {
                    pixelStream.CopyTo(outputStream);
                }
            }
            return combinedBitmap;
        }


        private async void btnExportImage_Click(object sender, RoutedEventArgs e)
        {
            // ensure somthing to save
            if (writableBitmap == null)
            {
                ContentDialog message = new ContentDialog();
                message.Content = "Nothing to save";
                message.PrimaryButtonText = "Ok";
                await message.ShowAsync();
                return;
            }

            
            // if not exists
            // Save new myink file
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("jpg", new List<string>() { ".jpg" });
            savePicker.FileTypeChoices.Add("png", new List<string>() { ".png" });
            savePicker.FileTypeChoices.Add("bmp", new List<string>() { ".bmp" });
            savePicker.FileTypeChoices.Add("tiff", new List<string>() { ".tiff" });
            savePicker.FileTypeChoices.Add("gif", new List<string>() { ".gif" });
            savePicker.FileTypeChoices.Add("heif", new List<string>() { ".heif" });
            
            //savePicker.SuggestedFileName = "Custom"; // + mapFile.DisplayName;

            StorageFile exportfile = await savePicker.PickSaveFileAsync();
            if (exportfile == null)
            {
                ContentDialog message = new ContentDialog();
                message.Content = "No save file seleted";
                message.PrimaryButtonText = "Ok";
                await message.ShowAsync();
                return;
            }

            // Combine the WritableBitmap with the ink strokes from InkCanvas
            //WriteableBitmap combinedBitmap = await CombineInkWithBitmap(inkCanvas, writableBitmap);

            //
            Guid bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            switch (exportfile.FileType.ToLower())
            {
                case ".jpeg":
                    bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;
                case ".jpg":
                    bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;
                case ".png":
                    bitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                    break;
                case ".bmp":
                    bitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                    break;
                case ".tiff":
                    bitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                    break;
                case ".gif":
                    bitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                    break;
                case ".heif":
                    bitmapEncoderGuid = BitmapEncoder.HeifEncoderId;
                    break;

            }



            HashSet<string> transparentImageFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png",
                ".gif",
                ".tiff",
                ".bmp"
            };

            BitmapAlphaMode bitmapAlphaMode = BitmapAlphaMode.Ignore;
            if (transparentImageFormats.Contains(exportfile.FileType.ToLower()))
            {
                ContentDialog backgroundcolourchoice = new ContentDialog();
                backgroundcolourchoice.Content = "Make background transparent?";
                backgroundcolourchoice.PrimaryButtonText = "No";
                backgroundcolourchoice.SecondaryButtonText = "Transparent";
                ContentDialogResult backgroundcolourchoiceresult = await backgroundcolourchoice.ShowAsync();
                
                if (backgroundcolourchoiceresult == ContentDialogResult.Secondary)
                {
                    bitmapAlphaMode = BitmapAlphaMode.Straight;
                }
            }

            // Write writablebitmap to file
            // Convert WriteableBitmap to a PNG image stream
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                Debug.Write(writableBitmap.GetHashCode());
                // Encode the WriteableBitmap to a PNG format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(bitmapEncoderGuid, stream);
                Stream pixelStream = writableBitmap.PixelBuffer.AsStream();
                var xxxxxtt = writableBitmap.PixelBuffer.ToArray();
                byte[] pixels = new byte[pixelStream.Length];
                pixelStream.Read(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, bitmapAlphaMode, (uint)writableBitmap.PixelWidth, (uint)writableBitmap.PixelHeight, 96, 96, pixels);
                await encoder.FlushAsync();

                var xxxxx = writableBitmap.PixelBuffer.ToArray();

                // Save the stream to the file
                using (IRandomAccessStream fileStream = await exportfile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    stream.Seek(0); // Ensure the stream is at the beginning
                    await RandomAccessStream.CopyAsync(stream, fileStream);
                }
            }
            ContentDialog cdFeedBack = new ContentDialog();
            cdFeedBack.Title = "File saved";
            cdFeedBack.PrimaryButtonText = "Ok";
            await cdFeedBack.ShowAsync();
        }

        private void legPicker_Click(object sender, RoutedEventArgs e)
        {

        }

        private void testPen_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += StartLine;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += ContinueLine;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += CompleteLine;
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
        }

        Line line = null;

        private void StartLine(InkUnprocessedInput sender, PointerEventArgs args)
        {
            line = new Line();
            line.X1 = args.CurrentPoint.RawPosition.X;
            line.Y1 = args.CurrentPoint.RawPosition.Y;
            line.X2 = args.CurrentPoint.RawPosition.X;
            line.Y2 = args.CurrentPoint.RawPosition.Y;

            line.Stroke = new SolidColorBrush(Colors.Purple);
            line.StrokeThickness = 4;
            mainCanvas.Children.Add(line);
        }
        private void ContinueLine(InkUnprocessedInput sender,PointerEventArgs args)
        {
            line.X2 = args.CurrentPoint.RawPosition.X;
            line.Y2 = args.CurrentPoint.RawPosition.Y;
        }
        private void CompleteLine(InkUnprocessedInput sender, PointerEventArgs args)
        {
            List<InkPoint> points = new List<InkPoint>();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            InkPoint pointOne = new InkPoint(new Point(line.X1, line.Y1), 0.5f);
            InkPoint pointTwo = new InkPoint(new Point(line.X2, line.Y2), 0.5f);
            points.Add(pointOne);
            points.Add(pointTwo);
            InkStroke stroke = builder.CreateStrokeFromInkPoints(points, System.Numerics.Matrix3x2.Identity);
            InkDrawingAttributes ida = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            stroke.DrawingAttributes = ida;
            inkCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
            mainCanvas.Children.Remove(line);
        }
    }
}