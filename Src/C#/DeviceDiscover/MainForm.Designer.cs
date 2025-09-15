namespace DeviceDiscover
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonDiscover = new Button();
            listBoxFound = new ListBox();
            SuspendLayout();
            // 
            // buttonDiscover
            // 
            buttonDiscover.Location = new Point(12, 12);
            buttonDiscover.Name = "buttonDiscover";
            buttonDiscover.Size = new Size(75, 23);
            buttonDiscover.TabIndex = 0;
            buttonDiscover.Text = "Discover";
            buttonDiscover.UseVisualStyleBackColor = true;
            buttonDiscover.Click += ButtonDiscover_Click;
            // 
            // listBoxFound
            // 
            listBoxFound.FormattingEnabled = true;
            listBoxFound.ItemHeight = 17;
            listBoxFound.Location = new Point(93, 12);
            listBoxFound.Name = "listBoxFound";
            listBoxFound.Size = new Size(695, 429);
            listBoxFound.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(listBoxFound);
            Controls.Add(buttonDiscover);
            Name = "Form1";
            Text = "Discover motion controllers in the network via UDP broadcast.";
            ResumeLayout(false);
        }

        #endregion

        private Button buttonDiscover;
        private ListBox listBoxFound;
    }
}
