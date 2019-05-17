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
using WindowsInput.Native;
using WindowsInput;

namespace IECMate
{

    public partial class MainWindow : MetroWindow
    {
        public static BrushConverter bc = new BrushConverter();
        public Brush DarkBackground = (Brush)bc.ConvertFromString("#4A4A4A");
        public string[] variablen_liste = new string[] { "Variable_1", "Variable_2", "Variable_3" };
        public Stack<string> undoList = new Stack<string>();
        private string[] AccentColor = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        public InputSimulator sim = new InputSimulator();
        public int prevHotComment;
        public int prevHotBeginEnd;
        public int prevHotPlai;

        public MainWindow()
        {
            InitializeComponent();

            //Hotkey
            var key1 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_comment);
            var key2 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_beginend);
            var key3 = (Key)Enum.Parse(typeof(Key), Properties.Settings.Default.hotkey_plain);
            HotkeyManager.Current.AddOrReplace("PxComment", key1, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            HotkeyManager.Current.AddOrReplace("PxBeginEnd", key2, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            HotkeyManager.Current.AddOrReplace("PxPlain", key3, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);

            // Editor Setup
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"resources\st_syntax.xshd");
            Stream xshd_stream = File.OpenRead(file);
            XmlTextReader xshd_reader = new XmlTextReader(xshd_stream);
            text_code_template.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(xshd_reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
            text_code_output.SyntaxHighlighting = text_code_template.SyntaxHighlighting;
            xshd_reader.Close();
            xshd_stream.Close();

            text_code_template.TextArea.FontFamily = new FontFamily("Consolas");
            text_code_template.Options.ConvertTabsToSpaces = true;

            text_code_output.IsReadOnly = true;
            text_code_output.TextArea.Caret.CaretBrush = Brushes.Transparent;
            text_code_output.TextArea.FontFamily = new FontFamily("Consolas");
            text_code_output.Options.ConvertTabsToSpaces = true;

            //ComoBoxen
            combo_vars.ItemsSource = variablen_liste;
            cb_akzent_farbe.ItemsSource = AccentColor;
            cb_akzent_farbe.SelectedItem = Properties.Settings.Default.akzentfarbe;
            foreach (var letter in Enum.GetValues(typeof(Key)))
            {
                cb_hotkey_pxBeginEnd.Items.Add(letter);
                cb_hotekey_plain.Items.Add(letter);
                cb_hotkey_pxComment.Items.Add(letter);
            }

            //Einstellungen laden
            ThemeManager.ChangeAppStyle(Application.Current,
                            ThemeManager.GetAccent(Properties.Settings.Default.akzentfarbe),
                            ThemeManager.GetAppTheme(Properties.Settings.Default.theme));

            if (Properties.Settings.Default.theme == "BaseDark")
            {
                text_code_template.TextArea.Foreground = Brushes.White;
                text_code_output.TextArea.Foreground = Brushes.White;

                text_code_output.Background = DarkBackground;
                border_code_output.Background = DarkBackground;

                text_decode_out1.Background = DarkBackground;
                text_decode_out2.Background = DarkBackground;

                text_encode_bin.Background = DarkBackground;
                text_encode_dec.Background = DarkBackground;
                text_encode_hex.Background = DarkBackground;

                tg_theme.IsChecked = true;

                //Decode Matrix von Bitset neu Aufbauen
                DecodeText();
            }

            text_code_template.ShowLineNumbers = Properties.Settings.Default.zeilennummern;
            text_code_output.ShowLineNumbers = Properties.Settings.Default.zeilennummern;
            tg_line_no.IsChecked = Properties.Settings.Default.zeilennummern;

            text_code_template.Options.ShowSpaces = Properties.Settings.Default.leerzeichen;
            tg_leerzeichen.IsChecked = Properties.Settings.Default.leerzeichen;

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
            tc_root.SelectedIndex = Properties.Settings.Default.tabcontrol_index;
            ts_hotkey.IsChecked = Properties.Settings.Default.hotkey;
            text_px_nummer.Text = Properties.Settings.Default.pxnummer;

            cb_hotkey_pxBeginEnd.Text = key2.ToString();
            cb_hotekey_plain.Text = key3.ToString();
            cb_hotkey_pxComment.Text = key1.ToString();
            prevHotComment = cb_hotkey_pxComment.SelectedIndex;
            prevHotBeginEnd = cb_hotkey_pxBeginEnd.SelectedIndex;
            prevHotPlai = cb_hotekey_plain.SelectedIndex;

        //Inhalt laden
        text_var1.Text = Properties.Settings.Default.variable_1;
            text_var2.Text = Properties.Settings.Default.variable_2;
            text_var3.Text = Properties.Settings.Default.variable_3;
            text_code_template.Text = Properties.Settings.Default.vorlage;
        }

        private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
        {
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
                            sim.Keyboard.TextEntry(text);
                            break;
                        case "PxBeginEnd":
                            text = "// " + px + " begin" + Environment.NewLine + Environment.NewLine + "// " + px + " end";
                            sim.Keyboard.TextEntry(text);
                            break;
                        case "PxPlain":
                            text = px;
                            sim.Keyboard.TextEntry(text);
                            break;
                    }
                    e.Handled = true;
                }  
            }
            catch (Exception)
            {
                ;
            }         
        }

        private async void Ts_hotkey_IsCheckedChanged(object sender, EventArgs e)
        {
            if (((bool)ts_hotkey.IsChecked) && (String.IsNullOrWhiteSpace(text_px_nummer.Text)))
            {
                await this.ShowMessageAsync("Hotkey", "Bitte eine PX Nummer eingeben.", MessageDialogStyle.Affirmative);
                ts_hotkey.IsChecked = false;
                await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_px_nummer.Focus()));
            }
        }

        private void Btn_ersetzten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string undoListInhalt = "";

                if (undoList.Count >= 1)
                {
                    undoListInhalt = undoList.Peek();
                }

                if ((!String.Equals(text_code_template.Text, undoListInhalt)))
                {
                    undoList.Push(text_code_template.Text);
                }

                if (undoList.Count >= 1)
                {
                    btn_template_undo.IsEnabled = true;
                    mitem_undo.IsEnabled = true;
                }

                text_code_template.Text = text_code_template.Text.Replace(text_suchen.Text, combo_vars.SelectedItem.ToString());
            }
            catch (Exception)
            {
                ;
            }
        }

        private void Btn_template_speichern_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text File (*.txt)|*.txt";

            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, text_code_template.Text);
        }

        private void Btn_template_offnen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text File (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == true)
                text_code_template.Text = File.ReadAllText(openFileDialog.FileName);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //Wenn ctrl + F gedrückt wird, wird das ausgewählte Wort in das Suchfeld kopiert
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                text_suchen.Text = text_code_template.SelectedText;
            }

        }

        private async void Btn_gen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = Code_gen("Variable_1", text_var1.Text,
                                                 "Variable_2", text_var2.Text,
                                                 "Variable_3", text_var3.Text,
                                                 text_code_template.Text);

                if (code.StartsWith("#-->"))
                {
                    var message = code.Replace("#--> ", "");
                    await this.ShowMessageAsync("Fehler beim Erzeugen vom Code", message, MessageDialogStyle.Affirmative);
                }
                else
                {
                    text_code_output.Text = code;
                }

            }
            catch (Exception)
            {
                ;
            }
        }

        private string Code_gen(string var_1, string var_1_text,
                                string var_2, string var_2_text,
                                string var_3, string var_3_text,
                                string template)
        {
            string[] split = new string[] { "\r\n" };
            string[] vars_1 = var_1_text.Split(split, StringSplitOptions.RemoveEmptyEntries);
            string[] vars_2 = var_2_text.Split(split, StringSplitOptions.RemoveEmptyEntries);
            string[] vars_3 = var_3_text.Split(split, StringSplitOptions.RemoveEmptyEntries);

            string outtext = "";
            string error0 = "#--> Die Anzahl Variabeln ist nicht identisch.";
            string error1 = "#--> Keine Variable in Liste 1.";
            string error2 = "#--> Keine Variable in Liste 2.";
            string error3 = "#--> Keine Variable in Liste 3.";

            try
            {
                int lines = vars_1.Length;

                if (lines == 0)
                {
                    return error1;
                }

                if ((lines < vars_2.Length) || (lines < vars_3.Length))
                {
                    return error0;
                }

                if ((template.Contains(var_2)) && (vars_2.Length == 0))
                {
                    return error2;
                }

                if ((template.Contains(var_3)) && (vars_3.Length == 0))
                {
                    return error3;
                }

                for (int i = 0; i < lines; i++)
                {
                    string temp_text = template.Replace(var_1, vars_1[i]);

                    if (!string.IsNullOrEmpty(var_2_text))
                    {
                        temp_text = temp_text.Replace(var_2, vars_2[i]);
                    }

                    if (!string.IsNullOrEmpty(var_3_text))
                    {
                        temp_text = temp_text.Replace(var_3, vars_3[i]);
                    }

                    if (outtext == "")
                    {
                        outtext = temp_text;
                    }
                    else
                    {
                        outtext = outtext + Environment.NewLine + Environment.NewLine + temp_text;
                    }
                }
            }
            catch (Exception)
            {
                return error0;
            }
            return outtext;
        }

        private void Btn_gen_loschen_Click(object sender, RoutedEventArgs e)
        {
            text_code_output.Text = "";
        }

        private void Btn_gen_speichern_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text File (*.txt)|*.txt";

            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, text_code_output.Text);
        }

        private void Mi_app_beenden_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CopyToClipboard_Click_1(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(text_code_output.Text);
        }

        private void Tg_leerzeichen_IsCheckedChanged(object sender, EventArgs e)
        {
            text_code_template.Options.ShowSpaces = (bool)tg_leerzeichen.IsChecked;
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            text_code_template.TextArea.FontSize = (double)nc_font_size.Value;
            text_code_output.TextArea.FontSize = (double)nc_font_size.Value;
        }

        private void Tg_line_no_IsCheckedChanged(object sender, EventArgs e)
        {
            text_code_template.ShowLineNumbers = (bool)tg_line_no.IsChecked;
            text_code_output.ShowLineNumbers = (bool)tg_line_no.IsChecked;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
            Properties.Settings.Default.tabcontrol_index = tc_root.SelectedIndex;
            Properties.Settings.Default.schriftgrosse = (double)nc_font_size.Value;
            Properties.Settings.Default.zeilennummern = (bool)tg_line_no.IsChecked;
            Properties.Settings.Default.projekt_pfad_suche = text_projktpfad_suche.Text;
            Properties.Settings.Default.projekt_pfad_helfer = text_projktpfad_helfer.Text;
            Properties.Settings.Default.akzentfarbe = cb_akzent_farbe.SelectedValue.ToString();
            Properties.Settings.Default.hotkey = (bool)ts_hotkey.IsChecked;
            Properties.Settings.Default.pxnummer = text_px_nummer.Text;
            Properties.Settings.Default.hotkey_beginend = cb_hotkey_pxBeginEnd.SelectedValue.ToString();
            Properties.Settings.Default.hotkey_plain = cb_hotekey_plain.SelectedValue.ToString();
            Properties.Settings.Default.hotkey_comment = cb_hotkey_pxComment.SelectedValue.ToString(); 

            Properties.Settings.Default.Save();
        }

        private void Btn_template_loschen_Click(object sender, RoutedEventArgs e)
        {
            text_code_template.Text = "";
            combo_vars.SelectedIndex = -1;
        }


        private void MenuItem_Click_About(object sender, RoutedEventArgs e)
        {
            child_Infos.IsOpen = true;
            try
            {
                // get deployment version
                lb_version.Content = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (InvalidDeploymentException)
            {
                // you cannot read publish version when app isn't installed 
                // (e.g. during debug)
                lb_version.Content = "not installed";
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            //Mailprogramm öffnen wenn auf Link geklickt wird
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void Btn_template_undo_Click(object sender, RoutedEventArgs e)
        {
            //Nur wenn es etwas im Stack hat dann wird auch das Undo gemacht
            if (undoList.Count > 0)
            {
                text_code_template.Text = undoList.Pop();
            }

            //Wenn Stack leer ist, dann wir der Button deaktiviert
            if (undoList.Count == 0)
            {
                btn_template_undo.IsEnabled = false;
                mitem_undo.IsEnabled = false;
            }
        }

        private void Btn_pfad_auswahlen_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = false;
            if (Directory.Exists(text_projktpfad_suche.Text))
            {
                folderDialog.SelectedPath = text_projktpfad_suche.Text;
            }
            else
            {
                folderDialog.SelectedPath = "c:\\";
            }
           
            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                text_projktpfad_suche.Text = folderDialog.SelectedPath;
            }
        }

        private async void Btn_suche_Click(object sender, RoutedEventArgs e)
        {
            //Listebox löschen
            listbox_ergebnis.Items.Clear();

            //Wennn etwas im Suchfeld steht und das Verzeichnis existiert dann wird gesucht
            if ((!String.IsNullOrWhiteSpace(text_pattern_suche.Text)) && (Directory.Exists(text_projktpfad_suche.Text)))
            {
                //Dialog öffnen
                var mymessageboxsettings = new MetroDialogSettings(){NegativeButtonText = "Abbrechen"};
                var x = await this.ShowProgressAsync("Suchen", "Die Suche läuft. Bite warten...", true, mymessageboxsettings) as ProgressDialogController;
                x.SetIndeterminate();

                try
                {
                    List<string> allFiles = new List<string>();
                    string suchpfad = "";

                    //Wenn der Swicth "Nur HW suche" ein ist, wird der Pfad angepasst
                    if ((bool)ts_kbus_suche.IsChecked)
                    {
                        suchpfad = text_projktpfad_suche.Text + "\\application\\control\\config";
                        //Überprüfen ob Pfad existiert, wenn nicht, gibt es eine exeption
                        Directory.Exists(suchpfad);
                    }
                    else
                    {
                        suchpfad = text_projktpfad_suche.Text;
                    }

                    AddFileNamesToList(suchpfad, allFiles, (bool)ts_binar_suche.IsChecked);

                    //Sobald der Dialog offen ist wird mit der suche gestartet
                    if (x.IsOpen)
                    {
                        foreach (string fileName in allFiles)
                        {
                            if (x.IsCanceled)
                            {
                                break;
                            }

                            using (var reader = File.OpenText(fileName))
                            {
                                var fileText = await reader.ReadToEndAsync();
                                if ((bool)ts_exakte_suche.IsChecked)
                                {
                                    if (Regex.IsMatch(fileText, string.Format(@"\b{0}\b", Regex.Escape(text_pattern_suche.Text))))
                                    {
                                        listbox_ergebnis.Items.Add(fileName);
                                    }
                                }
                                else
                                {
                                    //Nicht Case Sensitive
                                    if (fileText.IndexOf(text_pattern_suche.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        listbox_ergebnis.Items.Add(fileName);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    await this.ShowMessageAsync("Fehler bei der Suche", "Das Verzeichnis ist kein IEC Projekt.", MessageDialogStyle.Affirmative);
                    await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
                }

                await x.CloseAsync();
            }
            else
            {
                await this.ShowMessageAsync("Fehler bei der Suche", "Suchfeld ist leer oder das Verzeichnis existiert nicht.", MessageDialogStyle.Affirmative);
                await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
            }
        }

        public static void AddFileNamesToList(string sourceDir, List<string> allFiles, bool bin)
        {
            IEnumerable<string> fileEntries = Enumerable.Empty<string>();
            if (bin)
            {
                fileEntries = Directory.GetFiles(sourceDir);
            }
            else
            {
                //Hier werden die Binär Files aus dem IEC Projekt ausgeschlossen
                fileEntries = Directory.GetFiles(sourceDir).Where(name => !name.EndsWith(".fu") &&
                                                                          !name.EndsWith(".fud") &&
                                                                          !name.EndsWith(".ful") &&
                                                                          !name.EndsWith("O"));
            }

            foreach (string fileName in fileEntries)
            {
                allFiles.Add(fileName);
            }

            //Recursion    
            string[] subdirectoryEntries = Directory.GetDirectories(sourceDir);
            foreach (string item in subdirectoryEntries)
            {
                // Avoid "reparse points"
                if ((File.GetAttributes(item) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    AddFileNamesToList(item, allFiles, bin);
                }
            }
        }

        private async void Btn_suche_offnen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(listbox_ergebnis.SelectedItem.ToString());
            }
            catch (Exception)
            {
                await this.ShowMessageAsync("Fehler beim Öffnen", "Bitte eine Datei auswählen.", MessageDialogStyle.Affirmative);
            }
        }

        private void Encoding_Checked(object sender, RoutedEventArgs args)
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

        public void DecodeText()
        {
            //Wenn alles gelöscht wird
            if (String.IsNullOrWhiteSpace(text_decode.Text))
            {
                List<char> BinList = new List<char>();
                BinList.Insert(0, '0');
                ShowDecodeResult(BinList);

                text_decode_out1.Text = "";
                text_decode_out2.Text = "";
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
                        ShowFehlerBitsetAsync("Die Hex - Eingabe enthält andere Zeichen als 0-9, a-f oder A-F.");
                        return;
                    }

                    if (Convert.ToInt64(hex, 16) > 4294967295)
                    {
                        ShowFehlerBitsetAsync("Die eingegebene Zahl ist grösser als 32bit.");
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

                    text_decode_out1.Text = "";
                    text_decode_out2.Text = "";
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
                        ShowFehlerBitsetAsync("Die Binär - Eingabe enthält andere Zeichen als 0 oder 1.");
                        return;
                    }

                    if (Convert.ToInt64(bin, 2) > 4294967295)
                    {
                        ShowFehlerBitsetAsync("Die eingegebene Zahl ist grösser als 32bit.");
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

                    text_decode_out1.Text = "";
                    text_decode_out2.Text = "";
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
                    ShowFehlerBitsetAsync("Die Dezimal - Eingabe enthält andere Zeichen als 0 - 9.");
                    return;
                }

                if (Convert.ToInt64(dez) > 4294967295)
                {
                    ShowFehlerBitsetAsync("Die eingegebene Zahl ist grösser als 32bit.");
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

                text_decode_out1.Text = "";
                text_decode_out2.Text = "";
            }
        }

        private void Text_decode_TextChanged(object sender, TextChangedEventArgs e)
        {
            DecodeText();   
        }

        private async void ShowFehlerBitsetAsync(string message)
        {
            text_decode.IsEnabled = false;
            MessageDialogResult result = await this.ShowMessageAsync("Fehler bei der Eingabe", message, MessageDialogStyle.Affirmative);

            if (result == MessageDialogResult.Affirmative)
            {
                text_decode.IsEnabled = true;
                await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_decode.Focus()));
            }
        }

        private void ShowDecodeResult(List<char> list)
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

        private void Btn_pfad_helfer_auswahlen_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = false;

            if (Directory.Exists(text_projktpfad_helfer.Text))
            {
                folderDialog.SelectedPath = text_projktpfad_helfer.Text;
            }
            else
            {
                folderDialog.SelectedPath = "c:\\";
            }

            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                text_projktpfad_helfer.Text = folderDialog.SelectedPath;
            }
        }

        private async void FehlerHelferAsync()
        {
            string message = "Datei oder  Ordner nicht vorhanden. Bitte den Projektpfad überprüfen.";
            string titel = "Fehler beim öffnen";
            await this.ShowMessageAsync(titel, message, MessageDialogStyle.Affirmative);
        }

        private async void FehlerHelferAsyncME()
        {
            string message = "Bitte eine ME auswählen.";
            string titel = "Fehler beim öffnen";
            await this.ShowMessageAsync(titel, message, MessageDialogStyle.Affirmative);
        }

        private void OpenFileOrFolder(string input)
        {
            try
            {
                System.Diagnostics.Process.Start(input);
            }
            catch (Exception)
            {
                FehlerHelferAsync();
            }
        }

        private void Btn_open_systemoptions_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + "\\data_Machine\\machineSetup\\systemOptions.properties";
            OpenFileOrFolder(open);
        }

        private void Bt_openFormProgram_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + "\\data_Customer\\formProgram";
            OpenFileOrFolder(open);
        }

        private void Bt_openDiagnoseData_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + "\\data_Customer\\diagnoseData";
            OpenFileOrFolder(open);
        }

        private void Bt_openDiagramSetup_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + "\\data_Customer\\diagramSetup";
            OpenFileOrFolder(open);
        }

        private void Bt_simStarten_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + "\\Start_Simulation.bat";
            OpenFileOrFolder(open);
        }

        private void Bt_visuStarten_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + "\\Start_Visualization.bat";
            OpenFileOrFolder(open);
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
                var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = "Abbrechen" };
                var xp = await this.ShowProgressAsync("Backup", "Das Backup läuft bitte warten...", false, mymessageboxsettings);
                xp.SetIndeterminate();

                //Process
                Process x = new Process();
                ProcessStartInfo p = new ProcessStartInfo();
                p.FileName = @"resources\7z\7za.exe";
                p.Arguments = string.Format("a -t7z \"{0}\" \"{1}\" -mx=9", targetArchive, sourceName);
                p.WindowStyle = ProcessWindowStyle.Normal;

                await Task.Run(() =>
                {
                    x = Process.Start(p);
                    x.WaitForExit();
                });

                //Dateigrösse
                //Wenn Datei sehr klein ist, dann wurde abgebrochen
                //FIX IT
                long length = new System.IO.FileInfo(targetArchive).Length;
                if (length < 40)
                {
                    File.Delete(targetArchive);
                }
                await xp.CloseAsync();
            }
            catch (Exception ex)
            {
                string message = "Projektordner existiert nicht " + ex.Message;
                string titel = "Fehler beim Backup";
                await this.ShowMessageAsync(titel, message, MessageDialogStyle.Affirmative);
            }
        }

        private void Cb_select_me_DropDownOpened(object sender, EventArgs e)
        {
            string mepfad = text_projktpfad_helfer.Text + "\\application\\control\\ieccontrol";

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

        private void Btn_open_me_folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string me = cb_select_me.SelectedValue.ToString();
                string open = text_projktpfad_helfer.Text + "\\application\\control\\ieccontrol\\" + me;
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

        private void Tc_root_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void Cb_akzent_farbe_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void Tg_theme_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)tg_theme.IsChecked)
            {
                ThemeManager.ChangeAppStyle(Application.Current,
                                            ThemeManager.GetAccent(cb_akzent_farbe.SelectedValue.ToString()),
                                            ThemeManager.GetAppTheme("BaseDark"));

                text_code_template.TextArea.Foreground = Brushes.White;
                text_code_output.TextArea.Foreground = Brushes.White;
                text_code_output.Background = DarkBackground;
                border_code_output.Background = DarkBackground;

                text_decode_out1.Background = DarkBackground;
                text_decode_out2.Background = DarkBackground;
                text_encode_bin.Background = DarkBackground;
                text_encode_dec.Background = DarkBackground;
                text_encode_hex.Background = DarkBackground;
            }
            else
            {
                ThemeManager.ChangeAppStyle(Application.Current,
                                            ThemeManager.GetAccent(cb_akzent_farbe.SelectedValue.ToString()),
                                            ThemeManager.GetAppTheme("BaseLight"));

                text_code_template.TextArea.Foreground = Brushes.Black;
                text_code_output.TextArea.Foreground = Brushes.Black;

                text_code_output.Background = Brushes.Gainsboro;
                border_code_output.Background = Brushes.Gainsboro;

                text_decode_out1.Background = Brushes.Gainsboro;
                text_decode_out2.Background = Brushes.Gainsboro;
                text_encode_bin.Background = Brushes.Gainsboro;
                text_encode_dec.Background = Brushes.Gainsboro;
                text_encode_hex.Background = Brushes.Gainsboro;
            }

            //Decode Matrix von Bitset neu Aufbauen
            DecodeText();
        }

        private void Btn_encoding_loschen_Click(object sender, RoutedEventArgs e)
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
        }

        private void Btn_vaiablenliste_loschen_Click(object sender, RoutedEventArgs e)
        {
            text_var1.Text = "";
            text_var2.Text = "";
            text_var3.Text = "";
        }

        private void Text_encode_dec_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Set the event as handled
            e.Handled = true;
            // Select the Text
            (sender as TextBox).SelectAll();
        }

        private async void Bt_lock_loschen_Click(object sender, RoutedEventArgs e)
        {
            var mymessageboxsettings = new MetroDialogSettings() { NegativeButtonText = "Abbrechen", AffirmativeButtonText = "OK"};
            string message = "Es werden alle Datein mit der Dateiendung *.puLock gelöscht. Möchten Sie fortfahren?";
            string titel = "Löschen von puLock Datein";
            MessageDialogResult result = await this.ShowMessageAsync(titel, message, MessageDialogStyle.AffirmativeAndNegative, mymessageboxsettings);

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

                AddFileNamesToList(suchpfad, allFiles, (bool)ts_binar_suche.IsChecked);
                //DirectoryInfo directory = new DirectoryInfo(suchpfad);
                //FileInfo[] files = directory.GetFiles();

                foreach (var file in allFiles)
                {
                    FileInfo info = new FileInfo(file);
                    string actual = info.Extension;

                    if (actual.Equals(".puLock"))
                    {
                        File.Delete(file);
                    }
                }

            }
            catch (Exception)
            {
                FehlerHelferAsync();
            }
        }

        private async void Cb_hotekey_plain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_hotekey_plain.SelectedIndex == cb_hotkey_pxBeginEnd.SelectedIndex || cb_hotekey_plain.SelectedIndex == cb_hotkey_pxComment.SelectedIndex)
            {
                cb_hotekey_plain.SelectedIndex = prevHotPlai;
                await this.ShowMessageAsync("Fehler", "Der Hot Key wird schon verwedent.", MessageDialogStyle.Affirmative);
            }
            else
            {
                var key = (Key)cb_hotekey_plain.SelectedValue;
                HotkeyManager.Current.AddOrReplace("PxPlain", key, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            }
        }

        private async void Cb_hotkey_pxBeginEnd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_hotkey_pxBeginEnd.SelectedIndex == cb_hotekey_plain.SelectedIndex || cb_hotkey_pxBeginEnd.SelectedIndex == cb_hotkey_pxComment.SelectedIndex)
            {
                cb_hotkey_pxBeginEnd.SelectedIndex = prevHotBeginEnd;
                await this.ShowMessageAsync("Fehler", "Der Hot Key wird schon verwedent.", MessageDialogStyle.Affirmative);
            }
            else
            {
                var key = (Key)cb_hotkey_pxBeginEnd.SelectedValue;
                HotkeyManager.Current.AddOrReplace("PxBeginEnd", key, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            }
        }

        private async void Cb_hotkey_pxComment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_hotkey_pxComment.SelectedIndex == 0)
            {
                HotkeyManager.Current.Remove("PxComment");
                return;
            }

            if (cb_hotkey_pxComment.SelectedIndex == cb_hotekey_plain.SelectedIndex || cb_hotkey_pxComment.SelectedIndex == cb_hotkey_pxBeginEnd.SelectedIndex)
            {
                cb_hotkey_pxComment.SelectedIndex = prevHotComment;
                await this.ShowMessageAsync("Fehler", "Der Hot Key wird schon verwedent.", MessageDialogStyle.Affirmative);
            }
            else
            {
                var key = (Key)cb_hotkey_pxComment.SelectedValue;
                HotkeyManager.Current.AddOrReplace("PxComment", key, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
            }
        }
    }
}

