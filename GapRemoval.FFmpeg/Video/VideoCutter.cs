using System;
namespace GapRemoval.FFmpeg.Video {
    public class VideoCutter {
        public void Cut(string inputPath, string outputPath) {
            Console.WriteLine($"Cutting video: {inputPath} -> {outputPath}");
        }
    }
}
