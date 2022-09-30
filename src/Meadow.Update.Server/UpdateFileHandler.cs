using Meadow.Foundation.Web.Maple;
using Meadow.Foundation.Web.Maple.Routing;
using System;
using System.IO;

namespace Meadow.Update
{
    public class UpdateFileHandler : RequestHandlerBase
    {
        public override bool IsReusable => true;

        public string SourceFolder { get; }

        public UpdateFileHandler()
        {
            SourceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WildernessLabs", "Updates");
        }

        [HttpGet("/update/{filename}")]
        public IActionResult GetUpdateFile(string fileName)
        {
            var file = Path.Combine(SourceFolder, fileName, "update.zip");

            if (!File.Exists(file))
            {
                return new NotFoundResult();
            }

            return new FileStreamResult(File.OpenRead(file), "application/binary");
        }
    }
}