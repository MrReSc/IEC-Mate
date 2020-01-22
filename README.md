# IEC-Mate

[![Maintenance](https://img.shields.io/badge/Maintained-yes-green.svg)](https://github.com/MrReSc/IEC-Mate/pulse) [![GitHub license](https://img.shields.io/github/license/MrReSc/IEC-Mate.svg)](https://github.com/MrReSc/IEC-Mate/blob/master/LICENSE) ![](https://img.shields.io/github/downloads/MrReSc/IEC-Mate/total.svg) ![](https://img.shields.io/github/downloads/MrReSc/IEC-Mate/latest/total) [![Releases](https://img.shields.io/github/release/MrReSc/IEC-Mate.svg)](https://github.com/MrReSc/IEC-Mate/releases) ![](https://badges.frapsoft.com/os/v2/open-source.png?v=103)

[![Open Issues](https://img.shields.io/github/issues/MrReSc/IEC-Mate.svg)](https://github.com/MrReSc/IEC-Mate/issues) [![Closed Issues](https://img.shields.io/github/issues-closed/MrReSc/IEC-Mate.svg)](https://github.com/MrReSc/IEC-Mate/issues?q=is%3Aissue+is%3Aclosed)


Die neuste Version kann [hier](https://github.com/MrReSc/IEC-Mate/releases) heruntergeladen werden. Bei jedem Start von IEC-Mate wird automatisch auf Updates überprüft.

Fehler können [hier](https://github.com/MrReSc/IEC-Mate/issues) gemeldet werden. Über das Einstellungsmenu können die Log Dateien eingesehen werden. Bei einem Bug, bitte Log Datei anhängen.

IEC-Mate soll die Entwicklung von Software unterstützen. Dazu gibt es die folgenden Kernfunktionen:

- Code
- Suche
- Bitset
- dat@net
- DataView
- Helfer

Die dat@net und DataView Funktionen sind sehr spezifisch für KEBA IEC Projekte abgestimmt.

## Code

Mit der Code Funktion kann eine Vorlage erstellt werden die drei Variablen enthält. Diese Variablen werden beim generieren durch die Variablen in der Variablenliste ersetzt. So ist es möglich, schnell repetitiven Code zu erstellen.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/code.gif?raw=true)

Die Code Vorlagen können auch als ``*.txt`` Datei gespeichert und bei Bedarf wieder importiert werden.

## Suche

Mit der Suchfunktion können Wörter innerhalb von Dateien gesucht werden. Als Suchvorschlag werden die im ausgewählten IEC Projekt konfigurierten IO Variablen (``Projektname\application\control\config``) angezeigt.

Es gibt verschiedene Einstellungen:

- Ganzes Wort suchen: Es werden nur Dateien angezeigt die einen exakten Treffer enthalten (Case-Insensitiv)
- Nur Verzeichnis \config durchsuchen: Es wird nur im Unterverzeichnis  `Projektname\application\control\config\` gesucht. Dies verkürzt die Suchzeit wenn nur nach einem Hardware-Endpunkt gesucht wird.
- Nur Mask *.xml Dateien durchsuchen: Es werden nur die `.xml` Dateien die für das HMI relevant sind durchsucht. 
- Nur *.java Dateien durchsuchen: Es werden nur Dateien mit dem Dateityp `.java` durchsucht auch wenn dieser Dateityp ausgeschlossen wurde.
- Im Einstellungsmenu können Dateitypen von der Suche ausgeschlossen werden (Standardmässig sind alle Binärdateien und Java Dateien ausgeschlossen).
- Im Einstellungsmenu kann der Suchvorschlag deaktiviert werden.
- Im Einstellungsmenu kann das Standardmässige öffnen mit `Notepad ++` ausgewählt werden. Dies bringt den Vorteil, dass die Datei direkt beim ersten Treffer geöffnet wird.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/suche.gif?raw=true)

Die Dateien können mit einem Doppelklick geöffnet werden. Der Ordner in dem die Datei liegt kann mit einem Rechtsklick  über das Menu geöffnet werden.

## Bitset

Die Bitset-Funktion bietet die Möglichkeit schnell zwischen Binär-, Hexadezimal- und Dezimalzahlen umzurechnen.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/bitset.gif?raw=true)

## dat@net

Spezifisch auf IEC Projekte zugeschnitten sind die Ordner- und Datei-Helfer. Sie ermöglichen einen schnellen Zugriff auf häufig genutzt Dateien und Ordner.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/datanet.png?raw=true)

Mit der Backupfunktion kann das aktuell ausgewählte Verzeichnis als Archiv `*.7z` gespeichert werden. 

Die Simulation und die Visualisierung für das ausgewählte Projekt kann direkt gestartet werden.

Auch gibt es einen Button um die ``*.puLock`` Dateien im ausgewählten Projekt zu löschen.

## DataView

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/dataview.png?raw=true)

#### DataView Entwicklung

Die Funktionen in der Box `DataView Entwicklung` beziehen sich immer auf das Entwicklungsprojekt `.\DVIEW_Work\`.

#### DataView Simulation

Die Funktionen in der Box `DataView Simulation` beziehen sich immer auf das Simulationsprojekt `.\view\`.

#### DataView Datenbank

Wenn ein Kundenordner eingestellt ist, kann hier der Batch `update_Dataview_simulation_DB_to_new_vers_and_this_order.bat` ausgeführt werden.

#### DataView Datenbank Bitset

Hier kann das Bitset zu einem Kundenspez. in der DataView Datenbank abgerufen und geändert werden. 

## Helfer

Wenn IEC-Mate geöffnet ist, kann mit den konfigurierten Tastenkombinationen die PX Nummer in verschiedenen Variationen in andere Applikationen eingefügt werden. 

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/helper.png?raw=true)

## Einstellungen

IEC-Mate ist auf Deutsch und Englisch lokalisiert. Der Editor hat verschieden Einstellungsmöglichkeiten und ein dunkles Thema ist auch verfügbar.

![code](https://github.com/MrReSc/IEC-Mate/blob/master/screenshots/settings.gif?raw=true)

## Anforderungen

IEC-Mate wurde auf MS Windows 10 getestet.

- Windows 10 x86/x64
- .NET Framework 4.7.2

Falls das .NET Framework 4.7.2 nicht vorhanden sein sollte, bitte der Installationsaufforderung folgen.

## Lizenz

IEC-Mate steht unter der [MIT Lizenz](https://raw.githubusercontent.com/MrReSc/IEC-Mate/master/LICENSE).

## Verwendetet Bibliotheken

Grossen Dank an die Programmierer die Open Source Bibliothek zur Verfügung stellen.

- [mahapps](https://github.com/MahApps/MahApps.Metro)
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
- [NHotkey](https://github.com/thomaslevesque/NHotkey)
- [Windows Input Simulator Plus](https://github.com/TChatzigiannakis/InputSimulatorPlus)
- [7-Zip](https://www.7-zip.org/)
- [Serilog](https://github.com/serilog/serilog)
- [Async MySQL Connector](https://github.com/mysql-net/MySqlConnector)

## Entwicklungsumgebung einrichten

Als Entwicklungsumgebung dient Visual Studio 2017 oder 2019. Alle Erweiterungen können direkt in Visual Studio über das Menu `Extensions -> Extension Manager` installiert werden.

Um die Entwicklung zu vereinfachen sollten folgende Erweiterungen installiert werden:

- [GitHub](https://visualstudio.github.com/)
- [AutomaticVersion](https://marketplace.visualstudio.com/items?itemName=PrecisionInfinity.AutomaticVersions)

Um das Projekt erstellen zu können, werden zwingend folgende Erweiterung benötigt:

- [Visual Studio Installer Project](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2017InstallerProjects)

Nun kann über die GitHub Erweiterung das Projekt geklont werden.

Nun kann die `IEC Mate.sln` Solution geöffnet werden. Wenn die Solution offen ist, kann mit einem Rechtsklick auf `Soloution (oberste Ebene) -> Restore NuGet Packages` alle benötigten Bibliotheken wieder heruntergeladen werden.

### Bekannte Probleme

- Wenn die ganzen Referenzen nicht erkannt werden, muss einfach im Solution Explorer zu den Referenzen navigiert werden. Alle gelben Warnzeichen sollten nach ein paar Sekunden verschwinden.
- Wenn der Error `XDG0008` bei Übersetzten der Software auftritt, dann muss die Datei `MainWindow.xaml` mit einem `Rechtsklick -> Exluce from Project` vom Projekt entfernt werden. Danach sollte ein Übersetzten ohne Fehler funktionieren. Wenn es immer noch Fehler gibt, kann versucht werden, das Target auf `Release` zu wechseln und wieder auf `Debug` zurück. Wenn das Übersetzten fehlerfrei geklappt hat, muss mittel Rechtsklick auf das Projekt `IEC Mate -> Add -> Existing Item` die Datei `MainWindow.xaml.cs` wieder hinzugefügt werden.
