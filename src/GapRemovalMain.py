import tkinter as tk
from tkinter import filedialog, messagebox
import ttkbootstrap as ttk
import os
import tempfile
from VideoPreview import VideoPreview

class GapRemovalMain:
    def __init__(self, root):
        self.root = root
        self.root.overrideredirect(True)
        self.video_path = None
        self.audio_path = None

        # Inicializa frames
        self.init_main_frame()
        self.main_frame.pack(fill="both", expand=True)

    def init_main_frame(self):
        """Tela inicial"""
        # Barra de título personalizada
        title_bar = ttk.Frame(root, bootstyle='dark')
        title_bar.pack(fill='x')

        # Label com o título da janela
        ttk.Label(title_bar, text="Zuin - GapRemoval", bootstyle='inverse-dark').pack(side='left', padx=10)

        # Botão para fechar
        ttk.Button(title_bar, text="✖", command=root.destroy, bootstyle='danger').pack(side='right')
        title_bar.bind('<Button-1>', self.start_move)
        title_bar.bind('<B1-Motion>', self.do_move)

        # Conteúdo da janela
        self.main_frame = ttk.Frame(self.root)

        ttk.Label(self.main_frame, text="📂 Selecione um vídeo:").pack(anchor="w", pady=5)

        ttk.Button(
            self.main_frame, text="Selecionar Vídeo", command=self.select_video
        ).pack(fill="x", pady=5)

        self.lbl_video = ttk.Label(self.main_frame, text="Nenhum vídeo selecionado")
        self.lbl_video.pack(anchor="w", pady=2)

        ttk.Button(
            self.main_frame,
            text="📊 Abrir Preview",
            command=self.open_preview,
            style="success",
        ).pack(fill="x", pady=20)

    def select_video(self):
        self.video_path = filedialog.askopenfilename(filetypes=[("Vídeos", "*.mp4;*.mkv;*.avi")])
        if self.video_path:
            self.lbl_video.config(text=f"Vídeo: {os.path.basename(self.video_path)}")
            self.audio_path = self.extract_audio(self.video_path)

    def extract_audio(self, video_path):
        temp_audio = tempfile.NamedTemporaryFile(delete=False, suffix=".wav").name
        os.system(f'ffmpeg -i "{video_path}" -q:a 0 -map a "{temp_audio}" -y -loglevel quiet')
        return temp_audio

    def open_preview(self):
        if not self.video_path or not self.audio_path:
            messagebox.showerror("Erro", "Selecione um vídeo primeiro!")
            return

        # 🔥 Não destrua a janela, apenas esconda o frame
        self.main_frame.pack_forget()

        # Inicializa a preview passando callback para retornar
        self.preview = VideoPreview(
            self.video_path, self.audio_path, self.root, voltar_callback=self.voltar_da_preview
        )
        self.preview.generate_preview()

    def voltar_da_preview(self):
        """Callback ao voltar da preview"""
        self.main_frame.pack(fill="both", expand=True)
        
    def start_move(self, event):
        self.x_offset = event.x
        self.y_offset = event.y

    def do_move(self, event):
        x = event.x_root - self.x_offset
        y = event.y_root - self.y_offset
        self.root.geometry(f'+{x}+{y}')



if __name__ == "__main__":
    root = ttk.Window(themename="darkly")
    root.title("Remover Silêncio do Vídeo - Estilo CapCut")
    root.geometry("800x400")
    root.resizable(False, False)
    app = GapRemovalMain(root)
    root.mainloop()
