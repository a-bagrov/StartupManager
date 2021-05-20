using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StartupManager.MVVM
{
    internal class INPCBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string caller = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
    }
}
