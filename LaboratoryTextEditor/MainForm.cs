using System;
using System.Drawing;
using System.Windows.Forms;
using LaboratoryTextEditor.Commands;

namespace LaboratoryTextEditor;

public sealed class MainForm : Form, IEditorView
{
    private readonly TabControl _tabs = new();
    private readonly MenuStrip _menuStrip = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Text = "Готово." };

    private readonly ToolStripMenuItem _recentMenuItem = new("Recent");
    private readonly Editor _editor;

    public MainForm()
    {
        Text = "Rustam Gabdulbarov's Text Editor";
        Width = 1000;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        _editor = new Editor(this);

        BuildUi();

        // Создаём стартовый документ, чтобы приложение было сразу готово к вводу.
        _editor.NewDoc();
    }

    public IWin32Window Owner => this;
    public Form Form => this;
    public TabControl Tabs => _tabs;
    public ToolStripMenuItem RecentMenu => _recentMenuItem;

    public void SetStatus(string text) => _statusLabel.Text = text;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        // Exit — с проверкой на сохранение открытых документов.
        if (!_editor.ConfirmCloseAllDocuments())
            e.Cancel = true;
    }

    private void BuildUi()
    {
        SuspendLayout();

        // Tabs (добавляем первым, чтобы Dock=Fill корректно учитывал Top/Bottom панели)
        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(16, 4); // чуть больше места под заголовки
        Controls.Add(_tabs);

        // StatusStrip
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Dock = DockStyle.Bottom;
        Controls.Add(_statusStrip);

        // Menu
        _menuStrip.Dock = DockStyle.Top;

        var fileMenu = new ToolStripMenuItem("File");

        fileMenu.DropDownItems.Add(MakeCommandItem("New", Keys.Control | Keys.N, new DelegateCommand(_editor.NewDoc)));
        fileMenu.DropDownItems.Add(MakeCommandItem("Open", Keys.Control | Keys.O, new DelegateCommand(_editor.OpenDoc)));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(MakeCommandItem("Save", Keys.Control | Keys.S, new DelegateCommand(_editor.SaveDoc)));
        fileMenu.DropDownItems.Add(MakeCommandItem("Save As", Keys.Control | Keys.Shift | Keys.S, new DelegateCommand(_editor.SaveDocAs)));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(MakeCommandItem("Close", Keys.Control | Keys.W, new DelegateCommand(_editor.CloseActiveDoc)));
        fileMenu.DropDownItems.Add(_recentMenuItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(MakeCommandItem("Exit", Keys.Alt | Keys.F4, new DelegateCommand(Close)));

        _menuStrip.Items.Add(fileMenu);

        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);

        // Context menu on tabs
        var tabMenu = new ContextMenuStrip();
        var closeTabItem = new ToolStripMenuItem("Close tab")
        {
            Tag = new DelegateCommand(_editor.CloseActiveDoc)
        };
        closeTabItem.Click += _editor.OnCommandMenuClick;
        tabMenu.Items.Add(closeTabItem);

        _tabs.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;

            for (int i = 0; i < _tabs.TabCount; i++)
            {
                if (_tabs.GetTabRect(i).Contains(e.Location))
                {
                    _tabs.SelectedIndex = i;
                    tabMenu.Show(_tabs, e.Location);
                    return;
                }
            }
        };

        // Middle click closes tab (удобно, но не обязательно)
        _tabs.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Middle) return;

            for (int i = 0; i < _tabs.TabCount; i++)
            {
                if (_tabs.GetTabRect(i).Contains(e.Location))
                {
                    _tabs.SelectedIndex = i;
                    _editor.CloseActiveDoc();
                    return;
                }
            }
        };

        ResumeLayout(performLayout: true);
    }

    private ToolStripMenuItem MakeCommandItem(string text, Keys shortcutKeys, ICommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var item = new ToolStripMenuItem(text)
        {
            ShortcutKeys = shortcutKeys,
            ShowShortcutKeys = true,
            Tag = command
        };

        item.Click += _editor.OnCommandMenuClick;
        return item;
    }
}
