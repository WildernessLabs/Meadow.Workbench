﻿using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Meadow.Workbench.Services;

public class SettingsService
{
    public string SettingsFile { get; set; } = "workbench.settings";

    public SettingsService()
    {
        if (!Path.Exists(SettingFilePath))
        {
            using (var f = File.CreateText(SettingFilePath))
            {
                f.Write("{}");
                f.Close();
            }
        }
    }

    private string SettingFilePath
    {
        get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WildernessLabs",
            SettingsFile);
    }

    public string LastFeature
    {
        get => GetString(nameof(LastFeature)) ?? string.Empty;
        set => SetString(nameof(LastFeature), value);
    }

    public string LocalFilesFolder
    {
        get => GetString(nameof(LocalFilesFolder)) ?? Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        set => SetString(nameof(LocalFilesFolder), value);
    }

    public string? GetString(string key)
    {
        using (var f = File.OpenText(SettingFilePath))
        {
            var doc = JsonDocument.Parse(f.ReadToEnd());

            if (doc.RootElement.TryGetProperty(key, out JsonElement e))
            {
                return e.GetString();
            }
        }
        return null;
    }

    public void SetString(string key, string value)
    {
        string text;
        using (var f = File.OpenText(SettingFilePath))
        {
            text = f.ReadToEnd();
        }

        var json = JsonObject.Parse(text);
        json[key] = value;

        File.WriteAllText(SettingFilePath, json.ToString());
    }

    public int? GetInt(string key)
    {
        using (var f = File.OpenText(SettingFilePath))
        {
            JsonDocument doc = JsonDocument.Parse(f.ReadToEnd());

            if (doc.RootElement.TryGetProperty(key, out JsonElement e))
            {

                if (e.TryGetInt32(out int iv))
                {
                    return iv;
                }
                else
                {
                    return null;
                }
            }
        }
        return null;
    }
}
