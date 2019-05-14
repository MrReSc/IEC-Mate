using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using Path = System.IO.Path;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro;
using MahApps.Metro.SimpleChildWindow;
using System.Diagnostics;
using System.Deployment.Application;
using WinForms = System.Windows.Forms; //FolderDialog
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls.Primitives;

namespace IECMate
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : MetroWindow
    {
        public static BrushConverter bc = new BrushConverter();
        public Brush DarkBackground = (Brush)bc.ConvertFromString("#4A4A4A");
        public string[] variablen_liste = new string[] { "Variable_1", "Variable_2", "Variable_3" };
        public Stack<string> undoList = new Stack<string>();

        public MainWindow()
        {
            InitializeComponent();

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

            //SplitButton
            combo_vars.ItemsSource = variablen_liste;

            //Einstellungen laden
            ThemeManager.ChangeAppStyle(Application.Current,
                            ThemeManager.GetAccent("Steel"),
                            ThemeManager.GetAppTheme(Properties.Settings.Default.theme));

            if (Properties.Settings.Default.theme == "BaseDark")
            {
                text_code_template.TextArea.Foreground = Brushes.White;
                text_code_output.TextArea.Foreground = Brushes.White;

                text_code_output.Background = DarkBackground;
                border_code_output.Background = DarkBackground;

                tg_theme.IsChecked = true;
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

            //Inhalt laden
            text_var1.Text = Inhalt.Default.variable_1;
            text_var2.Text = Inhalt.Default.variable_2;
            text_var3.Text = Inhalt.Default.variable_3;
            text_code_template.Text = Inhalt.Default.vorlage;
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
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                text_suchen.Text = text_code_template.SelectedText;
            }

        }

        private void Btn_gen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                text_code_output.Text = Code_gen("Variable_1", text_var1.Text,
                                                 "Variable_2", text_var2.Text,
                                                 "Variable_3", text_var3.Text,
                                                 text_code_template.Text);
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
            string error0 = "#--> Fehler beim Erzeugen vom Code!" + Environment.NewLine + "#--> Die Anzahl Variabeln ist nicht identisch.";
            string error1 = "#--> Fehler beim Erzeugen vom Code!" + Environment.NewLine + "#--> Keine Variable in Liste 1.";
            string error2 = "#--> Fehler beim Erzeugen vom Code!" + Environment.NewLine + "#--> Keine Variable in Liste 2.";
            string error3 = "#--> Fehler beim Erzeugen vom Code!" + Environment.NewLine + "#--> Keine Variable in Liste 3.";

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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CopyToClipboard_Click_1(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(text_code_output.Text);
        }

        private void Tg_leerzeichen_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)tg_leerzeichen.IsChecked)
            {
                text_code_template.Options.ShowSpaces = true;

                Properties.Settings.Default.leerzeichen = true;
                Properties.Settings.Default.Save();
            }
            else
            {
                text_code_template.Options.ShowSpaces = false;

                Properties.Settings.Default.leerzeichen = false;
                Properties.Settings.Default.Save();
            }
        }

        private void Tg_theme_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)tg_theme.IsChecked)
            {
                ThemeManager.ChangeAppStyle(Application.Current,
                                            ThemeManager.GetAccent("Steel"),
                                            ThemeManager.GetAppTheme("BaseDark"));

                text_code_template.TextArea.Foreground = Brushes.White;
                text_code_output.TextArea.Foreground = Brushes.White;
                text_code_output.Background = DarkBackground;
                border_code_output.Background = DarkBackground;

                Properties.Settings.Default.theme = "BaseDark";
                Properties.Settings.Default.Save();
            }
            else
            {
                ThemeManager.ChangeAppStyle(Application.Current,
                                            ThemeManager.GetAccent("Steel"),
                                            ThemeManager.GetAppTheme("BaseLight"));

                text_code_template.TextArea.Foreground = Brushes.Black;
                text_code_output.TextArea.Foreground = Brushes.Black;

                text_code_output.Background = Brushes.Gainsboro;
                border_code_output.Background = Brushes.Gainsboro;

                Properties.Settings.Default.theme = "BaseLight";
                Properties.Settings.Default.Save();
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            text_code_template.TextArea.FontSize = (double)nc_font_size.Value;
            text_code_output.TextArea.FontSize = (double)nc_font_size.Value;

            Properties.Settings.Default.schriftgrosse = (double)nc_font_size.Value;
            Properties.Settings.Default.Save();
        }

        private void Tg_line_no_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)tg_line_no.IsChecked)
            {
                text_code_template.ShowLineNumbers = true;
                text_code_output.ShowLineNumbers = true;

                Properties.Settings.Default.zeilennummern = true;
                Properties.Settings.Default.Save();
            }
            else
            {
                text_code_template.ShowLineNumbers = false;
                text_code_output.ShowLineNumbers = false;

                Properties.Settings.Default.zeilennummern = false;
                Properties.Settings.Default.Save();
            }
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

            Inhalt.Default.variable_1 = text_var1.Text;
            Inhalt.Default.variable_2 = text_var2.Text;
            Inhalt.Default.variable_3 = text_var3.Text;
            Inhalt.Default.vorlage = text_code_template.Text;

            Inhalt.Default.Save();
            Properties.Settings.Default.Save();
        }

        private void Btn_template_loschen_Click(object sender, RoutedEventArgs e)
        {
            text_code_template.Text = "";
        }


        private void MenuItem_Click_About(object sender, RoutedEventArgs e)
        {
            child_Infos.IsOpen = true;
            try
            {
                //// get deployment version
                lb_version.Content = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (InvalidDeploymentException)
            {
                //// you cannot read publish version when app isn't installed 
                //// (e.g. during debug)
                lb_version.Content = "not installed";
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void Btn_template_undo_Click(object sender, RoutedEventArgs e)
        {
            if (undoList.Count > 0)
            {
                text_code_template.Text = undoList.Pop();
            }

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
            if (!String.IsNullOrWhiteSpace(text_projktpfad_suche.Text))
            {
                folderDialog.SelectedPath = text_projktpfad_suche.Text;
            }
            else
            {
                folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            }
           
            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                String sPath = folderDialog.SelectedPath;
                text_projktpfad_suche.Text = sPath;

                Properties.Settings.Default.projekt_pfad_suche = sPath;
                Properties.Settings.Default.Save();
            }
        }

        private async void Btn_suche_Click(object sender, RoutedEventArgs e)
        {
            listbox_ergebnis.Items.Clear();

            if ((!String.IsNullOrEmpty(text_pattern_suche.Text)) && (!(String.IsNullOrWhiteSpace(text_pattern_suche.Text))))
            {
                var x = await this.ShowProgressAsync("Suchen", "Die Suche läuft. Bite warten...", true) as ProgressDialogController;
                x.SetIndeterminate();

                try
                {
                    List<string> allFiles = new List<string>();
                    string suchpfad = "";

                    if ((bool)ts_kbus_suche.IsChecked)
                    {
                        suchpfad = text_projktpfad_suche.Text + "\\application\\control\\config";
                    }
                    else
                    {
                        suchpfad = text_projktpfad_suche.Text;
                    }

                    AddFileNamesToList(suchpfad, allFiles, (bool)ts_binar_suche.IsChecked);

                    if ((!String.IsNullOrEmpty(text_pattern_suche.Text)) && (x.IsOpen))
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
                    ;
                }

                await x.CloseAsync();
            }
            else
            {
                if (String.IsNullOrWhiteSpace(text_pattern_suche.Text))
                {
                    text_pattern_suche.Text = "";
                }
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

        private void Btn_suche_offnen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedText = listbox_ergebnis.SelectedItem.ToString();

                if (!String.IsNullOrEmpty(selectedText))
                {
                    System.Diagnostics.Process.Start(selectedText);
                }
            }
            catch (Exception)
            {
                ;
            }
            
        }


        private void Encoding_Checked(object sender, RoutedEventArgs args)
        {
            long ResulateDezimal = 0;
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
    }
}
