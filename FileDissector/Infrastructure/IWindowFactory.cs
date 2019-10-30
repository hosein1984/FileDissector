namespace FileDissector.Infrastructure
{
    public interface IWindowFactory
    {
        MainWindow Create(bool showMenu = false);
    }
}
