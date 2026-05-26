using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Reflection;
using System.IO;
using System.Net;
using System.Xml;
using System.Windows.Forms;

namespace ComputerandDomain
{
    public partial class About : Form
    {
        private const string updateUrl = "";//The url of check version xml file.
        public About()
        {
            InitializeComponent();
        }

        private void checkUpdate_Click(object sender, EventArgs e)
        {
            checkIfUpdate();
        }

        private void checkIfUpdate()
        {
            try
            {
                //bool isUpdate = false;
                string newVersion = "";
                string newSoftAddr = "";
                WebClient wc = new WebClient();
                Stream stream = wc.OpenRead(updateUrl);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(stream);
                XmlNode list = xmlDoc.SelectSingleNode("Update");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "Software" && node.Attributes["Name"].Value == "RenameAndJoinDomainTool")
                    {
                        foreach (XmlNode xml in node)
                        {
                            if (xml.Name == "Version")
                            {
                                newVersion = xml.InnerText;
                            }
                            else
                            {
                                newSoftAddr = xml.InnerText;
                            }
                        }
                    }
                }
                Version verNew = new Version(newVersion);
                Version verCurrent = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                int comResult = verCurrent.CompareTo(verNew);
                string currentDirPath = System.Environment.CurrentDirectory;
                if (comResult >= 0)
                {
                    //isUpdate = false;
                    MessageBox.Show("这是最新版本！", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    //isUpdate = true;
                    //MessageBox.Show("New " + currentDirPath + verNew.ToString());
                    if (MessageBox.Show("新版本 v" + newVersion + " 如果可以的话，您是否想要将其下载到当前目录？", "信息", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        wc.DownloadFile(newSoftAddr, currentDirPath + "\\RenameAndJoinDomainTool-v" + newVersion + ".zip");
                        wc.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("检查更新错误！请确认您是否能够成功连接到互联网，或者前往 http://sourceforge.net/projects/renameandjoindomaintool 下载!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }
    }
}
