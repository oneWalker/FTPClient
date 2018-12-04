using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace FTPClient
{
    public partial class Form1 : Form
    {
        private FtpClient client = new FtpClient();

        //初始化窗体程序
        public Form1()
        {
            InitializeComponent();
            buttonConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            buttonDownload.Enabled = false;
            buttonUpload.Enabled = false;
        }
        //选择保存路径，并和折叠窗口进行绑定以便于进行演示
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBoxLocalPath.Text = folderBrowserDialog1.SelectedPath;
        }

        //获取文件目录并将其显示在指定的窗口
        private void GetFtpContent(ArrayList directoriesList) {
            {
                listBoxFtpDir.Items.Clear();
                listBoxFtpDir.Items.Add("[...]");
                directoriesList.Sort();
                foreach(string name in directoriesList)
                {
                    string position = name.Substring(name.LastIndexOf(' ') + 1, name.Length - name.LastIndexOf(' ') - 1);
                    if (position != ".." && position != ".")
                    {
                        switch (name[0])
                        {
                            case 'd':
                                listBoxFtpDir.Items.Add("[" + position + "]");
                                break;
                            case 'l':
                                listBoxFtpDir.Items.Add("->" + position);
                                break;
                            default:
                                listBoxFtpDir.Items.Add(position);
                                break;
                        }
                    }

                }
            }
        }

        //登录按钮单击事件
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if(comboBoxServer.Text != string.Empty && comboBoxServer.Text.Trim()!= string.Empty)
            {
                try
                {
                    string serverName = comboBoxServer.Text;
                    if (serverName.StartsWith("ftp://"))
                        serverName = serverName.Replace("ftp://", "");
                    client = new FtpClient(serverName, textBoxLogin.Text, maskedTextBoxPass.Text);
                    client.DownProgressChanged += new FtpClient.DownProgressChangedEventHandler(client_DownProgressChanged);
                    client.DownCompleted += new FtpClient.DownCompletedEventHandler(client_DownloadFileCompleted);
                    client.UpProgressChanged += new FtpClient.UpProgressChangedEventHandler(client_UploadProgressChanged);
                    client.UpCompleted += new FtpClient.UpCompletedEventHandler(client_UploadFileCompleted);
                    GetFtpContent(client.GetDirectiories());
                    textBoxFtpPath.Text = client.FtpDirectory;
                    toolStripStatusLabelServer.Text = "Server: ftp://" + client.Host;

                    buttonConnect.Enabled = false;
                    buttonDisconnect.Enabled = true;
                    buttonDownload.Enabled = true;
                    buttonUpload.Enabled = true;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("请输入ftp名字", "Error");
                comboBoxServer.Text = string.Empty;
            }
        }

        //完成上传
        private void client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                MessageBox.Show("Error: " + e.Error.Message);
                client.UploadCompleted = true;
                buttonDownload.Enabled = true;
                buttonUpload.Enabled = true;
                return;
            }
            client.UploadCompleted = true;
            buttonDownload.Enabled = true;
            buttonUpload.Enabled = true;
            MessageBox.Show("文件已上传");
            try
            {
                GetFtpContent(client.GetDirectiories());
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error: " + exc.Message);
            }

            client.DownloadCompleted = true;

        }
        //显示文件上传状态
        private void client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            toolStripStatusLabelDownload.Text = "Uploaded: " + (e.BytesSent / (double)1024).ToString() + " kB";
        }

        //进行文件的下载的
        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                MessageBox.Show("Error: " + e.Error.Message);
            }
            else
                MessageBox.Show("下载完成");
            client.DownloadCompleted = true;
            buttonDownload.Enabled = true;
            buttonUpload.Enabled = true;
        }
        //显示文件下载状态
        private void client_DownProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            toolStripStatusLabelDownload.Text = "Downloaded: " + (e.BytesReceived / (double)1024).ToString() + " kB";
        }
        //事件：单击进行下载
        private void buttonDownload_Click(object sender, EventArgs e)
        {
            int index = listBoxFtpDir.SelectedIndex;
            if(listBoxFtpDir.Items[index].ToString()[0] != '[')
            {
                if(MessageBox.Show("是否下载这个文档?","完成文档下载", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    try
                    {
                        string localFile = textBoxLocalPath.Text + "\\" + listBoxFtpDir.Items[index].ToString();
                        FileInfo fi = new FileInfo(localFile);
                        if (fi.Exists == false)
                        {
                            client.DownloadFileAsync(listBoxFtpDir.Items[index].ToString(), localFile);
                            buttonDownload.Enabled = false;
                            buttonUpload.Enabled = false;
                        }
                        else
                            MessageBox.Show("文件不存在.");
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message, "错误");
                    }
                }
            }
        }


        //事件：在文件夹目录中进行单击选择目录
        private void listBoxFtpDir_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBoxFtpDir.SelectedIndex;
            try
            {
                if(index > -1)
                {
                    if (index == 0)
                        GetFtpContent(client.ChangeDirectoryUp());
                    else
                    {
                        if (listBoxFtpDir.Items[index].ToString()[0] == '[')
                        {
                            string directory = listBoxFtpDir.Items[index].ToString().Substring(1, listBoxFtpDir.Items[index].ToString().Length - 2);
                            GetFtpContent(client.ChangeDirectory(directory));
                        }
                        else
                        {
                            if (listBoxFtpDir.Items[index].ToString()[0] == '-' && listBoxFtpDir.Items[index].ToString()[2] == '.')
                            {
                                string link = listBoxFtpDir.Items[index].ToString().Substring(5, listBoxFtpDir.Items[index].ToString().Length - 5);
                                client.FtpDirectory = "ftp://" + client.Host;
                                GetFtpContent(client.ChangeDirectory(link));
                            }
                            listBoxFtpDir.SelectedIndex = 0;
                        }
                    }
                }
                textBoxFtpPath.Text = client.FtpDirectory;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        //选择对应文件夹
        private void listBoxFtpDir_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.listBoxFtpDir_MouseDoubleClick(sender, null);
        }

        //事件：上传目录
        private void buttonUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    client.UploadFileAsync(openFileDialog1.FileName);
                    buttonDownload.Enabled = false;
                    buttonUpload.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }


        //事件：断开连接
        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            client.DownProgressChanged -= new FtpClient.DownProgressChangedEventHandler(client_DownProgressChanged);
            client.DownCompleted -= new FtpClient.DownCompletedEventHandler(client_DownloadFileCompleted);
            client.UpProgressChanged -= new FtpClient.UpProgressChangedEventHandler(client_UploadProgressChanged);
            client.UpCompleted -= new FtpClient.UpCompletedEventHandler(client_UploadFileCompleted);
            listBoxFtpDir.Items.Clear();
            textBoxFtpPath.Text = "";

            buttonConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            buttonDownload.Enabled = false;
            buttonUpload.Enabled = false;
        }

        //删除指定文件
        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.D)
            {
                int index = listBoxFtpDir.SelectedIndex;
                if(index > -1)
                    if(listBoxFtpDir.Items[index].ToString()[0] != '[')
                    {
                        try
                        {
                            MessageBox.Show(client.DeleteFile(listBoxFtpDir.Items[index].ToString()));
                            GetFtpContent(client.GetDirectiories());
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("无法删除 (" + exc.Message + ")");
                        }
                    }
            }
        }
    }
}
