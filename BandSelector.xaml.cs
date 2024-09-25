
using System.Windows.Controls;


namespace TornadoSAR
{
    public partial class BandSelector : UserControl
    {
        public BandSelector()
        {
            InitializeComponent();

            selectionBox.Items.Add("Band 1");
            selectionBox.Items.Add("Band 2");
            selectionBox.Items.Add("Band 3");
        }

        public int SelectedIndex
        {
            get
            {
                return selectionBox.SelectedIndex;
            }
            set
            {
                selectionBox.SelectedIndex = value;
            }
        }
    }
}
