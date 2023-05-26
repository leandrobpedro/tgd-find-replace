using Microsoft.Win32;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;

namespace TGDFindReplace
{
    public partial class FormMain : Form
    {
        CultureInfo ci = Thread.CurrentThread.CurrentCulture;
        private bool drag = false;                  // determine if we should be moving the form.
        private Point startPoint = new Point(0, 0); // also for the moving.
        public ResourceManager rm;

        private long count = 0;

        public Type tgd;
        public object objTgd;

        public FormMain()
        {

            rm = new ResourceManager(
                    "TGDFindReplace.Resources.local_" + ci.TwoLetterISOLanguageName.ToLower(),
                    typeof(FormMain).Assembly);

            try
            {
                Console.WriteLine(rm.GetString("hello"));
            }
            catch (MissingManifestResourceException)
            {
                rm = new ResourceManager(
                    "TGDFindReplace.Resources.local_en", typeof(FormMain).Assembly);
            }

            try
            {
                tgd = Type.GetTypeFromProgID(
                        "TagGroupDefinitionInterfaceDll.TagGroupDefinitionInterface");
                objTgd = Activator.CreateInstance(tgd);
            }
            catch
            {
                MessageBox.Show(rm.GetString("iFixInstallationWarning"), "", MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
            }

            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    txtTgdDir.Text = dialog.SelectedPath;
                }
            }

        }

        private void btnListFiles_Click(object sender, EventArgs e)
        {
            ListFiles();
        }

        private void btnReplace_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lbxTgdFiles.Items.Count; i++)
            {
                if (lbxTgdFiles.GetItemChecked(i))
                {
                    RetrieveDefinition(lbxTgdFiles.Items[i].ToString());
                }
            }

            if (count > 0)
            {
                string s;
                s = string.Format(rm.GetString("AllDone"), count);
                MessageBox.Show(s, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                count = 0;
            }
            else
            {
                if (this.lbxTgdFiles.Items.Count > 0)
                {
                    MessageBox.Show(rm.GetString("NoSubstitutionWereMade"), "", MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                }
            }
        }

        private void ListFiles()
        {
            string path = txtTgdDir.Text;

            lbxTgdFiles.Items.Clear();

            if (Directory.Exists(path))
            {
                try
                {
                    string[] files = Directory.GetFiles(path, "*.tgd",
                            SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        lbxTgdFiles.Items.AddRange(files);

                        for (int i = 0; i < lbxTgdFiles.Items.Count; i++)
                        {
                            lbxTgdFiles.SetItemChecked(i, true);
                        }
                    }
                    else
                    {

                        MessageBox.Show(rm.GetString("NoTGDFilesWasFound") + path, "",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show(rm.GetString("UnauthorizedAccess") + path, "",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

            }
            else
            {
                lbxTgdFiles.Items.Clear();
                MessageBox.Show(rm.GetString("InvalidPathWarning") + path, "",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void RetrieveDefinition(string file)
        {
            try
            {
                object[] args = new object[5];
                ParameterModifier[] mod = new ParameterModifier[1];

                mod[0] = new ParameterModifier(5);
                mod[0][2] = true; // set the 2nd param to be an out param
                mod[0][3] = true; // set the 3rd param to be an out param
                mod[0][4] = true; // set the 4rd param to be an out param

                args[0] = file;
                args[1] = 0;

                object result = tgd.InvokeMember("RetrieveDefinition",
                            BindingFlags.InvokeMethod, null, objTgd, args, mod, null, null);

                UpdateDefinition(args);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateDefinition(object[] args)
        {

            string[] tokenList = (string[])args[2];
            string[] replacementList = (string[])args[3];
            string[] descriptionList = (string[])args[4];

            short numOfTokens = (short)replacementList.Length;

            args[1] = numOfTokens;

            if (!String.IsNullOrEmpty(this.txtFind.Text))
            {
                for (int i = 0; i < numOfTokens; i++)
                {
                    if (this.chkSymbol.Checked)
                    {
                        if (tokenList[i].IndexOf(this.txtFind.Text) > -1)
                        {
                            tokenList[i] = tokenList[i].Replace(this.txtFind.Text,
                                                                this.txtReplace.Text);
                            count++;
                        }

                    }
                    if (this.chkSubstitution.Checked)
                    {
                        if (replacementList[i].IndexOf(this.txtFind.Text) > -1)
                        {
                            replacementList[i] = replacementList[i].Replace(this.txtFind.Text,
                                                                            this.txtReplace.Text);
                            count++;
                        }

                    }
                    if (this.chkDescription.Checked)
                    {
                        if (descriptionList[i].IndexOf(this.txtFind.Text) > -1)
                        {
                            descriptionList[i] = descriptionList[i].Replace(this.txtFind.Text,
                                                                            this.txtReplace.Text);
                            count++;
                        }
                    }
                }
            }

            args[2] = tokenList;
            args[3] = replacementList;
            args[4] = descriptionList;

            object result = tgd.InvokeMember("UpdateDefinition",
                    BindingFlags.InvokeMethod, null, objTgd, args);
        }

        void Title_MouseUp(object sender, MouseEventArgs e)
        {
            this.drag = false;
        }

        void Title_MouseDown(object sender, MouseEventArgs e)
        {
            this.startPoint = e.Location;
            this.drag = true;
        }

        void Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.drag)
            { // if we should be dragging it, we need to figure out some movement
                Point p1 = new Point(e.X, e.Y);
                Point p2 = this.PointToScreen(p1);
                Point p3 = new Point(p2.X - this.startPoint.X,
                                     p2.Y - this.startPoint.Y);
                this.Location = p3;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // Init labels according to culture
            this.lblAppName.Text = rm.GetString("AppName");

            this.chkSymbol.Text = rm.GetString("Symbol");
            this.chkSubstitution.Text = rm.GetString("Substitution");
            this.chkDescription.Text = rm.GetString("Description");

            this.btnReplace.Text = rm.GetString("Replace");

            this.lblFind.Text = rm.GetString("Find");
            this.lblReplace.Text = rm.GetString("Replace");

            // Load current PIC path
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\GE Fanuc\\Proficy iFIX\\ProjectPaths"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("PICPATH");
                        if (o != null)
                        {
                            txtTgdDir.Text = o.ToString();
                        } 
                    }
                    else
                    {
                        // If key not found, use current directory
                        txtTgdDir.Text = Path.GetDirectoryName(Application.ExecutablePath);
                    }
                }
            }
            catch (Exception ex)
            {
                
            }

        }
    }
}