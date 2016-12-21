using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CoachDraw
{
    public partial class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            this.DoubleBuffered = true;
        }
    }
}
