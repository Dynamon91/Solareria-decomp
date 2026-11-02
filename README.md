# Solateria — GitHub Lite

Sorgenti C# recuperati e procedura locale sequenziale. Il repository contiene 1.567 file C# utili; due segnaposto vuoti del recupero precedente sono stati eliminati. Le risorse originali non vengono ridimensionate o alterate.

## Cosa diventa leggero

Git conserva sorgenti, progetti, script e impronte delle dipendenze. DLL, build, audio, texture e scene esportate rimangono locali e sono escluse da Git. Questo riduce il pacchetto versionato, non la dimensione del gioco completo: le risorse vanno ancora recuperate dalla copia locale.

La decompilazione lavora su un assembly alla volta. L'esportazione delle risorse usa un processo nuovo per ciascun bundle, chiude il processo al termine e riparte dai lavori completati. Le DLL identiche delle esportazioni condividono lo spazio tramite hard link, quando il filesystem lo consente; altrimenti restano copie normali. I file condivisi vanno trattati come dipendenze di sola lettura.

## Avvio rapido su Windows

Servono Python 3.10 o successivo, .NET SDK 8 o successivo e una cartella del gioco gia estratta. Aprire un terminale nella cartella del progetto:

```powershell
py manage.py prepare "C:\Giochi\Solateria"
py manage.py build
```

`prepare` controlla SHA-256 e dimensioni delle 171 DLL della build analizzata, quindi copia le dipendenze in `References/`. Se la build e diversa si ferma con un messaggio; non mescola versioni incompatibili. `build` ricompila i quattro assembly uno alla volta. Su Linux/macOS usare `python3` al posto di `py`.

## Decompilazione ripetibile

Il codice e gia incluso: questa operazione serve per rigenerarlo dal compilato.

Installare [ILSpy](https://github.com/icsharpcode/ILSpy) 9.1.0.7988 nella cartella locale:

```powershell
dotnet tool install --tool-path .local/tools ilspycmd --version 9.1.0.7988
py manage.py decompile --ilspy .local/tools/ilspycmd.exe --verify
```

Su Linux/macOS il nome dell'eseguibile e `.local/tools/ilspycmd`. Per una distribuzione DLL si puo passare il percorso di `ilspycmd.dll`.

Il risultato nuovo va in `.local/fresh/`, senza sovrascrivere `Code/`. La procedura recupera separatamente i tipi che nel primo passaggio possono restare vuoti, separa le due arita dei tipi generici, corregge gli accessori di interfaccia, ripristina gli import Spine e i riferimenti Unity. `--verify` esegue la compilazione del risultato. Se `.local/fresh` esiste gia, rinominarla prima di ricominciare.

Limite predefinito dell'heap gestito per la decompilazione: 1.024 MiB. Modificabile con `--heap-mib`. Questo non e un limite della RAM totale del computer o della somma dei processi.

## Risorse: un bundle alla volta

Usare [AssetRipper](https://github.com/AssetRipper/AssetRipper) 2.0.0, scaricato separatamente.

Prova rapida:

```powershell
py manage.py export --asset-ripper "C:\Tools\AssetRipper\AssetRipper.GUI.Free.exe" --bundle animation_assets_all.bundle
```

Tutte le risorse:

```powershell
py manage.py export --asset-ripper "C:\Tools\AssetRipper\AssetRipper.GUI.Free.exe"
```

Ogni esecuzione elabora prima i dati di base, poi i 53 bundle separatamente. Output in `.local/assets/`, log in `.local/logs/`. I lavori completati con input invariato vengono saltati al riavvio. Una cartella parzialmente esportata viene segnalata: rinominarla per riprovare quel lavoro, mantenendo i log per la diagnosi.

Il limite predefinito dell'heap gestito e 2.048 MiB, configurabile con `--heap-mib`. Memoria nativa, buffer e processi aggiuntivi richiedono RAM oltre questo valore. I bundle piu grandi possono ancora esaurire la memoria: lo script segnala l'errore, non dichiara completato un lavoro fallito. Le esportazioni a blocchi possono avere riferimenti mancanti tra bundle; non costituiscono un progetto Unity funzionante gia verificato. Le banche FMOD restano nella copia locale e nel precedente recupero completo, non vengono convertite da questo comando.

## Verifiche eseguite

- Preparazione e hash delle 171 dipendenze: superati.
- Compilazione sequenziale dei quattro progetti inclusi: superata, zero errori e avvisi presenti.
- Nuova decompilazione e compilazione automatica dei quattro assembly: superate; 1.567 sorgenti, nessun file vuoto.
- Esportazione `animation_assets_all.bundle`: 535 file; picco RSS del processo piu pesante circa 1.017 MiB.
- Esportazione `font_assets_all.bundle`: 197 file; picco RSS del processo piu pesante circa 960 MiB.
- Ripresa di un'esportazione completata: verificata.
- Seconda esportazione: 38.548.480 byte di DLL duplicate condivisi tramite hard link.

Il test completo decompilazione + compilazione ha rilevato circa 541 MiB come RSS massimo del singolo processo piu pesante. Non e una misura della RAM simultanea totale, ne una garanzia per altri bundle o computer. I 53 bundle non sono stati riesportati tutti con questa nuova procedura; erano stati elaborati nel recupero precedente. Unity Editor e il gioco ricostruito non sono stati avviati.

I test sono stati eseguiti in ambiente Linux con .NET SDK 8.0.100 e MSBuild diretto. I comandi PowerShell sono forniti per Windows, ma non sono stati eseguiti su Windows in questa sessione.

## GitHub

Il `.gitignore` esclude dipendenze e risorse locali. Per controllare cosa verra caricato:

```powershell
git init
git add .
git status --short
```

Committare e inviare al repository Solateria scelto. Non aggiungere forzatamente `References/`, `.local/` o cartelle `bin/obj`. Repository: [Dynamon91/Solareria-decomp](https://github.com/Dynamon91/Solareria-decomp). Per ottenere il progetto usare `git clone https://github.com/Dynamon91/Solareria-decomp.git` e poi eseguire i comandi di preparazione dalla cartella clonata.
