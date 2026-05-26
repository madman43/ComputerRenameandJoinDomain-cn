using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
namespace ComputerandDomain
{
    public partial class Form1 : Form
    {
        string currentPCName = "";
        string currentDomain = "";
        string hostNameWithoutdomain = "";
        public Form1()

        {
            InitializeComponent();
            currentPCName = GetComputerName();
            currentDomain = GetDomainName();
            hostNameWithoutdomain = Environment.MachineName;
            textBoxPCName.Text = currentPCName;
            if (currentDomain != "")
            {
                textBoxDomainName.Text = currentDomain;
            }
            else
            {
                textBoxDomainName.Text = "无域名";
            }
            LocalizeTreeView();
        }
        private void LocalizeTreeView()
        {
            // 这里可以添加本地化 TreeView 的代码，如果暂时不需要实现，可以留空
            // 获取当前 UI 语言
            var culture = Thread.CurrentThread.CurrentUICulture;

            if (culture.Name.StartsWith("zh"))
            {
                // 中文环境
                SetTreeNodeText("unjoinDomainAndRename", "退出域并重命名");
                SetTreeNodeText("unjoinDomain", "退出域");
                SetTreeNodeText("rename", "重命名");
                SetTreeNodeText("joinDomain", "加入域");
                SetTreeNodeText("renameAndJoinDomain", "重命名并加入域");
            }
            // 如果需要支持其他语言，可以加 else if

        }
        private void SetTreeNodeText(string nodeName, string text)
        {
            var node = treeViewRenameAndJoinDomain.Nodes.Find(nodeName, false);
            if (node.Length > 0)
            {
                node[0].Text = text;
            }
        }

        public string GetComputerName()
        {
            string pcName = "";
            ManagementClass objMC;
            ManagementObjectCollection objMOC;

            try
            {
                objMC = new ManagementClass("Win32_ComputerSystem");
                objMOC = objMC.GetInstances();
            }
            catch (Exception e)
            {
                textBoxOutput.AppendText(e.Message);
                return pcName;
            }

            foreach (ManagementObject objMO in objMOC)
            {
                if (null != objMO)
                {
                    pcName = objMO["Name"].ToString();
                    break;
                }
            }

            return pcName;
        }

        public string GetDomainName()
        {
            string domainName = "";
            ManagementClass objMC;
            ManagementObjectCollection objMOC;

            try
            {
                objMC = new ManagementClass("Win32_ComputerSystem");
                objMOC = objMC.GetInstances();
            }
            catch (Exception e)
            {
                textBoxOutput.AppendText(e.Message);
                return domainName;
            }

            foreach (ManagementObject objMO in objMOC)
            {
                if (null != objMO)
                {
                    if ((bool)objMO["partofdomain"])
                    {
                        domainName = objMO["domain"].ToString();
                        textBoxOutput.AppendText("当前计算机处于域模式：" + domainName + Environment.NewLine);
                    }
                    else
                    {
                        textBoxOutput.AppendText("当前的计算机处于工作组模式，而非域模式！" + Environment.NewLine);
                    }
                }
            }

            return domainName;
        }

        private void unjoinDomainAndRename()
        {
            if (MessageBox.Show("您确定要离开当前网络环境并重新给您的电脑命名吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {

                string currentHostname = currentPCName;
                ManagementObject objMO = new ManagementObject("Win32_ComputerSystem.Name='" + currentHostname + "'");
                ManagementBaseObject result;
                objMO.Scope.Options.EnablePrivileges = true;
                objMO.Scope.Options.Authentication = AuthenticationLevel.PacketPrivacy;
                objMO.Scope.Options.Impersonation = ImpersonationLevel.Impersonate;
                if (currentDomain != "")
                {
                    try
                    {
                        ManagementBaseObject query;
                        query = objMO.GetMethodParameters("UnjoinDomainOrWorkgroup");
                        query["UserName"] = "username";
                        query["Password"] = "password";
                        query["FUnjoinOptions"] = 0;

                        result = objMO.InvokeMethod("UnjoinDomainOrWorkgroup", query, null);
                        if ((uint)result["ReturnValue"] == 0)
                        {
                            textBoxOutput.AppendText("已成功脱离该域！要登录您的计算机，您需要知道本地管理员账户的密码。您必须重启计算机以使这些更改生效！" + Environment.NewLine);
                        }
                        else
                        {
                            textBoxOutput.AppendText("脱离该域失败！请以管理员身份运行此工具！" + Environment.NewLine);
                        }
                    }
                    catch (ManagementException e)
                    {
                        textBoxOutput.AppendText("脱离域操作失败，错误代码为：" + (uint)e.ErrorCode + " , 不能离开该域: " + e.Message + Environment.NewLine);
                        return;
                    }
                }
                else
                {
                    textBoxOutput.AppendText("脱离域操作失败，当前计算机不在域内！" + Environment.NewLine);
                }

                if (textBoxNewName.Text.Trim() != "" && textBoxNewName.Text.Trim().ToLower() != hostNameWithoutdomain.ToLower())
                {
                    string newPCName = textBoxNewName.Text.Trim();

                    try
                    {
                        ManagementBaseObject rename;
                        rename = objMO.GetMethodParameters("Rename");
                        rename["Name"] = newPCName;
                        rename["Password"] = null;
                        rename["UserName"] = null;

                        result = objMO.InvokeMethod("Rename", rename, null);
                        if ((uint)result["ReturnValue"] == 0)
                        {
                            textBoxOutput.AppendText("成功重命名当前计算机！您必须重启计算机以应用这些更改！" + Environment.NewLine);
                        }
                        else
                        {
                            textBoxOutput.AppendText("当前计算机重命名操作失败，请以管理员身份运行此工具！" + Environment.NewLine);
                        }
                    }
                    catch (ManagementException ex)
                    {
                        textBoxOutput.AppendText("当前电脑重命名操作失败，错误代码为：" + (uint)ex.ErrorCode + ", 不能重命名：" + ex.Message + Environment.NewLine);
                        return;
                    }
                }
                else
                {
                    textBoxOutput.AppendText("当前计算机重命名失败，请输入一个新的计算机名称！" + Environment.NewLine);
                }
            }
        }

        public void unJoinDomain()
        {
            if (MessageBox.Show("你确定要离开这个领域吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (currentDomain != "")
                {
                    try
                    {
                        string currentHostname = currentPCName;
                        ManagementObject objMO = new ManagementObject("Win32_ComputerSystem.Name='" + currentHostname + "'");
                        ManagementBaseObject result;
                        objMO.Scope.Options.EnablePrivileges = true;
                        objMO.Scope.Options.Authentication = AuthenticationLevel.PacketPrivacy;
                        objMO.Scope.Options.Impersonation = ImpersonationLevel.Impersonate;

                        ManagementBaseObject query;
                        query = objMO.GetMethodParameters("UnjoinDomainOrWorkgroup");
                        query["UserName"] = "username";
                        query["Password"] = "password";
                        query["FUnjoinOptions"] = 0;

                        result = objMO.InvokeMethod("UnjoinDomainOrWorkgroup", query, null);
                        if ((uint)result["ReturnValue"] == 0)
                        {
                            textBoxOutput.AppendText("已成功脱离该域！要登录您的计算机，您需要知道本地管理员账户的密码。您必须重启计算机以使这些更改生效！" + Environment.NewLine);
                        }
                        else
                        {
                            textBoxOutput.AppendText("脱离该域失败！请以管理员身份运行此工具！" + Environment.NewLine);
                        }
                    }
                    catch (ManagementException e)
                    {
                        textBoxOutput.AppendText("脱离该区域失败，错误代码为： " + (uint)e.ErrorCode + " , 不能离开该域 " + e.Message + Environment.NewLine);
                        return;
                    }
                }
                else
                {
                    textBoxOutput.AppendText("脱离域操作失败，当前计算机不在域内！" + Environment.NewLine);
                    MessageBox.Show("脱离域操作失败，当前计算机不在域内！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public void renamePCName(string newPCName)
        {
            try
            {
                string currentHostname = currentPCName;
                ManagementObject objMO = new ManagementObject("Win32_ComputerSystem.Name='" + currentHostname + "'");
                ManagementBaseObject result;
                objMO.Scope.Options.EnablePrivileges = true;
                objMO.Scope.Options.Authentication = AuthenticationLevel.PacketPrivacy;
                objMO.Scope.Options.Impersonation = ImpersonationLevel.Impersonate;

                ManagementBaseObject rename;
                rename = objMO.GetMethodParameters("Rename");
                rename["Name"] = newPCName;
                rename["Password"] = null;
                rename["UserName"] = null;

                result = objMO.InvokeMethod("Rename", rename, null);
                if ((uint)result["ReturnValue"] == 0)
                {
                    textBoxOutput.AppendText("当前计算机的名称已成功更改！您必须重启计算机以使这些更改生效！" + Environment.NewLine);
                }
                else
                {
                    textBoxOutput.AppendText("当前计算机重命名操作失败，请以管理员身份运行此工具！" + Environment.NewLine);
                }
            }
            catch (ManagementException e)
            {
                textBoxOutput.AppendText("当前电脑重命名操作失败，错误代码为：" + (uint)e.ErrorCode + ", 不能重命名：" + e.Message + Environment.NewLine);
                return;
            }
        }

        private void joinDomain()
        {
            if (currentDomain == "")
            {
                if (MessageBox.Show("你确定要加入这个新域名吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string newDomain = textBoxNewDomain.Text.Trim();
                    string username = textBoxUsername.Text.Trim();
                    string password = textBoxPwd.Text.Trim();
                    if (newDomain != "" && username != "" && password != "")
                    {
                        joinNewDomain(newDomain, username, password);
                    }
                    else
                    {
                        MessageBox.Show("请输入新的域名、域名用户名以及密码。!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                textBoxOutput.AppendText("当前计算机处于域环境中，请先退出该域，然后再加入新的域！" + Environment.NewLine);
                MessageBox.Show("当前计算机处于当前域中，请先退出当前域，然后再加入新的域！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        public void joinNewDomain(string newDomainName, string username, string password)
        {
            try
            {
                string currentHostname = currentPCName;
                ManagementObject objMO = new ManagementObject("Win32_ComputerSystem.Name='" + currentHostname + "'");
                ManagementBaseObject result;
                objMO.Scope.Options.EnablePrivileges = true;
                objMO.Scope.Options.Authentication = AuthenticationLevel.PacketPrivacy;
                objMO.Scope.Options.Impersonation = ImpersonationLevel.Impersonate;

                ManagementBaseObject query;
                query = objMO.GetMethodParameters("JoinDomainOrWorkgroup");
                query["Name"] = newDomainName;
                query["Password"] = password;
                query["UserName"] = username;
                query["FJoinOptions"] = 3;

                result = objMO.InvokeMethod("JoinDomainOrWorkgroup", query, null);
                if ((uint)result["ReturnValue"] == 0)
                {
                    textBoxOutput.AppendText("当前计算机已成功加入新域！您必须重启计算机以使这些更改生效！" + Environment.NewLine);
                }
                else
                {
                    textBoxOutput.AppendText("当前计算机加入新域失败！请以管理员身份运行此工具，并确保域中不存在相同名称的计算机！" + Environment.NewLine);
                }

                if (checkBoxAddtoLocalAdmins.Checked == true)
                {
                    AddDomainUsertoLocalGroup(username);
                }
            }
            catch (ManagementException e)
            {
                textBoxOutput.AppendText("加入新域名失败，错误代码为：" + (uint)e.ErrorCode + " 无法加入域：" + e.Message + Environment.NewLine);
                return;
            }
        }
        public void HandleJoinandRenameDomain(string newDomainName, string newHostName, string username, string password)
        {
            ManagementObject objMO = new ManagementObject("Win32_ComputerSystem.Name='" + currentPCName + "'");
            if (null != objMO)
            {
                ManagementBaseObject result;

                objMO.Scope.Options.EnablePrivileges = true;
                objMO.Scope.Options.Authentication = AuthenticationLevel.PacketPrivacy;
                objMO.Scope.Options.Impersonation = ImpersonationLevel.Impersonate;

                if ("" != currentDomain)
                {
                    if (currentDomain.ToUpper() == newDomainName.ToUpper())
                    {
                        textBoxOutput.AppendText("当前的计算机已经处于一个新的领域之中：" + currentDomain + Environment.NewLine);
                        return;

                    }
                    else
                    {
                        textBoxOutput.AppendText("当前的计算机已经在其他领域得到了应用：" + currentDomain + Environment.NewLine);
                        return;
                    }
                }
                else
                {
                    ManagementBaseObject query;
                    try
                    {
                        query = objMO.GetMethodParameters("JoinDomainOrWorkgroup");
                    }
                    catch (ManagementException)
                    {
                        textBoxOutput.AppendText("加入新域失败！无法找到计算机名称！" + Environment.NewLine);
                        return;
                    }
                    query["Name"] = newDomainName;
                    query["Password"] = password;
                    query["UserName"] = username;
                    query["FJoinOptions"] = 3;

                    try
                    {
                        result = objMO.InvokeMethod("JoinDomainOrWorkgroup", query, null);
                    }
                    catch (ManagementException e)
                    {
                        textBoxOutput.AppendText("加入新域名失败，错误代码为：" + (uint)e.ErrorCode + ", 无法加入域：" + e.Message + Environment.NewLine);
                        return;
                    }

                    if (0 != (uint)result["ReturnValue"])
                    {
                        textBoxOutput.AppendText("加入新域名失败： " + (uint)result["ReturnValue"] + ", 无法加入域。" + Environment.NewLine);
                        return;
                    }
                    else
                    {
                        textBoxOutput.AppendText("加入新域" + newDomainName + " 成功了！您必须重启电脑以使这些更改生效！" + Environment.NewLine);
                    }
                }

                //Thread.Sleep(45000);

                if ((currentPCName.ToUpper() != newHostName.ToUpper()) && ("" != newHostName))
                {
                    try
                    {
                        textBoxOutput.AppendText("New computer name: " + newHostName + Environment.NewLine);
                        ManagementBaseObject query2;
                        query2 = objMO.GetMethodParameters("Rename");
                        query2["Name"] = newHostName;
                        query2["Password"] = password;
                        query2["UserName"] = username;

                        result = objMO.InvokeMethod("Rename", query2, null);

                        if (0 != (uint)result["ReturnValue"])
                        {
                            textBoxOutput.AppendText("重命名计算机失败，错误代码为：" + result["ReturnValue"].ToString() + Environment.NewLine);
                        }
                        else
                        {
                            textBoxOutput.AppendText("计算机重命名成功！您必须重启计算机以使这些更改生效！" + Environment.NewLine);
                        }

                    }
                    catch (InvalidOperationException e)
                    {
                        textBoxOutput.AppendText("计算机重命名失败 " + e.Message + Environment.NewLine);
                        return;
                    }
                    catch (ManagementException e)
                    {
                        textBoxOutput.AppendText("计算机重命名失败，无法重命名计算机：" + e.Message + Environment.NewLine);
                        return;
                    }
                }

                if (checkBoxAddtoLocalAdmins.Checked == true)
                {
                    AddDomainUsertoLocalGroup(username);
                }
            }
            else
            {
                textBoxOutput.AppendText("加入新域名失败：无法打开该对象！" + Environment.NewLine);
                return;
            }
        }

        #region Add domain user to local administrators group (.NET 4.8)
        public void AddDomainUsertoLocalGroup(string username)
        {
            try
            {
                DirectoryEntry adRoot = new DirectoryEntry($"WinNT://{textBoxNewDomain.Text.Trim()}", textBoxUsername.Text.Trim(), textBoxPwd.Text.Trim());
                DirectoryEntry user = adRoot.Children.Find(username, "User");

                DirectoryEntry localGroupRoot = new DirectoryEntry($"WinNT://{Environment.MachineName},Computer");
                DirectoryEntry localGroup = localGroupRoot.Children.Find("Administrators", "group");

                localGroup.Invoke("Add", new object[] { user.Path.ToString() });
                textBoxOutput.AppendText($"添加用户 {username} 已成功添加到“本地管理员”组！" + Environment.NewLine);
            }
            catch (Exception e)
            {
                textBoxOutput.AppendText(e.Message + Environment.NewLine);
            }
        }
        #endregion


        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show();
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (treeViewRenameAndJoinDomain.SelectedNode.Name == "unjoinDomainAndRename")
                unjoinDomainAndRename();
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "unjoinDomain")
                unJoinDomain();
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "rename")
            {
                if (MessageBox.Show("你确定要给你的电脑改名吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string newPCName = textBoxNewName.Text.Trim();
                    if (newPCName != "" && newPCName.ToLower() != hostNameWithoutdomain.ToLower())
                    {
                        renamePCName(newPCName);
                    }
                    else
                    {
                        textBoxOutput.AppendText("重命名计算机失败！请输入一个不同的新计算机名称！" + Environment.NewLine);
                        MessageBox.Show("重命名计算机失败！请输入一个不同的新计算机名称！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "joinDomain")
                joinDomain();
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "renameAndJoinDomain")
            {
                if (currentDomain == "")
                {
                    if (MessageBox.Show("您确定要更改电脑名称并加入新的域吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string newDomain = textBoxNewDomain.Text.Trim();
                        string newHostName = textBoxNewName.Text.Trim();
                        string username = textBoxUsername.Text.Trim();
                        string password = textBoxPwd.Text.Trim();
                        if (newHostName != "" && newHostName.ToLower() != hostNameWithoutdomain.ToLower() && newDomain != "" && username != "" && password != "")
                        {
                            HandleJoinandRenameDomain(newDomain, newHostName, username, password);
                        }
                        else
                        {
                            textBoxOutput.AppendText("重命名并加入新域的操作失败，请输入不同的新计算机名称、域用户名和密码！" + Environment.NewLine);
                            MessageBox.Show("重命名并加入新域的操作失败，请输入不同的新计算机名称、域用户名和密码！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    textBoxOutput.AppendText("重命名并加入新域的操作失败，您的计算机已处于域环境中，请先退出当前域，然后再加入新域！" + Environment.NewLine);
                    MessageBox.Show("重命名并加入新域失败，您的计算机已处于域环境中，请先离开当前域然后再加入新域！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("请在左侧的树状结构中选择一个选项！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void treeViewRenameAndJoinDomain_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeViewRenameAndJoinDomain.SelectedNode.Name == "unjoinDomainAndRename")
            {
                textBoxNewName.ReadOnly = false;
                textBoxNewDomain.ReadOnly = true;
                textBoxUsername.ReadOnly = true;
                textBoxPwd.ReadOnly = true;
                checkBoxAddtoLocalAdmins.Enabled = false;
            }
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "unjoinDomain")
            {
                textBoxNewName.ReadOnly = true;
                textBoxNewDomain.ReadOnly = true;
                textBoxUsername.ReadOnly = true;
                textBoxPwd.ReadOnly = true;
                checkBoxAddtoLocalAdmins.Enabled = false;
            }
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "rename")
            {
                textBoxNewName.ReadOnly = false;
                textBoxNewDomain.ReadOnly = true;
                textBoxUsername.ReadOnly = true;
                textBoxPwd.ReadOnly = true;
                checkBoxAddtoLocalAdmins.Enabled = false;
            }
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "joinDomain")
            {
                textBoxNewName.ReadOnly = true;
                textBoxNewDomain.ReadOnly = false;
                textBoxUsername.ReadOnly = false;
                textBoxPwd.ReadOnly = false;
                checkBoxAddtoLocalAdmins.Enabled = true;
            }
            else if (treeViewRenameAndJoinDomain.SelectedNode.Name == "renameAndJoinDomain")
            {
                textBoxNewName.ReadOnly = false;
                textBoxNewDomain.ReadOnly = false;
                textBoxUsername.ReadOnly = false;
                textBoxPwd.ReadOnly = false;
                checkBoxAddtoLocalAdmins.Enabled = true;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBoxPwd_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
