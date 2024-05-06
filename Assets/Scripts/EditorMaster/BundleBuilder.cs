using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using U3DUtility;
using System.Xml.Linq;

/// <summary>
/// 几个打包使用的菜单功能实现
/// </summary>
namespace U3DEditorUtility
{
    public class BundleBuilder
    {
        //如果isValidateFunction为true，则这是一个验证函数，并在之前被调用
        //调用具有相同itemName的菜单函数。
        [MenuItem(itemName: "Tools/打包工具/清空所有打包名", isValidateFunction: false, priority: 20)]
        private static void CleanResourcesAssetBundleName()
        {
            string appPath = Application.dataPath + "/";
            //返回被截取出来的字符串，不包含结束下标对应的字符，包头不包尾
            string projPath = appPath.Substring(0, appPath.Length - 7);   //....找到Asstes同级路径
            string fullPath = projPath + "/Assets/Resources";  

            //它用于在指定的路径上初始化DirectoryInfo类的新实例    ---DirectoryInfo构造
            DirectoryInfo dir = new DirectoryInfo(fullPath); 
            var files = dir.GetFiles("*", SearchOption.AllDirectories);   //它从当前目录返回文件列表  *通配符查找所有文件
            for (var i = 0; i < files.Length; ++i)
            {
                // FileInfo提供用于创建、复制、删除、移动和打开文件的属性和实例方法，并且帮助创建 FileStream 对象。此类不能被继承
                //或者通过指定路径创建一个文件实例对象 可对其进行操作 var fi1 = new FileInfo(path);
                var fileInfo = files[i];   //获取每一个文件
                //FullName 获取目录或文件的完整目录。
                // Name 获取文件名。
                //将截取从第projPath.Length个字符开始的字符串，直到字符串末尾
                string path = fileInfo.FullName.Replace('\\', '/').Substring(projPath.Length);

                //显示或更新进度条。
                //窗口标题将设置为 / title /，信息将设置为 / info /。
                //进度应设置为 0.0 和 1.0 之间的一个值，0 表示一点儿也没有完成，1.0 表示完成 100 %。
                EditorUtility.DisplayProgressBar("清理打包资源名称", "正在处理" + fileInfo.Name, 1f * i / files.Length);
                var importer = AssetImporter.GetAtPath(path);
                if (importer)
                {
                    importer.assetBundleName = null;
                }
            }

            AssetDatabase.Refresh();

            //清空进度帧
            EditorUtility.ClearProgressBar();

            Debug.Log("=========clean Lua bundle name finished.." + files.Length + " processed");
        }

        [MenuItem(itemName: "Tools/打包工具/生成资源打包名", isValidateFunction: false, priority: 20)]
        private static void SetResourcesAssetBundleName()
        {
            string appPath = Application.dataPath + "/";
            string projPath = appPath.Substring(0, appPath.Length - 7);

            //正则表达式
            //通俗的讲就是按照某种规则去匹配符合条件的字符串
            string[] searchExtensions = new[] {".prefab", ".mat", ".txt", ".png", ".jpg", ".shader", ".fbx", ".controller", ".anim", ".tga"};
            Regex[] excluseRules = new Regex[] 
            {
                //new Regex (@"^.*/Lua/.*$"), //忽略掉lua脚本，这些脚本会单独打包
            };

            string fullPath = projPath + "/Assets/Resources";

            SetDirAssetBundleName(fullPath, searchExtensions, excluseRules);

            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();

            Debug.Log("=========Set resource bundle name finished....");
        }

        [MenuItem(itemName: "Tools/打包工具/生成打包文件Android", isValidateFunction: false, priority: 20)]
        private static void BuildAllAssetBundlesAndroid()
        {
            UnityEngine.Debug.Log("=========Build AssetBundles Android start..");
            //用lz4格式压缩
            BuildAssetBundleOptions build_options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.IgnoreTypeTreeChanges | BuildAssetBundleOptions.DeterministicAssetBundle;
            string assetBundleOutputDir = Application.dataPath + "/../AssetBundles/Android/";
            if (!Directory.Exists(assetBundleOutputDir))
            {
                Directory.CreateDirectory(assetBundleOutputDir);
            }

            string projPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            BuildPipeline.BuildAssetBundles(assetBundleOutputDir.Substring(projPath.Length), build_options, BuildTarget.Android);

            Debug.Log("=========Build AssetBundles Android finished..");

            GenerateIndexFile(assetBundleOutputDir);
        }

        [MenuItem(itemName: "Tools/打包工具/生成打包文件Windows64", isValidateFunction: false, priority: 21)]
        private static void BuildAllAssetBundlesWindows()
        {
            UnityEngine.Debug.Log("=========Build AssetBundles Window 64 start..");
            //用lz4格式压缩
            BuildAssetBundleOptions build_options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.IgnoreTypeTreeChanges | BuildAssetBundleOptions.DeterministicAssetBundle;
            string assetBundleOutputDir = Application.dataPath + "/../AssetBundles/Windows/";
            if (!Directory.Exists(assetBundleOutputDir))
            {
                Directory.CreateDirectory(assetBundleOutputDir);
            }

            string projPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            BuildPipeline.BuildAssetBundles(assetBundleOutputDir.Substring(projPath.Length), build_options, BuildTarget.StandaloneWindows64);

            Debug.Log("=========Build AssetBundles Windows 64 finished..");

            GenerateIndexFile(assetBundleOutputDir);
        }

        [MenuItem(itemName: "Tools/打包工具/生成Windows64 Player", isValidateFunction: false, priority: 25)]
        private static void BuildWindowsPlayer()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/main.unity" }; //根据情况修改场景路径名
            buildPlayerOptions.locationPathName = "Win64Player";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        [MenuItem(itemName: "Tools/打包工具/生成 Android Player", isValidateFunction: false, priority: 26)]
        private static void BuildAndroidPlayer()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/main.unity" }; //根据情况修改场景路径名
            buildPlayerOptions.locationPathName = "AndroidPlayer";
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        /// <summary>
        /// 遍历目录下的资源文件，生成索引文件
        /// </summary>
        /// <param name="resDir">要遍历的目录</param>
        private static void GenerateIndexFile(string resDir)
        {
            string platName = resDir;

            if (platName[platName.Length - 1] == '/')
                platName = platName.Substring(0, platName.Length - 1);

            platName = platName.Substring(platName.LastIndexOf('/') + 1);
            DirectoryInfo dirInfo = new DirectoryInfo(resDir);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            List<BundleItem> items = new List<BundleItem>();
            foreach (var file in files)
            {
                if (file.Extension != ResUtils.BundleExtension && file.Name != platName)
                    continue; //只处理资源关系文件和特定后缀的文件

                BundleItem item = new BundleItem();
                item.m_HashCode = ResUtils.GetFileHash(file.FullName);
                item.m_FileSize = ResUtils.GetFileSize(file.FullName);
                item.m_Name = file.FullName.Substring(resDir.Length);
                items.Add(item);
            }

            IdxFile idx = new IdxFile();
            string idxContent = IdxFile.SaveString(items, resDir);
            string filePath = resDir + ResUtils.BundleIndexFileName;
            File.WriteAllText(filePath, idxContent);

            Debug.Log("=========Generated index file to .." + filePath);
        }

        /// <summary>
        /// 设置某个目录及子目录下资源打包名称
        /// </summary>
        /// <param name="fullPath">搜索资源的目录路径</param>
        /// <param name="searchExtensions">要打包的资源扩展名</param>
        /// <param name="excluseRules">要排除掉的资源，用正则表达式</param>
        private static void SetDirAssetBundleName(string fullPath, string[] searchExtensions, Regex[] excluseRules)
        {
            if (!Directory.Exists(fullPath))
            {
                return;
            }

            string appPath = Application.dataPath + "/";
            string projPath = appPath.Substring(0, appPath.Length - 7);
            DirectoryInfo dir = new DirectoryInfo(fullPath);
            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; ++i)
            {
                var fileInfo = files[i];

                string ext = fileInfo.Extension.ToLower();     //Extension获取文件的拓展名部分  转为小写
                bool isFound = false;
                foreach (var v in searchExtensions)
                {
                    if (ext == v)
                    {
                        isFound = true;
                        break;
                    }
                }

                if (!isFound)
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar("设置打包资源名称", "正在处理" + fileInfo.Name, 1f * i / files.Length);
                string fullName = fileInfo.FullName.Replace('\\', '/');
                bool isExcluse = false;
                foreach (Regex excluseRule in excluseRules)
                {
                    if (excluseRule.Match(fullName).Success)
                    {
                        isExcluse = true;
                        break;
                    }
                }

                if (isExcluse)
                {
                    continue;
                }

                string path = fileInfo.FullName.Replace('\\', '/').Substring(projPath.Length);
                var importer = AssetImporter.GetAtPath(path);
                if (importer)
                {
                    string name = path.Substring(fullPath.Substring(projPath.Length).Length);
                    string targetName = "";
                    targetName = name.ToLower() + ResUtils.BundleExtension;
                    if (importer.assetBundleName != targetName)
                    {
                        importer.assetBundleName = targetName;
                    }
                }
            }
        }
    }
}
