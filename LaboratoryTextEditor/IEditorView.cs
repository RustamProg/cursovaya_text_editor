using System.Windows.Forms;

namespace LaboratoryTextEditor;

public interface IEditorView
{
    IWin32Window Owner { get; }
    Form Form { get; }
    TabControl Tabs { get; }
    ToolStripMenuItem RecentMenu { get; }

    void SetStatus(string text);
}
