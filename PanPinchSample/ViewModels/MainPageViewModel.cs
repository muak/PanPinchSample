using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using PanPinchSample.Resources;
using Xamarin.Forms;
using System.Net.Http;
using Prism.Services;
using System.IO;

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

        private IPageDialogService _pageDialog;

        public MainPageViewModel(IPageDialogService pageDlg)
        {
            _pageDialog = pageDlg;
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

        private DelegateCommand _CropCommand;
        public DelegateCommand CropCommand {
            get {
                return _CropCommand = _CropCommand ?? new DelegateCommand(async () => {
                    await _pageDialog.DisplayAlertAsync("切り取る範囲（相対値）", $"X:{CropRect.X:0.00} Y:{CropRect.Y:0.00}\nWidth:{CropRect.Width:0.00} Height:{CropRect.Height:0.00}", "OK");
                });
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
            CropRect = new Rectangle(0, 0, 100, 100);

            var httpClient = new HttpClient();
            var stream = await httpClient.GetByteArrayAsync("http://free-photos-ls02.gatag.net/images/lgf01a201309171000.jpg");
            ImageSrc = ImageSource.FromStream(() => new MemoryStream(stream));
            BackImage = ImageSource.FromStream(() => new MemoryStream(stream));
        }
    }
}

