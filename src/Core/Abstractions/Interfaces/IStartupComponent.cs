namespace NileCore.Raar.Core.Interfaces;

public interface IStartupComponent
{
    string Name { get; }
    string Version { get; }
    void Initialize();
}
