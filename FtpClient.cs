using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace FTPClient
{
    public class FtpClient
    {
        #region Field & Property
        private string _host;
        private string _userName;
        private string _password;
        private string ftpDirectory;
        private bool downloadCompleted;
        private bool uploadCompleted;

        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                _host = value;
            }
        }
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
            }
        }
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }

        public string FtpDirectory
        {
            get
            {
                if (ftpDirectory.StartsWith("ftp://"))
                    return ftpDirectory;
                else
                    return "ftp://" + ftpDirectory;
            }
            set
            {
                ftpDirectory = value;
            }
        }

        public bool DownloadCompleted
        {
            get
            {
                return downloadCompleted;
            }
            set
            {
                downloadCompleted = value;
            }
        }

        public bool UploadCompleted
        {
            get
            {
                return uploadCompleted;
            }
            set
            {
                uploadCompleted = value;
            }
        }

        #endregion
        //基本属性定义
        public FtpClient()
        {
            downloadCompleted = true;
            uploadCompleted = true;
            ftpDirectory = "";
        }
        //创建FTP基本登录类
        public FtpClient(string host, string userName, string password)
        {
            _host = host;
            _userName = userName;
            _password = password;
            ftpDirectory = "ftp://" + _host;
        }
        //获取基本FTP请求
        public FtpWebRequest CreateFtpWebRequest()         
        {
            FtpWebRequest request;
            request = (FtpWebRequest)WebRequest.Create(ftpDirectory);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            request.Credentials = new NetworkCredential(_userName, _password);
            //reqFtp.UsePassive = false;这是因为在下载文件或者获取文件夹信息过程中出现读取中断而将其设置为false的，即出现“应PASV命令的请求，服务器返回了一个与FTP连接地址不同的地址”的异常。
            request.KeepAlive = false;
            return request;
        } 
        //获取文件目录
        public ArrayList GetDirectiories()
        {
            ArrayList directories = new ArrayList();
            try
            {
                using(FtpWebResponse response = (FtpWebResponse)CreateFtpWebRequest().GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.Default))
                    {
                        string directory;
                        while((directory = reader.ReadLine())!= null)
                        {
                            directories.Add(directory);
                        }
                    }
                }
                return directories;
            }
            catch
            {
                throw new Exception("错误:无法连接到: " + _host);
            }
        }

        //改变文件目录
        public ArrayList ChangeDirectory(string directoryName)
        {
            ftpDirectory += "/" + directoryName;
            return GetDirectiories();
        }

        public ArrayList ChangeDirectoryUp()
        {
            if(ftpDirectory != "ftp://" + _host)
            {
                ftpDirectory = ftpDirectory.Remove(ftpDirectory.LastIndexOf("/"), ftpDirectory.Length - ftpDirectory.LastIndexOf("/"));
                return GetDirectiories();
            }
            else
            {
                return GetDirectiories();
            }
        }

        //用异步方式进行文件的下载
        public void DownloadFileAsync(string ftpFileName, string localFileName)
        {
            WebClient client = new WebClient();
            try
            {
                Uri uri = new Uri(ftpDirectory + "/" + ftpFileName);
                FileInfo file = new FileInfo(localFileName);
                if (file.Exists)
                {
                    throw new Exception("错误: 文件 " + localFileName + " 不存在.");
                }
                else
                {
                    client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.Credentials = new NetworkCredential(_userName, _password);
                    client.DownloadFileAsync(uri, localFileName);
                    downloadCompleted = false;
                }

            }
            catch (Exception)
            {
                client.Dispose();
                throw new Exception("错误: 此文件不可下载.");
            }
        }
        //将文件进行上传的函数
        public void UploadFileAsync(string fileName)
        {
            try
            {
                RequestCachePolicy cache = new RequestCachePolicy(RequestCacheLevel.Reload);
                WebClient client = new WebClient();
                FileInfo file = new FileInfo(fileName);
                Uri uri = new Uri((ftpDirectory + "/" + file.Name).ToString());
                
                client.Credentials = new NetworkCredential(_userName, _password);
                uploadCompleted = false;
                if (file.Exists)
                {
                    client.UploadFileCompleted += new UploadFileCompletedEventHandler(client_UploadFileCompleted);
                    client.UploadProgressChanged += new UploadProgressChangedEventHandler(client_UploadProgressChanged);
                    client.UploadFileAsync(uri, fileName);
                }

            }
            catch (Exception)
            {
                throw new Exception("错误: 不能够上传");
            }
        }
        //删除文件目录的主函数
        public string DeleteFile(string fileName)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpDirectory + "//" + fileName);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(_userName, _password);
                request.KeepAlive = false;
                request.UsePassive = true;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                return response.StatusDescription;
            }
            catch (Exception exc)
            {
                throw new Exception("错误: 无法删除文件" + fileName + " (" +exc.Message + ")");
            }
        }


        private void client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            this.OnUploadProgressChanged(sender, e);
        }

        private void client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            this.OnUploadCompleted(sender, e);
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.OnDownloadProgressChanged(sender, e);
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.OnDownloadCompleted(sender, e);
        }

        #region Events
        public delegate void DownProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e);
        public event DownProgressChangedEventHandler DownProgressChanged;
        protected virtual void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (DownProgressChanged != null) DownProgressChanged(sender, e);
        }

        public delegate void DownCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
        public event DownCompletedEventHandler DownCompleted;
        protected virtual void OnDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (DownCompleted != null) DownCompleted(sender, e);
        }

        public delegate void UpProgressChangedEventHandler(object sender,UploadProgressChangedEventArgs e);
        public event UpProgressChangedEventHandler UpProgressChanged;
        protected virtual void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            if (UpProgressChanged != null) UpProgressChanged(sender, e);
        }

        public delegate void UpCompletedEventHandler(object sender, UploadFileCompletedEventArgs e);
        public event UpCompletedEventHandler UpCompleted;
        protected virtual void OnUploadCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            if (UpCompleted != null) UpCompleted(sender, e);
        }
        #endregion



    }


}
