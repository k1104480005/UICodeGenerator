using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UICodeGeneration
{
    /// <summary>
    /// 代码生成器都需要继承该接口
    /// </summary>
    public interface IGenerateCode
    {
        /// <summary>
        /// 生成代码文件的文本并返回
        /// </summary>
        /// <returns></returns>
        public abstract string GenerationCustom();

        /// <summary>
        /// 生成代码文件的文本并返回
        /// </summary>
        /// <param name="info"> View组件数据 </param>
        /// <returns></returns>
        public abstract string Generation(ViewCodeInfo info);


        #region View相关接口

        /// <summary>
        /// 通过检验代码文件的文本内容来确定是否是View文件
        /// </summary>
        /// <param name="CodeTextContent"> 传入的代码文件全文本 </param>
        /// <param name="className"> 传出类名 </param>
        /// <returns> 返回是否View文件 </returns>
        public bool FilterViewByCodeContent(string CodeTextContent, out string className);

        /// <summary>
        /// 解析View文件并返回组件数据字典（正则匹配）
        /// </summary>
        /// <param name="CodeTextContent"> 传入的代码文件全文本 </param>
        /// <returns>  </returns>
        public Dictionary<string, UIComponentInfo> ParseViewCode(GameObject UIPrefabInst, string CodeTextContent, out string error);

        #endregion

    }
}