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
            this.userTextComponent = new TextComponent.UserTextComponent();
            this.SuspendLayout();
            // 
            // userTextComponent
            // 
            resources.ApplyResources(this.userTextComponent, "userTextComponent");
            this.userTextComponent.Name = "userTextComponent";
            this.userTextComponent.TextChanged += new TextComponent.UserTextComponent.TextChangedEvent(this.TextEdited);
            // 
            // FormForText
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Controls.Add(this.userTextComponent);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "FormForText";
            this.ResumeLayout(false);

        }

        #endregion

        private UserTextComponent userTextComponent;
    }
}