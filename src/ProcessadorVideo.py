import ffmpeg
import os
import subprocess
from pydub import AudioSegment, silence


class ProcessadorVideo:
    def __init__(self, video_path, silence_threshold, output_path):
        self.video_path = video_path
        self.silence_threshold = int(silence_threshold)
        self.output_path = output_path
        self.min_silence_len = 700
        self.temp_folder = "temp_parts"

        # Detecta o melhor codec para GPU disponível
        self.codec = self.get_gpu_codec()

    def get_gpu_codec(self):
        """Detecta a GPU disponível e retorna o codec de vídeo e áudio correspondente"""
        try:
            output = subprocess.check_output(
                "ffmpeg -hide_banner -encoders", shell=True, text=True
            )

            if "h264_nvenc" in output:
                return {"video": "h264_nvenc", "audio": "aac", "gpu": "NVIDIA"}
            elif "h264_amf" in output:
                return {"video": "h264_amf", "audio": "aac", "gpu": "AMD"}
            elif "h264_qsv" in output:
                return {"video": "h264_qsv", "audio": "aac", "gpu": "Intel"}
            else:
                return {"video": "libx264", "audio": "aac", "gpu": "CPU (fallback)"}
        except Exception:
            return {"video": "libx264", "audio": "aac", "gpu": "CPU (fallback)"}

    def detect_silence(self):
        """Extrai o áudio e detecta os trechos silenciosos"""
        audio_path = "temp_audio.wav"
        ffmpeg.input(self.video_path).output(audio_path, format="wav").run(
            overwrite_output=True
        )

        audio = AudioSegment.from_file(audio_path, format="wav")
        os.remove(audio_path)

        silent_parts = silence.detect_silence(
            audio,
            min_silence_len=self.min_silence_len,
            silence_thresh=self.silence_threshold,
        )
        return [(start / 1000, end / 1000) for start, end in silent_parts]

    def cut_video(self, silent_parts):
        """Corta o vídeo removendo as partes silenciosas"""
        os.makedirs(self.temp_folder, exist_ok=True)
        input_video = ffmpeg.input(self.video_path)
        last_end = 0
        cut_files = []

        for idx, (start, end) in enumerate(silent_parts):
            if start > last_end:
                output_part = os.path.join(self.temp_folder, f"part_{idx}.mp4")
                trimmed_video = (
                    input_video.trim(start=last_end, end=start)
                    .setpts("PTS-STARTPTS")
                    .filter(
                        "scale", "trunc(iw/2)*2", "trunc(ih/2)*2"
                    )  # 🔹 Ajusta resolução
                    .filter("format", "yuv420p")  # 🔹 Força conversão para 8 bits
                )
                trimmed_audio = input_video.filter_(
                    "atrim", start=last_end, end=start
                ).filter_("asetpts", "PTS-STARTPTS")

                codec_video = self.codec["video"]

                # Parâmetros NVENC
                codec_args = {
                    "rc": "vbr_hq",
                    "cq": 19,
                    "preset": "slow",
                    "bf": 2,
                    "g": 60,
                    "maxrate": "50M",
                    "bufsize": "25M",
                }

                try:
                    # 🔥 Primeiro tenta NVENC
                    ffmpeg.output(
                        trimmed_video,
                        trimmed_audio,
                        output_part,
                        vcodec=codec_video,
                        acodec=self.codec["audio"],
                        **codec_args,
                        threads=2,
                    ).run(overwrite_output=True)
                    cut_files.append(output_part)
                    print(f"✅ Trecho {idx} salvo com NVENC: {output_part}")

                except Exception as e:
                    print(f"⚠️ NVENC falhou no trecho {idx}. Erro: {e}")
                    print("🔄 Tentando novamente com libx264...")

                    # 🔥 Fallback para libx264 caso NVENC falhe
                    try:
                        ffmpeg.output(
                            trimmed_video,
                            trimmed_audio,
                            output_part,
                            vcodec="libx264",
                            acodec=self.codec["audio"],
                            crf=16,
                            preset="slow",
                            bf=2,
                            g=60,
                            threads=2,
                        ).run(overwrite_output=True)
                        cut_files.append(output_part)
                        print(f"✅ Trecho {idx} salvo com libx264: {output_part}")

                    except Exception as e:
                        print(
                            f"❌ Erro ao processar trecho {idx}, ignorando. Erro: {e}"
                        )

            last_end = end

        return cut_files

    def concatenate_videos(self, part_files):
        """Concatena os segmentos do vídeo sem os trechos silenciosos"""
        file_list_path = "file_list.txt"

        valid_files = [
            file
            for file in part_files
            if os.path.exists(file) and os.path.getsize(file) > 0
        ]

        if not valid_files:
            print("❌ Nenhum arquivo válido para concatenação. Abortando.")
            return

        with open(file_list_path, "w", encoding="utf-8") as f:
            for part in valid_files:
                f.write(f"file '{os.path.abspath(part)}'\n")

        codec_video = self.codec["video"]

        codec_args = {
            "rc": "vbr_hq",
            "cq": 19,
            "preset": "slow",
            "bf": 2,
            "g": 60,
            "maxrate": "50M",
            "bufsize": "25M",
        }

        try:
            print("🔹 Tentando concatenar com NVENC...")
            ffmpeg.input(file_list_path, format="concat", safe=0).output(
                self.output_path,
                vcodec=codec_video,
                acodec="aac",
                **codec_args,
                threads=2,
            ).run(overwrite_output=True)

            print(f"✅ Vídeo final gerado com sucesso com NVENC: {self.output_path}")

        except Exception as e:
            print(f"⚠️ NVENC falhou na concatenação. Erro: {e}")
            print("🔄 Tentando novamente com libx264...")

            try:
                ffmpeg.input(file_list_path, format="concat", safe=0).output(
                    self.output_path,
                    vcodec="libx264",
                    acodec="aac",
                    crf=16,
                    preset="slow",
                    bf=2,
                    g=60,
                    threads=2,
                ).run(overwrite_output=True)

                print(
                    f"✅ Vídeo final gerado com sucesso com libx264: {self.output_path}"
                )

            except Exception as e:
                print(f"❌ Erro na concatenação. Detalhes: {e}")

        os.remove(file_list_path)

    def remove_silence(self):
        """Pipeline completo"""
        silent_parts = self.detect_silence()

        if not silent_parts:
            print("Nenhuma parte silenciosa detectada. O vídeo permanecerá inalterado.")
            os.rename(self.video_path, self.output_path)
            return

        cut_files = self.cut_video(silent_parts)
        self.concatenate_videos(cut_files)

        # Limpar arquivos temporários
        for file in cut_files:
            try:
                os.remove(file)
            except Exception as e:
                print(f"❌ Erro ao apagar arquivo. Detalhes: {e}")
        try:
            os.rmdir(self.temp_folder)
        except Exception as e:
            print(f"❌ Erro ao apagar a pasta. Detalhes: {e}")
