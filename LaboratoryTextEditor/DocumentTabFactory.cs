using System;
using System.Windows.Forms;

namespace LaboratoryTextEditor;

/// <summary>
/// Factory Method: создаёт "вкладку документа" как связанный набор TabPage + RichTextBox + Document.
/// </summary>
public sealed class DocumentTabFactory
{
    public (TabPage tab, Document document) CreateNew()
    {
        var rtb = CreateEditorBox();
        var doc = new Document(rtb);

        var tab = new TabPage(doc.StringShortName)
        {
            Tag = doc
        };
        tab.Controls.Add(rtb);

        return (tab, doc);
    }

    public (TabPage tab, Document document) CreateFromFile(string fileName)
    {
        var rtb = CreateEditorBox();
        var doc = new Document(rtb);
        doc.Open(fileName);

        var tab = new TabPage(doc.StringShortName)
        {
            Tag = doc
        };
        tab.Controls.Add(rtb);

        return (tab, doc);
    }

    private static RichTextBox CreateEditorBox()
    {
        return new RichTextBox
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            AcceptsTab = true,
            DetectUrls = true,
            WordWrap = true
        };
    }
}
