import ttkbootstrap as ttk

class DesignSystem:
    """Classe para criar e gerenciar os componentes da interface de forma padronizada"""

    def __init__(self, root):
        self.root = root
        self.style = ttk.Style("darkly")  # Define o tema escuro

        # Definições globais de estilo
        self.window_size = "600x400"
        self.title_font = ("Arial", 12, "bold")
        self.text_font = ("Arial", 10)
        self.button_style = "primary-outline"
        self.output_button_style = "secondary-outline"
        self.process_button_style = "success"
        self.text_color = "white"
        self.label_color = "gray"
        self.padding = 20

    def create_label(self, parent, text):
        """Cria um rótulo padrão"""
        return ttk.Label(parent, text=text, font=self.title_font, foreground=self.text_color)

    def create_button(self, parent, text, command, style=None):
        """Cria um botão padrão"""
        style = style or self.button_style
        return ttk.Button(parent, text=text, bootstyle=style, command=command)

    def create_slider(self, parent, from_, to, value):
        """Cria um slider padrão"""
        return ttk.Scale(parent, from_=from_, to=to, value=value, length=400, orient="horizontal")

    def create_frame(self, parent):
        """Cria um frame com padding padrão"""
        return ttk.Frame(parent, padding=self.padding)