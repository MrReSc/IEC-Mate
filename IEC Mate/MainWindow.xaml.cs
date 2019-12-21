using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Path = System.IO.Path;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.Diagnostics;
using System.Deployment.Application;
using WinForms = System.Windows.Forms; //FolderDialog
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using NHotkey.Wpf;
using NHotkey;
using WindowsInput;
using System.Threading;
using System.Globalization;
using WindowsInput.Native;
using ICSharpCode.AvalonEdit;
using octokit = Octokit;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using Serilog;
using MySql.Data.MySqlClient;

namespace IECMate
{

    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        #region Globale Variablen
        public string[] variablen_liste = new string[] { "Variable_1", "Variable_2", "Variable_3" };
        private string[] AccentColor = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        public InputSimulator sim = new InputSimulator();
        public string filename;
        public string TempFolder;
        public string logfile;
        public bool InitRunning;
        #endregion

        #region Benachrichtigung der DataBinding Variablen
        private bool _SucheIstIecProjekt;
        public bool SucheIstIecProjekt
        {
            get { return _SucheIstIecProjekt; }
            set
            {
                _SucheIstIecProjekt = value;
                OnPropertyChanged("SucheIstIecProjekt");
            }
        }
        private bool _HelferIstIecProjekt;
        public bool HelferIstIecProjekt
        {
            get { return _HelferIstIecProjekt; }
            set
            {
                _HelferIstIecProjekt = value;
                OnPropertyChanged("HelferIstIecProjekt");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        public MainWindow()
        {
            #region Initialisierung
            InitRunning = true;

            //Ordner in LocalApplicationData/Temp für IEC Mate erstellen
            //Wenn Ordner schon existier, dann passiert nichts
            TempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp\\IECMate\\");
            Directory.CreateDirectory(TempFolder);

            //Log
            logfile = TempFolder + Properties.Paths.LogFile;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logfile, 
                              rollingInterval: RollingInterval.Day, 
                              fileSizeLimitBytes: Properties.Settings.Default.LogFileSize, 
                              retainedFileCountLimit: Properties.Settings.Default.LofFileRetain)
                .CreateLogger();

            Log.Information("IEC Mate wurde gestartet.");

            //Wenn es eine neue Version gibt, dann werden die Einstellungen von der schon installierten Version übernommen
            if (Properties.Settings.Default.updatesettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.updatesettings = false;
                Properties.Settings.Default.Save();
                Log.Information("Einstellung: Werte von älterer Version übernommen.");
            }

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.sprache);
           
            InitializeComponent();
            DataContext = this;

            //Hotkey
            Key key1 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_comment);
            Key key2 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_beginend);
            Key key3 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_plain);
            Key key4 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_brackets);
            HotkeyManager.Current.AddOrReplace("PxComment", key1, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            HotkeyManager.Current.AddOrReplace("PxBeginEnd", key2, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            HotkeyManager.Current.AddOrReplace("PxPlain", key3, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            HotkeyManager.Current.AddOrReplace("PxBrackets", key4, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);

            // Editor Setup
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"resources\st_syntax.xshd");
            Stream xshd_stream = File.OpenRead(file);
            XmlTextReader xshd_reader = new XmlTextReader(xshd_stream);
            text_code_template.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(xshd_reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
            text_code_output.SyntaxHighlighting = text_code_template.SyntaxHighlighting;
            xshd_reader.Close();
            xshd_stream.Close();

            text_code_template.TextArea.FontFamily = new FontFamily("Consolas");

            text_code_output.IsReadOnly = true;
            text_code_output.TextArea.Caret.CaretBrush = Brushes.Transparent;
            text_code_output.TextArea.FontFamily = new FontFamily("Consolas");

            //Wenn in einem der beiden Editoren kopiert wird, dann wird die Methode onTextViewSettingDataHandler aufgerufen         
            DataObject.AddSettingDataHandler(text_code_template, onTextViewSettingDataHandler);
            DataObject.AddSettingDataHandler(text_code_output, onTextViewSettingDataHandler);

            //ComoBoxen
            combo_vars.ItemsSource = variablen_liste;
            cb_akzent_farbe.ItemsSource = AccentColor;
            cb_akzent_farbe.SelectedItem = Properties.Settings.Default.akzentfarbe;

            char[] alpha = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
            foreach (var letter in alpha)
            {
                KeyConverter k = new KeyConverter();
                var le = (Key)k.ConvertFromString(letter.ToString());

                cb_hotkey_pxBeginEnd.Items.Add(le);
                cb_hotekey_plain.Items.Add(le);
                cb_hotkey_pxComment.Items.Add(le);
                cb_hotekey_brackets.Items.Add(le);
            }

            string[] Sprachen = new string[] { Properties.Resources.lanDE, Properties.Resources.lanEN };
            cb_sprache.ItemsSource = Sprachen;

            //Einstellungen laden
            ThemeManager.ChangeAppStyle(Application.Current,
                            ThemeManager.GetAccent(Properties.Settings.Default.akzentfarbe),
                            ThemeManager.GetAppTheme(Properties.Settings.Default.theme));

            if (Properties.Settings.Default.theme == "BaseDark")
            {
                tg_theme.IsChecked = true;

                //Decode Matrix von Bitset neu Aufbauen
                DecodeText();
            }

            text_code_template.Options.ConvertTabsToSpaces = Properties.Settings.Default.converttabtospace;
            text_code_output.Options.ConvertTabsToSpaces = Properties.Settings.Default.converttabtospace;

            text_code_template.Options.ShowEndOfLine = Properties.Settings.Default.showendofline;
            text_code_output.Options.ShowEndOfLine = Properties.Settings.Default.showendofline;

            text_code_template.Options.ShowTabs = Properties.Settings.Default.showtab;
            text_code_output.Options.ShowTabs = Properties.Settings.Default.showtab;

            text_code_template.Options.ShowSpaces = Properties.Settings.Default.leerzeichen;
            text_code_output.Options.ShowSpaces = Properties.Settings.Default.leerzeichen;

            text_code_template.ShowLineNumbers = Properties.Settings.Default.zeilennummern;
            text_code_output.ShowLineNumbers = Properties.Settings.Default.zeilennummern;

            tg_line_no.IsChecked = Properties.Settings.Default.zeilennummern;
            tg_leerzeichen.IsChecked = Properties.Settings.Default.leerzeichen;
            tg_showendofline.IsChecked = Properties.Settings.Default.showendofline;
            tg_showtab.IsChecked = Properties.Settings.Default.showtab;
            tg_converttospace.IsChecked = Properties.Settings.Default.converttabtospace;
            ts_exakte_suche.IsChecked = Properties.Settings.Default.exakte_suche;

            nc_font_size.Value = Properties.Settings.Default.schriftgrosse;
            text_code_output.TextArea.FontSize = Properties.Settings.Default.schriftgrosse;
            text_code_template.TextArea.FontSize = Properties.Settings.Default.schriftgrosse;

            this.Top = Properties.Settings.Default.fentser_top;
            this.Left = Properties.Settings.Default.fenster_left;
            this.Height = Properties.Settings.Default.fenster_hohe;
            this.Width = Properties.Settings.Default.fenster_breite;
            if (Properties.Settings.Default.fenster_max)
            {
                WindowState = WindowState.Maximized;
            }

            text_projktpfad_suche.Text = Properties.Settings.Default.projekt_pfad_suche;
            text_projktpfad_helfer.Text = Properties.Settings.Default.projekt_pfad_helfer;
            text_projktpfad_dataview.Text = Properties.Settings.Default.projekt_pfad_dataview;
            text_kundenprojekt.Text = Properties.Settings.Default.projekt_pfad_kundenordner;
            text_kundenspez.Text = Properties.Settings.Default.kundenspez;
            text_db_connectionstring.Text = Properties.Settings.Default.sql_connection_string;
            tc_root.SelectedIndex = Properties.Settings.Default.tabcontrol_index;
            ts_hotkey.IsChecked = Properties.Settings.Default.hotkey;
            text_px_nummer.Text = Properties.Settings.Default.pxnummer;
            ts_offnen_nppp.IsChecked = Properties.Settings.Default.offnen_mit_nppp;

            cb_hotkey_pxBeginEnd.Text = key2.ToString();
            cb_hotekey_plain.Text = key3.ToString();
            cb_hotkey_pxComment.Text = key1.ToString();
            cb_hotekey_brackets.Text = key4.ToString();

            text_file_ext.Text = Properties.Settings.Default.file_ext_user;

            if (Properties.Settings.Default.sprache == "de-DE")
            {
                cb_sprache.Text = Properties.Resources.lanDE;
            }
            if (Properties.Settings.Default.sprache == "en-GB")
            {
                cb_sprache.Text = Properties.Resources.lanEN;
            }
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.me_auswahl))
            {
                cb_select_me.Items.Add(Properties.Settings.Default.me_auswahl);
                cb_select_me.SelectedIndex = 0;
                TexteMEFunktionen();
            }

            //Inhalt laden
            text_var1.Text = Properties.Settings.Default.variable_1;
            text_var2.Text = Properties.Settings.Default.variable_2;
            text_var3.Text = Properties.Settings.Default.variable_3;
            text_code_template.Text = Properties.Settings.Default.vorlage;

            //Release überprüfen
            ReleaseCheck();

            Log.Information("Einstellungen geladen und Init fertig.");
            InitRunning = false;
            #endregion
        }

        #region Allgemein
        public async void ReleaseCheck()
        {
            try
            {
                var client = new octokit.GitHubClient(new octokit.ProductHeaderValue("IEC-Mate"));
                var releases = await client.Repository.Release.GetAll("MrReSc", "IEC-Mate");
                var latest = releases[0];
                var relVersion = latest.TagName.Split('.');
                var aseVersion = AssemblyVersion(false).Split('.');

                if (Convert.ToInt32(relVersion[0]) > Convert.ToInt32(aseVersion[0]) ||
                    Convert.ToInt32(relVersion[1]) > Convert.ToInt32(aseVersion[1]) ||
                    Convert.ToInt32(relVersion[2]) > Convert.ToInt32(aseVersion[2]))
                {
                    Log.Information("Allgemein: Neues Release {Rel} verfügbar.", latest.Name);
                    var mymessageboxsettings = new MetroDialogSettings()
                    { 
                        AffirmativeButtonText = Properties.Resources.dialogDownloadUpdateButton,
                        FirstAuxiliaryButtonText = Properties.Resources.dialogUpdateOffnenButton,
                        NegativeButtonText = Properties.Resources.dialogNegButton,
                    };

                    var updateMsg = Properties.Resources.dialogMsgUpdate + Environment.NewLine + Environment.NewLine + latest.Body;
                    var updateTitle = Properties.Resources.dialogTitleUpdate + " " + latest.Name;

                    MessageDialogResult x = await this.ShowMessageAsync(updateTitle, updateMsg, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, mymessageboxsettings);
                    if (x == MessageDialogResult.Affirmative)
                    {
                        Log.Information("Allgemein: Udapte wurde gestartet.");
                        Uri uri = new Uri(latest.Assets[0].BrowserDownloadUrl);
                        filename = TempFolder + latest.Assets[0].Name;

                        try
                        {
                            if (File.Exists(filename))
                            {
                                File.Delete(filename);
                            }

                            WebClient wc = new WebClient();
                            wc.DownloadFileAsync(uri, filename);
                            wc.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);
                            await this.ShowProgressAsync(Properties.Resources.dialogTitelDownlaod, Properties.Resources.dialogMsgDownload);
                        }
                        catch (Exception ex)
                        {
                            await this.ShowMessageAsync(Properties.Resources.dialogTitelDownlaod, ex.Message.ToString(), MessageDialogStyle.Affirmative);
                            Log.Error(ex, "Error Update");
                        }
                    }

                    if (x == MessageDialogResult.FirstAuxiliary)
                    {
                        Process.Start(latest.HtmlUrl);
                    }

                    if (x == MessageDialogResult.Negative)
                    {
                        Log.Information("Allgemein: Udapte wurde abgebrochen.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Log.Information("Allgemein: Update Download fertig. IEC Mate wird geschlossen und neu gestartet.");
                Process.Start(filename);
                Close();
                Application.Current.Shutdown();
            }
            else
            {
                Log.Error(e.Error, "Error");
                await this.ShowMessageAsync(Properties.Resources.dialogTitelDownloadUpdateFehler, Properties.Resources.dialogMsgDownloadUpdateFehler, MessageDialogStyle.Affirmative);
            }
        }

        public void onTextViewSettingDataHandler(object sender, DataObjectSettingDataEventArgs e)
        {
            //hier wird die HTML formatierung vom Text im Editor entfernt
            //da nie eine Art formatierung mit Kopiert werden soll.
            var textView = sender as TextEditor;
            if (textView != null && e.Format == DataFormats.Html)
            {
                e.CancelCommand();
            }
        }

        private void CopyToClipboard_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(text_code_output.Text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Tc_root_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                //Wenn ein Tab gewechselt wird, dann wird der Fokus entsprechend ins richtige Feld gesetzt
                //Dsipachter wird benötigt da das Fenster noch nicht fertig geladen aber der Event schon abgesetzt wird

                if (ti_suche.IsSelected)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
                }

                if (ti_bitset.IsSelected)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_decode.Focus()));
                }

                if (ti_code.IsSelected)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_code_template.Focus()));
                }

            }
            
        }

        private void TexteMEFunktionen()
        {
            try
            {
                string me;

                // Wenn eine ME ausgewählt ist, dann werden die Texte für ME Funktionen angepasst
                if (!string.IsNullOrWhiteSpace(cb_select_me.Text))
                {
                    me = cb_select_me.Text.Replace("_", "__");
                }
                else
                {
                    me = "ME";
                }

                // Header von GroupBox
                gb_me_funktionen.Header = Properties.Resources.gb_helfer_funkionen_me + " " + me;

                //Buttons
                lb_me.Content = Properties.Resources.lb_me + " " + me;
                lb_me_xml.Content = Properties.Resources.lb_me_xml + " " + me;
                lb_me_tu.Content = me + ".tu " + Properties.Resources.lb_me_tu;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_vaiablenliste_loschen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                text_var1.Text = String.Empty;
                text_var2.Text = String.Empty;
                text_var3.Text = String.Empty;
                text_var1.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            
        }
        #endregion

        #region About Fenster
        private void MenuItem_Click_About(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Allgemein: About Fenster wurde geöffnet.");
                child_Infos.IsOpen = true;
                lb_version.Content = AssemblyVersion(true);
                text_lizenzen.Text = File.ReadAllText(@"resources\oss_lizenzen_iec_mate.txt");
                text_iecmate_lizenz.Text = File.ReadAllText(@"resources\iec_mate_lizenz.txt");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }  
        }

        public string AssemblyVersion(bool wDay)
        {
            try
            {
                // get deployment version
                string[] assemblyversion = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
                if (wDay)
                {
                    return assemblyversion[0] + "." + assemblyversion[1] + "." + assemblyversion[2] + " (" + assemblyversion[3] + ")";
                }
                else
                {
                    return assemblyversion[0] + "." + assemblyversion[1] + "." + assemblyversion[2];
                }
                
            }
            catch (InvalidDeploymentException)
            {
                // you cannot read publish version when app isn't installed 
                // (e.g. during debug)
                return Properties.Resources.lb_version;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
                return String.Empty;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            
        }
        #endregion

        #region Hot Key
        private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
        {
            Log.Debug("Hotkey: Kombination für {hk} wurde gedrückt.", e.Name);
            try
            {
                string px = text_px_nummer.Text;

                if ((bool)ts_hotkey.IsChecked && !String.IsNullOrWhiteSpace(px))
                {
                    string text = "";
                    switch (e.Name)
                    {
                        case "PxComment":
                            text = "// " + px;                           
                            PasteTextFromHotkey(text);
                            break;
                        case "PxBeginEnd":
                            text = "// " + px + " begin" + Environment.NewLine + "// " + px + " end";
                            PasteTextFromHotkey(text);
                            break;
                        case "PxPlain":
                            text = px;
                            PasteTextFromHotkey(text);
                            break;
                        case "PxBrackets":
                            text = "(" + px + ")";
                            PasteTextFromHotkey(text);
                            break;
                    }
                    e.Handled = true;
                }  
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }         
        }

        private void PasteTextFromHotkey(string text)
        {
            try
            {

                if (root_window.IsActive)
                {
                    return;
                }

                var isControlKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.CONTROL);
                var isShiftKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT);

                //Erst wenn CTRL und SHIFT wieder losgelassen werden, wird der Text eingefügt
                Log.Debug("Hotkey: Loop zum warten bis CTRL und SHIFT losgelassen werden wird gestartet.");
                do
                {
                    isShiftKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT);
                    isControlKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.CONTROL);
                } while (isControlKeyDown || isShiftKeyDown);

                sim.Keyboard.TextEntry(text);

                //if (root_window.IsActive)
                //{
                //    return;
                //}

                ////Wenn Text in der Zwischenablage ist, dann wird dieser Zwischengespeichert
                //var zwischenspeicher = String.Empty;
                //if (Clipboard.ContainsText())
                //{
                //    zwischenspeicher = Clipboard.GetText();
                //    Log.Debug("Hotkey: Zwischenablage wird temporär gespeichert.");
                //}

                //Clipboard.SetDataObject(text);
                //Log.Debug("Hotkey: Text wird kopiert. --> {hk}", text);

                //var isControlKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.CONTROL);
                //var isShiftKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT);

                ////Erst wenn CTRL und SHIFT wieder losgelassen werden, wird der Text eingefügt
                //Log.Debug("Hotkey: Loop zum warten bis CTRL und SHIFT losgelassen werden wird gestartet.");
                //do
                //{
                //    isShiftKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT);
                //    isControlKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.CONTROL);
                //} while (isControlKeyDown || isShiftKeyDown);

                //Log.Debug("Hotkey: Virtuell CTRL + V drücken.");
                ////sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                //System.Windows.Forms.SendKeys.SendWait("^v");

                ////Zwischengespeicherter Wert wieder zurück in die Zwischenablage
                //if (!String.IsNullOrWhiteSpace(zwischenspeicher))
                //{
                //    Clipboard.SetDataObject(zwischenspeicher);
                //    Log.Debug("Hotkey: Zwischenspeicher zurück in Zwischenablage.");
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            
        }

        private async void Ts_hotkey_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (((bool)ts_hotkey.IsChecked) && (String.IsNullOrWhiteSpace(text_px_nummer.Text)))
                {
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelHotkey, Properties.Resources.dialogMsgHotkey, MessageDialogStyle.Affirmative);
                    ts_hotkey.IsChecked = false;
                    await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_px_nummer.Focus()));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            
        }

        private void Cb_hotekey_plain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RegisterHotKey("PxPlain", (ComboBox)sender);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            
        }

        private void Cb_hotkey_pxBeginEnd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RegisterHotKey("PxBeginEnd", (ComboBox)sender);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Cb_hotkey_pxComment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RegisterHotKey("PxComment", (ComboBox)sender);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Cb_hotekey_brackets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RegisterHotKey("PxBrackets", (ComboBox)sender);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void RegisterHotKey(string name, ComboBox combobox)
        {
            try
            {
                if (combobox.SelectedIndex == 0)
                {
                    //Wenn kein Key selected ist, dann wird er abgemeldet
                    HotkeyManager.Current.Remove(name);
                    Log.Debug("HotKey: {name} wurde abgemeldet.", name);
                    return;
                }

                //Beim Aufstarten kann es sonst zu null reference exeptions kommen oder doppelte Registrierung
                if (InitRunning)
                {
                    return;
                }

                var key = (Key)combobox.SelectedValue;

                HotkeyManager.Current.AddOrReplace(name, key, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
                Log.Debug("HotKey: {name} wurde mit dem Zeichen {z} registriert.", name, key);
            }
            catch (HotkeyAlreadyRegisteredException exo)
            {
                combobox.SelectedIndex = 0;
                await this.ShowMessageAsync(Properties.Resources.dialogTitelHotkey, Properties.Resources.dialogMsgHotkeyFehler, MessageDialogStyle.Affirmative);
                Log.Error(exo, "Hotkey: Fehler");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Text_px_nummer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(text_px_nummer.Text))
            {
                ts_hotkey.IsChecked = false;
            }
        }
        #endregion

        #region Code Vorlage
        private void Btn_template_speichern_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text File (*.txt)|*.txt";

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, text_code_template.Text);
                    Log.Information("Code: Vorlage wurde gespeichert unter dem Pfad: {p}.", saveFileDialog.FileName);
                }
                    
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_template_offnen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Text File (*.txt)|*.txt";

                if (openFileDialog.ShowDialog() == true)
                {
                    text_code_template.Text = File.ReadAllText(openFileDialog.FileName);
                    Log.Information("Code: Vorlage wurde geöffnet: {p}.", openFileDialog.FileName);
                }    
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                //Wenn ctrl + F gedrückt wird, wird das ausgewählte Wort in das Suchfeld kopiert
                if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    text_suchen.Text = text_code_template.SelectedText;
                    text_suchen.Focus();
                    text_suchen.CaretIndex = text_suchen.Text.Length;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_template_loschen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Code: Vorlage wurde gelöscht.");
                text_code_template.Text = String.Empty;
                combo_vars.SelectedIndex = -1;
                text_code_template.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_ersetzten_ein_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Code: Vorlage ersetzten einzel wurde gedrückt.");
                Replace(text_suchen.Text, combo_vars.Text, text_code_template);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_ersetzten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(combo_vars.Text) && !String.IsNullOrWhiteSpace(text_suchen.Text))
                {
                    Log.Debug("Code: Vorlage alle ersetzten wurde gedrückt.");
                    do
                    {
                        Replace(text_suchen.Text, combo_vars.Text, text_code_template, true);
                    } while (text_code_template.Text.Contains(text_suchen.Text));
                }
               
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private int lastUsedIndex = 0;

        public void Replace(string s, string replacement, ICSharpCode.AvalonEdit.TextEditor editor, bool replaceAll = false)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }

                int nIndex = -1;

                if (editor.SelectedText.Equals(s) &! replaceAll)
                {
                    nIndex = editor.SelectionStart;
                }
                else
                {
                    nIndex = editor.Text.IndexOf(s, lastUsedIndex);
                    if (nIndex == -1)
                    {
                        nIndex = editor.Text.IndexOf(s);
                    }
                }

                if (nIndex != -1)
                {
                    editor.Document.Replace(nIndex, s.Length, replacement);
                    editor.Select(nIndex, replacement.Length);
                    lastUsedIndex = nIndex + s.Length;
                }
                else
                {
                    lastUsedIndex = 0;
                }

                if (editor.Text.Contains(s) == false)
                {
                    lastUsedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                lastUsedIndex = 0;
                Log.Error(ex, "Error");
            }
        }
        #endregion

        #region Code generieren

        private async void Btn_gen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Code: Generieren wurde gedrückt.");
                var code = Code_gen(variablen_liste[0], text_var1.Text,
                                    variablen_liste[1], text_var2.Text,
                                    variablen_liste[2], text_var3.Text,
                                    text_code_template.Text);

                if (!String.IsNullOrWhiteSpace(code.error))
                {
                    Log.Warning("Code: Generieren Fehler \"{F}\" ist aufgetreten.", code.error);
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelCodeGen, code.error, MessageDialogStyle.Affirmative);
                }
                else
                {
                    text_code_output.Text = code.outtext;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private (string outtext, string error) Code_gen(string var_1, string var_1_text,
                                                        string var_2, string var_2_text,
                                                        string var_3, string var_3_text,
                                                        string template)
        {
            string[] split = new string[] { "\r\n" };
            string[] vars_1 = var_1_text.Split(split, StringSplitOptions.RemoveEmptyEntries);
            string[] vars_2 = var_2_text.Split(split, StringSplitOptions.RemoveEmptyEntries);
            string[] vars_3 = var_3_text.Split(split, StringSplitOptions.RemoveEmptyEntries);

            string outtext = "";

            try
            {
                var lines1 = vars_1.Length;
                var lines2 = vars_2.Length;
                var lines3 = vars_3.Length;

                var var1_vorhanden = !String.IsNullOrWhiteSpace(var_1_text);
                var var2_vorhanden = !String.IsNullOrWhiteSpace(var_2_text);
                var var3_vorhanden = !String.IsNullOrWhiteSpace(var_3_text);

                
                //Keine Variable in Liste 1 aber "Varibale_1" in Template
                if (lines1 == 0 && template.Contains(var_1))
                {
                    return (outtext, Properties.Resources.dialogMsgCodeGen01 + " 1");
                }

                //Keine Variable in Liste 2 aber "Varibale_2" in Template
                if (lines2 == 0 && template.Contains(var_2))
                {
                    return (outtext, Properties.Resources.dialogMsgCodeGen01 + " 2");
                }

                //Keine Variable in Liste 3 aber "Varibale_3" in Template
                if (lines3 == 0 && template.Contains(var_3))
                {
                    return (outtext, Properties.Resources.dialogMsgCodeGen01 + " 3");
                }

                ////Keine "Varibale_1" in Template aber in Liste
                //if (lines1 > 0 && !template.Contains(var_1))
                //{
                //    return (outtext, Properties.Resources.dialogMsgCodeGen02 + " 1");
                //}

                ////Keine "Varibale_2" in Template aber in Liste
                //if (lines2 > 0 && !template.Contains(var_2))
                //{
                //    return (outtext, Properties.Resources.dialogMsgCodeGen02 + " 2");
                //}

                ////Keine "Varibale_3" in Template aber in Liste
                //if (lines3 > 0 && !template.Contains(var_3))
                //{
                //    return (outtext, Properties.Resources.dialogMsgCodeGen02 + " 3");
                //}

                //Die Anzahl Variablen in den Listen ist unterschiedlich und enthalten auch Text
                if ((var1_vorhanden && var2_vorhanden && lines1 != lines2) ||
                     (var1_vorhanden && var3_vorhanden && lines1 != lines3) ||
                     (var2_vorhanden && var3_vorhanden && lines2 != lines3)
                   )
                {
                    return (outtext, Properties.Resources.dialogMsgCodeGen00);
                }

                var lines = Math.Max(Math.Max(lines1, lines2), lines3);

                for (int i = 0; i < lines; i++)
                {
                    string temp_text = template;

                    if (!String.IsNullOrWhiteSpace(var_1_text))
                    {
                        temp_text = temp_text.Replace(var_1, vars_1[i]);
                    }

                    if (!String.IsNullOrWhiteSpace(var_2_text))
                    {
                        temp_text = temp_text.Replace(var_2, vars_2[i]);
                    }

                    if (!String.IsNullOrWhiteSpace(var_3_text))
                    {
                        temp_text = temp_text.Replace(var_3, vars_3[i]);
                    }

                    if (outtext == "")
                    {
                        outtext = temp_text;
                    }
                    else
                    {
                        outtext = outtext + Environment.NewLine + temp_text;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
                return (outtext, Properties.Resources.dialogMsgCodeGen03);
            }
            return (outtext, String.Empty);
        }

        private void Btn_gen_loschen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                text_code_output.Text = String.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_gen_speichern_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text File (*.txt)|*.txt";

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, text_code_output.Text);
                    Log.Information("Code: Generierter Code wurde gespeichter unter {p}", saveFileDialog.FileName);
                }
                    
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }
        #endregion

        #region Einstellungen

        private void Tg_leerzeichen_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                text_code_template.Options.ShowSpaces = (bool)tg_leerzeichen.IsChecked;
                text_code_output.Options.ShowSpaces = (bool)tg_leerzeichen.IsChecked;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            try
            {
                text_code_template.TextArea.FontSize = (double)nc_font_size.Value;
                text_code_output.TextArea.FontSize = (double)nc_font_size.Value;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Tg_line_no_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                text_code_template.ShowLineNumbers = (bool)tg_line_no.IsChecked;
                text_code_output.ShowLineNumbers = (bool)tg_line_no.IsChecked;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Tg_showtab_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                text_code_template.Options.ShowTabs = (bool)tg_showtab.IsChecked;
                text_code_output.Options.ShowTabs = (bool)tg_showtab.IsChecked;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Tg_showendofline_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                text_code_template.Options.ShowEndOfLine = (bool)tg_showendofline.IsChecked;
                text_code_output.Options.ShowEndOfLine = (bool)tg_showendofline.IsChecked;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }          
        }

        private void Tg_converttospace_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                text_code_template.Options.ConvertTabsToSpaces = (bool)tg_converttospace.IsChecked;
                text_code_output.Options.ConvertTabsToSpaces = (bool)tg_converttospace.IsChecked;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }            
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
            try
            {
                if (WindowState == WindowState.Maximized)
                {
                    // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                    Properties.Settings.Default.fentser_top = RestoreBounds.Top;
                    Properties.Settings.Default.fenster_left = RestoreBounds.Left;
                    Properties.Settings.Default.fenster_hohe = RestoreBounds.Height;
                    Properties.Settings.Default.fenster_breite = RestoreBounds.Width;
                    Properties.Settings.Default.fenster_max = true;
                }
                else
                {
                    Properties.Settings.Default.fentser_top = this.Top;
                    Properties.Settings.Default.fenster_left = this.Left;
                    Properties.Settings.Default.fenster_hohe = this.Height;
                    Properties.Settings.Default.fenster_breite = this.Width;
                    Properties.Settings.Default.fenster_max = false;
                }

                if ((bool)tg_theme.IsChecked)
                {
                    Properties.Settings.Default.theme = "BaseDark";
                }
                else
                {
                    Properties.Settings.Default.theme = "BaseLight";
                }

                Properties.Settings.Default.variable_1 = text_var1.Text;
                Properties.Settings.Default.variable_2 = text_var2.Text;
                Properties.Settings.Default.variable_3 = text_var3.Text;
                Properties.Settings.Default.vorlage = text_code_template.Text;
                Properties.Settings.Default.leerzeichen = (bool)tg_leerzeichen.IsChecked;
                Properties.Settings.Default.converttabtospace = (bool)tg_converttospace.IsChecked;
                Properties.Settings.Default.showtab = (bool)tg_showtab.IsChecked;
                Properties.Settings.Default.showendofline = (bool)tg_showendofline.IsChecked;
                Properties.Settings.Default.tabcontrol_index = tc_root.SelectedIndex;
                Properties.Settings.Default.schriftgrosse = (double)nc_font_size.Value;
                Properties.Settings.Default.zeilennummern = (bool)tg_line_no.IsChecked;
                Properties.Settings.Default.projekt_pfad_suche = text_projktpfad_suche.Text;
                Properties.Settings.Default.projekt_pfad_helfer = text_projktpfad_helfer.Text;
                Properties.Settings.Default.projekt_pfad_dataview = text_projktpfad_dataview.Text;
                Properties.Settings.Default.projekt_pfad_kundenordner = text_kundenprojekt.Text;
                Properties.Settings.Default.kundenspez = text_kundenspez.Text;
                Properties.Settings.Default.sql_connection_string = text_db_connectionstring.Text;
                Properties.Settings.Default.akzentfarbe = cb_akzent_farbe.SelectedValue.ToString();
                Properties.Settings.Default.hotkey = (bool)ts_hotkey.IsChecked;
                Properties.Settings.Default.pxnummer = text_px_nummer.Text;
                Properties.Settings.Default.hotkey_beginend = cb_hotkey_pxBeginEnd.SelectedValue.ToString();
                Properties.Settings.Default.hotkey_plain = cb_hotekey_plain.SelectedValue.ToString();
                Properties.Settings.Default.hotkey_comment = cb_hotkey_pxComment.SelectedValue.ToString();
                Properties.Settings.Default.hotkey_brackets = cb_hotekey_brackets.SelectedValue.ToString();
                if (cb_select_me.SelectedIndex == -1)
                {
                    Properties.Settings.Default.me_auswahl = "";
                }
                else
                {
                    Properties.Settings.Default.me_auswahl = cb_select_me.SelectedValue.ToString();
                }
                Properties.Settings.Default.file_ext_user = text_file_ext.Text;
                Properties.Settings.Default.exakte_suche = (bool)ts_exakte_suche.IsChecked;
                Properties.Settings.Default.offnen_mit_nppp = (bool)ts_offnen_nppp.IsChecked;

                Properties.Settings.Default.Save();

                HotkeyManager.Current.Remove("PxBeginEnd");
                HotkeyManager.Current.Remove("PxPlain");
                HotkeyManager.Current.Remove("PxComment");
                HotkeyManager.Current.Remove("PxBrackets");

                Log.Information("Einstellung: Alle Einstellung gespeichert.");
                Log.Information("Einstellung: IEC Mate wurde durch Anwender beendet.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Cb_akzent_farbe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string theme;
                if ((bool)tg_theme.IsChecked)
                {
                    theme = "BaseDark";
                }
                else
                {
                    theme = "BaseLight";
                }

                ThemeManager.ChangeAppStyle(Application.Current,
                                            ThemeManager.GetAccent(cb_akzent_farbe.SelectedValue.ToString()),
                                            ThemeManager.GetAppTheme(theme));

                //Decode Matrix von Bitset neu Aufbauen damit die Farben stimmen
                DecodeText();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Tg_theme_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if ((bool)tg_theme.IsChecked)
                {
                    ThemeManager.ChangeAppStyle(Application.Current,
                                                ThemeManager.GetAccent(cb_akzent_farbe.SelectedValue.ToString()),
                                                ThemeManager.GetAppTheme("BaseDark"));
                }
                else
                {
                    ThemeManager.ChangeAppStyle(Application.Current,
                                                ThemeManager.GetAccent(cb_akzent_farbe.SelectedValue.ToString()),
                                                ThemeManager.GetAppTheme("BaseLight"));
                }

                //Decode Matrix von Bitset neu Aufbauen
                DecodeText();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void Cb_sprache_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string setting = String.Empty;

                if (Properties.Settings.Default.sprache == "de-DE")
                {
                    setting = Properties.Resources.lanDE;
                }

                if (Properties.Settings.Default.sprache == "en-GB")
                {
                    setting = Properties.Resources.lanEN;
                }

                if (cb_sprache.SelectedValue.ToString() == Properties.Resources.lanDE && cb_sprache.SelectedValue.ToString() != setting)
                {
                    var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = Properties.Resources.dialogNegButton };
                    var result = await this.ShowMessageAsync(Properties.Resources.lb_sprache, Properties.Resources.dialogMsgSpracheUmschalten, MessageDialogStyle.AffirmativeAndNegative, mymessageboxsettings);

                    if (!(result == MessageDialogResult.Affirmative))
                    {
                        cb_sprache.Text = setting;
                        return;
                    }
                    else
                    {
                        Properties.Settings.Default.sprache = "de-DE";
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        Log.Debug("Einstellung: Spache wird umgeschaltet nach {l}.", Properties.Settings.Default.sprache);
                        Application.Current.Shutdown();
                    }

                }

                if (cb_sprache.SelectedValue.ToString() == Properties.Resources.lanEN && cb_sprache.SelectedValue.ToString() != setting)
                {
                    var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = Properties.Resources.dialogNegButton };
                    var result = await this.ShowMessageAsync(Properties.Resources.lb_sprache, Properties.Resources.dialogMsgSpracheUmschalten, MessageDialogStyle.AffirmativeAndNegative, mymessageboxsettings);

                    if (!(result == MessageDialogResult.Affirmative))
                    {
                        cb_sprache.Text = setting;
                        return;
                    }
                    else
                    {
                        Properties.Settings.Default.sprache = "en-GB";
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        Log.Debug("Einstellung: Spache wird umgeschaltet nach {l}.", Properties.Settings.Default.sprache);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_default_ext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                text_file_ext.Text = Properties.Settings.Default.file_ext_default;
                text_file_ext.Focus();
                text_file_ext.CaretIndex = text_file_ext.Text.Length;
                Log.Information("Suche: Default Dateiendungen wurden geladen.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_default_db_connectionstring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                text_db_connectionstring.Text = Properties.Settings.Default.sql_connection_string_default;
                text_db_connectionstring.Focus();
                text_db_connectionstring.CaretIndex = text_db_connectionstring.Text.Length;
                Log.Information("DataView: Default connectionstring wurden geladen.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }

        }

        private void Btn_logfile_offnen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(TempFolder + "\\Log");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }
        #endregion

        #region Suchfunktion
        private void Btn_pfad_auswahlen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
                folderDialog.ShowNewFolderButton = false;
                if (Directory.Exists(text_projktpfad_suche.Text))
                {
                    folderDialog.SelectedPath = text_projktpfad_suche.Text;
                }
                else
                {
                    folderDialog.SelectedPath = Properties.Paths.drive_c;
                }

                WinForms.DialogResult result = folderDialog.ShowDialog();

                if (result == WinForms.DialogResult.OK)
                {
                    text_projktpfad_suche.Text = folderDialog.SelectedPath;
                    Log.Information("Suche: Suchpfad wurde auf {p} festgelegt.", folderDialog.SelectedPath);
                }
                text_pattern_suche.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        public class SucheDatei
        {
            public string Pfad { get; set; }

            public string Typ { get; set; }

            public string Linie { get; set; }

            public int LinieInt { get; set; }

        }

        public ObservableCollection<SucheDatei> suchdatei = new ObservableCollection<SucheDatei>();

        private async void Suche()
        {
            try
            {
                //Listebox löschen
                suchdatei.Clear();
                listbox_ergebnis.ItemsSource = suchdatei;

                //Wennn etwas im Suchfeld steht und das Verzeichnis existiert und es nicht zwei Wörter sind dann wird gesucht
                if ((!String.IsNullOrWhiteSpace(text_pattern_suche.Text)) && (Directory.Exists(text_projktpfad_suche.Text)) && !text_pattern_suche.Text.Contains(" "))
                {
                    //Dialog öffnen
                    var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = Properties.Resources.dialogNegButton };
                    var x = await this.ShowProgressAsync(Properties.Resources.dialogTitelSuche, Properties.Resources.dialogMsgSucheLauft, true, mymessageboxsettings) as ProgressDialogController;
                    double percent = 0;
                    double filecount = 0;
                    double count = 0;
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    x.SetProgress(percent);

                    try
                    {
                        List<string> allFiles = new List<string>();
                        string suchpfad = "";

                        if ((bool)ts_kbus_suche.IsChecked)
                        {
                            //Wenn der Switch "Nur HW suche" ein ist, wird der Pfad angepasst
                            suchpfad = text_projktpfad_suche.Text + Properties.Paths.config;
                            suchpfad = suchpfad.Replace("\\\\", "\\");
                            //Überprüfen ob Pfad existiert, wenn nicht, gibt es eine exeption
                            Directory.Exists(suchpfad);
                        }
                        else if ((bool)ts_xml_suche.IsChecked)
                        {
                            //Wenn der Switch "Nur HMI xml Datein durchsuchen" ein ist, wird der Pfad angepasst
                            suchpfad = text_projktpfad_suche.Text + Properties.Paths.view;
                            suchpfad = suchpfad.Replace("\\\\", "\\");
                            //Überprüfen ob Pfad existiert, wenn nicht, gibt es eine exeption
                            Directory.Exists(suchpfad);
                        }
                        else
                        {
                            suchpfad = text_projktpfad_suche.Text;
                        }
                       
                        AddFileNamesToList(suchpfad, allFiles, (bool)ts_binar_suche.IsChecked, (bool)ts_java_suche.IsChecked, (bool)ts_xml_suche.IsChecked);
                        filecount = allFiles.Count();
                        count = 0;

                        //Sobald der Dialog offen ist wird mit der suche gestartet
                        if (x.IsOpen)
                        {
                            Log.Debug("Suche: Suche gestartet im Pfad {p} mit den Optionen HW: {kb}, JAVA: {j} und XML: {xml} Wort: {w}", 
                                suchpfad, ts_kbus_suche.IsChecked, ts_java_suche.IsChecked, ts_xml_suche.IsChecked, ts_exakte_suche.IsChecked);

                            foreach (string fileName in allFiles)
                            {
                                count++;
                                percent = 100 / filecount * count / 100;
                                if (percent > 1.0)
                                {
                                    percent = 1.0;
                                }
                                x.SetProgress(percent);

                                if (x.IsCanceled)
                                {
                                    Log.Debug("Suche: Suche wurde abgebrochen.");
                                    break;
                                }

                                try
                                {
                                    using (var reader = File.OpenText(fileName))
                                    {
                                        var fileText = await reader.ReadToEndAsync();
                                        if ((bool)ts_exakte_suche.IsChecked)
                                        {
                                            //Der \b ist ein Wortgrenzen-Check, {0} ist die Variable --> Format \bsvDI_BlaFo\b
                                            if (Regex.IsMatch(fileText, string.Format(@"\b{0}\b", Regex.Escape(text_pattern_suche.Text)), RegexOptions.IgnoreCase))
                                            {
                                                //Hier wird der Text des Files Linie für Linie anaylsiert und hochgezählt
                                                //Sobald der Erste treffer da ist, wird der Loop beendet
                                                int _count = 0;
                                                using (StringReader _reader = new StringReader(fileText))
                                                {
                                                    string line;
                                                    while ((line = _reader.ReadLine()) != null)
                                                    {
                                                        _count++;
                                                        if (Regex.IsMatch(line, string.Format(@"\b{0}\b", Regex.Escape(text_pattern_suche.Text)), RegexOptions.IgnoreCase))
                                                        {
                                                            break;
                                                        }
                                                    }
                                                }
                                                string linie = Properties.Resources.suche_erster_treffer + " " + _count.ToString();
                                                suchdatei.Add(new SucheDatei() { Pfad = fileName, Typ = Path.GetExtension(fileName), Linie = linie, LinieInt = _count });
                                            }
                                        }
                                        else
                                        {
                                            //Nicht Case Sensitive
                                            if (fileText.IndexOf(text_pattern_suche.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                //Hier wird der Text des Files Linie für Linie anaylsiert und hochgezählt
                                                //Sobald der Erste treffer da ist, wird der Loop beendet
                                                int _count = 0;
                                                using (StringReader _reader = new StringReader(fileText))
                                                {
                                                    string line;
                                                    while ((line = _reader.ReadLine()) != null)
                                                    {
                                                        _count++;
                                                        if (line.IndexOf(text_pattern_suche.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                                                        {
                                                            break;
                                                        }
                                                    }
                                                }
                                                string linie = Properties.Resources.suche_erster_treffer + " " + _count.ToString();
                                                suchdatei.Add(new SucheDatei() { Pfad = fileName, Typ = Path.GetExtension(fileName), Linie = linie, LinieInt = _count });
                                            }
                                        }
                                    }
                                }
                                catch (Exception exx)
                                {
                                    Log.Error(exx, "Error");
                                }
                               
                                TimeSpan timeSpan = stopWatch.Elapsed;
                                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                                text_suche_count.Text = Properties.Resources.suche_dateien + count.ToString() + "/" + filecount.ToString() +
                                                        "   " + Properties.Resources.suche_gefunden + suchdatei.Count.ToString() +
                                                        "   " + Properties.Resources.suche_zeit + elapsedTime;


                            }
                        }
                    }
                    catch (Exception exs)
                    {
                        await this.ShowMessageAsync(Properties.Resources.dialogTitelSuche, Properties.Resources.dialogMsgSucheVerzeichnisFehler, MessageDialogStyle.Affirmative);
                        await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
                        stopWatch.Stop();
                        Log.Error(exs, "Error");
                    }

                    await x.CloseAsync();
                    stopWatch.Stop();
                }
                else
                {
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelSuche, Properties.Resources.dialogMsgSucheLeer, MessageDialogStyle.Affirmative);
                    await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
                    text_suche_count.Text = String.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            
        }

        private void Btn_suche_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Suche();
                text_pattern_suche.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        public void AddFileNamesToList(string sourceDir, List<string> allFiles, bool bin, bool java, bool xml)
        {
            try
            {
                IEnumerable<string> fileEntries = Enumerable.Empty<string>();
                if (bin)
                {
                    //Alle Dateien werden zurückgegeben
                    fileEntries = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                }

                if (java)
                {
                    //Nur *.java Dateien werden zurückgegeben
                    fileEntries = Directory.GetFiles(sourceDir, "*.java", SearchOption.AllDirectories);
                }

                if (xml)
                {
                    //Nur *.xml Dateien aus den Verzeichnisen application\view\me\hmi\text werden zurückgegeben
                    fileEntries = Directory.GetFiles(sourceDir, "*.xml", SearchOption.AllDirectories);
                }

                if (!bin && !java && !xml)
                {
                    //Alle Dateien ausser die ausgeschlossenen werden zurückgegeben
                    var exttemp = text_file_ext.Text.Split(' ').ToList();
                    var ext = new List<string>();
                    foreach (var item in exttemp)
                    {
                        if (!item.StartsWith("."))
                        {
                            ext.Add("." + item);
                        }
                        else
                        {
                            ext.Add(item);
                        }
                    }

                    fileEntries = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories).Where(name => !ext.Contains(Path.GetExtension(name)));
                }

                foreach (string fileName in fileEntries)
                {
                    allFiles.Add(fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Text_pattern_suche_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                //Wenn Enter gedrückt wird, wird das Suchen ausgelöst
                if (e.Key == Key.Enter)
                {
                    Suche();
                    text_pattern_suche.Focus();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void Listbox_ergebnis_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.OriginalSource is DataGridRowHeader)
                {
                    return;
                }
                if (listbox_ergebnis.SelectedIndex > -1)
                {
                    try
                    {
                        DataGridRow dg = sender as DataGridRow;
                        SucheDatei row = (SucheDatei)dg.Item;
                        var pfad = row.Pfad;
                        var zeile = row.LinieInt;

                        if ((bool)ts_offnen_nppp.IsChecked)
                        {
                            //Wenn Notepad++ vorhanden ist dann wird bei Doppelklick die korrekte Zeile geöffnet
                            try
                            {
                                OpenFileNotepadPp(pfad, zeile);
                                Log.Information("Suche: Datei {d} wird mit Notepad++ bei Zeile {z} geöffnet.", pfad, zeile);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    //Wenn es einen Fehler gibt, dann ohne Zeile mit n++ öffen
                                    Process.Start("notepad++", pfad);
                                    Log.Information("Suche: Datei {d} wird mit Notepad++ geöffnet.", pfad);
                                }
                                catch (Exception)
                                {
                                    //Wenn n++ nicht vorhanden dann mit notepad öffnen, nicht mit IEC edit
                                    Process.Start("notepad", pfad);
                                    Log.Information("Suche: Datei {d} wird mit Notepad geöffnet.", pfad);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Process.Start("notepad++", pfad);
                                Log.Information("Suche: Datei {d} wird mit Notepad++ geöffnet.", pfad);
                            }
                            catch (Exception)
                            {
                                //Wenn n++ nicht vorhanden dann mit notepad öffnen, nicht mit IEC edit
                                Process.Start("notepad", pfad);
                                Log.Information("Suche: Datei {d} wird mit Notepad geöffnet.", pfad);
                            }
                        }
                    }
                    catch (Exception exo)
                    {
                        await this.ShowMessageAsync(Properties.Resources.dialogTitelDateiOffnen, Properties.Resources.dialogMsgDateiOffnenFehler, MessageDialogStyle.Affirmative);
                        Log.Error(exo, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void OpenFileNotepadPp(string pfad, int zeile)
        {
            try
            {
                Process process = new Process();
                ProcessStartInfo procInfo = new ProcessStartInfo()
                {
                    FileName = "notepad++.exe",
                    Arguments = "-n" + zeile + " " + pfad
                };
                process.StartInfo = procInfo;
                process.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void Mi_open_file_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listbox_ergebnis.SelectedIndex > -1)
                {
                    try
                    {
                        SucheDatei row = (SucheDatei)listbox_ergebnis.SelectedItems[0];
                        var pfad = row.Pfad;
                        var zeile = row.LinieInt;

                        if ((bool)ts_offnen_nppp.IsChecked)
                        {
                            //Wenn Notepad++ vorhanden ist dann wird bei Doppelklick die korrekte Zeile geöffnet
                            try
                            {
                                OpenFileNotepadPp(pfad, zeile);
                                Log.Information("Suche: Datei {d} wird mit Notepad++ bei Zeile {z} geöffnet.", pfad, zeile);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    //Wenn es einen Fehler gibt, dann ohne Zeile mit n++ öffen
                                    Process.Start("notepad++", pfad);
                                    Log.Information("Suche: Datei {d} wird mit Notepad++ geöffnet.", pfad);
                                }
                                catch (Exception)
                                {
                                    //Wenn n++ nicht vorhanden dann mit notepad öffnen, nicht mit IEC edit
                                    Process.Start("notepad", pfad);
                                    Log.Information("Suche: Datei {d} wird mit Notepad geöffnet.", pfad);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Process.Start("notepad++", pfad);
                                Log.Information("Suche: Datei {d} wird mit Notepad++ geöffnet.", pfad);
                            }
                            catch (Exception)
                            {
                                //Wenn n++ nicht vorhanden dann mit notepad öffnen, nicht mit IEC edit
                                Process.Start("notepad", pfad);
                                Log.Information("Suche: Datei {d} wird mit Notepad geöffnet.", pfad);
                            }
                        }
                    }
                    catch (Exception exo)
                    {
                        await this.ShowMessageAsync(Properties.Resources.dialogTitelDateiOffnen, Properties.Resources.dialogMsgDateiOffnenFehler, MessageDialogStyle.Affirmative);
                        Log.Error(exo, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void Mi_open_folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listbox_ergebnis.SelectedIndex > -1)
                {
                    try
                    {
                        SucheDatei row = (SucheDatei)listbox_ergebnis.SelectedItems[0];
                        Process.Start(Path.GetDirectoryName(row.Pfad));
                        Log.Information("Suche: Ordner {p} wurde geöffnet.", Path.GetDirectoryName(row.Pfad));
                    }
                    catch (Exception exo)
                    {
                        await this.ShowMessageAsync(Properties.Resources.dialogTitelDateiOffnen, Properties.Resources.dialogMsgDateiOffnenFehler, MessageDialogStyle.Affirmative);
                        Log.Error(exo, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Ts_exakte_suche_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if ((bool)ts_binar_suche.IsChecked)
                {
                    text_file_ext.IsEnabled = false;
                    text_file_ext.IsReadOnly = true;
                    btn_default_ext.IsEnabled = false;
                }
                else
                {
                    text_file_ext.IsEnabled = true;
                    text_file_ext.IsReadOnly = false;
                    btn_default_ext.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Text_projktpfad_suche_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if ((bool)ts_hw_suchvorschalg.IsChecked)
                {
                    text_pattern_suche.ItemsSource = FilterIO();
                }

                //Hier wird einfach überprüft ob der Dateipfad zu \config vorhanden ist
                //Wenn ja, wird davon ausgegangen das es sich um ein IEC Projekt handelt
                var suchpfad = text_projktpfad_suche.Text + Properties.Paths.config;
                suchpfad = suchpfad.Replace("\\\\", "\\");
                if (Directory.Exists(suchpfad))
                {
                    SucheIstIecProjekt = true;
                }
                else
                {
                    SucheIstIecProjekt = false;
                }

                Log.Information("Suche: IEC Projekt Pfad ausgewählt {v}.", SucheIstIecProjekt);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Ts_hw_suchvorschalg_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!(bool)ts_hw_suchvorschalg.IsChecked)
                {
                    text_pattern_suche.ItemsSource = new List<string>();
                }
                else
                {
                    text_pattern_suche.ItemsSource = FilterIO();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private List<string> FilterIO()
        {
            var ids = new List<string>();
            try
            {
                //Hier wird versucht die IO's auf den Konfig File zu Indexieren
                //Dies geschieht allerdings nur wenn auch ein IEC Projekt ausgewählt ist
                var suchpfad = text_projktpfad_suche.Text + Properties.Paths.config;
                suchpfad = suchpfad.Replace("\\\\", "\\");

                //Überprüfen ob Pfad existiert, wenn nicht, gibt es eine exeption
                if (Directory.Exists(suchpfad))
                {
                    try
                    {
                        List<string> allFilesTemp = new List<string>();
                        List<string> allFiles = new List<string>();

                        AddFileNamesToList(suchpfad, allFilesTemp, false, false, false);

                        foreach (var file in allFilesTemp)
                        {
                            if (file.EndsWith(".cfg") || file.EndsWith(".CFG"))
                            {
                                allFiles.Add(file);
                            }
                        }

                        foreach (var file in allFiles)
                        {
                            using (var sr = new StreamReader(file, true))
                            {
                                var s = "";
                                while ((s = sr.ReadLine()) != null)
                                {
                                    if (s.StartsWith("name"))
                                    {
                                        if (s.Contains("svDI") || s.Contains("svDO") || s.Contains("svAI") || s.Contains("svAO"))
                                        {
                                            string[] tokens = s.Split('.');
                                            string[] tok = tokens[1].Split('"');
                                            string io = tok[0].Replace("\"", "");
                                            ids.Add(io);
                                        }
                                    }
                                }
                            }
                        }
                        ids.Sort();
                        return ids;
                    }
                    catch (Exception)
                    {
                        ids.Clear();
                        return ids;
                    }
                }
                ids.Clear();
                return ids;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
                return ids;
            }
        }

        private void Ts_kbus_suche_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if ((bool)ts_kbus_suche.IsChecked)
                {
                    ts_xml_suche.IsChecked = false;
                    ts_java_suche.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Ts_xml_suche_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if ((bool)ts_xml_suche.IsChecked)
                {
                    ts_kbus_suche.IsChecked = false;
                    ts_java_suche.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Ts_java_suche_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if ((bool)ts_java_suche.IsChecked)
                {
                    ts_kbus_suche.IsChecked = false;
                    ts_xml_suche.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }         
        }
        #endregion

        #region Bitset
        private void Encoding_Checked(object sender, RoutedEventArgs args)
        {
            try
            {
                long ResulateDezimal = 0;
                //Alle Objekte vom Grid holen
                var objects = grid_encoding.GetChildObjects();
                foreach (object child in objects)
                {
                    if (child.GetType() == typeof(StackPanel))
                    {
                        StackPanel ch = child as StackPanel;
                        var obj = ch.GetChildObjects();
                        foreach (object item in obj)
                        {
                            if (item.GetType() == typeof(ToggleButton))
                            {
                                ToggleButton tb = item as ToggleButton;
                                if ((bool)tb.IsChecked)
                                {
                                    int bit = Int32.Parse(tb.Content.ToString());
                                    ResulateDezimal = ResulateDezimal + Convert.ToInt64(Math.Pow(2, bit));
                                }
                            }
                        }
                    }
                }

                string binary = Convert.ToString(ResulateDezimal, 2);
                text_encode_dec.Text = ResulateDezimal.ToString();
                text_encode_hex.Text = "16#" + ResulateDezimal.ToString("X");
                text_encode_bin.Text = "2#" + binary;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        public void DecodeText()
        {
            try
            {
                //Wenn alles gelöscht wird
                if (String.IsNullOrWhiteSpace(text_decode.Text))
                {
                    List<char> BinList = new List<char>();
                    BinList.Insert(0, '0');
                    ShowDecodeResult(BinList);

                    text_decode_out1.Text = String.Empty;
                    text_decode_out2.Text = String.Empty;
                    return;
                }

                //Hex Zahl wurde eingegeben
                if (text_decode.Text.StartsWith("16#"))
                {
                    var hex = text_decode.Text.Split('#')[1];

                    if (!String.IsNullOrWhiteSpace(hex))
                    {

                        if (!System.Text.RegularExpressions.Regex.IsMatch(hex, @"\A\b[0-9a-fA-F]+\b\Z"))
                        {
                            ShowFehlerBitsetAsync(Properties.Resources.dialogMsgBitsetHexFehler);
                            return;
                        }

                        if (Convert.ToInt64(hex, 16) > 4294967295)
                        {
                            ShowFehlerBitsetAsync(Properties.Resources.dialogMsgBitsetGrosseFehler);
                            return;
                        }

                        List<char> BinList = new List<char>();
                        foreach (var bit in Convert.ToString(Convert.ToInt64(Convert.ToInt64(hex, 16)), 2))
                        {
                            BinList.Insert(0, bit);
                        }
                        ShowDecodeResult(BinList);

                        text_decode_out1.Text = "2#" + Convert.ToString(Convert.ToInt64(hex, 16), 2);
                        text_decode_out2.Text = "10#" + Convert.ToString(Convert.ToInt64(hex, 16), 10);
                    }
                    else
                    {
                        List<char> BinList = new List<char>();
                        BinList.Insert(0, '0');
                        ShowDecodeResult(BinList);

                        text_decode_out1.Text = String.Empty;
                        text_decode_out2.Text = String.Empty;
                    }
                    return;
                }

                //Binärzahl wurde eingegeben
                if (text_decode.Text.StartsWith("2#"))
                {
                    var bin = text_decode.Text.Split('#')[1];

                    if (!String.IsNullOrWhiteSpace(bin))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(bin, @"\A\b[0-1]+\b\Z"))
                        {
                            ShowFehlerBitsetAsync(Properties.Resources.dialogMsgBitsetBinFehler);
                            return;
                        }

                        if (Convert.ToInt64(bin, 2) > 4294967295)
                        {
                            ShowFehlerBitsetAsync(Properties.Resources.dialogMsgBitsetGrosseFehler);
                            return;
                        }

                        List<char> BinList = new List<char>();
                        foreach (var bit in bin)
                        {
                            BinList.Insert(0, bit);
                        }
                        ShowDecodeResult(BinList);

                        text_decode_out1.Text = "10#" + Convert.ToInt64(bin, 2).ToString();
                        text_decode_out2.Text = "16#" + Convert.ToInt64(bin, 2).ToString("X");
                    }
                    else
                    {
                        List<char> BinList = new List<char>();
                        BinList.Insert(0, '0');
                        ShowDecodeResult(BinList);

                        text_decode_out1.Text = String.Empty;
                        text_decode_out2.Text = String.Empty;
                    }
                    return;
                }

                //Dezimal Zahl
                string dez = "";
                if (text_decode.Text.StartsWith("10#"))
                {
                    dez = text_decode.Text.Split('#')[1];
                }
                else
                {
                    dez = text_decode.Text;
                }

                if (!String.IsNullOrWhiteSpace(dez))
                {
                    if (!dez.All(Char.IsDigit))
                    {
                        ShowFehlerBitsetAsync(Properties.Resources.dialogMsgBitsetBinFehler);
                        return;
                    }

                    if (Convert.ToInt64(dez) > 4294967295)
                    {
                        ShowFehlerBitsetAsync(Properties.Resources.dialogMsgBitsetDezFehler);
                        return;
                    }

                    List<char> BinList = new List<char>();
                    foreach (var bit in Convert.ToString(Convert.ToInt64(dez), 2))
                    {
                        BinList.Insert(0, bit);
                    }
                    ShowDecodeResult(BinList);
                    text_decode_out1.Text = "2#" + Convert.ToString(Convert.ToInt64(dez, 10), 2);
                    text_decode_out2.Text = "16#" + Convert.ToInt64(dez, 10).ToString("X");
                    return;
                }
                else
                {
                    List<char> BinList = new List<char>();
                    BinList.Insert(0, '0');
                    ShowDecodeResult(BinList);

                    text_decode_out1.Text = String.Empty;
                    text_decode_out2.Text = String.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Text_decode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DecodeText();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void ShowFehlerBitsetAsync(string message)
        {
            try
            {
                Log.Debug("Bitset: Fehler \"{f}\" aufgetreten.", message);
                text_decode.IsEnabled = false;
                MessageDialogResult result = await this.ShowMessageAsync(Properties.Resources.dialogTitelBitset, message, MessageDialogStyle.Affirmative);

                if (result == MessageDialogResult.Affirmative)
                {
                    text_decode.IsEnabled = true;
                    await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_decode.Focus()));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void ShowDecodeResult(List<char> list)
        {
            try
            {
                var converter = new System.Windows.Media.BrushConverter();
                //Akzentfarbe von Theme
                var accentColor = (Brush)converter.ConvertFromString(ThemeManager.GetResourceFromAppStyle(this, "AccentColor").ToString());
                var accentColor2 = (Brush)converter.ConvertFromString(ThemeManager.GetResourceFromAppStyle(this, "AccentColor2").ToString());
                //Alle Objekte von Grid
                var objects = grid_decoding.GetChildObjects();

                foreach (object child in objects)
                {
                    if (child.GetType() == typeof(StackPanel))
                    {
                        StackPanel ch = child as StackPanel;
                        var obj = ch.GetChildObjects();
                        foreach (object item in obj)
                        {
                            if (item.GetType() == typeof(Grid))
                            {
                                Grid grid = item as Grid;
                                var gr = grid.GetChildObjects();

                                foreach (object it in gr)
                                {
                                    //Wennn die Eingabe gelöscht wird dann zurücksetzten
                                    if (it.GetType() == typeof(Ellipse))
                                    {
                                        Ellipse el = it as Ellipse;
                                        el.Fill = Brushes.Transparent;
                                        el.Stroke = Brushes.Silver;
                                    }

                                    //Schriftfarbe abhängig von Theme einstellen
                                    if (it.GetType() == typeof(TextBlock))
                                    {
                                        TextBlock el = it as TextBlock;

                                        if ((bool)tg_theme.IsChecked)
                                        {
                                            el.Foreground = Brushes.White;
                                        }
                                        else
                                        {
                                            el.Foreground = Brushes.Black;
                                        }
                                    }

                                    //Abhängig vom Bit in der Liste Elippse mit Akzentfarbe füllen
                                    if (it.GetType() == typeof(Ellipse))
                                    {
                                        Ellipse el = it as Ellipse;
                                        int bit = Int32.Parse(el.Name.Replace("el_bit", ""));

                                        var len = list.Count();

                                        if (bit < len)
                                        {
                                            if (list[bit] == '1')
                                            {
                                                el.Fill = accentColor;
                                                el.Stroke = accentColor2;
                                            }
                                            else
                                            {
                                                el.Fill = Brushes.Transparent;
                                                el.Stroke = Brushes.Silver;
                                            }
                                        }
                                    }

                                    if (it.GetType() == typeof(TextBlock))
                                    {
                                        TextBlock el = it as TextBlock;
                                        int bit = Int32.Parse(el.Text);

                                        var len = list.Count();

                                        if (bit < len)
                                        {
                                            if (list[bit] == '1')
                                            {
                                                el.Foreground = Brushes.White;
                                            }
                                            else
                                            {
                                                if ((bool)tg_theme.IsChecked)
                                                {
                                                    el.Foreground = Brushes.White;
                                                }
                                                else
                                                {
                                                    el.Foreground = Brushes.Black;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_encoding_loschen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var objects = grid_encoding.GetChildObjects();
                foreach (object child in objects)
                {
                    if (child.GetType() == typeof(StackPanel))
                    {
                        StackPanel ch = child as StackPanel;
                        var obj = ch.GetChildObjects();
                        foreach (object item in obj)
                        {
                            if (item.GetType() == typeof(ToggleButton))
                            {
                                ToggleButton tb = item as ToggleButton;
                                if ((bool)tb.IsChecked)
                                {
                                    tb.IsChecked = false;
                                }
                            }
                        }
                    }
                }
                Log.Debug("Bitset: Encoding wurde gelöscht durch Anwender.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Text_encode_dec_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Set the event as handled
                e.Handled = true;
                // Select the Text
                (sender as TextBox).SelectAll();
                Log.Debug("Bitset: Encoding Wert {w} wurde durch Benutzer ausgewählt.", (sender as TextBox).Text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }
        #endregion

        #region Helferfunktionen
        private void Btn_pfad_helfer_auswahlen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
                folderDialog.ShowNewFolderButton = false;

                if (Directory.Exists(text_projktpfad_helfer.Text))
                {
                    folderDialog.SelectedPath = text_projktpfad_helfer.Text;
                }
                else
                {
                    folderDialog.SelectedPath = Properties.Paths.drive_c;
                }

                WinForms.DialogResult result = folderDialog.ShowDialog();

                if (result == WinForms.DialogResult.OK)
                {
                    text_projktpfad_helfer.Text = folderDialog.SelectedPath;
                    Log.Information("Helfer: Pfad {p} wurde ausgewählt.", folderDialog.SelectedPath);
                }
                cb_select_me.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_pfad_dataview_auswahlen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
                folderDialog.ShowNewFolderButton = false;

                if (Directory.Exists(text_projktpfad_dataview.Text))
                {
                    folderDialog.SelectedPath = text_projktpfad_dataview.Text;
                }
                else
                {
                    folderDialog.SelectedPath = Properties.Paths.drive_c;
                }

                WinForms.DialogResult result = folderDialog.ShowDialog();

                if (result == WinForms.DialogResult.OK)
                {
                    text_projktpfad_dataview.Text = folderDialog.SelectedPath;
                    Log.Information("Helfer: DataView Pfad {p} wurde ausgewählt.", folderDialog.SelectedPath);
                }
                ti_dataview.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_pfad_kundenprojekt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
                folderDialog.ShowNewFolderButton = false;

                if (Directory.Exists(text_kundenprojekt.Text))
                {
                    folderDialog.SelectedPath = text_kundenprojekt.Text;
                }
                else
                {
                    folderDialog.SelectedPath = Properties.Paths.drive_c;
                }

                WinForms.DialogResult result = folderDialog.ShowDialog();

                if (result == WinForms.DialogResult.OK)
                {
                    text_kundenprojekt.Text = folderDialog.SelectedPath;
                    Log.Information("DataView: Kundenordner Pfad {p} wurde ausgewählt.", folderDialog.SelectedPath);
                }
                ti_dataview.Focus();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void FehlerHelferAsync()
        {
            try
            {
                Log.Debug("Helfer: Fehler \"{f}\" wurde ausgelöst.", Properties.Resources.dialogMsgHelferFehler);
                await this.ShowMessageAsync(Properties.Resources.dialogTitelHelfer, Properties.Resources.dialogMsgHelferFehler, MessageDialogStyle.Affirmative);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void FehlerHelferDatenbank()
        {
            try
            {
                Log.Debug("DataView: Datenbank Fehler \"{f}\" wurde ausgelöst.", Properties.Resources.dialogMsgDatenbankFehler);
                await this.ShowMessageAsync(Properties.Resources.dialogTitelDatenbankFehler, Properties.Resources.dialogMsgDatenbankFehler, MessageDialogStyle.Affirmative);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void FehlerHelferAsyncME()
        {
            try
            {
                Log.Debug("Helfer: Fehler \"{f}\" wurde ausgelöst.", Properties.Resources.dialogMsgHelferFehlerME);
                await this.ShowMessageAsync(Properties.Resources.dialogTitelHelfer, Properties.Resources.dialogMsgHelferFehlerME, MessageDialogStyle.Affirmative);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void OpenFileOrFolder(string input)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(text_projktpfad_helfer.Text))
                {
                    FehlerHelferAsync();
                }
                else
                {
                    try
                    {
                        Process.Start(input);
                        Log.Information("Helfer: Datei oder Ordner {p} wurde geöffnet.", input);
                    }
                    catch (Exception)
                    {
                        FehlerHelferAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }   
        }

        private void Btn_open_systemoptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.systemOptions;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openFormProgram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.formProgram;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openDiagnoseData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.diagnoseData;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }          
        }

        private void Bt_openDiagramSetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.diagramSetup;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openPrjectFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.machineParameter;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openMachineParameter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openDataViewFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openDataViewUSBfolderDev_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text + Properties.Paths.dv_usb_dev;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openDataViewBilderFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text + Properties.Paths.dv_bilder_dev;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openDataViewUSBfolderSim_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text + Properties.Paths.dv_usb_sim;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openKundendatenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_kundenprojekt.Text;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openDataViewToolsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text + Properties.Paths.dv_tools;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_disableDataView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = File.ReadAllText(text_projktpfad_helfer.Text + Properties.Paths.Start_Simulation);

                if (!text.Contains("rem start DataView.exe"))
                {
                    text = text.Replace("start DataView.exe", "rem start DataView.exe");
                }
                
                File.WriteAllText(text_projktpfad_helfer.Text + Properties.Paths.Start_Simulation, text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Bt_enableDataView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = File.ReadAllText(text_projktpfad_helfer.Text + Properties.Paths.Start_Simulation);
                text = text.Replace("rem start DataView.exe", "start DataView.exe");
                File.WriteAllText(text_projktpfad_helfer.Text + Properties.Paths.Start_Simulation, text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_open_machinesetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.MachSetup;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }          
        }

        private void Bt_simStarten_Click(object sender, RoutedEventArgs e)
        {  
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.Start_Simulation;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
                Log.Information("Helfer: Simulation wurde gestartet.");
            }
            catch (Exception ex)
            {
                FehlerHelferAsync();
                Log.Error(ex, "Error");
            }
        }
        private void Bt_simStopen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("putty"))
                {
                    process.Kill();
                }

                foreach (var process in Process.GetProcessesByName("K2Ctrl"))
                {
                    process.Kill();
                }

                foreach (var process in Process.GetProcessesByName("cmd"))
                {
                    process.Kill();
                }

                foreach (var process in Process.GetProcessesByName("DataView"))
                {
                    process.Kill();
                }

                foreach (var process in Process.GetProcessesByName("java"))
                {
                    process.Kill();
                }

                Log.Information("Helfer: Simulation wurde beendet.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }

        }

        private void Bt_visuStarten_Click(object sender, RoutedEventArgs e)
        {          
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.Start_Visualization;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
                Log.Information("dat@net: Visualisierung dat@net wurde gestartet.");
            }
            catch (Exception ex)
            {
                FehlerHelferAsync();
                Log.Error(ex, "Error");
            }
        }

        private void Bt_visuStartenDataView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text + Properties.Paths.Start_VisualizationDataView;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
                Log.Information("DataView: Visualisierung DataView wurde gestartet.");
            }
            catch (Exception ex)
            {
                FehlerHelferAsync();
                Log.Error(ex, "Error");
            }
        }

        private void Btn_open_postcheckout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_dataview.Text + Properties.Paths.dv_tools + Properties.Paths.dv_PostCheckoutNoPause;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
                Log.Information("DataView: PostCheckoutNoPause.bat wurde gestartet.");
            }
            catch (Exception ex)
            {
                FehlerHelferAsync();
                Log.Error(ex, "Error");
            }
        }

        private void Btn_open_update_DB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_kundenprojekt.Text + Properties.Paths.kundenordner_install + Properties.Paths.Start_db_update_kundenordner;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
                Log.Information("DataView: update_Dataview_simulation_DB_to_new_vers_and_this_order.bat wurde gestartet.");
            }
            catch (Exception ex)
            {
                FehlerHelferAsync();
                Log.Error(ex, "Error");
            }
        }

        private void Bt_openConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string open = text_projktpfad_helfer.Text + Properties.Paths.config;
                OpenFileOrFolder(open);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void Bt_BackupProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string foldername = new DirectoryInfo(text_projktpfad_helfer.Text).Name;
                string targetArchive = text_projktpfad_helfer.Text.Replace(foldername, "") + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + foldername + ".7z";
                string sourceName = text_projktpfad_helfer.Text;

                //Check if Path exists
                Directory.GetAccessControl(sourceName);

                //New Messagbox
                var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = Properties.Resources.dialogNegButton };
                var xp = await this.ShowProgressAsync(Properties.Resources.dialogTitelBackup, Properties.Resources.dialogMsgBackup, true, mymessageboxsettings);

                xp.SetIndeterminate();

                //Process
                Process x = new Process();
                ProcessStartInfo p = new ProcessStartInfo();
                p.FileName = @"resources\7z\7za.exe";
                p.Arguments = string.Format("a -t7z \"{0}\" \"{1}\" -mx=9", targetArchive, sourceName);
                p.CreateNoWindow = true;
                p.UseShellExecute = false;

                await Task.Run(() =>
                {
                    x = Process.Start(p);
                    
                    EventHandler canceled = (o, args) =>
                    {
                        x?.Kill();
                    };
                    xp.Canceled += canceled;

                    x?.WaitForExit();
                });

                //Wenn abgebrochen wurde, dann datei löschen
                if (xp.IsCanceled)
                {
                    File.Delete(targetArchive);
                }
                else
                {
                    OpenFileOrFolder(sourceName.Replace(foldername, ""));
                }
                await xp.CloseAsync();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Properties.Resources.dialogTitelBackup, Properties.Resources.dialogMsgBackupFehler, MessageDialogStyle.Affirmative);
                Log.Error(ex, "Error");
                Log.Error("Helfer: Fehler \"{f}\" ist aufgetreten.", Properties.Resources.dialogMsgBackupFehler);
            }
        }


        private void Cb_select_me_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                TexteMEFunktionen();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Cb_select_me_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                cb_select_me.Items.Clear();
                string mepfad = text_projktpfad_helfer.Text + Properties.Paths.ieccontrol;

                if (String.IsNullOrWhiteSpace(text_projktpfad_helfer.Text))
                {
                    FehlerHelferAsync();
                    return;
                }

                if (Directory.Exists(mepfad))
                {
                    string[] subdirectoryEntries = Directory.GetDirectories(mepfad);

                    foreach (string subdirectory in subdirectoryEntries)
                    {
                        cb_select_me.Items.Add(new DirectoryInfo(subdirectory).Name);
                    }
                }
                else
                {
                    cb_select_me.Items.Clear();
                    FehlerHelferAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Btn_open_me_folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string me = cb_select_me.SelectedValue.ToString();
                string open = text_projktpfad_helfer.Text + Properties.Paths.ieccontrol+ "\\" + me;
                OpenFileOrFolder(open);
            }
            catch (Exception)
            {
                FehlerHelferAsyncME();
            }

        }

        private void Btn_open_me_tu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string me = cb_select_me.SelectedValue.ToString();
                string open = text_projktpfad_helfer.Text + Properties.Paths.ieccontrol + "\\" + me + "\\" + me + ".tu";
                OpenFileOrFolder(open);
            }
            catch (Exception)
            {
                FehlerHelferAsyncME();
            }       
        }

        private void Btn_open_xml_hmi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string me = cb_select_me.SelectedValue.ToString();
                string open = text_projktpfad_helfer.Text + "\\application\\view\\" + me + "\\hmi\\text";
                OpenFileOrFolder(open);
            }
            catch (Exception)
            {
                FehlerHelferAsyncME();
            }
        }

        private async void Bt_lock_loschen_Click(object sender, RoutedEventArgs e)
        {
            var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = Properties.Resources.dialogNegButton };
            MessageDialogResult result = await this.ShowMessageAsync(Properties.Resources.dialogTitelHelferLock, Properties.Resources.dialogMsgHelferLock, MessageDialogStyle.AffirmativeAndNegative, mymessageboxsettings);

            if (!(result == MessageDialogResult.Affirmative))
            {
                return;
            }

            try
            {
                List<string> allFiles = new List<string>();
                string suchpfad = text_projktpfad_helfer.Text;

                //Check if Path exists
                Directory.GetAccessControl(suchpfad);

                AddFileNamesToList(suchpfad, allFiles, false, false, false);
                int counter = 0;

                foreach (var file in allFiles)
                {
                    FileInfo info = new FileInfo(file);
                    string actual = info.Extension;

                    if (actual.EndsWith("Lock"))
                    { 
                        File.Delete(file);
                        counter++;
                    }
                }

                var message = counter.ToString() + " " + Properties.Resources.puLockDateien;
                await this.ShowMessageAsync(Properties.Resources.dialogTitelHelferLock, message, MessageDialogStyle.Affirmative);
                Log.Information("Helfer: Es wurden {a}", message);
            }
            catch (Exception)
            {
                FehlerHelferAsync();
            }
        }

        private void Text_projktpfad_helfer_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //Hier wird einfach überprüft ob der Dateipfad zu \config vorhanden ist
                //Wenn ja, wird davon ausgegangen das es sich um ein IEC Projekt handelt
                var suchpfad = text_projktpfad_helfer.Text + Properties.Paths.config;
                suchpfad = suchpfad.Replace("\\\\", "\\");
                if (Directory.Exists(suchpfad))
                {
                    HelferIstIecProjekt = true;
                }
                else
                {
                    HelferIstIecProjekt = false;
                }
                Log.Information("Helfer: IEC Projekt Pfad ausgewählt {v}.", HelferIstIecProjekt);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private void Text_projktpfad_dataview_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //Hier wird einfach überprüft ob der Dateipfad zu \Tools vorhanden ist
                //Wenn ja, wird davon ausgegangen das es sich um ein DataView Projekt handelt
                var suchpfad = text_projktpfad_dataview.Text + Properties.Paths.dv_tools;
                suchpfad = suchpfad.Replace("\\\\", "\\");
                if (Directory.Exists(suchpfad))
                {
                    HelferIstIecProjekt = true;
                }
                else
                {
                    HelferIstIecProjekt = false;
                }
                Log.Information("Helfer: DataView Projekt Pfad ausgewählt {v}.", HelferIstIecProjekt);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        private async void Btn_bitset_kundenspez_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connString = text_db_connectionstring.Text;
                var spez = text_kundenspez.Text;
                text_select_bitset_kundenspez.Text = String.Empty;

                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();

                    // Retrieve Bitset
                    using (var cmd = new MySqlCommand("SELECT SETintValue FROM bu_dc_control.tbl_setup WHERE SETname LIKE '" + spez + ".%';", conn))
                    using (var reader = await cmd.ExecuteReaderAsync()) 
                        while (await reader.ReadAsync())
                            text_select_bitset_kundenspez.Text = reader.GetInt32(0).ToString();
                }

            }
            catch (MySqlException ex)
            {
                FehlerHelferDatenbank();
                Log.Error(ex, "Error");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }

        #endregion

        private void Text_bitset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private async void Btn_update_bitset_kundenspez_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connString = text_db_connectionstring.Text;
                var spez = text_kundenspez.Text;
                int value = Int32.Parse(text_bitset.Text);
                int setid = 999999999;

                if (value > 2147483647)
                {
                    Log.Debug("DataView: Bitset zu grosse Zahl.");
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelDatenbankFehler, Properties.Resources.dialogMsgDatenbankBitsetFehler, MessageDialogStyle.Affirmative);
                    return;
                }

                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();

                    // Retrieve SETid
                    using (var cmd = new MySqlCommand("SELECT SETid FROM bu_dc_control.tbl_setup WHERE SETname LIKE '" + spez + ".%';", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                        while (await reader.ReadAsync())
                            setid = reader.GetInt32(0);

                    if (setid == 999999999)
                    {
                        Log.Debug("DataView: Keine passenden SETid gefunden.");
                        return;
                    }

                    // Update Bitset
                    using (var cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "UPDATE bu_dc_control.tbl_setup SET SETintValue = (@p) WHERE SETid = (@id)";
                        cmd.Parameters.AddWithValue("p", value);
                        cmd.Parameters.AddWithValue("id", setid);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }


            }
            catch (MySqlException ex)
            {
                FehlerHelferDatenbank();
                Log.Error(ex, "Error");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
        }


    }
}

