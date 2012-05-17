using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Morphology;

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public AotDictionary AotMorph;

        public MainWindow()
        {
            InitializeComponent();
            AotMorph = new AotDictionary(@"..\..");
        }

        public string Normalize(string[] words)
        {
            var result = new List<string>();
            foreach (string word in words)
            {
                string[] gs = AotMorph.GetGramem(word.ToUpper());
                string g = String.Join(" ",gs);
                result.Add(g);
            }
            string[] ss = result.ToArray<string>();
            return String.Join(" ",ss);
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            char[] dd = {' '};
            string[] words = textBox1.Text.Split(dd, StringSplitOptions.RemoveEmptyEntries);
            textBox1.Text = Normalize(words);
        }
    }
}
