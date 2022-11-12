using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Meadow.Workbench;

public class UserSettingsService
{
	private FileInfo _settingsFile;
	private ILogger? _logger;

	public UserSettings Settings { get; private set; }

	public UserSettingsService(ILogger? logger = null)
	{
		_settingsFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WildernessLabs", "workbench.settings"));
		_logger = logger;

		if (!_settingsFile.Directory.Exists)
		{
			_settingsFile.Directory.Create();
		}

		Settings = LoadSettings();
	}

	public void SaveCurrentSettings()
	{
		using (var stream = _settingsFile.Create())
		{
			try
			{
				JsonSerializer.Serialize(stream, Settings);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex.Message);
			}
		}
	}

	public UserSettings LoadSettings()
	{
		_settingsFile.Refresh();

		if (!_settingsFile.Exists)
		{
			return UserSettings.Default;
		}

		using (var stream = _settingsFile.OpenRead())
		{
			try
			{
				return JsonSerializer.Deserialize<UserSettings>(stream) ?? UserSettings.Default;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex.Message);
				return UserSettings.Default;
			}
		}
	}
}

