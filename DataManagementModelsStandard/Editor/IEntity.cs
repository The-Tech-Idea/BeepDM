using System.ComponentModel;

namespace TheTechIdea.Beep.Editor
{
    public interface IEntity
    {
        event PropertyChangedEventHandler PropertyChanged;
    }
}