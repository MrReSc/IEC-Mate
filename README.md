# IEC-Mate

IEC-Mate ist noch in der Beta Phase.

IEC-Mate soll die Entwicklung von Software unterstützen. Dazu gibt es die folgenden Kernfunktionen:

- Code
- Suche
- Bitset
- Helfer

Die Helfer Funktionen sind sehr spezifisch für KEBA IEC Projekte abgestimmt.

## Code

Mit der Code Funktion kann eine Vorlage erstellt werden die drei Variablen enthält. Diese Variablen werden beim generieren durch die Variablen in der Variablenliste ersetzt. So ist es möglich, schnell repetitiven Code zu erstellen.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/code.gif?raw=true)

Die Code Vorlagen können auch als ``*.txt`` Datei gespeichert und bei Bedarf wieder importiert werden.

## Suche

Mit der Suchfunktion können Wörter innerhalb von Dateien gesucht werden.

Es gibt drei Verschiedene Schalter:

- Exakte Zeichenfolge: Es werden nur Dateien angezeigt die einen exakten Treffer enthalten
- Binär Dateien: Die Suche in Binärdateien kann aktiviert werden
- HW-Konfig: Es wird nur im Unterverzeichnis  `Projektname\application\control\config\` gesucht

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/suche.gif?raw=true)

Die Dateien können mit einem Doppelklick oder der Ordner in dem die Datei liegt mit einem Rechtsklick  geöffnet werden.

## Bitset

Die Bitset-Funktion bietet die Möglichkeit schnell zwischen Binär-, Hexadezimal- und Dezimalzahlen umzurechnen.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/bitset.gif?raw=true)

## Helfer

IEC-Mate hat auch einige Helfer-Funktionen. 

Spezifisch auf IEC Projekte zugeschnitten sind die Ordner- und Datei-Helfer. Sie ermöglichen einen schnellen Zugriff auf häufig genutzt Dateien und Ordner.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/helper_folder.gif?raw=true)

Mit der Backupfunktion kann das aktuell ausgewählte Verzeichnis als Archiv `*.7z` gespeichert werden. 

Die Simulation und die Visualisierung für das ausgewählte Projekt kann direkt gestartet werden.

Auch gibt es einen Button um die ``*.puLock`` Dateien im ausgewählten Projekt zu löschen.

Wenn IEC-Mate geöffnet ist, kann mit den konfigurierten Tastenkombinationen die PX Nummer in verschiedenen Variationen in andere Applikationen eingefügt werden. 

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/helper_hotkey.gif?raw=true)

## Einstellungen

IEC-Mate ist auf Deutsch und Englisch lokalisiert. Der Editor hat verschieden Einstellungsmöglichkeiten und ein dunkles Thema ist auch verfügbar.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/settings.gif?raw=true)

## Anforderungen

IEC-Mate läuft auf Microsoft Windows.

- Windows 7, 8, 8.1, 10 x86/x64
- .NET Framework 4.7.2

## Lizenz

IEC-Mate steht unter der MIT Lizenz.

## Verwendetet Bibliotheken

Grossen Dank an die Programmierer die Open Source Bibliothek zur Verfügung stellen.

- [mahapps](https://github.com/MahApps/MahApps.Metro)
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
- [NHotkey](https://github.com/thomaslevesque/NHotkey)
- [Windows Input Simulator Plus](https://github.com/TChatzigiannakis/InputSimulatorPlus)
- [7-Zip](https://www.7-zip.org/)

