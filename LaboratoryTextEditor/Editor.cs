using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LaboratoryTextEditor;

/// <summary>
/// Редактор (контроллер/сервис) - управляет вкладками, документами и списком Recent.
/// </summary>
public sealed class Editor
{
    private readonly IEditorView _view;
    private readonly DocumentTabFactory _tabFactory = new();
    private readonly RecentList _recentList = new();

    public Editor(IEditorView view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));

        _recentList.LoadData();
        RebuildRecentMenu();

        _view.Tabs.SelectedIndexChanged += (_, __) => UpdateUiState();
        _view.Tabs.ControlAdded += (_, __) => UpdateUiState();
        _view.Tabs.ControlRemoved += (_, __) => UpdateUiState();

        UpdateUiState();
    }

    public RecentList Recent => _recentList;

    public void NewDoc()
    {
        var (tab, doc) = _tabFactory.CreateNew();
        HookDocument(doc, tab);
        _view.Tabs.TabPages.Add(tab);
        _view.Tabs.SelectedTab = tab;

        _view.SetStatus("Создан новый документ.");
        UpdateUiState();
    }

    public void CloseActiveDoc()
    {
        if (_view.Tabs.SelectedTab is null) return;

        var tab = _view.Tabs.SelectedTab;
        if (!TryCloseTab(tab)) return;

        _view.Tabs.TabPages.Remove(tab);

        // Удобство: если закрыли последнюю вкладку, создадим пустую.
        if (_view.Tabs.TabPages.Count == 0)
            NewDoc();

        UpdateUiState();
    }

    public void OpenDoc()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Open"
        };

        if (dlg.ShowDialog(_view.Owner) != DialogResult.OK) return;

        OpenDocumentFromPath(dlg.FileName);
    }

    public void SaveDoc()
    {
        var doc = GetActiveDocument();
        if (doc is null) return;

        if (doc.BoolHasName)
        {
            doc.Save();
            AfterSuccessfulSave(doc);
            return;
        }

        SaveDocAs();
    }

    public void SaveDocAs()
    {
        var doc = GetActiveDocument();
        if (doc is null) return;

        using var dlg = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Save As",
            FileName = doc.BoolHasName ? Path.GetFileName(doc.Name) : "document.txt"
        };

        if (dlg.ShowDialog(_view.Owner) != DialogResult.OK) return;

        doc.SaveAs(dlg.FileName);
        AfterSuccessfulSave(doc);
    }

    public void OpenDocByRecentIndex(int index)
    {
        if (index < 0 || index >= _recentList.Items.Count)
            return;

        var path = _recentList.Items[index];
        if (!File.Exists(path))
        {
            MessageBox.Show(_view.Owner, $"Файл не найден:\n{path}", "Recent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _recentList.Remove(path);
            RebuildRecentMenu();
            return;
        }

        OpenDocumentFromPath(path);
    }

    public bool DocOpened(string fileName)
    {
        var full = Path.GetFullPath(fileName);

        foreach (TabPage tab in _view.Tabs.TabPages)
        {
            if (tab.Tag is Document d && d.BoolHasName)
            {
                if (string.Equals(d.Name, full, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public bool ConfirmCloseAllDocuments()
    {
        // Закрываем вкладки по одной, чтобы не нарушать логику диалогов сохранения.
        for (int i = _view.Tabs.TabPages.Count - 1; i >= 0; i--)
        {
            var tab = _view.Tabs.TabPages[i];
            if (!TryCloseTab(tab))
                return false;
        }

        _recentList.SaveData();
        return true;
    }

    public void RebuildRecentMenu()
    {
        var recentMenu = _view.RecentMenu;
        recentMenu.DropDownItems.Clear();

        var items = _recentList.Items.Take(5).ToArray();

        if (items.Length == 0)
        {
            var empty = new ToolStripMenuItem("(empty)") { Enabled = false };
            recentMenu.DropDownItems.Add(empty);
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            var path = items[i];
            var text = $"&{i + 1} {path}";
            var item = new ToolStripMenuItem(text);

            // Command pattern - пункт меню знает только "команду"
            item.Tag = new Commands.OpenRecentCommand(this, i);
            item.Click += OnCommandMenuClick;

            recentMenu.DropDownItems.Add(item);
        }
    }

    public void OnCommandMenuClick(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: Commands.ICommand cmd })
            cmd.Execute();
    }

    private void OpenDocumentFromPath(string fileName)
    {
        var full = Path.GetFullPath(fileName);

        // Если уже открыт - просто активируем вкладку
        var already = FindTabByPath(full);
        if (already is not null)
        {
            _view.Tabs.SelectedTab = already;
            _recentList.Add(full);
            RebuildRecentMenu();
            _view.SetStatus("Документ уже был открыт, активирована вкладка.");
            return;
        }

        var (tab, doc) = _tabFactory.CreateFromFile(full);
        HookDocument(doc, tab);

        _view.Tabs.TabPages.Add(tab);
        _view.Tabs.SelectedTab = tab;

        _recentList.Add(full);
        RebuildRecentMenu();

        _view.SetStatus($"Открыт файл: {full}");
        UpdateUiState();
    }

    private TabPage? FindTabByPath(string fullPath)
    {
        foreach (TabPage tab in _view.Tabs.TabPages)
        {
            if (tab.Tag is Document d && d.BoolHasName &&
                string.Equals(d.Name, fullPath, StringComparison.OrdinalIgnoreCase))
                return tab;
        }
        return null;
    }

    private void AfterSuccessfulSave(Document doc)
    {
        _recentList.Add(doc.Name!);
        _recentList.SaveData();
        RebuildRecentMenu();
        UpdateActiveTabTitle();
        _view.SetStatus("Документ сохранён.");
    }

    private void HookDocument(Document doc, TabPage tab)
    {
        doc.ModifiedChanged += (_, __) =>
        {
            // При изменении флага обновим заголовок вкладки
            tab.Text = FormatTabTitle(doc);
        };
        tab.Text = FormatTabTitle(doc);
    }

    private string FormatTabTitle(Document doc)
    {
        var name = doc.StringShortName;
        return doc.BoolModified ? $"{name}*" : name;
    }

    private bool TryCloseTab(TabPage tab)
    {
        if (tab.Tag is not Document doc)
            return true;

        if (!doc.BoolModified)
            return true;

        var result = MessageBox.Show(
            _view.Owner,
            $"Документ \"{doc.StringShortName}\" был изменён. Сохранить изменения?",
            "Close",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Cancel)
            return false;

        if (result == DialogResult.No)
            return true;

        // Yes
        if (doc.BoolHasName)
        {
            try
            {
                doc.Save();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(_view.Owner, ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Save As
        using var dlg = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Save",
            FileName = "document.txt"
        };

        if (dlg.ShowDialog(_view.Owner) != DialogResult.OK)
            return false;

        try
        {
            doc.SaveAs(dlg.FileName);
            AfterSuccessfulSave(doc);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(_view.Owner, ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private Document? GetActiveDocument()
        => _view.Tabs.SelectedTab?.Tag as Document;

    private void UpdateActiveTabTitle()
    {
        if (_view.Tabs.SelectedTab?.Tag is Document doc)
            _view.Tabs.SelectedTab.Text = FormatTabTitle(doc);
    }

    private void UpdateUiState()
    {
        var hasDoc = _view.Tabs.TabPages.Count > 0 && _view.Tabs.SelectedTab?.Tag is Document;

        _view.SetStatus(hasDoc ? "Готово." : "Нет открытых документов.");
    }
}
