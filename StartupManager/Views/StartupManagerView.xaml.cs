using StartupManager.Interfaces;
using System.Windows.Controls;

namespace StartupManager.Views
{
    /// <summary>
    /// Interaction logic for StartupManagerView.xaml
    /// </summary>
    public partial class StartupManagerView : UserControl
    {
        private readonly IExplorerOpener _explorerOpener;
        internal StartupManagerView(IExplorerOpener opener)
        {
            InitializeComponent();
            _explorerOpener = opener;
        }

        private void ListView_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not ListView listView)
                return;

            if (listView.SelectedValue is not IStartupItem item)
                return;

            _explorerOpener.OpenExplorerAt(item.Path);
        }
    }
}
