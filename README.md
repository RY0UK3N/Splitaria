# Splitaria

Organizador local de fotos e vídeos para Windows. O Splitaria analisa uma ou mais pastas e prepara uma estrutura por ano e mês nos destinos de Fotos e Vídeos antes de copiar os arquivos.

## Princípios

- funciona inteiramente no computador;
- não exige conta nem envia arquivos para a internet;
- copia por padrão e preserva os originais;
- mostra o plano antes de executar;
- evita sobrescrever arquivos existentes.
- prioriza a data de captura armazenada nas fotos;
- detecta arquivos repetidos pelo conteúdo;
- permite escolher individualmente o que será copiado.

## Desenvolvimento

```powershell
dotnet build Splitaria.slnx
dotnet run --project src/Splitaria.App
dotnet run --project tests/Splitaria.Tests
```

## Publicação portátil

```powershell
dotnet publish src/Splitaria.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

O executável final será criado em `publish/win-x64/Splitaria.exe`.

## Instalador para Windows

Com o [Inno Setup 6](https://jrsoftware.org/isinfo.php) instalado:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

O pacote será criado em `publish/installer/`. A instalação padrão é feita por
usuário e não solicita privilégios administrativos.

## Licença

O código próprio do Splitaria é disponibilizado sob a [licença MIT](LICENSE).
Os componentes LibVLCSharp e libVLC incluídos na distribuição permanecem sob
a GNU LGPL 2.1 ou posterior; consulte `THIRD-PARTY-NOTICES.txt`.
