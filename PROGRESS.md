# Completamento verificato: 60%

L'indicatore conta cinque fasi tecniche con peso uguale (20% ciascuna). Tre fasi sono convalidate. Il tempo e la complessita delle due fasi restanti possono essere maggiori di quelli delle fasi completate. La percentuale non deriva da una prova di tutti i livelli o delle funzioni del gioco.

| Fase | Completamento verificato | Evidenza |
|---|---:|---|
| Recupero e compilazione C# | 100% | Quattro assembly verificati nel recupero precedente. |
| Esportazione dei bundle | 100% | 53/53 elaborati; l'esportazione puo contenere dati o riferimenti incompleti. |
| Procedura e salvataggio GitHub | 100% | Controlli di integrita, ripresa, test e audit delle risorse. |
| Integrazione delle risorse in Unity | 0% convalidato | Audit effettuato; riferimenti e script segnalati ancora da ripristinare. |
| Build avviabile e prova del gioco | 0% convalidato | Unity Editor e il gioco ricostruito non sono stati eseguiti. |

## Lavoro aggiunto

Nuovo comando di controllo statico `audit`, utilizzabile sullo ZIP completo o su una cartella ExportedProject. Legge i file YAML e i metadati senza estrarre le texture o riscrivere le risorse. Controlla GUID non risolti, metadati duplicati o mancanti, script nulli e nomi incompatibili con un filesystem non sensibile alle maiuscole. La modalita `--strict` restituisce il codice 2 quando trova casi da verificare.

## Risultato sul recupero reale

ZIP da 4.575.052.918 byte, ricomposto con SHA-256 verificato. Controllati 10 progetti e 27.329 file YAML. Picco misurato del processo di controllo: 106,5 MiB.

- 2 GUID distinti non risolti, presenti in 10 combinazioni progetto/GUID.
- 86 campi m_Script nulli; un solo asset di questi ha riferimenti GUID locali individuati. L'assenza di riferimenti locali non dimostra che un asset sia inutilizzato: caricamenti dinamici, fileID e bundle non vengono convalidati da questo controllo.
- 5 coppie di risorse con nomi che differiscono solo per maiuscole/minuscole; contate anche le rispettive meta, sono 10 coppie di nomi di file.
- Nessun GUID duplicato, metadato orfano o asset normale senza meta rilevato nello scope analizzato.

Il rapporto completo e in [reports/recovered-unity-audit.json](reports/recovered-unity-audit.json). I riferimenti interni standard di Unity sono esclusi dai GUID mancanti; i dati grezzi StreamingAssets non sono trattati come normali asset privi di meta.

## Prossime verifiche

1. Recuperare gli identificatori e i dati originali dei riferimenti non risolti, evitando assegnazioni di GUID per tentativi.
2. Ripristinare i tipi e i dati dei MonoBehaviour segnalati, partendo da quelli referenziati.
3. Preparare una copia di lavoro che conservi entrambe le risorse nei cinque conflitti di nomi, con GUID e meta associati; verificare anche gli eventuali accessi per nome.
4. Aprire il progetto con Unity 2022.3.62f2 e verificare importazione, scene, animazioni, audio e controlli; solo dopo produrre e provare una build.

Il controllo statico non risolve da solo i fileID dei sottoasset o i layout degli script e non dimostra che un progetto sia avviabile. Nessun asset originale e stato modificato in questa revisione.
