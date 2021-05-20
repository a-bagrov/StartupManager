using System.Threading.Tasks;
using System.Windows;
using StartupManager.Implementation.ExeNamesProviders;
using StartupManager.Implementation.Services;
using StartupManager.Interfaces;
using StartupManager.ViewModels;
using StartupManager.Views;
namespace StartupManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var providers = new IStartupExeValuesProvider[]
            {
                new RegistryStartupProvider(),
                new TaskSchedulerProvider(),
                new StartupFolderProvider()
            };
            var expOpener = new ExplorerOpener();

            var vm = new StartupManagerVm(new StartupItemsProvider(), new FileIconProvider(), new FileInfoProvider(), expOpener, providers);
            var view = new StartupManagerView(vm) { DataContext = vm };

            this.Content = view;

            Task.Run(() => vm.UpdateStartupItems());
        }
    }
}
