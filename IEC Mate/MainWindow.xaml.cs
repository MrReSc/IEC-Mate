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
using System.Text;
using System.Threading;
using System.Globalization;
using WindowsInput.Native;
using ICSharpCode.AvalonEdit;

namespace IECMate
{

    public partial class MainWindow : MetroWindow
    {
        public static BrushConverter bc = new BrushConverter();
        public Brush DarkBackground = (Brush)bc.ConvertFromString("#4A4A4A");
        public string[] variablen_liste = new string[] { "Variable_1", "Variable_2", "Variable_3" };
        //public Stack<string> undoList = new Stack<string>();
        private string[] AccentColor = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        public InputSimulator sim = new InputSimulator();
        public RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion");
        public string os_version;

        public MainWindow()
        {
            #region Initialisierung
            //Wenn es eine neue Version gibt, dann werden die Einstellungen von der schon installierten Version übernommen
            if (Properties.Settings.Default.updatesettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.updatesettings = false;
                Properties.Settings.Default.Save();
            }

            DataContext = this;
            //var liste = new List<string>() { "svDO", "Mark", "Doe" };
            //vorhandeneIO = liste;

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.sprache);
            InitializeComponent();

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
            cb_hotekey_brackets.Text = key4.ToString();

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
            }

            //Inhalt laden
            text_var1.Text = Properties.Settings.Default.variable_1;
            text_var2.Text = Properties.Settings.Default.variable_2;
            text_var3.Text = Properties.Settings.Default.variable_3;
            text_code_template.Text = Properties.Settings.Default.vorlage;

            //OS Version
            os_version = (string)registryKey.GetValue("productName");
            #endregion
        }

        #region Allgemein
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

        private void Mi_app_beenden_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CopyToClipboard_Click_1(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(text_code_output.Text);
            text_code_output.Focus();
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

        private void Btn_vaiablenliste_loschen_Click(object sender, RoutedEventArgs e)
        {
            text_var1.Text = "";
            text_var2.Text = "";
            text_var3.Text = "";
            text_var1.Focus();
        }
        #endregion

        #region About Fenster
        private void MenuItem_Click_About(object sender, RoutedEventArgs e)
        {
            child_Infos.IsOpen = true;
            try
            {
                // get deployment version
                string[] assemblyversion = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
                lb_version.Content = assemblyversion[0] + "." + assemblyversion[1] + "." + assemblyversion[2] + " (" + assemblyversion[3] + ")";
            }
            catch (InvalidDeploymentException)
            {
                // you cannot read publish version when app isn't installed 
                // (e.g. during debug)
                lb_version.Content = Properties.Resources.lb_version;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
        #endregion

        #region Hot Key
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
                            PasteTextFromHotkey(text);
                            break;
                        case "PxBeginEnd":
                            text = "// " + px + " begin" + Environment.NewLine + Environment.NewLine + "// " + px + " end";
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
            catch (Exception)
            {
                ;
            }         
        }

        private void PasteTextFromHotkey(string text)
        {
            
            if (os_version.Contains("Windows 10"))
            {
                //Bei Windows 10 kann der Text aus dem Cliboard eingefügt werden
                //Bei Windows 7 funktioniert es nicht
                Clipboard.SetText(text);
                var isControlKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.CONTROL);
                var isShiftKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT);

                do
                {
                    isShiftKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT);
                    isControlKeyDown = sim.InputDeviceState.IsKeyDown(VirtualKeyCode.CONTROL);
                } while (isControlKeyDown || isShiftKeyDown);

                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            }
            else
            {
                sim.Keyboard.TextEntry(text);
            }           
        }

        private async void Ts_hotkey_IsCheckedChanged(object sender, EventArgs e)
        {
            if (((bool)ts_hotkey.IsChecked) && (String.IsNullOrWhiteSpace(text_px_nummer.Text)))
            {
                await this.ShowMessageAsync(Properties.Resources.dialogTitelHotkey, Properties.Resources.dialogMsgHotkey, MessageDialogStyle.Affirmative);
                ts_hotkey.IsChecked = false;
                await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_px_nummer.Focus()));
            }
        }

        private void Cb_hotekey_plain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegisterHotKey("PxPlain", (ComboBox)sender);
        }

        private void Cb_hotkey_pxBeginEnd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            RegisterHotKey("PxBeginEnd", (ComboBox)sender);
        }

        private void Cb_hotkey_pxComment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegisterHotKey("PxComment", (ComboBox)sender);
        }

        private void Cb_hotekey_brackets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegisterHotKey("PxBrackets", (ComboBox)sender);
        }

        private async void RegisterHotKey(string name, ComboBox combobox)
        {
            if (combobox.SelectedIndex == 0)
            {
                //Wenn kein Key selected ist, dann wird er abgemeldet
                HotkeyManager.Current.Remove(name);
                return;
            }

            try
            {
                //Aktuell Selektierter Keys auslesen
                var key_1 = (Key)cb_hotekey_brackets.SelectedValue;
                var key_2 = (Key)cb_hotekey_plain.SelectedValue;
                var key_3 = (Key)cb_hotkey_pxBeginEnd.SelectedValue;
                var key_4 = (Key)cb_hotkey_pxComment.SelectedValue;
                var key = (Key)combobox.SelectedValue;

                switch (name)
                {
                    case "PxComment":
                        key_1 = (Key)cb_hotekey_brackets.SelectedValue;
                        key_2 = (Key)cb_hotekey_plain.SelectedValue;
                        key_3 = (Key)cb_hotkey_pxBeginEnd.SelectedValue;
                        break;
                    case "PxBeginEnd":
                        key_1 = (Key)cb_hotekey_brackets.SelectedValue;
                        key_2 = (Key)cb_hotekey_plain.SelectedValue;
                        key_3 = (Key)cb_hotkey_pxComment.SelectedValue;
                        break;
                    case "PxPlain":
                        key_1 = (Key)cb_hotekey_brackets.SelectedValue;
                        key_2 = (Key)cb_hotkey_pxComment.SelectedValue;
                        key_3 = (Key)cb_hotkey_pxBeginEnd.SelectedValue;
                        break;
                    case "PxBrackets":
                        key_1 = (Key)cb_hotkey_pxComment.SelectedValue;
                        key_2 = (Key)cb_hotekey_plain.SelectedValue;
                        key_3 = (Key)cb_hotkey_pxBeginEnd.SelectedValue;
                        break;
                }


                if (key == key_1 || key == key_2 || key == key_3)
                {
                    combobox.SelectedIndex = 0;
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelHotkey, Properties.Resources.dialogMsgHotkeyFehler, MessageDialogStyle.Affirmative);
                }
                else
                {
                    HotkeyManager.Current.AddOrReplace(name, key, ModifierKeys.Control | ModifierKeys.Shift, OnHotkeyPressed);
                }
            }
            catch (Exception)
            {
                return;
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
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text File (*.txt)|*.txt";

            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, text_code_template.Text);

            text_code_template.Focus();
        }

        private void Btn_template_offnen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text File (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == true)
                text_code_template.Text = File.ReadAllText(openFileDialog.FileName);

            text_code_template.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //Wenn ctrl + F gedrückt wird, wird das ausgewählte Wort in das Suchfeld kopiert
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                text_suchen.Text = text_code_template.SelectedText;
                text_suchen.Focus();
                text_suchen.CaretIndex = text_suchen.Text.Length;
            }
        }

        private void Btn_template_loschen_Click(object sender, RoutedEventArgs e)
        {
            text_code_template.Text = "";
            combo_vars.SelectedIndex = -1;
            text_code_template.Focus();
        }

        private void Btn_ersetzten_ein_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Replace(text_suchen.Text, combo_vars.SelectedValue.ToString(), text_code_template);
                text_code_template.Focus();
            }
            catch (Exception)
            {
                ;
            }

        }

        private void Btn_ersetzten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                do
                {
                    Replace(text_suchen.Text, combo_vars.SelectedValue.ToString(), text_code_template);
                } while (text_code_template.Text.Contains(text_suchen.Text));
                text_code_template.Focus();

            }
            catch (Exception)
            {
                ;
            }
        }

        private int lastUsedIndex = 0;

        public void Replace(string s, string replacement, ICSharpCode.AvalonEdit.TextEditor editor)
        {
            int nIndex = -1;

            if (editor.SelectedText.Equals(s))
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
        }
        #endregion

        #region Code generieren

        private async void Btn_gen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = Code_gen("Variable_1", text_var1.Text,
                                    "Variable_2", text_var2.Text,
                                    "Variable_3", text_var3.Text,
                                    text_code_template.Text);

                if (code.StartsWith("--> "))
                {
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelCodeGen, code.Replace("--> ", ""), MessageDialogStyle.Affirmative);
                }
                else
                {
                    text_code_output.Text = code;
                    text_code_output.Focus();
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
            string error0 = Properties.Resources.dialogMsgCodeGen00;
            string error1 = Properties.Resources.dialogMsgCodeGen01;
            string error2 = Properties.Resources.dialogMsgCodeGen02;
            string error3 = Properties.Resources.dialogMsgCodeGen03;

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
                        outtext = outtext + Environment.NewLine + temp_text;
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
            text_code_output.Focus();
        }

        private void Btn_gen_speichern_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text File (*.txt)|*.txt";

            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, text_code_output.Text);
        }
        #endregion

        #region Einstellungen

        private void Tg_leerzeichen_IsCheckedChanged(object sender, EventArgs e)
        {
            text_code_template.Options.ShowSpaces = (bool)tg_leerzeichen.IsChecked;
            text_code_output.Options.ShowSpaces = (bool)tg_leerzeichen.IsChecked;
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

        private void Tg_showtab_IsCheckedChanged(object sender, EventArgs e)
        {
            text_code_template.Options.ShowTabs = (bool)tg_showtab.IsChecked;
            text_code_output.Options.ShowTabs = (bool)tg_showtab.IsChecked;
        }

        private void Tg_showendofline_IsCheckedChanged(object sender, EventArgs e)
        {
            text_code_template.Options.ShowEndOfLine = (bool)tg_showendofline.IsChecked;
            text_code_output.Options.ShowEndOfLine = (bool)tg_showendofline.IsChecked;
        }

        private void Tg_converttospace_IsCheckedChanged(object sender, EventArgs e)
        {
            text_code_template.Options.ConvertTabsToSpaces = (bool)tg_converttospace.IsChecked;
            text_code_output.Options.ConvertTabsToSpaces = (bool)tg_converttospace.IsChecked;
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
            Properties.Settings.Default.converttabtospace = (bool)tg_converttospace.IsChecked;
            Properties.Settings.Default.showtab = (bool)tg_showtab.IsChecked;
            Properties.Settings.Default.showendofline = (bool)tg_showendofline.IsChecked;
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
            if (cb_select_me.SelectedIndex == -1)
            {
                Properties.Settings.Default.me_auswahl = "";
            }
            else
            {
                Properties.Settings.Default.me_auswahl = cb_select_me.SelectedValue.ToString();
            }
           
            Properties.Settings.Default.Save();

            HotkeyManager.Current.Remove("PxBeginEnd");
            HotkeyManager.Current.Remove("PxPlain");
            HotkeyManager.Current.Remove("PxComment");
            HotkeyManager.Current.Remove("PxBrackets");
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

        private async void Cb_sprache_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string setting = "";

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
                    Application.Current.Shutdown();
                }
            }
        }
        #endregion

        #region Suchfunktion
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
                folderDialog.SelectedPath = Properties.Paths.drive_c;
            }
           
            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                text_projktpfad_suche.Text = folderDialog.SelectedPath;
            }
            text_pattern_suche.Focus();
        }

        private async void Suche()
        {
            //Listebox löschen
            listbox_ergebnis.Items.Clear();

            //Wennn etwas im Suchfeld steht und das Verzeichnis existiert dann wird gesucht
            if ((!String.IsNullOrWhiteSpace(text_pattern_suche.Text)) && (Directory.Exists(text_projktpfad_suche.Text)))
            {
                //Dialog öffnen
                var mymessageboxsettings = new MetroDialogSettings(){NegativeButtonText = Properties.Resources.dialogNegButton};
                var x = await this.ShowProgressAsync(Properties.Resources.dialogTitelSuche, Properties.Resources.dialogMsgSucheLauft, true, mymessageboxsettings) as ProgressDialogController;
                double percent = 0;
                x.SetProgress(percent);

                try
                {
                    List<string> allFiles = new List<string>();
                    string suchpfad = "";

                    //Wenn der Switch "Nur HW suche" ein ist, wird der Pfad angepasst
                    if ((bool)ts_kbus_suche.IsChecked)
                    {
                        suchpfad = text_projktpfad_suche.Text + Properties.Paths.config;
                        suchpfad = suchpfad.Replace("\\\\", "\\");
                        //Überprüfen ob Pfad existiert, wenn nicht, gibt es eine exeption
                        Directory.Exists(suchpfad);
                    }
                    else
                    {
                        suchpfad = text_projktpfad_suche.Text;
                    }

                    AddFileNamesToList(suchpfad, allFiles, (bool)ts_binar_suche.IsChecked);
                    double filecount = allFiles.Count();
                    double count = 0;

                    //Sobald der Dialog offen ist wird mit der suche gestartet
                    if (x.IsOpen)
                    {
                        foreach (string fileName in allFiles)
                        {
                            count ++;
                            percent = 100 / filecount * count / 100;
                            if (percent > 1.0)
                            {
                                percent = 1.0;
                            }
                            x.SetProgress(percent);

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
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelSuche, Properties.Resources.dialogMsgSucheVerzeichnisFehler, MessageDialogStyle.Affirmative);
                    await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
                }

                await x.CloseAsync();
            }
            else
            {
                await this.ShowMessageAsync(Properties.Resources.dialogTitelSuche, Properties.Resources.dialogMsgSucheLeer, MessageDialogStyle.Affirmative);
                await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.text_pattern_suche.Focus()));
            }
        }

        private void Btn_suche_Click(object sender, RoutedEventArgs e)
        {
            Suche();
            text_pattern_suche.Focus();
        }

        private void Text_pattern_suche_KeyDown(object sender, KeyEventArgs e)
        {
            //Wenn Enter gedrückt wird, wird das Suchen ausgelöst
            if (e.Key == Key.Enter)
            {
                Suche();
                text_pattern_suche.Focus();
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

        private async void Listbox_ergebnis_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listbox_ergebnis.SelectedIndex > -1)
            {
                try
                {
                    Process.Start(listbox_ergebnis.SelectedItem.ToString());
                }
                catch (Exception)
                {
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelDateiOffnen, Properties.Resources.dialogMsgDateiOffnenFehler, MessageDialogStyle.Affirmative);
                }
            }          
        }

        private async void Mi_open_file_Click(object sender, RoutedEventArgs e)
        {
            if (listbox_ergebnis.SelectedIndex > -1)
            {
                try
                {
                    Process.Start(listbox_ergebnis.SelectedItem.ToString());
                }
                catch (Exception)
                {
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelDateiOffnen, Properties.Resources.dialogMsgDateiOffnenFehler, MessageDialogStyle.Affirmative);
                }
            }
        }

        private async void Mi_open_folder_Click(object sender, RoutedEventArgs e)
        {
            if (listbox_ergebnis.SelectedIndex > -1)
            {
                try
                {
                    Process.Start(Path.GetDirectoryName(listbox_ergebnis.SelectedItem.ToString()));
                }
                catch (Exception)
                {
                    await this.ShowMessageAsync(Properties.Resources.dialogTitelDateiOffnen, Properties.Resources.dialogMsgDateiOffnenFehler, MessageDialogStyle.Affirmative);
                }
            }
        }

        private void Ts_exakte_suche_IsCheckedChanged(object sender, EventArgs e)
        {
            text_pattern_suche.Focus();
        }

        private void Text_projktpfad_suche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((bool)ts_hw_suchvorschalg.IsChecked)
            {
                text_pattern_suche.ItemsSource = FilterIO();
            }
        }

        private void Ts_hw_suchvorschalg_IsCheckedChanged(object sender, EventArgs e)
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

        private List<string> FilterIO()
        {
            //Hier wird versucht die IO's auf den Konfig File zu Indexieren
            //Dies geschieht allerdings nur wenn auch ein IEC Projekt ausgewählt ist
            var suchpfad = text_projktpfad_suche.Text + Properties.Paths.config;
            suchpfad = suchpfad.Replace("\\\\", "\\");
            var ids = new List<string>();

            //Überprüfen ob Pfad existiert, wenn nicht, gibt es eine exeption
            if (Directory.Exists(suchpfad))
            {
                try
                {
                    List<string> allFilesTemp = new List<string>();
                    List<string> allFiles = new List<string>();
                    
                    AddFileNamesToList(suchpfad, allFilesTemp, false);

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

                    return ids;
                }
                catch (Exception)
                {
                    return ids;
                }
            }
            return ids;
        }
        #endregion

        #region Bitset
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
            MessageDialogResult result = await this.ShowMessageAsync(Properties.Resources.dialogTitelBitset, message, MessageDialogStyle.Affirmative);

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

        private void Text_encode_dec_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Set the event as handled
            e.Handled = true;
            // Select the Text
            (sender as TextBox).SelectAll();
        }
        #endregion

        #region Helferfunktionen
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
                folderDialog.SelectedPath = Properties.Paths.drive_c;
            }

            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                text_projktpfad_helfer.Text = folderDialog.SelectedPath;
            }
            cb_select_me.Focus();

        }

        private async void FehlerHelferAsync()
        {
            await this.ShowMessageAsync(Properties.Resources.dialogTitelHelfer, Properties.Resources.dialogMsgHelferFehler, MessageDialogStyle.Affirmative);
        }

        private async void FehlerHelferAsyncME()
        {
            await this.ShowMessageAsync(Properties.Resources.dialogTitelHelfer, Properties.Resources.dialogMsgHelferFehlerME, MessageDialogStyle.Affirmative);
        }

        private void OpenFileOrFolder(string input)
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
                }
                catch (Exception)
                {
                    FehlerHelferAsync();
                }
            }
            
        }

        private void Btn_open_systemoptions_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.systemOptions;
            OpenFileOrFolder(open);
        }

        private void Bt_openFormProgram_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.formProgram;
            OpenFileOrFolder(open);
        }

        private void Bt_openDiagnoseData_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.diagnoseData;
            OpenFileOrFolder(open);
        }

        private void Bt_openDiagramSetup_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.diagramSetup;
            OpenFileOrFolder(open);
        }

        private void Bt_openPrjectFolder_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.machineParameter;
            OpenFileOrFolder(open);
        }

        private void Bt_openMachineParameter_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text;
            OpenFileOrFolder(open);
        }

        private void Btn_open_machinesetup_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.MachSetup;
            OpenFileOrFolder(open);
        }

        private void Bt_simStarten_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.Start_Simulation;
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
            }
            catch (Exception)
            {

                FehlerHelferAsync();
            }
        }

        private void Bt_visuStarten_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.Start_Visualization;
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = open,
                        WorkingDirectory = Path.GetDirectoryName(open)
                    }
                };
                process.Start();
            }
            catch (Exception)
            {

                FehlerHelferAsync();
            }
        }

        private void Bt_openConfig_Click(object sender, RoutedEventArgs e)
        {
            string open = text_projktpfad_helfer.Text + Properties.Paths.config;
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
            catch (Exception)
            {
                await this.ShowMessageAsync(Properties.Resources.dialogTitelBackup, Properties.Resources.dialogMsgBackupFehler, MessageDialogStyle.Affirmative);
            }
        }

        private void Cb_select_me_DropDownOpened(object sender, EventArgs e)
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

                AddFileNamesToList(suchpfad, allFiles, (bool)ts_binar_suche.IsChecked);

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


        #endregion

    }
}

