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

namespace IECcodeGen
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : MetroWindow
    {
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

            text_code_template.ShowLineNumbers = true;
            text_code_template.TextArea.Foreground = Brushes.White;
            text_code_template.TextArea.FontSize = 13;
            text_code_template.TextArea.FontFamily = new FontFamily("Consolas");
            text_code_template.Options.ConvertTabsToSpaces = true;

            text_code_output.IsReadOnly = true;
            text_code_output.TextArea.Caret.CaretBrush = Brushes.Transparent;
            text_code_output.TextArea.Foreground = Brushes.White;
            text_code_output.TextArea.FontSize = 13;
            text_code_output.TextArea.FontFamily = new FontFamily("Consolas");
            text_code_output.ShowLineNumbers = true;
            text_code_output.Options.ConvertTabsToSpaces = true;

            //Theme
            ThemeManager.ChangeAppStyle(Application.Current,
                ThemeManager.GetAccent("Steel"),
                ThemeManager.GetAppTheme("BaseDark"));

            //ComboBox

            combo_vars.Items.Add("Variable_1");
            combo_vars.Items.Add("Variable_2");
            combo_vars.Items.Add("Variable_3");
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

            try
            {
                int lines = vars_1.Length;

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

                if (lines == 0)
                {
                    return "Fehler! Keine Variabeln in der Liste 1.";
                }
            }
            catch (Exception)
            {
                return "Fehler! Die Anzahl Variabeln scheint nicht identisch zu sein.";
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

        private void mi_leerzeichen_ein(object sender, RoutedEventArgs e)
        {
            text_code_template.Options.ShowSpaces = true;
        }

        private void mi_leerzeichen_aus(object sender, RoutedEventArgs e)
        {
            text_code_template.Options.ShowSpaces = false;
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(text_code_output.Text);
        }
    }

}
