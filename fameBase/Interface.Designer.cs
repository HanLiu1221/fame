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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Interface));
            this.menu = new System.Windows.Forms.ToolStrip();
            this.model = new System.Windows.Forms.ToolStripDropDownButton();
            this.loadAPartBasedModel = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadPartBasedModels = new System.Windows.Forms.ToolStripMenuItem();
            this.file = new System.Windows.Forms.ToolStripDropDownButton();
            this.open3D = new System.Windows.Forms.ToolStripMenuItem();
            this.import3D = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAs3D = new System.Windows.Forms.ToolStripMenuItem();
            this.loadTriMeshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadHumanPoseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveHumanPoseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMergedMeshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unitifyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tools = new System.Windows.Forms.ToolStripDropDownButton();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.screenCaptureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchXYToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchXZToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.swtichYZToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderOption = new System.Windows.Forms.ToolStripDropDownButton();
            this.vertexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wireFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.faceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.boundingBoxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.axesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectElement = new System.Windows.Forms.ToolStripDropDownButton();
            this.vertexSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.edgeSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.faceSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.boxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelEdit = new System.Windows.Forms.ToolStripDropDownButton();
            this.addSelectedParts = new System.Windows.Forms.ToolStripMenuItem();
            this.translateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scaleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rotateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replicateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewPanel = new System.Windows.Forms.SplitContainer();
            this.fileNameTabs = new System.Windows.Forms.TabControl();
            this.statsLabel = new System.Windows.Forms.Label();
            this.glViewer = new FameBase.GLViewer();
            this.partBasket = new System.Windows.Forms.FlowLayoutPanel();
            this.modelViewLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.strokeColorDialog = new System.Windows.Forms.ColorDialog();
            this.partRelatedTools = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.groupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.composeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.viewPanel)).BeginInit();
            this.viewPanel.Panel1.SuspendLayout();
            this.viewPanel.Panel2.SuspendLayout();
            this.viewPanel.SuspendLayout();
            this.partRelatedTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.model,
            this.file,
            this.tools,
            this.renderOption,
            this.selectElement,
            this.modelEdit});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(1099, 39);
            this.menu.TabIndex = 0;
            this.menu.Text = "toolStrip1";
            // 
            // model
            // 
            this.model.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadAPartBasedModel,
            this.saveAModelToolStripMenuItem,
            this.loadPartBasedModels});
            this.model.Image = ((System.Drawing.Image)(resources.GetObject("model.Image")));
            this.model.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.model.Name = "model";
            this.model.Size = new System.Drawing.Size(54, 36);
            this.model.Text = "Model";
            this.model.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // loadAPartBasedModel
            // 
            this.loadAPartBasedModel.Name = "loadAPartBasedModel";
            this.loadAPartBasedModel.Size = new System.Drawing.Size(146, 22);
            this.loadAPartBasedModel.Text = "Load a model";
            this.loadAPartBasedModel.Click += new System.EventHandler(this.loadAPartBasedModel_Click);
            // 
            // saveAModelToolStripMenuItem
            // 
            this.saveAModelToolStripMenuItem.Name = "saveAModelToolStripMenuItem";
            this.saveAModelToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.saveAModelToolStripMenuItem.Text = "Save a model";
            this.saveAModelToolStripMenuItem.Click += new System.EventHandler(this.saveAModelToolStripMenuItem_Click);
            // 
            // loadPartBasedModels
            // 
            this.loadPartBasedModels.Name = "loadPartBasedModels";
            this.loadPartBasedModels.Size = new System.Drawing.Size(146, 22);
            this.loadPartBasedModels.Text = "Load models";
            this.loadPartBasedModels.Click += new System.EventHandler(this.loadPartBasedModels_Click);
            // 
            // file
            // 
            this.file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.open3D,
            this.import3D,
            this.saveAs3D,
            this.loadTriMeshToolStripMenuItem,
            this.loadHumanPoseToolStripMenuItem,
            this.saveHumanPoseToolStripMenuItem,
            this.saveMergedMeshToolStripMenuItem,
            this.unitifyToolStripMenuItem});
            this.file.Image = ((System.Drawing.Image)(resources.GetObject("file.Image")));
            this.file.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.file.Name = "file";
            this.file.Size = new System.Drawing.Size(45, 36);
            this.file.Text = "File";
            this.file.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // open3D
            // 
            this.open3D.Name = "open3D";
            this.open3D.Size = new System.Drawing.Size(174, 22);
            this.open3D.Text = "Open 3D file";
            this.open3D.Click += new System.EventHandler(this.open3D_Click);
            // 
            // import3D
            // 
            this.import3D.Name = "import3D";
            this.import3D.Size = new System.Drawing.Size(174, 22);
            this.import3D.Text = "Import 3D file";
            this.import3D.Click += new System.EventHandler(this.import3D_Click);
            // 
            // saveAs3D
            // 
            this.saveAs3D.Name = "saveAs3D";
            this.saveAs3D.Size = new System.Drawing.Size(174, 22);
            this.saveAs3D.Text = "Save as 3D file";
            this.saveAs3D.Click += new System.EventHandler(this.saveAs3D_Click);
            // 
            // loadTriMeshToolStripMenuItem
            // 
            this.loadTriMeshToolStripMenuItem.Name = "loadTriMeshToolStripMenuItem";
            this.loadTriMeshToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.loadTriMeshToolStripMenuItem.Text = "Load As TriMesh";
            this.loadTriMeshToolStripMenuItem.Click += new System.EventHandler(this.loadTriMeshToolStripMenuItem_Click);
            // 
            // loadHumanPoseToolStripMenuItem
            // 
            this.loadHumanPoseToolStripMenuItem.Name = "loadHumanPoseToolStripMenuItem";
            this.loadHumanPoseToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.loadHumanPoseToolStripMenuItem.Text = "Load HumanPose";
            this.loadHumanPoseToolStripMenuItem.Click += new System.EventHandler(this.loadHumanPoseToolStripMenuItem_Click);
            // 
            // saveHumanPoseToolStripMenuItem
            // 
            this.saveHumanPoseToolStripMenuItem.Name = "saveHumanPoseToolStripMenuItem";
            this.saveHumanPoseToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.saveHumanPoseToolStripMenuItem.Text = "Save HumanPose";
            this.saveHumanPoseToolStripMenuItem.Click += new System.EventHandler(this.saveHumanPoseToolStripMenuItem_Click);
            // 
            // saveMergedMeshToolStripMenuItem
            // 
            this.saveMergedMeshToolStripMenuItem.Name = "saveMergedMeshToolStripMenuItem";
            this.saveMergedMeshToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.saveMergedMeshToolStripMenuItem.Text = "Save merged mesh";
            this.saveMergedMeshToolStripMenuItem.Click += new System.EventHandler(this.saveMergedMeshToolStripMenuItem_Click);
            // 
            // unitifyToolStripMenuItem
            // 
            this.unitifyToolStripMenuItem.Checked = true;
            this.unitifyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.unitifyToolStripMenuItem.Name = "unitifyToolStripMenuItem";
            this.unitifyToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.unitifyToolStripMenuItem.Text = "Unitify";
            this.unitifyToolStripMenuItem.Click += new System.EventHandler(this.unitifyToolStripMenuItem_Click);
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
            this.renderToolStripMenuItem,
            this.clearAllToolStripMenuItem,
            this.switchXYToolStripMenuItem,
            this.switchXZToolStripMenuItem,
            this.swtichYZToolStripMenuItem});
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
            // switchXYToolStripMenuItem
            // 
            this.switchXYToolStripMenuItem.Name = "switchXYToolStripMenuItem";
            this.switchXYToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.switchXYToolStripMenuItem.Text = "Switch XY";
            this.switchXYToolStripMenuItem.Click += new System.EventHandler(this.switchXYToolStripMenuItem_Click);
            // 
            // switchXZToolStripMenuItem
            // 
            this.switchXZToolStripMenuItem.Name = "switchXZToolStripMenuItem";
            this.switchXZToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.switchXZToolStripMenuItem.Text = "Switch XZ";
            this.switchXZToolStripMenuItem.Click += new System.EventHandler(this.switchXZToolStripMenuItem_Click);
            // 
            // swtichYZToolStripMenuItem
            // 
            this.swtichYZToolStripMenuItem.Name = "swtichYZToolStripMenuItem";
            this.swtichYZToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.swtichYZToolStripMenuItem.Text = "Swtich YZ";
            this.swtichYZToolStripMenuItem.Click += new System.EventHandler(this.swtichYZToolStripMenuItem_Click);
            // 
            // renderOption
            // 
            this.renderOption.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vertexToolStripMenuItem,
            this.wireFrameToolStripMenuItem,
            this.faceToolStripMenuItem,
            this.boundingBoxToolStripMenuItem,
            this.axesToolStripMenuItem,
            this.groundToolStripMenuItem});
            this.renderOption.Image = ((System.Drawing.Image)(resources.GetObject("renderOption.Image")));
            this.renderOption.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.renderOption.Name = "renderOption";
            this.renderOption.Size = new System.Drawing.Size(47, 36);
            this.renderOption.Text = "Draw";
            this.renderOption.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // vertexToolStripMenuItem
            // 
            this.vertexToolStripMenuItem.Name = "vertexToolStripMenuItem";
            this.vertexToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.vertexToolStripMenuItem.Text = "Vertex";
            this.vertexToolStripMenuItem.Click += new System.EventHandler(this.pointToolStripMenuItem_Click);
            // 
            // wireFrameToolStripMenuItem
            // 
            this.wireFrameToolStripMenuItem.Name = "wireFrameToolStripMenuItem";
            this.wireFrameToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.wireFrameToolStripMenuItem.Text = "WireFrame";
            this.wireFrameToolStripMenuItem.Click += new System.EventHandler(this.wireFrameToolStripMenuItem_Click);
            // 
            // faceToolStripMenuItem
            // 
            this.faceToolStripMenuItem.Checked = true;
            this.faceToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.faceToolStripMenuItem.Name = "faceToolStripMenuItem";
            this.faceToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.faceToolStripMenuItem.Text = "Face";
            this.faceToolStripMenuItem.Click += new System.EventHandler(this.faceToolStripMenuItem_Click);
            // 
            // boundingBoxToolStripMenuItem
            // 
            this.boundingBoxToolStripMenuItem.Checked = true;
            this.boundingBoxToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.boundingBoxToolStripMenuItem.Name = "boundingBoxToolStripMenuItem";
            this.boundingBoxToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.boundingBoxToolStripMenuItem.Text = "BoundingBox";
            this.boundingBoxToolStripMenuItem.Click += new System.EventHandler(this.boundingBoxToolStripMenuItem_Click);
            // 
            // axesToolStripMenuItem
            // 
            this.axesToolStripMenuItem.Name = "axesToolStripMenuItem";
            this.axesToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.axesToolStripMenuItem.Text = "Axes";
            this.axesToolStripMenuItem.Click += new System.EventHandler(this.axesToolStripMenuItem_Click);
            // 
            // groundToolStripMenuItem
            // 
            this.groundToolStripMenuItem.Name = "groundToolStripMenuItem";
            this.groundToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.groundToolStripMenuItem.Text = "Ground";
            this.groundToolStripMenuItem.Click += new System.EventHandler(this.groundToolStripMenuItem_Click);
            // 
            // selectElement
            // 
            this.selectElement.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vertexSelection,
            this.edgeSelection,
            this.faceSelection,
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
            this.vertexSelection.Size = new System.Drawing.Size(105, 22);
            this.vertexSelection.Text = "vertex";
            this.vertexSelection.Click += new System.EventHandler(this.vertexSelection_Click);
            // 
            // edgeSelection
            // 
            this.edgeSelection.Name = "edgeSelection";
            this.edgeSelection.Size = new System.Drawing.Size(105, 22);
            this.edgeSelection.Text = "edge";
            this.edgeSelection.Click += new System.EventHandler(this.edgeSelection_Click);
            // 
            // faceSelection
            // 
            this.faceSelection.Name = "faceSelection";
            this.faceSelection.Size = new System.Drawing.Size(105, 22);
            this.faceSelection.Text = "face";
            this.faceSelection.Click += new System.EventHandler(this.faceSelection_Click);
            // 
            // boxToolStripMenuItem
            // 
            this.boxToolStripMenuItem.Name = "boxToolStripMenuItem";
            this.boxToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.boxToolStripMenuItem.Text = "box";
            this.boxToolStripMenuItem.Click += new System.EventHandler(this.boxToolStripMenuItem_Click);
            // 
            // modelEdit
            // 
            this.modelEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addSelectedParts,
            this.translateToolStripMenuItem,
            this.scaleToolStripMenuItem,
            this.rotateToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.replicateToolStripMenuItem,
            this.composeToolStripMenuItem});
            this.modelEdit.Image = ((System.Drawing.Image)(resources.GetObject("modelEdit.Image")));
            this.modelEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.modelEdit.Name = "modelEdit";
            this.modelEdit.Size = new System.Drawing.Size(45, 36);
            this.modelEdit.Text = "Edit";
            this.modelEdit.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.modelEdit.ToolTipText = "Edit operations";
            // 
            // addSelectedParts
            // 
            this.addSelectedParts.Name = "addSelectedParts";
            this.addSelectedParts.Size = new System.Drawing.Size(152, 22);
            this.addSelectedParts.Text = "Add selected";
            this.addSelectedParts.Click += new System.EventHandler(this.addSelectedParts_Click);
            // 
            // translateToolStripMenuItem
            // 
            this.translateToolStripMenuItem.Name = "translateToolStripMenuItem";
            this.translateToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.translateToolStripMenuItem.Text = "Translate";
            this.translateToolStripMenuItem.Click += new System.EventHandler(this.translateToolStripMenuItem_Click);
            // 
            // scaleToolStripMenuItem
            // 
            this.scaleToolStripMenuItem.Name = "scaleToolStripMenuItem";
            this.scaleToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.scaleToolStripMenuItem.Text = "Scale";
            this.scaleToolStripMenuItem.Click += new System.EventHandler(this.scaleToolStripMenuItem_Click);
            // 
            // rotateToolStripMenuItem
            // 
            this.rotateToolStripMenuItem.Name = "rotateToolStripMenuItem";
            this.rotateToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.rotateToolStripMenuItem.Text = "Rotate";
            this.rotateToolStripMenuItem.Click += new System.EventHandler(this.rotateToolStripMenuItem_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // replicateToolStripMenuItem
            // 
            this.replicateToolStripMenuItem.Name = "replicateToolStripMenuItem";
            this.replicateToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.replicateToolStripMenuItem.Text = "Replicate";
            this.replicateToolStripMenuItem.Click += new System.EventHandler(this.replicateToolStripMenuItem_Click);
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
            this.viewPanel.Panel2.Controls.Add(this.statsLabel);
            this.viewPanel.Panel2.Controls.Add(this.glViewer);
            this.viewPanel.Panel2.Controls.Add(this.partBasket);
            this.viewPanel.Panel2.Controls.Add(this.modelViewLayoutPanel);
            this.viewPanel.Size = new System.Drawing.Size(1099, 735);
            this.viewPanel.SplitterDistance = 27;
            this.viewPanel.TabIndex = 1;
            // 
            // fileNameTabs
            // 
            this.fileNameTabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileNameTabs.Location = new System.Drawing.Point(3, 0);
            this.fileNameTabs.Name = "fileNameTabs";
            this.fileNameTabs.SelectedIndex = 0;
            this.fileNameTabs.Size = new System.Drawing.Size(1093, 30);
            this.fileNameTabs.TabIndex = 0;
            // 
            // statsLabel
            // 
            this.statsLabel.AutoSize = true;
            this.statsLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.statsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statsLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.statsLabel.Location = new System.Drawing.Point(216, 3);
            this.statsLabel.Name = "statsLabel";
            this.statsLabel.Size = new System.Drawing.Size(41, 16);
            this.statsLabel.TabIndex = 14;
            this.statsLabel.Text = "Stats:";
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
            this.glViewer.Location = new System.Drawing.Point(219, 3);
            this.glViewer.Name = "glViewer";
            this.glViewer.Size = new System.Drawing.Size(671, 695);
            this.glViewer.StencilBits = ((byte)(0));
            this.glViewer.TabIndex = 16;
            // 
            // partBasket
            // 
            this.partBasket.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.partBasket.AutoScroll = true;
            this.partBasket.BackColor = System.Drawing.Color.White;
            this.partBasket.Location = new System.Drawing.Point(896, 3);
            this.partBasket.Name = "partBasket";
            this.partBasket.Size = new System.Drawing.Size(203, 701);
            this.partBasket.TabIndex = 15;
            // 
            // modelViewLayoutPanel
            // 
            this.modelViewLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.modelViewLayoutPanel.AutoScroll = true;
            this.modelViewLayoutPanel.BackColor = System.Drawing.Color.White;
            this.modelViewLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.modelViewLayoutPanel.Name = "modelViewLayoutPanel";
            this.modelViewLayoutPanel.Size = new System.Drawing.Size(210, 701);
            this.modelViewLayoutPanel.TabIndex = 13;
            // 
            // partRelatedTools
            // 
            this.partRelatedTools.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.groupToolStripMenuItem});
            this.partRelatedTools.Name = "partRelatedTools";
            this.partRelatedTools.Size = new System.Drawing.Size(108, 26);
            // 
            // groupToolStripMenuItem
            // 
            this.groupToolStripMenuItem.Name = "groupToolStripMenuItem";
            this.groupToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.groupToolStripMenuItem.Text = "Group";
            this.groupToolStripMenuItem.Click += new System.EventHandler(this.groupToolStripMenuItem_Click);
            // 
            // composeToolStripMenuItem
            // 
            this.composeToolStripMenuItem.Name = "composeToolStripMenuItem";
            this.composeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.composeToolStripMenuItem.Text = "Compose";
            this.composeToolStripMenuItem.Click += new System.EventHandler(this.composeToolStripMenuItem_Click);
            // 
            // Interface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(1099, 774);
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
            this.partRelatedTools.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion


		private System.Windows.Forms.ToolStrip menu;
		private System.Windows.Forms.ToolStripDropDownButton file;
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
        private System.Windows.Forms.ColorDialog strokeColorDialog;
        private System.Windows.Forms.ToolStripMenuItem reloadViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadTriMeshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem screenCaptureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem boxToolStripMenuItem;
        private GLViewer glViewer;
        private System.Windows.Forms.ToolStripDropDownButton model;
        private System.Windows.Forms.ToolStripMenuItem loadAPartBasedModel;
        private System.Windows.Forms.ToolStripMenuItem saveAModelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadPartBasedModels;
        private System.Windows.Forms.ContextMenuStrip partRelatedTools;
        private System.Windows.Forms.ToolStripMenuItem boundingBoxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem groupToolStripMenuItem;
        private System.Windows.Forms.FlowLayoutPanel modelViewLayoutPanel;
        private System.Windows.Forms.Label statsLabel;
        private System.Windows.Forms.FlowLayoutPanel partBasket;
        private System.Windows.Forms.ToolStripMenuItem loadHumanPoseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveHumanPoseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unitifyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem axesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMergedMeshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem swtichYZToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem switchXZToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton modelEdit;
        private System.Windows.Forms.ToolStripMenuItem addSelectedParts;
        private System.Windows.Forms.ToolStripMenuItem switchXYToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem translateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rotateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scaleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replicateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem groundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem composeToolStripMenuItem;
	}
}

