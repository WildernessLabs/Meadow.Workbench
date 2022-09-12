using Meadow.Foundation.Web.Maple;
using Meadow.Foundation.Web.Maple.Routing;

namespace Meadow.Workbench.ViewModels;

public class UpdateFileHandler : RequestHandlerBase
{
    public override bool IsReusable => true;

    [HttpGet("/update/{filename}")]
    public IActionResult GetUpdateFile(string fileName)
    {
        var path = Path.Combine(@"f:\temp\meadow", fileName);
        var file = Path.Combine(path, "update.zip");

        if (!File.Exists(file))
        {
            return new NotFoundResult();
        }

        return new FileStreamResult(File.OpenRead(file), "application/binary");
    }
}
