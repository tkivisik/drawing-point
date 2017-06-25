using Syncfusion.ListView.XForms.UWP;

namespace Drawesome.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
            SfListViewRenderer.Init();
            LoadApplication(new Drawesome.App());
        }
    }
}
