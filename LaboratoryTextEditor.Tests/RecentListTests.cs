using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LaboratoryTextEditor.Tests;

[TestFixture]
public sealed class RecentListTests
{
    private string _tempDir = null!;
    private string _storagePath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LaboratoryTextEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _storagePath = Path.Combine(_tempDir, "recent.json");
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    [Test]
    public void Add_ShouldKeepMostRecentFirst_AndLimitToFive()
    {
        var recent = new RecentList(_storagePath);

        for (var i = 0; i < 6; i++)
        {
            var path = Path.Combine(_tempDir, $"file{i}.txt");
            recent.Add(path);
        }

        Assert.That(recent.Items.Count, Is.EqualTo(5));

        var expectedMostRecent = Path.GetFullPath(Path.Combine(_tempDir, "file5.txt"));
        Assert.That(recent.Items[0], Is.EqualTo(expectedMostRecent));

        var droppedOldest = Path.GetFullPath(Path.Combine(_tempDir, "file0.txt"));
        Assert.That(recent.Items.Any(x => string.Equals(x, droppedOldest, StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    [Test]
    public void Add_Duplicate_ShouldMoveToTop_WithoutDuplicates()
    {
        var recent = new RecentList(_storagePath);

        var a = Path.Combine(_tempDir, "a.txt");
        var b = Path.Combine(_tempDir, "b.txt");

        recent.Add(a);
        recent.Add(b);
        recent.Add(a);

        Assert.That(recent.Items.Count, Is.EqualTo(2));
        Assert.That(recent.Items[0], Is.EqualTo(Path.GetFullPath(a)));
        Assert.That(recent.Items[1], Is.EqualTo(Path.GetFullPath(b)));
    }

    [Test]
    public void SaveLoad_ShouldRoundTrip_ItemsInSameOrder()
    {
        var recent = new RecentList(_storagePath);
        var a = Path.Combine(_tempDir, "a.txt");
        var b = Path.Combine(_tempDir, "b.txt");

        recent.Add(a);
        recent.Add(b);
        recent.SaveData();

        var loaded = new RecentList(_storagePath);
        loaded.LoadData();

        CollectionAssert.AreEqual(recent.Items, loaded.Items);
    }
}
