# Solateria — GitHub Lite

Sorgenti C# recuperati e procedura locale sequenziale. Il repository contiene 1.567 file C# utili; due segnaposto vuoti del recupero precedente sono stati eliminati. Le risorse originali non vengono ridimensionate o alterate.

## Cosa diventa leggero

Git conserva sorgenti, progetti, script e impronte delle dipendenze. DLL, build, audio, texture e scene esportate rimangono locali e sono escluse da Git. Questo riduce il pacchetto versionato, non la dimensione del gioco completo: le risorse vanno ancora recuperate dalla copia locale.

La decompilazione lavora su un assembly alla volta. L'esportazione delle risorse usa un processo nuovo per ciascun bundle, chiude il processo al termine e riparte dai lavori completati. Le DLL identiche delle esportazioni condividono lo spazio tramite hard link, quando il filesystem lo consente; altrimenti restano copie normali. I file condivisi vanno trattati come dipendenze di sola lettura.

## Avvio rapido su Windows

Servono Python 3.10 o successivo, .NET SDK 8 o successivo e una cartella del gioco gia estratta. Aprire un terminale nella cartella del progetto:

```powershell
py manage.py prepare "C:\Giochi\Solateria"
py manage.py doctor
py manage.py build
```

`prepare` controlla SHA-256 e dimensioni delle 171 DLL della build analizzata, quindi prepara una nuova cartella di dipendenze e la pubblica in `References/` solo dopo la verifica delle copie. Se la copia o la scrittura della configurazione fallisce, ripristina la cartella precedente. Se la build e diversa si ferma con un messaggio; non mescola versioni incompatibili. `build` ricompila i quattro assembly uno alla volta. Su Linux/macOS usare `python3` al posto di `py`.

## Decompilazione ripetibile

Il codice e gia incluso: questa operazione serve per rigenerarlo dal compilato.

Installare [ILSpy](https://github.com/icsharpcode/ILSpy) 9.1.0.7988 nella cartella locale:

```powershell
dotnet tool install --tool-path .local/tools ilspycmd --version 9.1.0.7988
py manage.py decompile --ilspy .local/tools/ilspycmd.exe --verify
```

Su Linux/macOS il nome dell'eseguibile e `.local/tools/ilspycmd`. Per una distribuzione DLL si puo passare il percorso di `ilspycmd.dll`.

Il risultato nuovo va in `.local/fresh/`, senza sovrascrivere `Code/`. La procedura recupera separatamente i tipi che nel primo passaggio possono restare vuoti, separa le due arita dei tipi generici, corregge gli accessori di interfaccia, ripristina gli import Spine e i riferimenti Unity. `--verify` esegue la compilazione del risultato. Se il lavoro si interrompe, riprenderlo con `decompile --resume` e gli stessi argomenti. Ogni assembly completato ha un checkpoint con hash dei sorgenti: gli assembly integri vengono saltati, quelli incompleti vengono rigenerati dopo aver conservato la vecchia cartella in `.local/interrupted/`. Un cambio di strumenti, dipendenze o versione della procedura richiede una nuova cartella `.local/fresh` (conservare quella vecchia rinominandola).

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

Ogni esecuzione elabora prima i dati di base, poi i 53 bundle separatamente. Output in `.local/assets/`, log in `.local/logs/`. Prima di saltare un lavoro completato, la procedura ricontrolla i nomi, le dimensioni e gli SHA-256 di tutti i file esportati. File mancanti o modificati, checkpoint danneggiati e cambi di input o strumento richiedono `--retry`. Questo conserva la vecchia cartella in `.local/interrupted/` e rigenera il lavoro, senza eliminare gli altri risultati integri. I checkpoint della versione precedente, privi di hash degli output, richiedono lo stesso passaggio una tantum.

Il limite predefinito dell'heap gestito e 2.048 MiB, configurabile con `--heap-mib`. Memoria nativa, buffer e processi aggiuntivi richiedono RAM oltre questo valore. I bundle piu grandi possono ancora esaurire la memoria: lo script segnala l'errore, non dichiara completato un lavoro fallito. Le esportazioni a blocchi possono avere riferimenti mancanti tra bundle; non costituiscono un progetto Unity funzionante gia verificato. Le banche FMOD restano nella copia locale e nel precedente recupero completo, non vengono convertite da questo comando.

## Diagnosi e recupero

Controllo iniziale, senza modificare i sorgenti:

```powershell
py manage.py doctor --ilspy .local/tools/ilspycmd.exe --asset-ripper "C:\Tools\AssetRipper\AssetRipper.GUI.Free.exe"
```

Il rapporto viene salvato in `.local/doctor.json`: presenza di un SDK .NET adatto, integrita delle dipendenze, presenza degli strumenti richiesti e disponibilita dei bundle nella copia locale. Non sostituisce una prova di compatibilita dei programmi o del gioco.

Ripresa della decompilazione:

```powershell
py manage.py decompile --ilspy .local/tools/ilspycmd.exe --resume --verify
```

Nuovo tentativo di esportazione, mantenendo i risultati precedenti:

```powershell
py manage.py export --asset-ripper "C:\Tools\AssetRipper\AssetRipper.GUI.Free.exe" --bundle animation_assets_all.bundle --retry
```

Un blocco del sistema operativo impedisce due lavori simultanei nella stessa cartella e si rilascia anche dopo la terminazione improvvisa di un processo. La preparazione delle dipendenze e transazionale; ogni build e decompilazione le ricontrolla. La cache condivisa delle DLL viene verificata prima del riuso. Le cartelle temporanee vengono rimosse anche se il programma esterno non si avvia, mentre log e output interrotti restano disponibili. Le chiamate al decompilatore hanno timeout: un arresto non crea un falso checkpoint di successo.

Test della procedura, senza gioco o strumenti esterni:

```powershell
py -m unittest discover -s tests -v
```

Sono test di regressione con input sintetici e risposte simulate per ILSpy/AssetRipper; non dimostrano una ricostruzione funzionante del gioco.

## Verifiche della revisione di affidabilita

21 test eseguiti con esito positivo in Linux: copia interrotta, rollback della configurazione, cache corrotta, asset cancellati o modificati, ripresa e nuovo tentativo, checkpoint corrotti, log di errori precedenti, mancato avvio del processo, cartelle con spazi, diagnostica SDK/dipendenze file in sola lettura e rilascio del blocco anche dopo un'uscita improvvisa. Nessun sorgente C# e stato modificato. In questa revisione non sono state rieseguite le compilazioni .NET, l'esportazione con gli strumenti reali o Unity; le verifiche precedenti sono riportate sotto come storico.

## Verifiche precedenti sul recupero reale

- Preparazione e hash delle 171 dipendenze: superati.
- Compilazione sequenziale dei quattro progetti inclusi: superata, zero errori e avvisi presenti.
- Nuova decompilazione e compilazione automatica dei quattro assembly: superate; 1.567 sorgenti, nessun file vuoto.
- Esportazione `animation_assets_all.bundle`: 535 file; picco RSS del processo piu pesante circa 1.017 MiB.
- Esportazione `font_assets_all.bundle`: 197 file; picco RSS del processo piu pesante circa 960 MiB.
- Ripresa di un'esportazione completata: verificata.
- Seconda esportazione: 38.548.480 byte di DLL duplicate condivisi tramite hard link.

Il test completo decompilazione + compilazione ha rilevato circa 541 MiB come RSS massimo del singolo processo piu pesante. Non e una misura della RAM simultanea totale, ne una garanzia per altri bundle o computer. I 53 bundle non sono stati riesportati tutti con questa nuova procedura; erano stati elaborati nel recupero precedente. Unity Editor e il gioco ricostruito non sono stati avviati.

Le verifiche storiche sul recupero reale sono state eseguite in ambiente Linux con .NET SDK 8.0.100 e MSBuild diretto. I comandi PowerShell sono forniti per Windows, ma non sono stati eseguiti su Windows in questa sessione.

## GitHub

Il workflow `Pipeline regression checks` esegue la suite su Linux e Windows con Python 3.10 e 3.12, senza richiedere asset o DLL del gioco. Controllare il risultato nella scheda Actions: un test della procedura non equivale a una build del gioco.

Il `.gitignore` esclude dipendenze e risorse locali. Per controllare cosa verra caricato:

```powershell
git init
git add .
git status --short
```

Committare e inviare al repository Solateria scelto. Non aggiungere forzatamente `References/`, `.local/` o cartelle `bin/obj`. Repository: [Dynamon91/Solareria-decomp](https://github.com/Dynamon91/Solareria-decomp). Per ottenere il progetto usare `git clone https://github.com/Dynamon91/Solareria-decomp.git` e poi eseguire i comandi di preparazione dalla cartella clonata.

