using System;
using ImageMagick;

namespace ConvertHeicMCP
{
    public static class HeicConverter
    {
        /// <summary>
        /// Converts a .heic file to a .jpg file.
        /// </summary>
        /// <param name="inputPath">Path to the input .heic file.</param>
        /// <param name="outputPath">Path to the output .jpg file.</param>
        public static void ConvertHeicToJpg(string inputPath, string outputPath)
        {
            using (var image = new MagickImage(inputPath))
            {
                image.Format = MagickFormat.Jpeg;
                image.Write(outputPath);
            }
        }

        /// <summary>
        /// Converts a .heic file to a .png file.
        /// </summary>
        /// <param name="inputPath">Path to the input .heic file.</param>
        /// <param name="outputPath">Path to the output .png file.</param>
        public static void ConvertHeicToPng(string inputPath, string outputPath)
        {
            using (var image = new MagickImage(inputPath))
            {
                image.Format = MagickFormat.Png;
                image.Write(outputPath);
            }
        }
    }
}
