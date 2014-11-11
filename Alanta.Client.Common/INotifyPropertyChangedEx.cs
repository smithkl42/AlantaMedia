using System.ComponentModel;

namespace Alanta.Client.Common
{
    public interface INotifyPropertyChangedEx : INotifyPropertyChanged
    {
        void RaisePropertyChanged(string propertyName);
    }
}
