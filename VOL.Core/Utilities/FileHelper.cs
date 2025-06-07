using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VOL.Core.Extensions;

namespace VOL.Core.Utilities
{
    public class FileHelper
    {
        private static object _filePathObj = new object();

        /// <summary>
        /// 通过迭代器读取平面文件行内容。注意：必须是带有\r\n换行的文件。对于百万行以上的内容，读取效率可能存在问题，适用于100M左右、100万行以内的文件，超出范围可能会导致卡顿。
        /// </summary>
        /// <param name="fullPath">文件全路径 : System.String</param>
        /// <param name="page">分页页数 : System.Int32</param>
        /// <param name="pageSize">分页大小 : System.Int32</param>
        /// <param name="seekEnd">是否从最后一行向前读取，默认为false（从前向后读取） : System.Boolean</param>
        /// <returns>读取到的文件行内容 : System.Collections.Generic.IEnumerable&lt;System.String&gt;</returns>
        public static IEnumerable<string> ReadPageLine(string fullPath, int page, int pageSize, bool seekEnd = false)
        {
            if (page <= 0)
            {
                page = 1;
            }
            fullPath = fullPath.ReplacePath();
            var lines = File.ReadLines(fullPath, Encoding.UTF8);
            if (seekEnd)
            {
                int lineCount = lines.Count();
                int linPageCount = (int)Math.Ceiling(lineCount / (pageSize * 1.00));
                //超过总页数，不处理
                if (page > linPageCount)
                {
                    page = 0;
                    pageSize = 0;
                }
                else if (page == linPageCount)//最后一页，取最后一页剩下所有的行
                {
                    pageSize = lineCount - (page - 1) * pageSize;
                    if (page == 1)
                    {
                        page = 0;
                    }
                    else
                    {
                        page = lines.Count() - page * pageSize;
                    }
                }
                else
                {
                    page = lines.Count() - page * pageSize;
                }
            }
            else
            {
                page = (page - 1) * pageSize;
            }
            lines = lines.Skip(page).Take(pageSize);

            var enumerator = lines.GetEnumerator();
            int count = 1;
            while (enumerator.MoveNext() || count <= pageSize)
            {
                yield return enumerator.Current;
                count++;
            }
            enumerator.Dispose();
        }

        /// <summary>
        /// 检查指定路径的文件是否存在。
        /// </summary>
        /// <param name="path">文件的完整路径 : System.String</param>
        /// <returns>如果文件存在则返回true，否则返回false : System.Boolean</returns>
        public static bool FileExists(string path)
        {
            return File.Exists(path.ReplacePath());
        }

        /// <summary>
        /// 获取当前应用程序的下载路径。
        /// </summary>
        /// <returns>应用程序的下载路径 : System.String</returns>
        public static string GetCurrentDownLoadPath()
        {
            return ("Download\\").MapPath();
        }

        /// <summary>
        /// 检查指定路径的目录是否存在。
        /// </summary>
        /// <param name="path">目录的完整路径 : System.String</param>
        /// <returns>如果目录存在则返回true，否则返回false : System.Boolean</returns>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path.ReplacePath());
        }

        /// <summary>
        /// 根据目录、文件名和后缀读取文件内容。
        /// </summary>
        /// <param name="fullpath">文件所在目录的完整路径 : System.String</param>
        /// <param name="filename">文件名（不含后缀） : System.String</param>
        /// <param name="suffix">文件后缀名 (例如: .txt) : System.String</param>
        /// <returns>文件内容 : System.String</returns>
        public static string Read_File(string fullpath, string filename, string suffix)
        {
            return ReadFile((fullpath + "\\" + filename + suffix).MapPath());
        }

        /// <summary>
        /// 根据文件的完整路径读取文件内容。
        /// </summary>
        /// <param name="fullName">文件的完整路径 (包括文件名和后缀) : System.String</param>
        /// <returns>文件内容 : System.String</returns>
        public static string ReadFile(string fullName)
        {
            //  Encoding code = Encoding.GetEncoding(); //Encoding.GetEncoding("gb2312");
            string temp = fullName.MapPath().ReplacePath();
            string str = "";
            if (!File.Exists(temp))
            {
                return str;
            }
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(temp);
                str = sr.ReadToEnd(); // 读取文件
            }
            catch { }
            sr?.Close();
            sr?.Dispose();
            return str;
        }



        /// <summary>
        /// 从完整文件名中提取并返回文件的后缀名。
        /// </summary>
        /// <param name="filename">包含后缀的文件名 (例如: "example.txt", "image.jpg") : System.String</param>
        /// <returns>文件的后缀名 (例如: ".txt", ".jpg") : System.String</returns>
        public static string GetPostfixStr(string filename)
        {
            int start = filename.LastIndexOf(".");
            int length = filename.Length;
            string postfix = filename.Substring(start, length - start);
            return postfix;
        }


        /// <summary>
        /// 将指定的文本内容写入到文件中。可以选择是覆盖现有文件还是追加到文件末尾。
        /// </summary>
        /// <param name="path">文件保存的目录路径 : System.String</param>
        /// <param name="fileName">要写入的文件名 (包含后缀) : System.String</param>
        /// <param name="content">要写入文件的文本内容 : System.String</param>
        /// <param name="appendToLast">如果为true，则将内容追加到文件末尾；如果为false（默认值），则覆盖现有文件内容 : System.Boolean</param>
        public static void WriteFile(string path, string fileName, string content, bool appendToLast = false)
        {
            path = path.ReplacePath();
            fileName = fileName.ReplacePath();
            if (!Directory.Exists(path))//如果不存在就创建file文件夹
                Directory.CreateDirectory(path);

            using (FileStream stream = File.Open(path + fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] by = Encoding.Default.GetBytes(content);
                if (appendToLast)
                {
                    stream.Position = stream.Length;
                }
                else
                {
                    stream.SetLength(0);
                }
                stream.Write(by, 0, by.Length);
            }
        }



        /// <summary>
        /// 向指定的文件中追加文本内容。
        /// </summary>
        /// <param name="Path">目标文件的完整路径 : System.String</param>
        /// <param name="strings">要追加到文件中的文本内容 : System.String</param>
        public static void FileAdd(string Path, string strings)
        {
            StreamWriter sw = File.AppendText(Path.ReplacePath());
            sw.Write(strings);
            sw.Flush();
            sw.Close();
            sw.Dispose();
        }


        /// <summary>
        /// 将源文件复制到目标文件路径。如果目标文件已存在，则覆盖它。
        /// </summary>
        /// <param name="OrignFile">原始文件的完整路径 : System.String</param>
        /// <param name="NewFile">目标文件的完整路径 : System.String</param>
        public static void FileCoppy(string OrignFile, string NewFile)
        {
            File.Copy(OrignFile.ReplacePath(), NewFile.ReplacePath(), true);
        }


        /// <summary>
        /// 删除指定路径的文件。
        /// </summary>
        /// <param name="Path">要删除的文件的完整路径 : System.String</param>
        public static void FileDel(string Path)
        {
            File.Delete(Path.ReplacePath());
        }

        /// <summary>
        /// 将源文件移动到目标文件路径。
        /// </summary>
        /// <param name="OrignFile">原始文件的完整路径 : System.String</param>
        /// <param name="NewFile">目标文件的完整路径 : System.String</param>
        public static void FileMove(string OrignFile, string NewFile)
        {
            File.Move(OrignFile.ReplacePath(), NewFile.ReplacePath());
        }

        /// <summary>
        /// 在指定的原始目录下创建一个新的子目录。
        /// </summary>
        /// <param name="OrignFolder">原始目录的路径，新目录将在此目录下创建 : System.String</param>
        /// <param name="NewFloder">要创建的新子目录的名称 : System.String</param>
        public static void FolderCreate(string OrignFolder, string NewFloder)
        {
            Directory.SetCurrentDirectory(OrignFolder.ReplacePath());
            Directory.CreateDirectory(NewFloder.ReplacePath());
        }

        /// <summary>
        /// 根据指定的路径创建文件夹。如果文件夹已存在，则不进行任何操作。
        /// </summary>
        /// <param name="Path">要创建的文件夹的完整路径 : System.String</param>
        public static void FolderCreate(string Path)
        {
            // 判断目标目录是否存在如果不存在则新建之
            if (!Directory.Exists(Path.ReplacePath()))
                Directory.CreateDirectory(Path.ReplacePath());
        }

        /// <summary>
        /// 根据指定的路径创建一个空文件。如果文件已存在，则不进行任何操作。
        /// </summary>
        /// <param name="Path">要创建的文件的完整路径 : System.String</param>
        public static void FileCreate(string Path)
        {
            FileInfo CreateFile = new FileInfo(Path.ReplacePath()); //创建文件 
            if (!CreateFile.Exists)
            {
                FileStream FS = CreateFile.Create();
                FS.Close();
            }
        }

        /// <summary>
        /// 递归删除指定目录及其所有子目录和文件。
        /// </summary>
        /// <param name="dir">要删除的目录的完整路径 : System.String</param>
        public static void DeleteFolder(string dir)
        {
            dir = dir.ReplacePath();
            if (Directory.Exists(dir)) //如果存在这个文件夹删除之 
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                        File.Delete(d); //直接删除其中的文件                        
                    else
                        DeleteFolder(d); //递归删除子文件夹 
                }
                Directory.Delete(dir, true); //删除已空文件夹                 
            }
        }


        /// <summary>
        /// 将源文件夹的所有内容（包括子文件夹和文件）复制到目标文件夹。
        /// </summary>
        /// <param name="srcPath">源文件夹的完整路径 : System.String</param>
        /// <param name="aimPath">目标文件夹的完整路径。如果目标文件夹不存在，将会被创建。 : System.String</param>
        public static void CopyDir(string srcPath, string aimPath)
        {
            try
            {
                aimPath = aimPath.ReplacePath();
                // 检查目标目录是否以目录分割字符结束如果不是则添加之
                if (aimPath[aimPath.Length - 1] != Path.DirectorySeparatorChar)
                    aimPath += Path.DirectorySeparatorChar;
                // 判断目标目录是否存在如果不存在则新建之
                if (!Directory.Exists(aimPath))
                    Directory.CreateDirectory(aimPath);
                // 得到源目录的文件列表，该里面是包含文件以及目录路径的一个数组
                //如果你指向copy目标文件下面的文件而不包含目录请使用下面的方法
                //string[] fileList = Directory.GetFiles(srcPath);
                string[] fileList = Directory.GetFileSystemEntries(srcPath.ReplacePath());
                //遍历所有的文件和目录
                foreach (string file in fileList)
                {
                    //先当作目录处理如果存在这个目录就递归Copy该目录下面的文件

                    if (Directory.Exists(file))
                        CopyDir(file, aimPath + Path.GetFileName(file));
                    //否则直接Copy文件
                    else
                        File.Copy(file, aimPath + Path.GetFileName(file), true);
                }
            }
            catch (Exception ee)
            {
                throw new Exception(ee.ToString());
            }
        }

        /// <summary>
        /// 计算指定文件夹的总大小（包括所有子文件夹和文件）。
        /// </summary>
        /// <param name="dirPath">要计算大小的文件夹的完整路径 : System.String</param>
        /// <returns>文件夹的总大小（单位：字节） : System.Int64</returns>
        public static long GetDirectoryLength(string dirPath)
        {
            dirPath = dirPath.ReplacePath();
            if (!Directory.Exists(dirPath))
                return 0;
            long len = 0;
            DirectoryInfo di = new DirectoryInfo(dirPath);
            foreach (FileInfo fi in di.GetFiles())
            {
                len += fi.Length;
            }
            DirectoryInfo[] dis = di.GetDirectories();
            if (dis.Length > 0)
            {
                for (int i = 0; i < dis.Length; i++)
                {
                    len += GetDirectoryLength(dis[i].FullName);
                }
            }
            return len;
        }

        /// <summary>
        /// 获取指定文件的详细属性信息，并以字符串形式返回。
        /// </summary>
        /// <param name="filePath">要获取属性的文件的完整路径 : System.String</param>
        /// <returns>一个包含文件详细属性（如路径、名称、大小、创建时间、修改时间等）的描述字符串 : System.String</returns>
        public static string GetFileAttibe(string filePath)
        {
            string str = "";
            filePath = filePath.ReplacePath();
            System.IO.FileInfo objFI = new System.IO.FileInfo(filePath);
            str += "详细路径:" + objFI.FullName + "<br>文件名称:" + objFI.Name + "<br>文件长度:" + objFI.Length.ToString() + "字节<br>创建时间" + objFI.CreationTime.ToString() + "<br>最后访问时间:" + objFI.LastAccessTime.ToString() + "<br>修改时间:" + objFI.LastWriteTime.ToString() + "<br>所在目录:" + objFI.DirectoryName + "<br>扩展名:" + objFI.Extension;
            return str;
        }

    }
}
