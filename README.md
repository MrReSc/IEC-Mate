# IEC-Mate

IEC-Mate ist noch in der Beta Phase. Die neuste Version kann [hier](https://github.com/MrReSc/IEC-Mate/releases) heruntergeladen werden.

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

Mit der Suchfunktion können Wörter innerhalb von Dateien gesucht werden. Als Suchvorschlag werden die im ausgewählten IEC Projekt konfigurierten IO Variablen (``Projektname\application\control\config``) angezeigt.

Es gibt verschiedene Einstellungen:

- Ganzes Wort suchen: Es werden nur Dateien angezeigt die einen exakten Treffer enthalten (Case-Insensitiv)
- HW-Konfig: Es wird nur im Unterverzeichnis  `Projektname\application\control\config\` gesucht
- Im Einstellungsmenu können Dateitypen von der Suche ausgeschlossen werden (Standardmässig sind alle Binärdateien und Java Dateien ausgeschlossen)
- Im Einstellungsmenu kann der Suchvorschlag deaktiviert werden

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/suche.gif?raw=true)

Die Dateien können mit einem Doppelklick geöffnet werden. Der Ordner in dem die Datei liegt kann mit einem Rechtsklick  über das Menu geöffnet werden.

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

IEC-Mate wurde auf MS Windows 7 und Windows 10 getestet.

- Windows 7, 10 x86/x64
- .NET Framework 4.7.2

Falls das .NET Framework 4.7.2 nicht vorhanden sein sollte, bitte der Installationsaufforderung folgen.

## Lizenz

IEC-Mate steht unter der MIT Lizenz.

## Verwendetet Bibliotheken

Grossen Dank an die Programmierer die Open Source Bibliothek zur Verfügung stellen.

- [mahapps](https://github.com/MahApps/MahApps.Metro)
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
- [NHotkey](https://github.com/thomaslevesque/NHotkey)
- [Windows Input Simulator Plus](https://github.com/TChatzigiannakis/InputSimulatorPlus)
- [7-Zip](https://www.7-zip.org/)

