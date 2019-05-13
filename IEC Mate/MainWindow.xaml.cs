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
            string error = "#--> Fehler beim Erzeugen vom Code!" + Environment.NewLine + "#--> Die Anzahl Variabeln ist nicht identisch.";

            try
            {
                int lines = vars_1.Length;
                

                if (lines == 0)
                {
                    return "#--> Keine Variabeln in der Liste 1.";
                }

                if ((lines < vars_2.Length) || (lines < vars_3.Length))
                {
                    return error;
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
                return error;
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


        //private void MetroWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    fo_einstellungen.IsOpen = false;
        //}

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
    }

}
