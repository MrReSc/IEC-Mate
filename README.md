# IEC-Mate
IEC-Mate soll die Entwicklung von Software unterstützen. Dazu gibt es die folgenden Kernfunktionen:

- Code
- Suche
- Bitset
- Helfer

Die Helfer Funktionen sind sehr spezifisch für IEC Projekte.

## Code

Mit der Code Funktion kann eine Vorlage erstellt werden die drei Variablen enthält. Diese Variablen werden beim generieren durch die Variablen in der Variablenliste ersetzt. So ist es möglich, schnell repetitiven Code zu erstellen.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/code.gif?raw=true)

## Suche

Mit der Suchfunktion können Wörter innerhalb von Dateien gesucht werden.

Es gibt drei Verschiedene Schalter:

- Exakte Zeichenfolge: Es werden nur Dateien angezeigt die einen exakten Treffer enthalten
- Binär Dateien: Es wird nicht in Binär Dateien gesucht
- HW-Konfig: Es wird nur im Unterverzeichnis  `Projektname\application\control\config\` gesucht

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/suche.gif?raw=true)

## Bitset

Die Bitset-Funktion bietet die Möglichkeit schnell zwischen Binär-, Hexadezimal- und Dezimalzahlen umzurechnen.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/bitset.gif?raw=true)

## Helfer

IEC-Mate hat auch einiger Helfer-Funktionen. 

Spezifisch auf IEC Projekte zugeschnitten sind die Ordner- und Datei-Helfer. Sie ermöglichen einen schnellen Zugriff auf häufig genutzt Dateien und Ordner.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/helper_folder.gif?raw=true)

Mit der Backupfunktion kann das aktuell ausgewählte Verzeichnis als Archiv `*.7z`gesepichert werden.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/helper_backup.gif?raw=true)

Wenn IEC-Mate geöffnet ist, kann mit den konfigurierten Tastenkombinationen schnell Text in andere Applikationen eingefügt werden.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/helper_hotkey.gif?raw=true)

## Einstellungen

IEC-Mate ist auf Deutsch und Englisch lokalisiert. Der Editor hat verschieden Einstellungsmöglichkeiten und ein dunkles Thema ist auch verfügbar.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/settings.gif?raw=true)

## Anforderungen

IEC-Mate läuft auf Microsoft Windows.

- Windows 7, 8, 8.1, 10 x64
- .NET Framework 4.7.2

## Lizenz

IEC-Mate steht unter der MIT Lizenz.

## Verwendetet Bibliotheken

- [mahapps](https://github.com/MahApps/MahApps.Metro)
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
- [NHotkey](https://github.com/thomaslevesque/NHotkey)