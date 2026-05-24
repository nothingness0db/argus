using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotspotManager.Services
{
    public class LocaleStrings : INotifyPropertyChanged
    {
        public string this[string key] => LocaleService.Get(key);

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
