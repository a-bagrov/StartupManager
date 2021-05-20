using StartupManager.Implementation.Services;
using StartupManager.Interfaces;
using StartupManager.MVVM;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace StartupManager.ViewModels
{
    internal class StartupManagerVm : INPCBase, IExplorerOpener
    {
        private double _progress;

        public double Progress
        {
            get { return _progress; }
            set { _progress = value; OnPropertyChanged(); }
        }

        private bool _isLoaded;

        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { _isLoaded = value; OnPropertyChanged(); }
        }

        public AsyncCommand UpdateCommand { get; }
        public AsyncCommand ExitCommand { get; }


        public ObservableCollection<IStartupItem> StartupItems { get; } = new();
        private readonly IStartupItemsProvider _siProvider;
        private readonly IFileIconProvider _iconProvider;
        private readonly IFileInfoProvider _fileInfoProvider;
        private readonly IStartupExeValuesProvider[] _nameProviders;
        private readonly IExplorerOpener _explorerOpener;

        public StartupManagerVm(StartupItemsProvider siProvider, IFileIconProvider iconProvider, IFileInfoProvider fileInfoProvider, IExplorerOpener explorerOpener, params IStartupExeValuesProvider[] nameProviders)
        {
            _siProvider = siProvider;
            _iconProvider = iconProvider;
            _fileInfoProvider = fileInfoProvider;
            _explorerOpener = explorerOpener;
            _nameProviders = nameProviders;
            _siProvider.ProgressChanged += (_, e) => Progress = e.ProgressPercentage;
            UpdateCommand = new(UpdateStartupItems, () => IsLoaded);
            ExitCommand = new(() => { Environment.Exit(0); return Task.CompletedTask; });
        }

        public async Task UpdateStartupItems()
        {
            IsLoaded = false;
            var siCol = await _siProvider.GetStartupItemsAsync(_iconProvider, _fileInfoProvider, _nameProviders);
            StartupItems.Clear();

            foreach (var c in siCol)
                StartupItems.Add(c);

            IsLoaded = true;
        }

        public void OpenExplorerAt(string filePath)
        {
            //здесь можно добавить валидацию или какую-нибудь проверку
            _explorerOpener.OpenExplorerAt(filePath);
        }
    }
}
