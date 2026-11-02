# Completamento verificato: 60%

Indicatore basato su cinque milestone tecniche di peso uguale (20% ciascuna). Le tre fasi convalidate non misurano il tempo necessario per le altre due.

| Fase | Verificato | Stato |
|---|---:|---|
| Recupero e compilazione C# | 100% | Quattro assembly verificati; codice C# invariato. |
| Esportazione dei bundle | 100% | 53/53 elaborati; fedelta da verificare in Unity. |
| Procedura e salvataggio GitHub | 100% | Controlli, ripresa, audit e copia verificata. |
| Integrazione delle risorse in Unity | 0% convalidato | Copia con cinque conflitti di nomi eliminati; baseline degli script nulli verificata per conteggi/nomi; GUID e integrazione da convalidare. |
| Build avviabile e prova del gioco | 0% convalidato | Editor e gioco ricostruito non avviati. |

Restano due GUID distinti non risolti. Gli 86 campi m_Script nulli hanno conteggi e nomi corrispondenti nei binari originali; la verifica individuale in Unity resta aperta. La copia di lavoro conserva 69.284 file verificati e presenta zero conflitti di nomi; i dieci progetti restano separati.

Dettagli, comandi e devlog: [README.md](README.md).
Rapporto originale: [reports/recovered-unity-audit.json](reports/recovered-unity-audit.json).
Rapporto della copia: [reports/windows-copy-verification.json](reports/windows-copy-verification.json).
