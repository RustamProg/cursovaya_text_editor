using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace LaboratoryTextEditor.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class DocumentTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LaboratoryTextEditorDocTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    [Test]
    public void NewDocument_ShouldStartUnmodified_AndWithoutName()
    {
        using var editor = new RichTextBox();
        editor.CreateControl();

        var doc = new Document(editor);
        Application.DoEvents();

        Assert.That(doc.BoolHasName, Is.False);
        Assert.That(doc.StringShortName, Is.EqualTo("Без имени"));
        Assert.That(doc.BoolModified, Is.False);
    }

    [Test]
    public void Editing_ShouldSetModifiedTrue()
    {
        using var editor = new RichTextBox();
        editor.CreateControl();

        var doc = new Document(editor);
        Application.DoEvents();

        editor.Text = "Hello";
        Application.DoEvents();

        Assert.That(doc.BoolModified, Is.True);
    }

    [Test]
    public void SaveAs_ShouldWriteFile_AndResetModified()
    {
        using var editor = new RichTextBox();
        editor.CreateControl();

        var doc = new Document(editor);
        Application.DoEvents();

        editor.Text = "ABC";
        Application.DoEvents();

        var fileName = Path.Combine(_tempDir, "test.txt");
        doc.SaveAs(fileName);

        Assert.That(File.Exists(fileName), Is.True);
        Assert.That(File.ReadAllText(fileName), Is.EqualTo("ABC"));
        Assert.That(doc.BoolModified, Is.False);
        Assert.That(doc.BoolHasName, Is.True);
        Assert.That(doc.StringShortName, Is.EqualTo("test.txt"));
    }

    [Test]
    public void Open_ShouldLoadText_AndBeClean()
    {
        var fileName = Path.Combine(_tempDir, "open.txt");
        File.WriteAllText(fileName, "Loaded");

        using var editor = new RichTextBox();
        editor.CreateControl();

        var doc = new Document(editor);
        Application.DoEvents();

        doc.Open(fileName);
        Application.DoEvents();

        Assert.That(editor.Text, Is.EqualTo("Loaded"));
        Assert.That(doc.BoolModified, Is.False);
        Assert.That(doc.Name, Is.EqualTo(Path.GetFullPath(fileName)));
        Assert.That(doc.StringShortName, Is.EqualTo("open.txt"));
    }
}
