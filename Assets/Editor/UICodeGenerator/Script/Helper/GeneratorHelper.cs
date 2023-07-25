using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UICodeGeneration
{
    public static class GeneratorHelper
    {
        /// <summary>
        /// ���ļ�����Ѱ��ĳ���ļ�
        /// </summary>
        /// <param name="folderPath"> �����ļ��� </param>
        /// <param name="fileName"> ����׺�� </param>
        /// <returns></returns>
        public static List<FileInfo> FindCodeFile(string folderPath, string fileName)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError("FindCodeFile folderPath null");
                return null;
            }

            DirectoryInfo direction = new DirectoryInfo(folderPath);
            FileInfo[] files = direction.GetFiles(fileName, SearchOption.AllDirectories);
            Debug.Log($"{folderPath} Ѱ�� {fileName} files:{files.Length}");
            List<FileInfo> temps = new List<FileInfo>();

            foreach (var i in files)
            {
                if (i.Name.EndsWith(".meta"))
                    continue;
                temps.Add(i);
            }
            return temps;
        }

        /// <summary>
        /// ͨ�õ� �ҵ��ļ����µ������ļ�
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="fullnames"></param>
        /// <param name="names"></param>
        /// <param name="suffix"> ��׺ </param>
        /// <returns></returns>
        public static int FindAllFiles(string folderPath, out List<string> fullnames, out List<string> names, string suffix = null)
        {
            fullnames = new List<string>();  //�ļ�����·��
            names = new List<string>();      //�ļ�����׺����

            int count = 0;

            //�ж��Ƿ��д��ļ���
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo direction = new DirectoryInfo(folderPath);
                FileInfo[] files = string.IsNullOrEmpty(suffix) ? direction.GetFiles("*") : direction.GetFiles($"*{suffix}");
                for (int i = 0; i < files.Length; i++)
                {
                    //ȥ��Unity�ڲ�.meta�ļ�
                    if (files[i].Name.EndsWith(".meta"))
                        continue;

                    fullnames.Add(files[i].FullName);
                    names.Add(files[i].Name);
                    count++;
                }
            }

            return count;
        }


        /// <summary>
        /// ������ͼ��
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public static SdfIconType GetComponentIcon(string componentName)
        {
            switch (componentName)
            {
                case "Transform":
                case "RectTransform":
                    return SdfIconType.ArrowsMove;
                case "Image":
                case "RawImage":
                case "BetterImage":
                    return SdfIconType.CardImage;
                case "Text":
                case "TextMeshProUGUI":
                    return SdfIconType.FileFont;
                default:
                    return SdfIconType.QuestionCircle;
            }
        }

        public static void Log(object message, Object context = null)
        {
            Debug.Log($"[UIGenerator]{message}", context);
        }

        public static void LogError(object message, Object context = null)
        {
            Debug.LogError($"[UIGenerator]{message}", context);
        }

        public static string GetHierarchyWithRoot(Transform obj, Transform root)
        {
            if (obj == null || obj == root)
                return "";
            string path = obj.name;

            while (obj.parent != root)
            {
                obj = obj.parent;
                path = obj.name + "/" + path;
            }
            return path;
        }

        public static List<T> ToList<T>(this IEnumerable<T> dataset)
        {
            List<T> list = new List<T>();
            if (dataset != null)
            {
                foreach (T item in dataset)
                    list.Add(item);
            }
            return list;
        }
    }
}