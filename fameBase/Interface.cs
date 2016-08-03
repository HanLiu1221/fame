using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;

namespace SketchPlatform
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
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string filename = dialog.FileName;
                this.glViewer.importMesh(filename);
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
            colorDialog.Color = GLViewer.ModelColor;
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GLViewer.ModelColor = colorDialog.Color;
                this.glViewer.setSegmentColor(colorDialog.Color);
                this.glViewer.Refresh();
            }
        }

        private void vertexSelection_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(2);
        }

        private void edgeSelection_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(3);
        }

        private void faceSelection_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(4);
        }

        private void loadSegmentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = "D:\\Projects\\sketchingTutorial\\SketchPlatform\\Data\\old\\segments";
            //dialog.SelectedPath = "D:\\Projects\\sketchingTutorial\\CGPlatform\\Data";
            //if (dialog.ShowDialog(this) == DialogResult.OK)
            //{
                string folderName = dialog.SelectedPath;
                this.glViewer.loadSegments(folderName);
            //}
            this.glViewer.Refresh();
        }

        private void reloadViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.reloadView();
            this.glViewer.Refresh();
        }

        private void outputSeqToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.outputBoxSequence();
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
                this.glViewer.Refresh();
            }
        }

        private void saveAs3D_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "3D model (*.obj; *.off; *.ply)|*.obj; *.off; *.ply|All Files(*.*)|*.*";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string filename = dialog.FileName;
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
			this.glViewer.Refresh();
		}

        private void strokeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(8);
        }

        private void boxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.boxToolStripMenuItem.Checked = true;
            this.glViewer.setUIMode(9);
        }

        private void sketchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(5);
        }

        private void eraserToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.glViewer.setUIMode(6);
        }

        private void clearAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.glViewer.clearAllStrokes();
            this.glViewer.Refresh();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            //adjustImageView();
        }
	}
}
