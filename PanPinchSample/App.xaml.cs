using System.Linq;
using System.Reflection;
using Microsoft.Practices.ObjectBuilder2;
using Prism.Unity;
using PanPinchSample.Views;
using Xamarin.Forms;

namespace PanPinchSample
{
    public partial class App : PrismApplication
    {
        public App(IPlatformInitializer initializer = null) : base(initializer) { }

        protected override void OnInitialized()
        {
            InitializeComponent();

            NavigationService.NavigateAsync("MyNavigationPage/MainPage");
        }

        protected override void RegisterTypes()
        {
            var types = this.GetType().GetTypeInfo().Assembly.DefinedTypes;
            this.GetType().GetTypeInfo().Assembly.DefinedTypes
                .Where(t => t.Namespace?.EndsWith(".Views", System.StringComparison.Ordinal) ?? false)
                .ForEach(t => {
                    Container.RegisterTypeForNavigation(t.AsType(), t.Name);
                });
        }

    }
}

