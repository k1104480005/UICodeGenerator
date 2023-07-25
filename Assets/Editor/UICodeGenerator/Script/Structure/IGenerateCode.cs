using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UICodeGeneration
{
    /// <summary>
    /// ��������������Ҫ�̳иýӿ�
    /// </summary>
    public interface IGenerateCode
    {
        /// <summary>
        /// ���ɴ����ļ����ı�������
        /// </summary>
        /// <returns></returns>
        public abstract string GenerationCustom();

        /// <summary>
        /// ���ɴ����ļ����ı�������
        /// </summary>
        /// <param name="info"> View������� </param>
        /// <returns></returns>
        public abstract string Generation(ViewCodeInfo info);


        #region View��ؽӿ�

        /// <summary>
        /// ͨ����������ļ����ı�������ȷ���Ƿ���View�ļ�
        /// </summary>
        /// <param name="CodeTextContent"> ����Ĵ����ļ�ȫ�ı� </param>
        /// <param name="className"> �������� </param>
        /// <returns> �����Ƿ�View�ļ� </returns>
        public bool FilterViewByCodeContent(string CodeTextContent, out string className);

        /// <summary>
        /// ����View�ļ���������������ֵ䣨����ƥ�䣩
        /// </summary>
        /// <param name="CodeTextContent"> ����Ĵ����ļ�ȫ�ı� </param>
        /// <returns>  </returns>
        public Dictionary<string, UIComponentInfo> ParseViewCode(GameObject UIPrefabInst, string CodeTextContent, out string error);

        #endregion

    }
}