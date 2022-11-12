namespace Meadow.Workbench;

public interface IFolderPicker
{
	Task<string> PickFolder(string? startLocation = null);
}
