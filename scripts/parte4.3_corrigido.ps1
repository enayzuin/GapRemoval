# Parte 4.3 do setup: Implementar o método CutVideo em VideoProcessor.cs
Write-Host '`n✂️ Inserindo lógica de corte de vídeo com Xabe.FFmpeg...'

$processorFile = './GapRemovalApp/Video/VideoProcessor.cs'

@'
public async Task<List<string>> CutVideo(List<(double start, double end)> silentParts)
{
    List<string> parts = new List<string>();
    string tempFolder = Path.Combine(Path.GetTempPath(), "gap_parts_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempFolder);

    double lastEnd = 0;
    int index = 0;

    foreach (var part in silentParts)
    {
        double start = part.start;
        double end = part.end;

        if (start > lastEnd)
        {
            string outputPath = Path.Combine(tempFolder, $"part_{index}.mp4");

            var conversion = FFmpeg.Conversions.New()
                .AddParameter("-ss " + lastEnd, ParameterPosition.PreInput)
                .AddParameter("-i \"" + VideoPath + "\"")
                .AddParameter("-t " + (start - lastEnd))
                .SetOutput(outputPath);

            await conversion.Start();
            parts.Add(outputPath);
            index++;
        }
        lastEnd = end;
    }

    IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(VideoPath);
    double duration = mediaInfo.Duration.TotalSeconds;

    if (duration > lastEnd)
    {
        string outputPath = Path.Combine(tempFolder, $"part_{index}.mp4");

        var conversion = FFmpeg.Conversions.New()
            .AddParameter("-ss " + lastEnd, ParameterPosition.PreInput)
            .AddParameter("-i \"" + VideoPath + "\"")
            .AddParameter("-t " + (duration - lastEnd))
            .SetOutput(outputPath);

        await conversion.Start();
        parts.Add(outputPath);
    }

    return parts;
}
'@ | Set-Content -Path $processorFile -Encoding utf8

Write-Host '`n✅ Parte 4.3 concluída com sucesso!'