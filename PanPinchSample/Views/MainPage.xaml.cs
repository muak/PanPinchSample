using Xamarin.Forms;
using Prism.Navigation;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
using Xamarin.Forms.PlatformConfiguration;
using System.Reactive.Disposables;

namespace PanPinchSample.Views
{
    public partial class MainPage : ContentPage, IDestructible
    {
        private PanGestureRecognizer _pan;
        private PinchGestureRecognizer _pinch;
        private CompositeDisposable _disposable = new CompositeDisposable();

        public MainPage()
        {
            InitializeComponent();

            backImage.SizeChanged += BackImage_SizeChanged;

            _pan = new PanGestureRecognizer();
            clipRealm.GestureRecognizers.Add(_pan);

            _pinch = new PinchGestureRecognizer();
            clipRealm.GestureRecognizers.Add(_pinch);

            SetPanObservable();
            SetPinchObservable();
        }

        void SetPinchObservable()
        {
            var minSize = 50;   //最小の大きさ
            var sideHitSize = 0.3; //ピンチの位置が相対的にどの辺で端と判断するか

            var pinchOrientation = ScrollOrientation.Both; //ピンチの方向

            var pinchObservable = PinchUpdateAsObervable();

            var started = pinchObservable.Where(x => x.Status == GestureStatus.Started);
            pinchObservable.Where(x => x.Status == GestureStatus.Completed).Subscribe(x => Debug.WriteLine("Pinch Completed"));;

            var startedSub = started.Subscribe(e => {
                Debug.WriteLine("Pinch Started");
                //拡大・縮小の方向の判定
                if ((e.ScaleOrigin.X <= sideHitSize || e.ScaleOrigin.X >= 1 - sideHitSize) && e.ScaleOrigin.Y <= 0.6 && e.ScaleOrigin.Y >= 0.4) {
                    pinchOrientation = ScrollOrientation.Vertical;
                }
                else if ((e.ScaleOrigin.Y <= sideHitSize || e.ScaleOrigin.Y >= 1 - sideHitSize) && e.ScaleOrigin.X <= 0.6 && e.ScaleOrigin.X >= 0.4) {
                    pinchOrientation = ScrollOrientation.Horizontal;
                }
                else {
                    pinchOrientation = ScrollOrientation.Both;
                }
            });

            _disposable.Add(startedSub);

            var running = pinchObservable
                .SkipWhile(x => x.Status != GestureStatus.Running) //Running以外はスキップ
                .TakeWhile(x => x.Status == GestureStatus.Running) //Runningが続く間は流し続ける
                .Repeat(); //繰り返す

            var dragSub = running.Subscribe(e => {

                //現在の状態を取得
                var rectClip = AbsoluteLayout.GetLayoutBounds(clipRealm);
                var rectImage = AbsoluteLayout.GetLayoutBounds(foreImage);
                //拡大縮小後のRectangle
                var scaledRect = rectClip;

                //垂直の座標計算
                if (pinchOrientation == ScrollOrientation.Vertical || pinchOrientation == ScrollOrientation.Both) {

                    scaledRect.Height = Math.Min(Math.Max(rectClip.Height * e.Scale, minSize), backImage.Height);
                    scaledRect.Y = Math.Min(Math.Max(rectClip.Center.Y - (scaledRect.Height / 2), 0), backImage.Height - scaledRect.Height);
                    rectImage.Y = scaledRect.Y * -1;

                }
                //水平の座標計算
                if (pinchOrientation == ScrollOrientation.Horizontal || pinchOrientation == ScrollOrientation.Both) {

                    scaledRect.Width = Math.Min(Math.Max(rectClip.Width * e.Scale, minSize), backImage.Width);
                    scaledRect.X = Math.Min(Math.Max(rectClip.Center.X - (scaledRect.Width / 2), 0), backImage.Width - scaledRect.Width);
                    rectImage.X = scaledRect.X * -1;
                }

                //レイアウトに反映
                AbsoluteLayout.SetLayoutBounds(clipRealm, scaledRect);
                AbsoluteLayout.SetLayoutBounds(foreImage, rectImage);

            });

            _disposable.Add(dragSub);

        }

        void SetPanObservable()
        {
            var sampleSeconds = 150; //サンプリングの間隔
            var moveSeconds = 50; //移動アニメーションの時間

            var panObservable = PanUpdateAsObservable();

            var started = panObservable.Where(x => x.StatusType == GestureStatus.Started);
            var completed = panObservable.Where(x => x.StatusType == GestureStatus.Completed);

            started.Subscribe(x => Debug.WriteLine("Pan Started"));
            completed.Subscribe(x => Debug.WriteLine("Pan Completed"));

            var running = panObservable
                .SkipWhile(x => x.StatusType != GestureStatus.Running)  //Running以外はスキップ
                .TakeWhile(x => x.StatusType == GestureStatus.Running); //Runningの間は流し続ける

            //Androidの場合一定時間でイベントをまとめて処理（カクカク対策）
            if (Device.OS == TargetPlatform.Android) {
                running = running
                    .Sample(TimeSpan.FromMilliseconds(sampleSeconds))   //一定時間ごとにサンプリング
                    .StartWith(new PanUpdatedEventArgs(GestureStatus.Running, 0, 0, 0)); //最初だけ起点用に発行
            }

            //前の値が必要なので一つずらしたものとZipで合成
            var drag = running.Zip(
                        running.Skip(1),
                        (p, n) => new { PreTotal = new Point(p.TotalX, p.TotalY), Total = new Point(n.TotalX, n.TotalY) }
                    ).Repeat(); //繰り返し


            var dragSub = drag.Subscribe(async p => {

                //移動距離の計算
                var distance = Device.OnPlatform(
                    iOS: new Point(p.Total.X - p.PreTotal.X, p.Total.Y - p.PreTotal.Y),  //iOSは移動いてもTotalはリセットされないので差分を使用する
                    Android: new Point(p.Total.X, p.Total.Y),    // Androidは移動後にTotalがリセットされるのでそのまま使う
                    WinPhone: new Point());

                //現在の位置を取得
                var rectClip = AbsoluteLayout.GetLayoutBounds(clipRealm);
                var rectImage = AbsoluteLayout.GetLayoutBounds(foreImage);

                //座標計算
                rectClip.X = Math.Max(0, Math.Min(backImage.Width - rectClip.Width, rectClip.X + distance.X));
                rectClip.Y = Math.Max(0, Math.Min(backImage.Height - rectClip.Height, rectClip.Y + distance.Y));

                rectImage.X = Math.Max(rectClip.Width - backImage.Width, Math.Min(0, rectImage.X - distance.X));
                rectImage.Y = Math.Max(rectClip.Height - backImage.Height, Math.Min(0, rectImage.Y - distance.Y));

                //Androidの場合はLayoutToで移動しないとカクカクMAXで使い物にならない
                //iOSはそのままでぬるぬる動くのでスキップ（LayoutToをすると逆にカクカクになる）
                if (Device.OS == TargetPlatform.Android) {
                    await Task.WhenAll(
                        clipRealm.LayoutTo(rectClip, (uint)moveSeconds, Xamarin.Forms.Easing.Linear),
                        foreImage.LayoutTo(rectImage, (uint)moveSeconds, Xamarin.Forms.Easing.Linear)
                        );
                }

                //AbsoluteLayoutのパラメータに反映
                AbsoluteLayout.SetLayoutBounds(clipRealm, rectClip);
                AbsoluteLayout.SetLayoutBounds(foreImage, rectImage);
            });

            _disposable.Add(dragSub);
        }


        IObservable<PanUpdatedEventArgs> PanUpdateAsObservable()
        {
            return Observable.FromEvent<EventHandler<PanUpdatedEventArgs>, PanUpdatedEventArgs>(
                handler => (sender, e) => handler(e),
                handler => _pan.PanUpdated += handler,
                handler => _pan.PanUpdated -= handler
            );
        }

        IObservable<PinchGestureUpdatedEventArgs> PinchUpdateAsObervable()
        {
            return Observable.FromEvent<EventHandler<PinchGestureUpdatedEventArgs>, PinchGestureUpdatedEventArgs>(
                handler => (sender, e) => handler(e),
                handler => _pinch.PinchUpdated += handler,
                handler => _pinch.PinchUpdated -= handler
            );
        }


        void BackImage_SizeChanged(object sender, EventArgs e)
        {
            if (backImage.Width <= double.Epsilon || backImage.Height <= double.Epsilon) {
                return;
            }

            //背景の大きさを設定
            AbsoluteLayout.SetLayoutBounds(foreImage, new Rectangle(0, 0, backImage.Width, backImage.Height));
        }

        public void Destroy()
        {
            _disposable.Dispose();
        }
    }
}

