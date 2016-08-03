namespace FameBase
{
	partial class Interface
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Interface));
            this.menu = new System.Windows.Forms.ToolStrip();
            this.ModelFile = new System.Windows.Forms.ToolStripDropDownButton();
            this.open3D = new System.Windows.Forms.ToolStripMenuItem();
            this.import3D = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAs3D = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSegmentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outputSeqToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadTriMeshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tools = new System.Windows.Forms.ToolStripDropDownButton();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.screenCaptureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractStrokesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderOption = new System.Windows.Forms.ToolStripDropDownButton();
            this.vertexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wireFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.faceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectElement = new System.Windows.Forms.ToolStripDropDownButton();
            this.vertexSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.edgeSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.faceSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.strokeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.boxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sketchTool = new System.Windows.Forms.ToolStripDropDownButton();
            this.sketchToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.eraserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.viewPanel = new System.Windows.Forms.SplitContainer();
            this.fileNameTabs = new System.Windows.Forms.TabControl();
            this.toolboxPanel = new System.Windows.Forms.Panel();
            this.glViewer = new FameBase.GLViewer();
            this.keyboardLabel = new System.Windows.Forms.Label();
            this.strokeColorDialog = new System.Windows.Forms.ColorDialog();
            this.menu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.viewPanel)).BeginInit();
            this.viewPanel.Panel1.SuspendLayout();
            this.viewPanel.Panel2.SuspendLayout();
            this.viewPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ModelFile,
            this.tools,
            this.renderOption,
            this.selectElement,
            this.sketchTool});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(866, 39);
            this.menu.TabIndex = 0;
            this.menu.Text = "toolStrip1";
            // 
            // ModelFile
            // 
            this.ModelFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.open3D,
            this.import3D,
            this.saveAs3D,
            this.loadSegmentsToolStripMenuItem,
            this.outputSeqToolStripMenuItem,
            this.loadTriMeshToolStripMenuItem});
            this.ModelFile.Image = ((System.Drawing.Image)(resources.GetObject("ModelFile.Image")));
            this.ModelFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ModelFile.Name = "ModelFile";
            this.ModelFile.Size = new System.Drawing.Size(45, 36);
            this.ModelFile.Text = "File";
            this.ModelFile.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // open3D
            // 
            this.open3D.Name = "open3D";
            this.open3D.Size = new System.Drawing.Size(183, 22);
            this.open3D.Text = "Open 3D model";
            this.open3D.Click += new System.EventHandler(this.open3D_Click);
            // 
            // import3D
            // 
            this.import3D.Name = "import3D";
            this.import3D.Size = new System.Drawing.Size(183, 22);
            this.import3D.Text = "Import 3D model";
            this.import3D.Click += new System.EventHandler(this.import3D_Click);
            // 
            // saveAs3D
            // 
            this.saveAs3D.Name = "saveAs3D";
            this.saveAs3D.Size = new System.Drawing.Size(183, 22);
            this.saveAs3D.Text = "Save As 3D model";
            this.saveAs3D.Click += new System.EventHandler(this.saveAs3D_Click);
            // 
            // loadSegmentsToolStripMenuItem
            // 
            this.loadSegmentsToolStripMenuItem.Name = "loadSegmentsToolStripMenuItem";
            this.loadSegmentsToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.loadSegmentsToolStripMenuItem.Text = "Load Segments";
            this.loadSegmentsToolStripMenuItem.Click += new System.EventHandler(this.loadSegmentsToolStripMenuItem_Click);
            // 
            // outputSeqToolStripMenuItem
            // 
            this.outputSeqToolStripMenuItem.Name = "outputSeqToolStripMenuItem";
            this.outputSeqToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.outputSeqToolStripMenuItem.Text = "Output Primitive seq";
            this.outputSeqToolStripMenuItem.Click += new System.EventHandler(this.outputSeqToolStripMenuItem_Click);
            // 
            // loadTriMeshToolStripMenuItem
            // 
            this.loadTriMeshToolStripMenuItem.Name = "loadTriMeshToolStripMenuItem";
            this.loadTriMeshToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.loadTriMeshToolStripMenuItem.Text = "Load As TriMesh";
            this.loadTriMeshToolStripMenuItem.Click += new System.EventHandler(this.loadTriMeshToolStripMenuItem_Click);
            // 
            // tools
            // 
            this.tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewToolStripMenuItem,
            this.resetViewToolStripMenuItem,
            this.modelColorToolStripMenuItem,
            this.reloadViewToolStripMenuItem,
            this.saveViewToolStripMenuItem,
            this.loadViewToolStripMenuItem,
            this.screenCaptureToolStripMenuItem,
            this.extractStrokesToolStripMenuItem,
            this.renderToolStripMenuItem,
            this.clearAllToolStripMenuItem});
            this.tools.Image = ((System.Drawing.Image)(resources.GetObject("tools.Image")));
            this.tools.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tools.Name = "tools";
            this.tools.Size = new System.Drawing.Size(49, 36);
            this.tools.Text = "Tools";
            this.tools.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.viewToolStripMenuItem.Text = "View";
            this.viewToolStripMenuItem.Click += new System.EventHandler(this.viewToolStripMenuItem_Click);
            // 
            // resetViewToolStripMenuItem
            // 
            this.resetViewToolStripMenuItem.Name = "resetViewToolStripMenuItem";
            this.resetViewToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.resetViewToolStripMenuItem.Text = "Reset View";
            this.resetViewToolStripMenuItem.Click += new System.EventHandler(this.resetViewToolStripMenuItem_Click);
            // 
            // modelColorToolStripMenuItem
            // 
            this.modelColorToolStripMenuItem.Name = "modelColorToolStripMenuItem";
            this.modelColorToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.modelColorToolStripMenuItem.Text = "Model Color";
            this.modelColorToolStripMenuItem.Click += new System.EventHandler(this.modelColorToolStripMenuItem_Click);
            // 
            // reloadViewToolStripMenuItem
            // 
            this.reloadViewToolStripMenuItem.Name = "reloadViewToolStripMenuItem";
            this.reloadViewToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.reloadViewToolStripMenuItem.Text = "Reload View";
            this.reloadViewToolStripMenuItem.Click += new System.EventHandler(this.reloadViewToolStripMenuItem_Click);
            // 
            // saveViewToolStripMenuItem
            // 
            this.saveViewToolStripMenuItem.Name = "saveViewToolStripMenuItem";
            this.saveViewToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.saveViewToolStripMenuItem.Text = "Save view";
            this.saveViewToolStripMenuItem.Click += new System.EventHandler(this.saveViewToolStripMenuItem_Click);
            // 
            // loadViewToolStripMenuItem
            // 
            this.loadViewToolStripMenuItem.Name = "loadViewToolStripMenuItem";
            this.loadViewToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.loadViewToolStripMenuItem.Text = "Load view";
            this.loadViewToolStripMenuItem.Click += new System.EventHandler(this.loadViewToolStripMenuItem_Click);
            // 
            // screenCaptureToolStripMenuItem
            // 
            this.screenCaptureToolStripMenuItem.Name = "screenCaptureToolStripMenuItem";
            this.screenCaptureToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.screenCaptureToolStripMenuItem.Text = "Screen Capture";
            this.screenCaptureToolStripMenuItem.Click += new System.EventHandler(this.screenCaptureToolStripMenuItem_Click);
            // 
            // extractStrokesToolStripMenuItem
            // 
            this.extractStrokesToolStripMenuItem.Name = "extractStrokesToolStripMenuItem";
            this.extractStrokesToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.extractStrokesToolStripMenuItem.Text = "Extract strokes";
            // 
            // renderToolStripMenuItem
            // 
            this.renderToolStripMenuItem.Name = "renderToolStripMenuItem";
            this.renderToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.renderToolStripMenuItem.Text = "Render";
            this.renderToolStripMenuItem.Click += new System.EventHandler(this.renderToolStripMenuItem_Click);
            // 
            // clearAllToolStripMenuItem
            // 
            this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
            this.clearAllToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.clearAllToolStripMenuItem.Text = "clear all";
            this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.clearAllToolStripMenuItem_Click);
            // 
            // renderOption
            // 
            this.renderOption.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vertexToolStripMenuItem,
            this.wireFrameToolStripMenuItem,
            this.faceToolStripMenuItem});
            this.renderOption.Image = ((System.Drawing.Image)(resources.GetObject("renderOption.Image")));
            this.renderOption.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.renderOption.Name = "renderOption";
            this.renderOption.Size = new System.Drawing.Size(57, 36);
            this.renderOption.Text = "Render";
            this.renderOption.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // vertexToolStripMenuItem
            // 
            this.vertexToolStripMenuItem.Name = "vertexToolStripMenuItem";
            this.vertexToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.vertexToolStripMenuItem.Text = "Vertex";
            this.vertexToolStripMenuItem.Click += new System.EventHandler(this.pointToolStripMenuItem_Click);
            // 
            // wireFrameToolStripMenuItem
            // 
            this.wireFrameToolStripMenuItem.Name = "wireFrameToolStripMenuItem";
            this.wireFrameToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.wireFrameToolStripMenuItem.Text = "WireFrame";
            this.wireFrameToolStripMenuItem.Click += new System.EventHandler(this.wireFrameToolStripMenuItem_Click);
            // 
            // faceToolStripMenuItem
            // 
            this.faceToolStripMenuItem.Checked = true;
            this.faceToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.faceToolStripMenuItem.Name = "faceToolStripMenuItem";
            this.faceToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.faceToolStripMenuItem.Text = "Face";
            this.faceToolStripMenuItem.Click += new System.EventHandler(this.faceToolStripMenuItem_Click);
            // 
            // selectElement
            // 
            this.selectElement.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vertexSelection,
            this.edgeSelection,
            this.faceSelection,
            this.strokeToolStripMenuItem,
            this.boxToolStripMenuItem});
            this.selectElement.Image = ((System.Drawing.Image)(resources.GetObject("selectElement.Image")));
            this.selectElement.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.selectElement.Name = "selectElement";
            this.selectElement.Size = new System.Drawing.Size(51, 36);
            this.selectElement.Text = "Select";
            this.selectElement.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // vertexSelection
            // 
            this.vertexSelection.Name = "vertexSelection";
            this.vertexSelection.Size = new System.Drawing.Size(106, 22);
            this.vertexSelection.Text = "vertex";
            this.vertexSelection.Click += new System.EventHandler(this.vertexSelection_Click);
            // 
            // edgeSelection
            // 
            this.edgeSelection.Name = "edgeSelection";
            this.edgeSelection.Size = new System.Drawing.Size(106, 22);
            this.edgeSelection.Text = "edge";
            this.edgeSelection.Click += new System.EventHandler(this.edgeSelection_Click);
            // 
            // faceSelection
            // 
            this.faceSelection.Name = "faceSelection";
            this.faceSelection.Size = new System.Drawing.Size(106, 22);
            this.faceSelection.Text = "face";
            this.faceSelection.Click += new System.EventHandler(this.faceSelection_Click);
            // 
            // strokeToolStripMenuItem
            // 
            this.strokeToolStripMenuItem.Name = "strokeToolStripMenuItem";
            this.strokeToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
            this.strokeToolStripMenuItem.Text = "stroke";
            this.strokeToolStripMenuItem.Click += new System.EventHandler(this.strokeToolStripMenuItem_Click);
            // 
            // boxToolStripMenuItem
            // 
            this.boxToolStripMenuItem.Name = "boxToolStripMenuItem";
            this.boxToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
            this.boxToolStripMenuItem.Text = "box";
            this.boxToolStripMenuItem.Click += new System.EventHandler(this.boxToolStripMenuItem_Click);
            // 
            // sketchTool
            // 
            this.sketchTool.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sketchToolStripMenuItem1,
            this.eraserToolStripMenuItem,
            this.clearAllToolStripMenuItem1});
            this.sketchTool.Image = ((System.Drawing.Image)(resources.GetObject("sketchTool.Image")));
            this.sketchTool.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.sketchTool.Name = "sketchTool";
            this.sketchTool.Size = new System.Drawing.Size(55, 36);
            this.sketchTool.Text = "Sketch";
            this.sketchTool.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // sketchToolStripMenuItem1
            // 
            this.sketchToolStripMenuItem1.Name = "sketchToolStripMenuItem1";
            this.sketchToolStripMenuItem1.Size = new System.Drawing.Size(116, 22);
            this.sketchToolStripMenuItem1.Text = "Sketch";
            this.sketchToolStripMenuItem1.Click += new System.EventHandler(this.sketchToolStripMenuItem1_Click);
            // 
            // eraserToolStripMenuItem
            // 
            this.eraserToolStripMenuItem.Name = "eraserToolStripMenuItem";
            this.eraserToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.eraserToolStripMenuItem.Text = "Eraser";
            this.eraserToolStripMenuItem.Click += new System.EventHandler(this.eraserToolStripMenuItem_Click_1);
            // 
            // clearAllToolStripMenuItem1
            // 
            this.clearAllToolStripMenuItem1.Name = "clearAllToolStripMenuItem1";
            this.clearAllToolStripMenuItem1.Size = new System.Drawing.Size(116, 22);
            this.clearAllToolStripMenuItem1.Text = "Clear all";
            this.clearAllToolStripMenuItem1.Click += new System.EventHandler(this.clearAllToolStripMenuItem1_Click);
            // 
            // viewPanel
            // 
            this.viewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewPanel.Location = new System.Drawing.Point(0, 39);
            this.viewPanel.Name = "viewPanel";
            this.viewPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // viewPanel.Panel1
            // 
            this.viewPanel.Panel1.Controls.Add(this.fileNameTabs);
            // 
            // viewPanel.Panel2
            // 
            this.viewPanel.Panel2.Controls.Add(this.toolboxPanel);
            this.viewPanel.Panel2.Controls.Add(this.glViewer);
            this.viewPanel.Panel2.Controls.Add(this.keyboardLabel);
            this.viewPanel.Size = new System.Drawing.Size(866, 735);
            this.viewPanel.SplitterDistance = 35;
            this.viewPanel.TabIndex = 1;
            // 
            // fileNameTabs
            // 
            this.fileNameTabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileNameTabs.Location = new System.Drawing.Point(3, 0);
            this.fileNameTabs.Name = "fileNameTabs";
            this.fileNameTabs.SelectedIndex = 0;
            this.fileNameTabs.Size = new System.Drawing.Size(860, 35);
            this.fileNameTabs.TabIndex = 0;
            // 
            // toolboxPanel
            // 
            this.toolboxPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.toolboxPanel.AutoSize = true;
            this.toolboxPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.toolboxPanel.Location = new System.Drawing.Point(3, 3);
            this.toolboxPanel.Name = "toolboxPanel";
            this.toolboxPanel.Size = new System.Drawing.Size(171, 694);
            this.toolboxPanel.TabIndex = 2;
            // 
            // glViewer
            // 
            this.glViewer.AccumBits = ((byte)(0));
            this.glViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.glViewer.AutoCheckErrors = false;
            this.glViewer.AutoFinish = false;
            this.glViewer.AutoMakeCurrent = true;
            this.glViewer.AutoSwapBuffers = true;
            this.glViewer.BackColor = System.Drawing.Color.Black;
            this.glViewer.ColorBits = ((byte)(32));
            this.glViewer.CurrentUIMode = FameBase.GLViewer.UIMode.Viewing;
            this.glViewer.DepthBits = ((byte)(16));
            this.glViewer.Location = new System.Drawing.Point(171, 3);
            this.glViewer.Name = "glViewer";
            this.glViewer.Size = new System.Drawing.Size(692, 694);
            this.glViewer.StencilBits = ((byte)(0));
            this.glViewer.TabIndex = 12;
            // 
            // keyboardLabel
            // 
            this.keyboardLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.keyboardLabel.AutoSize = true;
            this.keyboardLabel.BackColor = System.Drawing.Color.Aquamarine;
            this.keyboardLabel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.keyboardLabel.Location = new System.Drawing.Point(176, 615);
            this.keyboardLabel.Name = "keyboardLabel";
            this.keyboardLabel.Size = new System.Drawing.Size(120, 78);
            this.keyboardLabel.TabIndex = 6;
            this.keyboardLabel.Text = "Space: unlock view\r\nCtrl + C: clear all strokes\r\nS: sketch mode\r\nE: eraser mode\r\n" +
    "V: view mode\r\nR: reset view";
            // 
            // Interface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(866, 774);
            this.Controls.Add(this.viewPanel);
            this.Controls.Add(this.menu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Interface";
            this.Text = "Fame";
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.viewPanel.Panel1.ResumeLayout(false);
            this.viewPanel.Panel2.ResumeLayout(false);
            this.viewPanel.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.viewPanel)).EndInit();
            this.viewPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion


		private System.Windows.Forms.ToolStrip menu;
		private System.Windows.Forms.ToolStripDropDownButton ModelFile;
		private System.Windows.Forms.ToolStripMenuItem open3D;
		private System.Windows.Forms.ToolStripMenuItem import3D;
        private System.Windows.Forms.ToolStripMenuItem saveAs3D;
        private System.Windows.Forms.ToolStripDropDownButton selectElement;
        private System.Windows.Forms.ToolStripMenuItem vertexSelection;
        private System.Windows.Forms.ToolStripMenuItem edgeSelection;
        private System.Windows.Forms.ToolStripMenuItem faceSelection;
        private System.Windows.Forms.SplitContainer viewPanel;
        private System.Windows.Forms.TabControl fileNameTabs;
        private System.Windows.Forms.ToolStripDropDownButton renderOption;
        private System.Windows.Forms.ToolStripMenuItem vertexToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wireFrameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem faceToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tools;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modelColorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSegmentsToolStripMenuItem;
        private System.Windows.Forms.Panel toolboxPanel;
        private System.Windows.Forms.ColorDialog strokeColorDialog;
        private System.Windows.Forms.ToolStripMenuItem reloadViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outputSeqToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadTriMeshToolStripMenuItem;
        private System.Windows.Forms.Label keyboardLabel;
        private System.Windows.Forms.ToolStripMenuItem screenCaptureToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem extractStrokesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem strokeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem boxToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton sketchTool;
        private System.Windows.Forms.ToolStripMenuItem sketchToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem eraserToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem1;
        private GLViewer glViewer;

	}
}

