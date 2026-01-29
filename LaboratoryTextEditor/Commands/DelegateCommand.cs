using System;

namespace LaboratoryTextEditor.Commands;

public sealed class DelegateCommand : ICommand
{
    private readonly Action _execute;

    public DelegateCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public void Execute() => _execute();
}
