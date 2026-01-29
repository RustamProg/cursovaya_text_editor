using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LaboratoryTextEditor;

/// <summary>
/// Список последних открытых файлов (до 5).
/// </summary>
public sealed class RecentList
{
    private const int MaxItems = 5;
    private readonly List<string> _items = new();
    private readonly string _storagePath;

    public RecentList(string? storagePath = null)
    {
        _storagePath = string.IsNullOrWhiteSpace(storagePath)
            ? GetDefaultStoragePath()
            : storagePath;

        var dir = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }

    public IReadOnlyList<string> Items => _items;

    private static string GetDefaultStoragePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LaboratoryTextEditor");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "recent.json");
    }

    public void Add(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var full = Path.GetFullPath(fileName);

        _items.RemoveAll(x => string.Equals(x, full, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, full);

        if (_items.Count > MaxItems)
            _items.RemoveRange(MaxItems, _items.Count - MaxItems);
    }

    public void Remove(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var full = Path.GetFullPath(fileName);
        _items.RemoveAll(x => string.Equals(x, full, StringComparison.OrdinalIgnoreCase));
    }

    public void SaveData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items);
            File.WriteAllText(_storagePath, json);
        }
        catch
        {
            // важно не падать из-за недоступной файловой системы.
        }
    }

    public void LoadData()
    {
        _items.Clear();

        try
        {
            if (!File.Exists(_storagePath))
                return;

            var json = File.ReadAllText(_storagePath);
            var loaded = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

            // Add() вставляет элемент в начало списка (MRU),
            // поэтому при загрузке в прямом порядке список разворачивается.
            // Обходим в обратном порядке, чтобы сохранить исходный порядок (MRU -> LRU).
            foreach (var item in loaded.Where(s => !string.IsNullOrWhiteSpace(s)).Reverse())
                Add(item);
        }
        catch
        {
            // Игнорируем битый файл списка недавних.
        }
    }
}
