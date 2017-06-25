using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage;
using Windows.Storage.BulkAccess;
#endif

using FFImageLoading.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLStorage;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Syncfusion.Presentation;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CreationCollisionOption = PCLStorage.CreationCollisionOption;
using FileAccess = PCLStorage.FileAccess;

namespace Drawesome.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GetPhotoPage : ContentPage
    {
        const string subscriptionKey = "108a07b25f52409b8e9d4cb5e18d0d9f";
        private const string bingSubscription = "78d76116fad440ed81216c0babc0873d";
        const string uriBase = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/recognizeText";
        private const string bingSearch = "https://api.cognitive.microsoft.com/bing/v5.0/images/search?q=";
        public GetPhotoPage()
        {
            InitializeComponent();
        }

        public HttpClient GetClient(MsType type)
        {

            HttpClient client;

            // Request headers.
            switch (type)
            {
                case MsType.HandWritingRecognition:
                    string requestParameters = "handwriting=true";
                    string uri = uriBase + "?" + requestParameters;
                    client = new HttpClient { BaseAddress = new Uri(uri) };
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

                    break;
                case MsType.BingSearch:
                    client = new HttpClient { BaseAddress = new Uri(bingSearch) };
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", bingSubscription);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }



            return client;
        }

        public void RescaleImage(float maxHeight, float maxWidth, float oldImageWidth, float oldImageHeight, float slideWidth, float slideHeight)
        {
            var scaleRx = (slideWidth * maxWidth) / oldImageWidth;
            var scaleRy = slideHeight * maxHeight / oldImageHeight;
            var realScaler = Math.Min(scaleRx, scaleRy);
            var newImageWidth = oldImageWidth * realScaler;
            var newImageHeight = oldImageHeight * realScaler;
            var positionX = slideWidth - newImageWidth;
            var positionY = slideHeight - newImageHeight;
        }


        private async void Button_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", ":( No camera available.", "OK");
                return;
            }

            var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
            {
                Directory = "Sample",
                Name = "test.jpg",
                AllowCropping = true,
                CompressionQuality = 40, PhotoSize = PhotoSize.Custom, CustomPhotoSize = 50
            });

            if (file == null)
                return;

            var client = GetClient(MsType.HandWritingRecognition);

            HttpResponseMessage response;

            var ms = new MemoryStream();
            // Request body. Posts a locally stored JPEG image.
            (await (await FileSystem.Current.GetFileFromPathAsync(file.Path)).OpenAsync(FileAccess.Read)).CopyTo(ms);

            
            using (ByteArrayContent content = new ByteArrayContent(ms.ToArray()))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(client.BaseAddress, content);
                string operationLocation;

                if (response.IsSuccessStatusCode)
                {
                    operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
                }
                else
                {
                    // Display the JSON error data.
                    Console.WriteLine("\nError:\n");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return;
                }
                // Get the JSON response.
                string contentString;
                int i = 0;
                do
                {
                    await Task.Delay(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return;
                }

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n");
                var jo = JsonConvert.DeserializeObject<JObject>(contentString);
                var jo1 = jo["recognitionResult"]["lines"].Children().OrderBy(o=>o["boundingBox"][1].Value<int>()).ToArray();

                var test = jo1.Select(s => new DpItem(s)).ToArray();
                var minSize = test.Min(m => m.Height);
                var maxSize = test.Max(m => m.Height);
                var minimumLeft = test.Min(m => m.Left);
                var minimumTop = test.Min(m => m.Top);
                var maximumTop = test.Max(m => m.Top);


                IPresentation presentation = Presentation.Create();

                //Adds new Blank type of slide.

                ISlide slide = presentation.Slides.Add(SlideLayoutType.Blank);
                
                //Adds Rectangle auto shape with specified size and positions.

                IShape shape = slide.Shapes.AddShape(AutoShapeType.Rectangle, 0, 0, 500, 300);
                shape.Fill.FillType = FillType.Solid;
                shape.Fill.SolidFill.Color = ColorObject.White;
                shape.Fill.SolidFill.Transparency = 70;
                //Adds text into the shape.
                //var remail = new Regex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$1^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
                //var ruri = new Regex(@"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?$");

                foreach (var dpItem in test)
                {
                    var dp = dpItem.TextContents.ToLower().Trim();
                    if (dp.StartsWith("image") || dp.StartsWith("background"))
                    {
                        var searchItem = dp.Split('-').LastOrDefault()?.Trim();
                        if (string.IsNullOrWhiteSpace(searchItem))
                        {
                            //todo empty image
                        }
                        else
                        {
                            var bs = GetClient(MsType.BingSearch);
                            var ss = await bs.GetStringAsync(bs.BaseAddress + searchItem);
                            var jimg = JsonConvert.DeserializeObject<JObject>(ss);
                            var testImgs = jimg["value"].Children().ToArray();
                            var rndInx = new Random(Guid.NewGuid().GetHashCode()).Next(0, testImgs.Length);
                            var targetImgUri = testImgs[rndInx]["contentUrl"].Value<string>();
                            double targetImgWidth = Math.Abs(testImgs[rndInx]["width"].Value<int>());
                            double targetImgHeight = Math.Abs(testImgs[rndInx]["height"].Value<int>());



                            var sh = slide.SlideSize.Height;
                            var sw = slide.SlideSize.Width;

                            double scaleRx;
                            double scaleRy;
                            if (targetImgWidth > targetImgHeight)
                            {
                                scaleRx = (sw * 0.5f) / targetImgWidth;
                                scaleRy = sh * 0.3f / targetImgHeight;
                            }
                            else
                            {
                                scaleRx = (sw * 0.2f) / targetImgWidth;
                                scaleRy = sh * 0.6f / targetImgHeight;
                            }

                            var realScaler = Math.Min(scaleRx, scaleRy);
                            double newImageWidth = targetImgWidth * realScaler;
                            double newImageHeight = targetImgHeight * realScaler;
                            var positionX = sw - newImageWidth;
                            var positionY = sh - newImageHeight;


                            var pictStream = await bs.GetStreamAsync(targetImgUri);
                            var ms1 = new MemoryStream();
                            pictStream.CopyTo(ms1);
                            var sss = await ImageResizer.ResizeImage(ms1.ToArray(), newImageWidth, newImageHeight);
                            var ms2 = new MemoryStream(sss);


                            if (dp.StartsWith("background"))
                            {
                                slide.Background.Fill.PictureFill.ImageBytes = ms1.ToArray();
                                slide.Background.Fill.PictureFill.TileMode = TileMode.Tile;


                            }
                            else
                            {
                                slide.Pictures.AddPicture(ms2, positionX, positionY, newImageWidth, newImageHeight);

                            }



                            TestImage.Source = targetImgUri;
                        }

                    }
                    else
                    {

                        var pp = shape.TextBody.AddParagraph(dpItem.TextContents);
                        pp.Font.FontSize = dpItem.FontSize;
                        
                    }
                }


                //Creates new memory stream to save Presentation.

                MemoryStream stream = new MemoryStream();

                //Saves Presentation in stream format.

                presentation.Save(stream);

                presentation.Close();

                stream.Position = 0;
                var fname = $"{Guid.NewGuid()}.pptx";
                var f = await FileSystem.Current.LocalStorage.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting);
                var os = await f.OpenAsync(FileAccess.ReadAndWrite);
                await stream.CopyToAsync(os);
                await os.FlushAsync();

#if WINDOWS_UWP
                var fileFromPathAsync = await StorageFile.GetFileFromPathAsync(f.Path);
                Windows.System.Launcher.LaunchFileAsync(fileFromPathAsync);
#endif
            }

        }
    }

    public enum MsType
    {
        HandWritingRecognition,
        BingSearch
    }


    public class DpItem
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public DpItem(JToken jt)
        {
            var jtb = jt["boundingBox"].Values<int>().ToArray();
            Height = jtb[7] - jtb[1];
            Left = jtb[0];
            Top = jtb[1];
            //if (Height > 80)
            //{
            //    FontSize = 68;
            //}
            //else if (Height > 60)
            //{
            //    FontSize = 56;
            //}
            //else if (Height > 40)
            //{
            //    FontSize = 44;
            //}
            //else
            //{
            //    FontSize = 24;
            //}
            TextContents = jt["text"].Value<string>();
        }

        public int Height { get; set; }

        public int FontSize { get; set; }
        public string FontColor { get; set; }
        public string FontType { get; set; }
        public string TextContents { get; set; }
    }
}