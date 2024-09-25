using ActiproSoftware.Windows.Controls.Ribbon.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace TornadoSAR
{
    /// <summary>
    /// Interaction logic for FileSelector.xaml
    /// </summary>
    public partial class FileSelector : UserControl
    {
        private string filePath = null;

        public FileSelector()
        {
            InitializeComponent();
        }

        private void SelectFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "zip files (*.zip)|*.zip|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                fileBox.Text = openFileDialog.FileName;
                fileBox.Focus();
                // Move the caret to the end of the text box
                fileBox.Select(fileBox.Text.Length, 0);
            }
        }

        public bool IsEmpty()
        {
            return filePath == null;
        }

        public string GetFilePath()
        {
            return filePath;
        }
    }
}
