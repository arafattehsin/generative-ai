using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ConvertHeicMCP
{
    [McpServerToolType]
    public static class HeicTools
    {
        [McpServerTool, Description("Convert a HEIC file to JPG. Returns the output file path.")]
        public static string ConvertHeicToJpg(
            [Description("Input HEIC file path")] string inputPath,
            [Description("Output JPG file path")] string outputPath)
        {
            HeicConverter.ConvertHeicToJpg(inputPath, outputPath);
            return outputPath;
        }

        [McpServerTool, Description("Convert a HEIC file to PNG. Returns the output file path.")]
        public static string ConvertHeicToPng(
            [Description("Input HEIC file path")] string inputPath,
            [Description("Output PNG file path")] string outputPath)
        {
            HeicConverter.ConvertHeicToPng(inputPath, outputPath);
            return outputPath;
        }
    }
}
