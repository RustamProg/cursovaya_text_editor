using System;

namespace LaboratoryTextEditor.Commands;

public sealed class OpenRecentCommand : ICommand
{
    private readonly Editor _editor;
    private readonly int _index;

    public OpenRecentCommand(Editor editor, int index)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _index = index;
    }

    public void Execute() => _editor.OpenDocByRecentIndex(_index);
}
