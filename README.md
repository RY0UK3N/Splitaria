<p align="center">
  <img src="assets/brand/splitaria-mark-128.png" width="96" alt="Símbolo do Splitaria">
</p>

<h1 align="center">Splitaria</h1>

<p align="center">
  Organize fotos e vídeos por ano e mês, com clareza antes de copiar.
</p>

<p align="center">
  <img alt="Windows 10 ou superior" src="https://img.shields.io/badge/Windows-10%2B-5B57D9?style=flat-square">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-5B57D9?style=flat-square">
  <img alt="Licença MIT" src="https://img.shields.io/badge/licença-MIT-20B8D7?style=flat-square">
  <img alt="Processamento local" src="https://img.shields.io/badge/privacidade-processamento%20local-20B8D7?style=flat-square">
</p>

---

O Splitaria é um organizador de mídia para Windows. Ele analisa uma ou mais
pastas, identifica a melhor data disponível e prepara uma estrutura organizada
nos destinos de Fotos e Vídeos — sempre mostrando o resultado antes de copiar.

> Suas fotos e seus vídeos permanecem no computador. O Splitaria não exige
> conta e não envia arquivos para a internet.

## O que ele faz

| | Recurso | Como ajuda |
|:---:|---|---|
| 📅 | **Data mais confiável** | Prioriza a data de captura e usa o nome ou a modificação como alternativas. |
| 🗂️ | **Organização configurável** | Cria estruturas como `2026/03 - Março`, `2026/03` ou somente `2026`. |
| ✨ | **Revisão visual** | Permite visualizar, ordenar e selecionar os arquivos antes da organização. |
| ◇ | **Duplicados sob controle** | Compara o conteúdo e avisa sobre repetições ou conflitos no destino. |
| ▶ | **Fotos e vídeos** | Oferece preview integrado e visualização ampliada com controles de reprodução. |
| ◐ | **Claro, escuro ou automático** | Pode acompanhar o tema atual do Windows. |

## Um fluxo simples

1. Adicione uma ou mais pastas de origem.
2. Escolha os destinos de fotos e vídeos — ou mantenha os padrões do Windows.
3. Clique em **Analisar arquivos**.
4. Revise datas, destinos, duplicados e seleção.
5. Clique em **Organizar selecionados**.

Os arquivos são copiados para o novo local. Os originais são preservados e o
Splitaria nunca substitui silenciosamente um arquivo existente.

## Instalação

Baixe o instalador mais recente na página de
[releases](https://github.com/RY0UK3N/Splitaria/releases/latest):

```text
Splitaria-Setup-<versão>-win-x64.exe
```

A instalação é feita no perfil do usuário e normalmente não exige privilégios
administrativos. O pacote inclui o runtime necessário e os componentes de vídeo.

> O instalador ainda não possui certificado digital de uma autoridade
> certificadora. O Windows pode exibir um aviso ao abri-lo pela primeira vez.

## Para desenvolver

### Requisitos

- Windows 10 ou superior;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0);
- Inno Setup 6 apenas para gerar o instalador.

### Compilar e testar

```powershell
dotnet build Splitaria.slnx
dotnet run --project tests/Splitaria.Tests
dotnet run --project src/Splitaria.App
```

### Gerar a versão portátil

```powershell
dotnet publish src/Splitaria.App -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

### Gerar o instalador

Com o [Inno Setup 6](https://jrsoftware.org/isinfo.php) instalado:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

O pacote será criado em `publish/installer/`.

## Privacidade e licenças

O código próprio do Splitaria é disponibilizado sob a [licença MIT](LICENSE).
O preview de vídeos utiliza LibVLCSharp e libVLC, distribuídos sob a GNU LGPL
2.1 ou posterior. Consulte os [avisos de terceiros](THIRD-PARTY-NOTICES.txt).

---

<p align="center">
  <sub>Feito para deixar coleções de fotos e vídeos mais fáceis de encontrar.</sub>
</p>
