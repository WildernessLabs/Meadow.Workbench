using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Meadow.Workbench.Converters;

public static class StringConverters
{
    public static readonly IValueConverter FileSizeConverter = new FuncValueConverter<long, string>(size =>
    {
        if (size <= 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = size;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    });

    public static readonly IValueConverter BoolToFolderIcon = new FuncValueConverter<bool, string>(isDirectory =>
        isDirectory ? "📁" : "📄");
}