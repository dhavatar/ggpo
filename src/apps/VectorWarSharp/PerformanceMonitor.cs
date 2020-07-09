using System.Drawing;
using System.Windows.Forms;

namespace VectorWar
{
    public partial class PerformanceMonitor : Form
    {
        const int MaxGraphSize = 4096;
        const int MaxFairness = 20;

        Pen greenPen = new Pen(new SolidBrush(Color.Green));
        Pen redPen = new Pen(new SolidBrush(Color.Red));
        Pen bluePen = new Pen(new SolidBrush(Color.Blue));
        Pen yellowPen = new Pen(new SolidBrush(Color.Yellow));
        Pen grayPen = new Pen(new SolidBrush(Color.Gray));
        Pen pinkPen = new Pen(new SolidBrush(Color.Pink));

        public PerformanceMonitor()
        {
            InitializeComponent();
        }

        private void networkGraph_Paint(object sender, PaintEventArgs e)
        {

        }

        private void fairnessGraph_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}
