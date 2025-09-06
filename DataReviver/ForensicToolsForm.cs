using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace DataReviver
{
    public partial class ForensicToolsForm : Form
    {
        private TabControl tabControl;
        private ImageList tabIcons;

        public ForensicToolsForm()
        {
            this.Text = "Data Reviver - Working Forensic Tools";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(240, 248, 255);

            tabIcons = new ImageList();
            tabIcons.ImageSize = new Size(20, 20);
            // Add icons (use emoji for now, can be replaced with PNGs)
            tabIcons.Images.Add("fileinfo", CreateTabIcon("📄"));
            tabIcons.Images.Add("hash", CreateTabIcon("#"));
            tabIcons.Images.Add("viewer", CreateTabIcon("👁️"));
            tabIcons.Images.Add("type", CreateTabIcon("🔍"));

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.ImageList = tabIcons;

            // File Information Tab
            var fileInfoTab = new TabPage("File Information") { ImageIndex = 0 };
            // TODO: Add file info controls here
            tabControl.TabPages.Add(fileInfoTab);

            // Hash Calculator Tab
            var hashTab = new TabPage("Hash Calculator") { ImageIndex = 1 };
            // TODO: Add hash calculator controls here
            tabControl.TabPages.Add(hashTab);

            // Content Viewer Tab
            var viewerTab = new TabPage("Content Viewer") { ImageIndex = 2 };
            // TODO: Add content viewer controls here
            tabControl.TabPages.Add(viewerTab);

            // File Type Detector Tab
            var typeTab = new TabPage("File Type Detector") { ImageIndex = 3 };
            // TODO: Add file type detector controls here
            tabControl.TabPages.Add(typeTab);

            this.Controls.Clear();
            this.Controls.Add(tabControl);
        }

        // Helper to create a bitmap icon from emoji
        private Bitmap CreateTabIcon(string emoji)
        {
            Bitmap bmp = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Font font = new Font("Segoe UI Emoji", 13, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    g.DrawString(emoji, font, Brushes.Black, -2, 0);
                }
            }
            return bmp;
        }
    }
}
