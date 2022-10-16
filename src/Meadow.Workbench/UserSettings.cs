namespace Meadow.Workbench;

public class UserSettings
{
    public string LastAppFolder { get; set; }
    public List<string> KnownApplications { get; set; } = new List<string>();
    public Size ShellSize { get; set; } = new Size(1366, 768);
    public Point ShellPosition { get; set; } = new Point(0, 0);

    public static UserSettings Default
    {
        get
        {
            return new UserSettings
            {
                LastAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
            };
        }
    }
}
