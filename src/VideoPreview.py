import cv2
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.widgets import Slider, Button
from pydub import AudioSegment, silence
import asyncio
import threading
import time
import tkinter as tk
from tkinter import messagebox
from tkinter import filedialog
from ProcessadorVideo import ProcessadorVideo
import ttkbootstrap as ttk


class VideoPreview:
    """Cria um preview interativo sem timeline, apenas com ajuste de silêncio"""

    def __init__(self, video_path, audio_path, root, voltar_callback, preview_height=100, silence_threshold=-40):
        self.video_path = video_path
        self.audio_path = audio_path
        self.root = root
        self.voltar_callback = voltar_callback  # Callback ao retornar
        self.preview_height = preview_height
        self.silence_threshold = silence_threshold
        self.cap = cv2.VideoCapture(video_path)
        self.lock = (
            threading.Lock()
        )  # 🔒 Criamos um lock para evitar concorrência no OpenCV

        self.fps = int(self.cap.get(cv2.CAP_PROP_FPS))
        self.total_frames = int(self.cap.get(cv2.CAP_PROP_FRAME_COUNT))
        self.duration = self.total_frames / self.fps
        self.num_frames = self.total_frames

        self.timestamps = np.linspace(0, self.duration, self.total_frames)
        self.current_frame = 1  # 🔥 Agora inicia do frame 1

        # 🔹 Carregamos o áudio extraído
        self.audio_segment = AudioSegment.from_file(self.audio_path, format="wav")

        # 🔹 Detecta partes silenciosas com base no áudio
        self.silent_parts = self.detect_silence(self.silence_threshold)

    def detect_silence(self, threshold):
        """Detecta os trechos silenciosos apenas no áudio"""
        silent_parts = silence.detect_silence(
            self.audio_segment, min_silence_len=700, silence_thresh=threshold
        )
        return [(start / 1000, end / 1000) for start, end in silent_parts]

    def get_frame_at(self, time_sec):
        """Captura um frame do vídeo baseado no tempo em segundos"""
        with self.lock:  # 🔒 Garantimos que apenas uma thread acessa o OpenCV por vez
            self.cap.set(cv2.CAP_PROP_POS_MSEC, time_sec * 1000)
            ret, frame = self.cap.read()
            if not ret:
                return np.zeros((360, 640, 3), dtype=np.uint8)

            frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            frame = cv2.resize(frame, (640, 360))
            return frame

    def update_frame(self, slider_value):
        """Atualiza o frame do vídeo de maneira assíncrona, permitindo que o slider flua livremente"""
        new_frame = int(round(slider_value))

        if new_frame == self.current_frame:
            return

        self.current_frame = new_frame
        time_sec = self.timestamps[self.current_frame]

        threading.Thread(
            target=asyncio.run, args=(self.async_render_frame(time_sec),), daemon=True
        ).start()

    async def async_render_frame(self, time_sec):
        """Renderiza o frame de forma assíncrona para evitar travamentos"""
        await asyncio.sleep(0.03)  # 🔥 Pequeno delay para evitar sobrecarga
        frame = self.get_frame_at(time_sec)
        self.video_ax.imshow(frame)
        self.fig.canvas.flush_events()
        self.fig.canvas.draw_idle()

    def recalculate_silence(self, event):
        """Recalcula os trechos de silêncio e atualiza a barra do slider"""
        self.silence_threshold = int(self.silence_slider.val)
        self.silent_parts = self.detect_silence(self.silence_threshold)
        self.update_silence_visuals()

    def update_silence_visuals(self):
        """🔴 Adiciona marcações vermelhas sobre o slider indicando trechos silenciosos"""
        for line in getattr(self, "silence_markers", []):
            line.remove()
        self.silence_markers = []
        for start, end in self.silent_parts:
            start_idx = int((start / self.duration) * self.num_frames)
            end_idx = int((end / self.duration) * self.num_frames)
            line = self.slider_ax.axvspan(
                start_idx, end_idx, color="#FF3B30", alpha=0.5
            )
            self.silence_markers.append(line)
        self.fig.canvas.draw_idle()

    def generate_preview(self):
        
        plt.rcParams['toolbar'] = 'None'  # Remove a barra de ferramentas inferior (footer)

        """Gera o player interativo com controles estilizados e botão de exportação"""

        # 🔹 Criando a figura com fundo escuro
        self.fig, self.video_ax = plt.subplots(figsize=(10, 6), facecolor="#1E1E1E")
        #self.fig.canvas.manager.full_screen_toggle()
        self.video_ax.axis("off")
        self.video_ax.imshow(
            self.get_frame_at(self.timestamps[1])
        )  # 🔥 Inicia do frame 1

        # 🔹 Slider de Tempo (🔥 estilizado)
        self.slider_ax = self.fig.add_axes([0.15, 0.07, 0.7, 0.05], facecolor="#2C2F33")
        self.slider = Slider(
            self.slider_ax, "Tempo", 0, self.num_frames - 1, valinit=1, valstep=1
        )
        self.slider.label.set_color("#D3D3D3")
        self.slider.valtext.set_color("#D3D3D3")
        self.slider.poly.set_color("#007AFF")  # Azul iPhone
        self.slider.track.set_color("#4B4F55")
        self.slider.on_changed(self.update_frame)

        # 🔹 Botão Exportar vídeo (🔥 Posicionado abaixo do slider de tempo)
        export_button_ax = self.fig.add_axes(
            [0.4, 0.01, 0.2, 0.04], facecolor="#444444"
        )
        self.export_button = Button(export_button_ax, "Exportar Vídeo")
        self.export_button.label.set_color("#333333")
        self.export_button.on_clicked(self.export_video)
        
        # 🔹 Slider de Sensibilidade do Silêncio (🔥 iPhone-style, movido um pouco para cima)
        silence_slider_ax = self.fig.add_axes(
            [0.35, 0.93, 0.45, 0.05], facecolor="#2C2F33"
        )
        self.silence_slider = Slider(
            silence_slider_ax,
            "Sensibilidade do Silêncio",
            -60,
            -10,
            valinit=self.silence_threshold,
        )
        self.silence_slider.label.set_color("#D3D3D3")
        self.silence_slider.valtext.set_color("#D3D3D3")
        self.silence_slider.poly.set_color("#34C759")  # Verde iPhone
        self.silence_slider.track.set_color("#4B4F55")

        # 🔹 Botão Aplicar Sensibilidade (🔥 Ajustado abaixo do slider de sensibilidade)
        button_ax = self.fig.add_axes([0.4, 0.88, 0.2, 0.04], facecolor="#444444")
        self.recalculate_button = Button(button_ax, "Aplicar Sensibilidade")
        self.recalculate_button.label.set_color("#333333")
        self.recalculate_button.on_clicked(self.recalculate_silence)    
        
        # 🔹 Botão de Voltar (canto superior esquerdo)
        back_button_ax = self.fig.add_axes([0.02, 0.92, 0.05, 0.05], facecolor="#444444")
        self.back_button = Button(back_button_ax, "↩")
        self.back_button.label.set_color("#333333")
        self.back_button.on_clicked(self.voltar_tela_inicial)


        self.update_silence_visuals()
        plt.show()



    def export_video(self, event):
        output_path = filedialog.asksaveasfilename(
            defaultextension=".mp4",
            filetypes=[("Vídeos", "*.mp4;*.mkv;*.avi")]
        )
        if output_path:
            loading_window = tk.Toplevel()
            loading_window.title("Processando...")
            loading_window.geometry("300x100")
            loading_label = tk.Label(loading_window, text="🔄 Processando vídeo, aguarde...")
            loading_window.resizable(False, False)
            loading_window.grab_set()
            loading_window.transient(self.fig.canvas.manager.window)

            def process():
                try:
                    processador = ProcessadorVideo(self.video_path, self.silence_threshold, output_path)
                    processador.remove_silence()
                    loading_window.destroy()
                    tk.messagebox.showinfo("Sucesso", "Vídeo exportado com sucesso!")
                except Exception as e:
                    loading_window.destroy()
                    tk.messagebox.showerror("Erro", f"Erro ao exportar vídeo: {e}")

            threading.Thread(target=process, daemon=True).start()
            
        def __del__(self):
            """Libera os recursos corretamente ao fechar a aplicação"""
            if self.cap.isOpened():
                self.cap.release()



    def voltar_tela_inicial(self, event):
        import matplotlib.pyplot as plt
        plt.close(self.fig)
        self.voltar_callback()  # 🔥 Reativa o frame original
