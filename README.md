# Gap Removal - Removedor de Silêncios

Um software profissional para remover automaticamente silêncios de vídeos, desenvolvido em C# WPF com interface moderna e funcionalidades avançadas.

## 🎯 Funcionalidades

- **Detecção Inteligente de Silêncios**: Algoritmo avançado para identificar silêncios em vídeos
- **Interface Full Screen**: Tela de trabalho otimizada para edição profissional
- **Waveform Interativo**: Visualização detalhada do áudio com 800 barras de resolução
- **Controles de Sensibilidade**: Ajuste fino da detecção de silêncios
- **Player VLC Integrado**: Reprodução de vídeo com overlay de silêncios
- **Exportação Automática**: Geração de vídeo final sem silêncios
- **Suporte a Múltiplos Formatos**: MP4, AVI, MOV, MKV

## 🚀 Distribuição

### Para Usuários Finais

1. **Download**: Baixe o instalador `GapRemovalInstaller.exe`
2. **Instalação**: Execute o instalador como administrador
3. **Uso**: O software inclui automaticamente todos os binários necessários

### Para Desenvolvedores

#### Pré-requisitos
- .NET 8.0 SDK
- Inno Setup 6 (para gerar instalador)

#### Build Profissional

**Opção 1: Build Simples (Recomendado)**
```batch
# Execute o arquivo batch
build.bat
```

**Opção 2: MSBuild Direto**
```batch
# Build completo com download automático de binários
dotnet build GapRemovalApp --configuration Release --runtime win-x64 --self-contained

# Ou apenas gerar instalador (se binários já existem)
dotnet build GapRemovalApp --configuration Release --target BuildDistribution
```

**Opção 3: Visual Studio**
- Abra a solução no Visual Studio
- Build → Build Solution
- O instalador será gerado automaticamente

#### Scripts Disponíveis

- **`build.bat`**: Script profissional que:
  - Verifica pré-requisitos
  - Limpa builds anteriores
  - Compila o projeto
  - Gera instalador com todos os binários

## 📁 Estrutura do Projeto

```
GapRemoval/
├── GapRemovalApp/           # Projeto principal
│   ├── UI/                  # Interfaces de usuário
│   ├── Control/             # Controles customizados
│   ├── Video/               # Processamento de vídeo
│   ├── Utils/               # Utilitários
│   ├── ffmpeg/              # Binários FFmpeg (auto-download)
│   ├── vlc/                 # Binários VLC (auto-download)
│   └── Output/              # Instalador gerado
├── GapRemoval.Core/         # Biblioteca core
├── GapRemoval.FFmpeg/       # Integração FFmpeg
└── GapRemoval.UI/           # Componentes UI
```

## ⚙️ Configuração

### Controles de Sensibilidade

- **Sensibilidade de Áudio**: -60dB a -10dB (padrão: -40dB)
- **Delay Mínimo**: 100ms a 2000ms (padrão: 700ms)

### Interface

- **Full Screen**: Sempre maximizada para melhor produtividade
- **Waveform**: 800 barras de resolução para precisão máxima
- **Overlay**: Indicador visual de silêncios em tempo real

## 🎬 Como Usar

1. **Selecionar Vídeo**: Clique em "Selecionar vídeo"
2. **Ajustar Parâmetros**: Configure sensibilidade e delay conforme necessário
3. **Visualizar**: Use o waveform para identificar silêncios
4. **Exportar**: Clique em "Exportar vídeo" para gerar o resultado final

## 🔧 Desenvolvimento

### Tecnologias Utilizadas

- **C# WPF**: Interface de usuário
- **NAudio**: Processamento de áudio
- **VLC.DotNet**: Player de vídeo
- **FFmpeg**: Processamento de vídeo
- **Inno Setup**: Geração de instalador

### Estrutura de Código

- **MVVM Pattern**: Separação de responsabilidades
- **Async/Await**: Operações assíncronas
- **Dependency Injection**: Injeção de dependências
- **Logging**: Sistema de logs com Serilog

## 📦 Binários Incluídos

O instalador inclui automaticamente:

- **FFmpeg**: ffmpeg.exe, ffplay.exe, ffprobe.exe
- **VLC**: libvlc.dll, libvlccore.dll, plugins
- **.NET Runtime**: Self-contained deployment

## 🐛 Solução de Problemas

### Erro de Binários
Se o software não encontrar FFmpeg ou VLC:
1. Execute `.\build_distribution.ps1` para baixar automaticamente
2. Ou baixe manualmente e coloque nas pastas `ffmpeg/` e `vlc/`

### Performance
- Para vídeos muito longos, considere processar em partes
- Ajuste a sensibilidade conforme o tipo de conteúdo
- Use SSD para melhor performance de I/O

## 📄 Licença

Este projeto é distribuído sob a licença MIT. Veja o arquivo LICENSE para detalhes.

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📞 Suporte

Para suporte e dúvidas:
- Abra uma issue no GitHub
- Consulte a documentação técnica
- Verifique os logs em `logs/`

---

**Gap Removal** - Transformando a edição de vídeo com inteligência artificial! 🎬✨ 