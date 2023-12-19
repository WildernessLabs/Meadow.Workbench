﻿using System.Configuration;
using System.Text.Json;

namespace Meadow.Cli;

public class SettingsManager : ISettingsManager
{
    private class Settings
    {
        public Dictionary<string, string> Public { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, string> Private { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }

    public static class PublicSettings
    {
        public const string Route = "route";
    }

    private readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WildernessLabs", "clisettings.json");
    private const string PrivatePrefix = "private.";

    public Dictionary<string, string> GetPublicSettings()
    {
        var settings = GetSettings();
        return settings.Public;
    }

    public string? GetSetting(string setting)
    {
        var settings = GetSettings();
        if (settings.Public.TryGetValue(setting.ToString(), out var ret))
        {
            return ret;
        }
        else if (settings.Private.TryGetValue(setting.ToString(), out var pret))
        {
            return pret;
        }
        return null;
    }

    public void DeleteSetting(string setting)
    {
        var settings = GetSettings();
        Dictionary<string, string> target;

        if (setting.StartsWith(PrivatePrefix))
        {
            setting = setting.Substring(PrivatePrefix.Length);
            target = settings.Private;
        }
        else
        {
            target = settings.Public;
        }

        if (target.ContainsKey(setting.ToString()))
        {
            target.Remove(setting);

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(Path, json);
        }
    }

    public void SaveSetting(string setting, string value)
    {
        var settings = GetSettings();
        Dictionary<string, string> target;

        if (setting.StartsWith(PrivatePrefix))
        {
            setting = setting.Substring(PrivatePrefix.Length);
            target = settings.Private;
        }
        else
        {
            target = settings.Public;
        }

        if (target.ContainsKey(setting))
        {
            target[setting] = value;
        }
        else
        {
            target.Add(setting, value);
        }

        var json = JsonSerializer.Serialize(settings);
        File.WriteAllText(Path, json);
    }

    public string? GetAppSetting(string name)
    {
        if (ConfigurationManager.AppSettings.AllKeys.Contains(name))
        {
            return ConfigurationManager.AppSettings[name];
        }
        else
        {
            throw new ArgumentException($"{name} setting not found.");
        }
    }

    private Settings GetSettings()
    {
        var fi = new FileInfo(Path);

        if (!Directory.Exists(fi.Directory.FullName))
        {
            Directory.CreateDirectory(fi.Directory.FullName);
        }

        if (File.Exists(Path))
        {
            var json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }

        return new Settings();
    }
}