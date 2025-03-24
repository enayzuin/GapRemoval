# Cria a solução principal
dotnet new sln -n GapRemovalSuite

# Cria um projeto WPF chamado GapRemovalApp
dotnet new wpf -n GapRemovalApp

# Adiciona o projeto à solução
dotnet sln GapRemovalSuite.sln add GapRemovalApp/GapRemovalApp.csproj