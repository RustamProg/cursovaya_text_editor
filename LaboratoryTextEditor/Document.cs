using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace LaboratoryTextEditor;

/// <summary>
/// Модель документа + привязка к RichTextBox (WinForms-редактор).
/// Содержит логику открытия/сохранения файла и флаг изменённости.
/// </summary>
public sealed class Document
{
    private readonly RichTextBox _editor;

    // RichTextBox иногда генерирует TextChanged во время внутренней инициализации/создания хэндла.
    // Чтобы не считать это "пользовательскими правками", включаем отслеживание только после
    // стабилизации контрола (через BeginInvoke после HandleCreated).
    private bool _readyForUserEdits;

    private bool _suppressModifiedTracking;
    private bool _modified;
    private string? _name; // full path

    public Document(RichTextBox editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));

        _editor.TextChanged += (_, __) =>
        {
            if (_suppressModifiedTracking) return;
            if (!_readyForUserEdits) return;

            BoolModified = true;
        };

        _editor.HandleCreated += (_, __) =>
        {
            // Дадим WinForms/RichTextBox доинициализироваться и возможным "служебным" TextChanged пройти,
            // после чего сбросим флаг модификации и начнём отслеживать пользовательские изменения.
            _editor.BeginInvoke(new Action(() =>
            {
                MarkClean();
                _readyForUserEdits = true;
            }));
        };

        // Если хэндл уже создан (редкий случай), всё равно сбросим и включим отслеживание.
        if (_editor.IsHandleCreated)
        {
            _editor.BeginInvoke(new Action(() =>
            {
                MarkClean();
                _readyForUserEdits = true;
            }));
        }
    }

    /// <summary>Путь к файлу (полный), если задан.</summary>
    public string? Name => _name;

    /// <summary>Полное имя файла (строка). Для совместимости с требованиями.</summary>
    public string StringName => _name ?? string.Empty;

    /// <summary>Есть ли имя/путь у документа.</summary>
    public bool BoolHasName => !string.IsNullOrWhiteSpace(_name);

    /// <summary>Короткое имя файла для вкладки ("Без имени" для несохранённого).</summary>
    public string StringShortName => BoolHasName ? Path.GetFileName(_name!) : "Без имени";

    /// <summary>Флаг несохранённых изменений.</summary>
    public bool BoolModified
    {
        get => _modified;
        set
        {
            if (_modified == value) return;
            _modified = value;
            ModifiedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ModifiedChanged;

    public RichTextBox Editor => _editor;

    public void Open(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is empty.", nameof(fileName));

        var full = Path.GetFullPath(fileName);
        var text = File.ReadAllText(full, Encoding.UTF8);

        _suppressModifiedTracking = true;
        try
        {
            _editor.Text = text;
            _editor.SelectionStart = 0;
            _editor.SelectionLength = 0;
        }
        finally
        {
            _suppressModifiedTracking = false;
        }

        _name = full;
        MarkClean();
    }

    public void Save()
    {
        if (!BoolHasName)
            throw new InvalidOperationException("Document has no name. Use SaveAs().");

        File.WriteAllText(_name!, _editor.Text, Encoding.UTF8);
        MarkClean();
    }

    public void SaveAs(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is empty.", nameof(fileName));

        _name = Path.GetFullPath(fileName);
        Save();
    }

    private void MarkClean()
    {
        // RichTextBox.Modified — стандартный флаг, который WinForms использует для отслеживания изменений.
        // Сбрасываем его вместе с нашим флагом, чтобы поведение было предсказуемым.
        _editor.Modified = false;
        BoolModified = false;
    }
}
