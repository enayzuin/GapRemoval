# Parte 4.2 do setup: Implementar o método DetectSilence em VideoProcessor.cs
Write-Host '`n🔊 Inserindo lógica de detecção de silêncio com NAudio...'

$processorFile = './GapRemovalApp/Video/VideoProcessor.cs'

(Get-Content $processorFile) -replace '(?s)public async Task<List<\(double start, double end\)>> DetectSilence\(\).*?\{.*?\}', @'
public async Task<List<(double start, double end)>> DetectSilence()
{
    var silentParts = new List<(double start, double end)>();

    string tempAudioPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");

    // Extrair o áudio usando FFmpeg
    var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(VideoPath, tempAudioPath);
    await conversion.Start();

    using (var reader = new AudioFileReader(tempAudioPath))
    {
        float[] buffer = new float[reader.WaveFormat.SampleRate]; // 1 segundo de áudio
        int bytesRead;
        double positionSeconds = 0;
        bool inSilence = false;
        double silenceStart = 0;

        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            var avgVolume = buffer.Take(bytesRead).Select(Math.Abs).Average() * 100;
            bool isSilent = avgVolume < Math.Abs(SilenceThreshold);

            if (isSilent && !inSilence)
            {
                inSilence = true;
                silenceStart = positionSeconds;
            }
            else if (!isSilent && inSilence)
            {
                double silenceDuration = positionSeconds - silenceStart;
                if (silenceDuration * 1000 >= MinSilenceLengthMs)
                    silentParts.Add((silenceStart, positionSeconds));

                inSilence = false;
            }

            positionSeconds += 1; // 1 segundo por buffer
        }

        // Finaliza se terminar em silêncio
        if (inSilence)
        {
            double silenceDuration = positionSeconds - silenceStart;
            if (silenceDuration * 1000 >= MinSilenceLengthMs)
                silentParts.Add((silenceStart, positionSeconds));
        }
    }

    File.Delete(tempAudioPath);
    return silentParts;
}
'@ | Set-Content $processorFile -Encoding utf8

Write-Host '`n✅ Parte 4.2 concluída com sucesso!'