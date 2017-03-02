using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using PanPinchSample.Resources;
using Xamarin.Forms;
using System.Net.Http;
using Prism.Services;
using System.IO;
using Plugin.ImageEdit;
using System.Threading.Tasks;
using Plugin.ImageEdit.Abstractions;

namespace PanPinchSample.ViewModels
{
    public class MainPageViewModel : BindableBase, INavigationAware
    {
        private ImageSource _ImageSrc;
        public ImageSource ImageSrc {
            get { return _ImageSrc; }
            set { SetProperty(ref _ImageSrc, value); }
        }

        private ImageSource _BackImage;
        public ImageSource BackImage {
            get { return _BackImage; }
            set { SetProperty(ref _BackImage, value); }
        }


        private Rectangle _CropRect;
        public Rectangle CropRect {
            get { return _CropRect; }
            set { SetProperty(ref _CropRect, value); }
        }

        private Rectangle _ParentRect;
        public Rectangle ParentRect {
            get { return _ParentRect; }
            set { SetProperty(ref _ParentRect, value); }
        }

        private double _Degree;
        public double Degree {
            get { return _Degree; }
            set { SetProperty(ref _Degree, value); }
        }

        private bool _ResultVisible;
        public bool ResultVisible {
            get { return _ResultVisible; }
            set { SetProperty(ref _ResultVisible, value); }
        }

        private ImageSource _ResultImage;
        public ImageSource ResultImage {
            get { return _ResultImage; }
            set { SetProperty(ref _ResultImage, value); }
        }

        private IPageDialogService _pageDialog;
        private IImageEdit _imageEdit;
        private byte[] _orgImage;

        public MainPageViewModel(IPageDialogService pageDlg, IImageEdit imageEdit)
        {
            _pageDialog = pageDlg;
            _imageEdit = imageEdit;
            ResultVisible = false;
            Degree = 90;
            CropRect = new Rectangle(0, 0, 0, 0);
        }


        private DelegateCommand _AlterThemeCommand;
        public DelegateCommand AlterThemeCommand {
            get {
                return _AlterThemeCommand = _AlterThemeCommand ?? new DelegateCommand(() => {
                    var app = Xamarin.Forms.Application.Current;
                    var parent = Xamarin.Forms.Application.Current.MainPage.Parent;
                    app.MainPage.Parent = null;
                    if (app.Resources.GetType() == typeof(AppTheme)) {
                        app.Resources = new OtherTheme();
                    }
                    else {
                        app.Resources = new AppTheme();
                    }

                    app.MainPage.Parent = parent;
                });
            }
        }

        private DelegateCommand _RotateCommand;
        public DelegateCommand RotateCommand {
            get {
                return _RotateCommand = _RotateCommand ?? new DelegateCommand(() => {
                    Degree = 90;
                });
            }
        }

        private DelegateCommand _CropCommand;
        public DelegateCommand CropCommand {
            get {
                return _CropCommand = _CropCommand ?? new DelegateCommand(EditImage);
            }
        }

        private DelegateCommand _ResultCloseCommand;
        public DelegateCommand ResultCloseCommand {
            get {
                return _ResultCloseCommand = _ResultCloseCommand ?? new DelegateCommand(() => {
                    ResultVisible = false;
                    ResultImage = null;
                });
            }
        }

        private async void EditImage()
        {
            var degree = (float)Math.Round(Degree);

            var ret = await _pageDialog.DisplayAlertAsync(
                "Crop Range(relative) and Rotate", $"X:{CropRect.X:0.00} Y:{CropRect.Y:0.00}\nWidth:{CropRect.Width:0.00} Height:{CropRect.Height:0.00}\nRotate:{degree}", "OK", "Cancel");

            if (!ret) {
                return;
            }

            using (var image = await CrossImageEdit.Current.CreateImageAsync(_orgImage)) {
                var w = image.Width;
                var h = image.Height;

                var cropX = (int)(CropRect.X * w);
                var cropY = (int)(CropRect.Y * h);
                var cropW = (int)(CropRect.Width * w);
                var cropH = (int)(CropRect.Height * h);

                var resizeW = 320;

                var croped = await Task.Run(
                    () => image.Crop(cropX, cropY, cropW, cropH)
                        .Rotate(degree)
                        .Resize(resizeW, 0)
                        .ToJpeg()
                );

                ResultImage = ImageSource.FromStream(() => new MemoryStream(croped));
                ResultVisible = true;

            }

        }

        public void OnNavigatedFrom(NavigationParameters parameters)
        {

        }

        public void OnNavigatedTo(NavigationParameters parameters)
        {

        }

        public async void OnNavigatingTo(NavigationParameters parameters)
        {
            var httpClient = new HttpClient();
            _orgImage = await httpClient.GetByteArrayAsync("http://free-photos-ls02.gatag.net/images/lgf01a201309171000.jpg");

            //var image = _imageEdit.CreateImage(_orgImage);
            //image.Resize(100,0);

            ImageSrc = ImageSource.FromStream(() => new MemoryStream(_orgImage));
            BackImage = ImageSource.FromStream(() => new MemoryStream(_orgImage));
        }
    }
}

