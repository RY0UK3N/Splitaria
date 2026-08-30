<p align="center">
  <img src="assets/brand/splitaria-mark-128.png" width="96" alt="Símbolo do Splitaria">
</p>

<h1 align="center">Splitaria</h1>

<p align="center">
  <strong>Fotos e vídeos no lugar certo. Antes de copiar, você confere tudo.</strong><br>
  Um organizador de mídia para Windows, feito para colocar cada lembrança em seu ano e mês.
</p>

<p align="center">
  <a href="https://github.com/RY0UK3N/Splitaria/releases/latest"><strong>Baixar a versão mais recente</strong></a>
</p>

<p align="center">
  <img alt="Windows 10 ou superior" src="https://img.shields.io/badge/Windows-10%2B-5B57D9?style=flat-square">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-5B57D9?style=flat-square">
  <img alt="Licença MIT" src="https://img.shields.io/badge/licença-MIT-20B8D7?style=flat-square">
  <img alt="Processamento local" src="https://img.shields.io/badge/privacidade-processamento%20local-20B8D7?style=flat-square">
</p>

---

## Da pasta cheia à coleção organizada

O Splitaria analisa uma ou mais pastas, procura a melhor data disponível para
cada arquivo e prepara destinos separados para Fotos e Vídeos. Antes de copiar,
ele mostra o que encontrou, para onde cada item irá e quais arquivos precisam de
atenção.

<table>
  <tr>
    <td width="33%"><h3>Data mais confiável</h3></td>
    <td width="33%"><h3>Revisão antes de copiar</h3></td>
    <td width="33%"><h3>Duplicados sob controle</h3></td>
  </tr>
  <tr>
    <td valign="top">Prioriza a data de captura e usa o nome do arquivo ou a modificação como alternativas.</td>
    <td valign="top">Permite visualizar, ordenar e selecionar fotos e vídeos antes da organização.</td>
    <td valign="top">Compara o conteúdo e avisa sobre repetições ou conflitos no destino.</td>
  </tr>
  <tr>
    <td valign="top"><strong>Resultado:</strong> escolha entre sete padrões, como <code>2026/03 - Março</code>, <code>2026/2026-03-08</code> ou <code>2026-03</code>.</td>
    <td valign="top"><strong>Resultado:</strong> nenhuma cópia começa sem que o lote esteja claro para você.</td>
    <td valign="top"><strong>Resultado:</strong> arquivos existentes nunca são substituídos silenciosamente.</td>
  </tr>
</table>

## Adicione, revise, organize

1. Adicione uma ou mais pastas de origem.
2. Escolha onde salvar Fotos e Vídeos — ou mantenha as pastas padrão do Windows.
3. Clique em **Analisar arquivos**.
4. Confira datas, destinos, duplicados e seleção.
5. Clique em **Organizar selecionados** e acompanhe o resumo final.

> **Suas lembranças continuam suas.** Todo o processamento acontece no
> computador. O Splitaria não exige conta, não envia fotos ou vídeos para a
> internet e sempre preserva os arquivos originais.

## Feito para revisar com calma

- Prévia integrada de fotos e vídeos, com visualização ampliada.
- Controles de reprodução, tempo e áudio na visualização maior de vídeos.
- Ordenação por arquivo, tipo, data, origem da data e situação.
- Seleção individual dos itens que realmente devem ser organizados.
- Temas claro, escuro ou automático, acompanhando o Windows.
- Resumo persistente com fotos, vídeos, itens ignorados, falhas e duração.
- Atualizações verificadas e instaladas pelo próprio aplicativo.

## Versão atual

**Splitaria 0.21.4 · Windows 64 bits · Instalador por usuário**

Baixe `Splitaria-Setup-0.21.4-win-x64.exe` na página de
[releases](https://github.com/RY0UK3N/Splitaria/releases/latest). A instalação
normalmente não exige privilégios administrativos e já inclui o runtime e os
componentes necessários para a reprodução de vídeos.

A partir da versão 0.19.2, futuras atualizações podem ser obtidas em
**Preferências > Verificar atualizações**. O instalador baixado é validado por
SHA-256 antes de ser executado.

Como os binários ainda não possuem assinatura digital — nem autoassinada, nem
emitida por uma autoridade certificadora — o Windows pode exibir um aviso de
segurança ao abri-los pela primeira vez.

## Licença

O Splitaria é disponibilizado sob a **MIT License**.

As fotos, os vídeos e as coleções organizadas com o Splitaria pertencem
integralmente a seus respectivos proprietários.

Os termos completos estão em [LICENSE](LICENSE). As bibliotecas utilizadas e
suas respectivas licenças estão nos
[avisos de terceiros](THIRD-PARTY-NOTICES.txt).

<details>
<summary><strong>Informações para desenvolvimento</strong></summary>

### Ambiente

Requer Windows 10 ou superior e [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
O Inno Setup 6 é necessário apenas para gerar o instalador.

```powershell
dotnet build Splitaria.slnx
dotnet run --project src/Splitaria.App
```

### Testes e empacotamento

```powershell
dotnet run --project tests/Splitaria.Tests
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

O instalador é gerado em `publish/installer/`. As pastas de publicação não são
versionadas.

### Principais diretórios

- [`src/Splitaria.App/`](src/Splitaria.App/) — interface WPF, preferências, previews e integração com o Windows.
- [`src/Splitaria.Core/`](src/Splitaria.Core/) — análise de mídia, datas, duplicidade e organização dos arquivos.
- [`tests/Splitaria.Tests/`](tests/Splitaria.Tests/) — testes automatizados das operações principais.
- [`scripts/`](scripts/) — geração do instalador e tarefas de empacotamento.
- [`installer/`](installer/) — configuração do instalador para Windows.

</details>

## Tecnologias e revisão

O projeto utiliza **C#**, **WPF** e **.NET 10**. A reprodução e a visualização de
vídeos utilizam **LibVLCSharp** e **libVLC**, enquanto o instalador para Windows
é gerado com **Inno Setup**. Os códigos-fonte estão organizados em [`src/`](src/),
com testes em [`tests/`](tests/) e ferramentas de compilação em
[`scripts/`](scripts/).

A revisão do projeto contam com **assistência de IA
(Codex/OpenAI)**, acompanhada de revisão humana e testes automatizados das
operações principais.

---

<p align="center">
  Feito para deixar fotos, vídeos e lembranças mais fáceis de encontrar.<br>
  <sub>Copyright © 2026 Marcos Luciano Tagliari Junior · Uso sujeito aos termos da licença MIT.</sub>
</p>
