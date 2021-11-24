namespace TextComponent
{
    partial class FormForText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormForText));
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SelectionStartLabel = new System.Windows.Forms.Label();
            this.SelectionFinishLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // richTextBox
            // 
            resources.ApplyResources(this.richTextBox, "richTextBox");
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.SelectionChanged += new System.EventHandler(this.RichTextBoxSelectionChanged);
            this.richTextBox.TextChanged += new System.EventHandler(this.richTextBoxTextChanged);
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.Name = "button2";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // SelectionStartLabel
            // 
            resources.ApplyResources(this.SelectionStartLabel, "SelectionStartLabel");
            this.SelectionStartLabel.ForeColor = System.Drawing.Color.White;
            this.SelectionStartLabel.Name = "SelectionStartLabel";
            // 
            // SelectionFinishLabel
            // 
            resources.ApplyResources(this.SelectionFinishLabel, "SelectionFinishLabel");
            this.SelectionFinishLabel.ForeColor = System.Drawing.Color.White;
            this.SelectionFinishLabel.Name = "SelectionFinishLabel";
            // 
            // FormForText
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Controls.Add(this.SelectionFinishLabel);
            this.Controls.Add(this.SelectionStartLabel);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "FormForText";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private RichTextBox richTextBox;
        private Button button1;
        private Button button2;
        private Label SelectionStartLabel;
        private Label SelectionFinishLabel;
    }
}