using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;

namespace FameBase
{
	public partial class Interface : Form
	{
		public Interface()
		{
			InitializeComponent();
            this.glViewer.Init();
		}

        /*********Var**********/
        private void open3D_Click(object sender, EventArgs e)
        {
            // clear existing models and load a new one
            var dialog = new OpenFileDialog()
            {
                Title = "Open a 3D model",
                Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filename = dialog.FileName;
                // load mesh
                this.glViewer.loadMesh(filename);
                // set tab page
                TabPage tp = new TabPage(Path.GetFileName(filename));
                this.fileNameTabs.TabPages.Clear();
                this.fileNameTabs.TabPages.Add(tp);
                this.fileNameTabs.SelectedTab = tp;
                this.glViewer.setTabIndex(this.fileNameTabs.TabCount);
                this.updateStats();
            }
            this.glViewer.Refresh();
        }

        private void open3DGroupedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // clear existing models and load a new one
            var dialog = new OpenFileDialog()
            {
                Title = "Open a 3D model",
                Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filename = dialog.FileName;
                // load mesh
                this.glViewer.loadMesh(filename);
                // set tab page
                TabPage tp = new TabPage(Path.GetFileName(filename));
                this.fileNameTabs.TabPages.Clear();
                this.fileNameTabs.TabPages.Add(tp);
                this.fileNameTabs.SelectedTab = tp;
                this.glViewer.setTabIndex(this.fileNameTabs.TabCount);
                this.updateStats();
            }
            this.glViewer.Refresh();
        }

        private void import3D_Click(object sender, EventArgs e)
        {
            // preserve the existing models
            var dialog = new OpenFileDialog()
            {
                Title = "Import a 3D model",
                Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*",
                CheckFileExists = true,
                Multiselect = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                bool multiple = dialog.FileNames.Length > 1;
                foreach (string filename in dialog.FileNames)
                {
                    this.glViewer.importMesh(filename, multiple);
                }
            }
            this.glViewer.Refresh();
        }

        private void pointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.vertexToolStripMenuItem.Checked = !this.vertexToolStripMenuItem.Checked;
            this.glViewer.setRenderOption(1);
        }

        private void wireFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.wireFrameToolStripMenuItem.Checked = !this.wireFrameToolStripMenuItem.Checked;
            this.glViewer.setRenderOption(2);
        }

        private void faceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.faceToolStripMenuItem.Checked = !this.faceToolStripMenuItem.Checked;
            this.glViewer.setRenderOption(3);
        }

        private void boundingBoxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.boundingBoxToolStripMenuItem.Checked = !this.boundingBoxToolStripMenuItem.Checked;
            this.glViewer.setRenderOption(4);
        }

        public void setCheckBox_drawBbox(bool isdraw)
        {
            this.boundingBoxToolStripMenuItem.Checked = isdraw;
        }

        private void viewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(1);
        }

        private void resetViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.resetView();
        }

        private void modelColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = GLDrawer.ModelColor;
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GLDrawer.ModelColor = colorDialog.Color;
                this.glViewer.setMeshColor(colorDialog.Color);
                this.glViewer.Refresh();
            }
        }

        private void vertexSelection_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(1);
        }

        private void edgeSelection_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(2);
        }

        private void faceSelection_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(3);
        }

        private void loadAPartBasedModel_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Open a part-based model info",
                DefaultExt = ".pam",
                Filter = "Part-based model (*.pam)|*.pam"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.glViewer.loadAPartBasedModel(dialog.FileName);
                this.updateStats();
                // set tab page
                TabPage tp = new TabPage(Path.GetFileName(dialog.FileName));
                this.fileNameTabs.TabPages.Clear();
                this.fileNameTabs.TabPages.Add(tp);
                this.fileNameTabs.SelectedTab = tp;
            }
        }

        private void importModelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preserve the existing models
            var dialog = new OpenFileDialog()
            {
                Title = "Import part-based model(s)",
                Filter = "Part-based model (*.pam)|*.pam",
                CheckFileExists = true,
                Multiselect = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                this.glViewer.importPartBasedModel(dialog.FileNames);
                this.updateStats();
            }
        }

        private void loadPartBasedModels_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog() { SelectedPath = @"C:\scratch\HLiu\Fame\data_sets\shelf" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string folderName = dialog.SelectedPath;
                List<ModelViewer> modelViewers = this.glViewer.loadPartBasedModels(folderName);
                layoutModelSet(modelViewers);
            }
        }

        private void saveAModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Save a part-based model",
                DefaultExt = ".pam",
                Filter = "Part-based model (*.pam)|*.pam",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.glViewer.saveAPartBasedModel(dialog.FileName);
            }
        }

        private void reloadViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.reloadView();
            this.glViewer.Refresh();
        }


        private void saveViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "MAT file (*.mat)|*.mat;|All Files(*.*)|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string filename = dialog.FileName;
                this.glViewer.writeModelViewMatrix(filename);
            }
        }

        private void loadViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "MAT file (*.mat)|*.mat;|All Files(*.*)|*.*";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string filename = dialog.FileName;
                this.glViewer.readModelModelViewMatrix(filename);
            }
        }

        private void saveAs3D_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                this.glViewer.saveObj(null, dialog.FileName);
            }
        }

        private void loadTriMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string filename = dialog.FileName;
                // load mesh
                this.glViewer.loadTriMesh(filename);
                // set tab page
                TabPage tp = new TabPage(Path.GetFileName(filename));
                this.fileNameTabs.TabPages.Add(tp);
                this.fileNameTabs.SelectedTab = tp;
            }
            this.glViewer.Refresh();
        }

        private void screenCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.captureScreen(0);
        }

        public int getScreenCaptureBoundaryX(int glViewLocX)
        {
            return 0;
        }

		private void renderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "image file (*.png)|*.png|All Files(*.*)|*.*";
			if (dialog.ShowDialog(this) == DialogResult.OK)
			{
				string filename = dialog.FileName;
				this.glViewer.renderToImage(filename);
				this.glViewer.Refresh();
			}
		}

		private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.glViewer.clearContext();
            this.updateStats();
			this.glViewer.Refresh();
		}

        private void boxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.boxToolStripMenuItem.Checked = true;
            this.glViewer.setUIMode(4);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            //adjustImageView();
        }

        private void groupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.groupParts();
            this.updateStats();
            this.Refresh();
        }

        public void updateStats()
        {
            string stats = this.glViewer.getStats();
            this.statsLabel.Text = stats;
        }

        public ContextMenuStrip getRightButtonMenu()
        {
            return this.partRelatedTools;
        }

        private void layoutModelSet(List<ModelViewer> modelViewers)
        {
            if (modelViewers == null) return;
            int w = 200;
            int h = 200;
            int i = 0;
            foreach (ModelViewer mv in modelViewers)
            {
                mv.SetBounds(i * w, 0, w, h);
                mv.BorderStyle = BorderStyle.FixedSingle;
                mv.BackColor = Color.White;
                this.modelViewLayoutPanel.Controls.Add(mv);
            }
            this.updateStats();
        }

        private void loadHumanPoseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Load a human pose",
                DefaultExt = ".pos"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.glViewer.loadHuamPose(dialog.FileName);
                this.updateStats();
            }
        }

        private void saveHumanPoseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Save a human pose",
                DefaultExt = ".pos",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.glViewer.saveHumanPose(dialog.FileName);
            }
        }

        private void unitifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.unitifyToolStripMenuItem.Checked = !this.unitifyToolStripMenuItem.Checked;
            this.glViewer._unitifyMesh = this.unitifyToolStripMenuItem.Checked;
        }

        private void axesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.axesToolStripMenuItem.Checked = !this.axesToolStripMenuItem.Checked;
            this.glViewer.setShowAxesOption(this.axesToolStripMenuItem.Checked);
        }

        private void saveMergedMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                this.glViewer.saveMergedObj(dialog.FileName);
            }
        }

        private void switchXYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.switchXYZ(1);
        }

        private void switchXZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.switchXYZ(2);
        }

        private void swtichYZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.switchXYZ(3);
        }

        private void translateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(6);
        }

        private void rotateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(8);
        }

        private void scaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(7);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.deleteParts();
        }

        private void replicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.duplicateParts();
        }

        private void groundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.groundToolStripMenuItem.Checked = !this.groundToolStripMenuItem.Checked;
            this.glViewer.drawGround = this.groundToolStripMenuItem.Checked;
            this.glViewer.Refresh();
        }

        private void addSelectedParts_Click(object sender, EventArgs e)
        {
            ModelViewer mv = this.glViewer.addSelectedPartsToBasket();
            if (mv != null)
            {
                mv.Width = 200;
                mv.Height = 200;
                mv.BorderStyle = BorderStyle.FixedSingle;
                mv.BackColor = Color.White;
                this.partBasket.Controls.Add(mv);
            }
        }

        private void composeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.composeSelectedParts();
        }

        private void translucentPoseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.translucentPoseToolStripMenuItem.Checked = !this.translucentPoseToolStripMenuItem.Checked;
            this.glViewer.setShowHumanPoseOption(this.translucentPoseToolStripMenuItem.Checked);
        }

        private void XYbutton_Click(object sender, EventArgs e)
        {
            this.glViewer.switchXYZ(1);
        }

        private void YZbutton_Click(object sender, EventArgs e)
        {
            this.glViewer.switchXYZ(2);
        }

        private void XZbutton_Click(object sender, EventArgs e)
        {
            this.glViewer.switchXYZ(3);
        }

        private void importHumanPoseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Load a human pose",
                DefaultExt = ".pos"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.glViewer.importHumanPose(dialog.FileName);
                this.updateStats();
            }
        }

        private void graphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.graphToolStripMenuItem.Checked = !this.graphToolStripMenuItem.Checked;
            this.glViewer.setRenderOption(5);
        }

        public void setCheckBox_drawGraph(bool isdraw)
        {
            this.graphToolStripMenuItem.Checked = isdraw;
        }

        private void addEdgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.addAnEdge();
            this.glViewer.Refresh();
        }

        private void delEdgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.deleteAnEdge();
            this.glViewer.Refresh();
        }

        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setSelectedNodes();
        }

        private void crossoverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<ModelViewer> modelViews = this.glViewer.crossOver();
            this.partBasket.Controls.Clear();
            if (modelViews != null)
            {
                foreach (ModelViewer mv in modelViews)
                {
                    addModelViewerToRightPanel(mv);
                }
            }
        }

        private void mutateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<ModelViewer> modelViews = this.glViewer.getMutateViewers();
            this.partBasket.Controls.Clear();
            if (modelViews != null)
            {
                foreach (ModelViewer mv in modelViews)
                {
                    addModelViewerToRightPanel(mv);
                }
            }
        }

        private void addModelViewerToRightPanel(ModelViewer mv)
        {
            mv.Width = 200;
            mv.Height = 200;
            mv.BorderStyle = BorderStyle.FixedSingle;
            mv.BackColor = Color.White;
            this.partBasket.Controls.Add(mv);
        }

        public void writeToConsole(string s)
        {
            Console.WriteLine(s);
        }

        private void symmetryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.markSymmetry();
        }

        private void randomColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setRandomColor();
        }

        private void autoGenerateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.autoGenerate();
        }

        private void contactToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(9);
        }
	}// Interface
}// namespace
