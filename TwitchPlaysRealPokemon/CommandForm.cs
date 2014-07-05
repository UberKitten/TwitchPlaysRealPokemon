using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TwitchPlaysRealPokemon
{
    class CommandForm : Form
    {
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // CommandForm
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(284, 350);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CommandForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CommandForm_FormClosed);
            this.Load += new System.EventHandler(this.CommandForm_Load);
            this.ResumeLayout(false);

        }


        public List<KeyValuePair<string, string>> lines = new List<KeyValuePair<string, string>>();
        private StringFormat nameFormat = new StringFormat();
        private StringFormat moveFormat = new StringFormat();
        private Font font = new Font("Minecraftia", 30);
        private Size fontSize;
        private Size rightBarSize;

        private System.Threading.Timer timer = null;

        private DateTime start = Properties.Settings.Default.StartTime;

        protected override void OnLoad(EventArgs e)
        {
            this.DesktopLocation = new Point(-1920, 0);
            this.ClientSize = new System.Drawing.Size(715, 678);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

            this.SetStyle(
     System.Windows.Forms.ControlStyles.UserPaint |
     System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
     System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer,
     true);

            nameFormat.Alignment = StringAlignment.Near;
            moveFormat.Alignment = StringAlignment.Far;

            fontSize = TextRenderer.MeasureText("TEST test TEST", font);
            rightBarSize = TextRenderer.MeasureText("select", font);


            timer = new System.Threading.Timer(new System.Threading.TimerCallback((o) =>
            {
                this.Invoke(new Action(() =>
                {
                    this.Invalidate();
                }));
            }), null, 0, 33); 

            base.OnLoad(e);
        }

        protected override void OnResize(EventArgs e)
        {
            this.Invalidate();
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);

            StringBuilder sbName = new StringBuilder();
            StringBuilder sbMove = new StringBuilder();

            Rectangle time = new Rectangle(e.ClipRectangle.X, e.ClipRectangle.Y, e.ClipRectangle.Width, fontSize.Height);
            Rectangle names = new Rectangle(e.ClipRectangle.X, e.ClipRectangle.Y + fontSize.Height, e.ClipRectangle.Width, e.ClipRectangle.Height - fontSize.Height);

            for (int i = Math.Max(0, lines.Count - (names.Height / fontSize.Height)); i < lines.Count; i++)
            {
                var nameSize = TextRenderer.MeasureText(lines[i].Key, font);
                double proportion = (double)(e.ClipRectangle.Width - rightBarSize.Width) / (double)nameSize.Width;

                if (proportion <= 1.02)
                {
                    sbName.AppendLine(lines[i].Key.Substring(0, (int)Math.Floor(((double)lines[i].Key.Length) * proportion)));
                }
                else
                {
                    sbName.AppendLine(lines[i].Key);
                }

                sbMove.AppendLine(lines[i].Value);
            }

            var span = DateTime.Now.Subtract(start);
            //var timer = String.Format("{0}d {1}h {2}m {3}s", span.Days, span.Hours, span.Minutes, span.Seconds);
            //TextRenderer.DrawText(e.Graphics, timer, font, time, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            TextRenderer.DrawText(e.Graphics, "WE BEAT RED! Read below!", font, time, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);

            TextRenderer.DrawText(e.Graphics, sbName.ToString(), font, names, Color.White, TextFormatFlags.Left | TextFormatFlags.Bottom);
            TextRenderer.DrawText(e.Graphics, sbMove.ToString(), font, names, Color.White, TextFormatFlags.Right | TextFormatFlags.Bottom);
           
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
        }

        private void CommandForm_Load(object sender, EventArgs e)
        {
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void CommandForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
