using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

namespace UICodeGeneration
{
    /// <summary>
    /// �ռ�����·�����൱�ھɰ汾�е�PathList�ļ���
    /// </summary>
    [CreateAssetMenu(fileName = "CodePathCollectSO", menuName = "OEngine/UI/CodePathCollectSO")]
    public class CodePathCollectSO : ScriptableObject
    {
        [ShowInInspector,ReadOnly,LabelText("����·���ռ�����")]
        public List<CodePathCollectData> View2ScriptPath = new List<CodePathCollectData>();
    }

    [Serializable]
    public class CodePathCollectData
    {
        [PropertySpace(0), PropertyOrder(0), HorizontalGroup("H"), LabelText("KEY"),LabelWidth(40), TableColumnWidth(100,false)]
        public string key;

        [PropertySpace(0), PropertyOrder(1), HorizontalGroup("H"), LabelText("PATH"), LabelWidth(40)]
        public string value;
    }
}