using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UICodeGeneration
{
    [CreateAssetMenu(fileName = "GenSettingSO", menuName = "OEngine/UI/GenSettingSO")]
    public class GenSettingSO : ScriptableObject
    {
        [HideLabel, HideReferenceObjectPicker]
        public CodeGenSetting CodeGenSetting;
    }

    /// <summary>
    /// ���������Ϣ
    /// </summary>
    [Serializable]
    public class CodeGenSetting
    {
        /// <summary>
        /// ������ɴ���ĸ�Ŀ¼
        /// </summary>
        [PropertyOrder(1), PropertySpace(2, 0), LabelText("������ɴ���ĸ��ļ���"), FolderPath()]
        public string OutputCodeRootFolder = "";

        [PropertyOrder(2), PropertySpace(4, 0), BoxGroup("b", ShowLabel = false), LabelText("View�������"), HideReferenceObjectPicker]
        public OutputRule View_OutputRule = new OutputRule();


        [PropertyOrder(4), PropertySpace(8, 0), LabelText("�����������"), ListDrawerSettings(ShowFoldout = true)]
        public List<OutputRule> OtherOutputRules = new List<OutputRule>();

    }

    [System.Flags]
    public enum ApplyTo
    {
        All = �������� | �ӽ���,
        �������� = 1 << 1,
        �ӽ��� = 1 << 2,
    }

    #region Output



    [Serializable]
    public class OutputRule
    {
        public OutputRule()
        {
            RuleName = $"New �������";
        }


        [PropertyOrder(-2), PropertySpace(4, 4), HorizontalGroup("beg", Width = 200), LabelWidth(55), LabelText("����ע��"), GUIColor("#56B5AA")]
        public string RuleName;

        [PropertyOrder(-1), PropertySpace(2, 4), HorizontalGroup("beg", Width = 120), LabelWidth(80), LabelText("�����Ǳ���?"), InfoBox("�Ѽ���Ǳ��棡", InfoMessageType.Warning, VisibleIf = "AllowOverwrite"), HideIf("InsertionMode")]
        public bool AllowOverwrite = false;

        [PropertyOrder(0), PropertySpace(2, 4), HorizontalGroup("beg2"), LabelWidth(40), LabelText("��Ч��"), EnumToggleButtons]
        public ApplyTo ApplyToWho = ApplyTo.All;

        [PropertyOrder(1), BoxGroup("b", LabelText = "Ŀ¼����", CenterLabel = true), HorizontalGroup("b/h", Width = 0.3f), LabelWidth(30), LabelText("����"), HideIf("InsertionMode")]
        public FolderRuleMode FolderRuleTp;

        [PropertyOrder(2), BoxGroup("b"), HorizontalGroup("b/h"), ShowInInspector, LabelWidth(170), LabelText("���·���������ģ���ļ��У�"), ShowIf("@FolderRuleTp == FolderRuleMode.Assign"), HideIf("InsertionMode")]
        public string AssignRelativeFolderPath = "";



        [PropertyOrder(10), BoxGroup("b3", CenterLabel = true, LabelText = "��������"), HideReferenceObjectPicker, LabelWidth(80), HideLabel, HideIf("InsertionMode")]
        public NamingRule namingRule;


        [PropertyOrder(20), BoxGroup("b2", CenterLabel = true, LabelText = "����ģʽ"), LabelWidth(80), LabelText("ģʽ")]
        public GenerationMode GenMode;

        [PropertyOrder(21), BoxGroup("b2"), ValueDropdown("@this.GetGeneratorList()", DropdownTitle = "��ѡ���������ű�"), LabelWidth(80), LabelText("ָ��������"), ShowIf("@GenMode == GenerationMode.GeneratorCode")]
        public string Gen_Generator = "";

        [PropertyOrder(22), BoxGroup("b2"), ValueDropdown("@this.GetGeneratorInterfaceList()", DropdownTitle = "��ѡ��ӿ�"), LabelWidth(80), LabelText("ָ���ӿ�"), ShowIf("@GenMode == GenerationMode.GeneratorCode")]
        public string Gen_GeneratorMethod = "";

        [PropertyOrder(21), BoxGroup("b2"), LabelWidth(80), LabelText("ָ��ģ���ļ�"), FilePath(ParentFolder = "$fileParent", Extensions = "cs"), ShowIf("@GenMode == GenerationMode.Template")]
        public string Gen_Template = "";

        bool InsertionMode => GenMode == GenerationMode.Insertion;//����ģʽ�ķ�����
        string fileParent => Application.dataPath;

        [PropertyOrder(22), BoxGroup("b2"), LabelWidth(80), LabelText("Ŀ���ļ�"), FilePath(ParentFolder = "$fileParent", Extensions = "cs"), ShowIf("InsertionMode")]
        public string InsertionTargetCodeFile = "";

        [PropertyOrder(23), ShowInInspector, BoxGroup("b2"), LabelText("������Ϣ"), ShowIf("InsertionMode"), HideReferenceObjectPicker, ListDrawerSettings(DraggableItems = false, ShowIndexLabels = true)]
        public List<InsertionData> InsertionList = new List<InsertionData>();

        [PropertyOrder(25), BoxGroup("b2"), HorizontalGroup("b2/h2", Width = 0.4f), ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false), HideReferenceObjectPicker, LabelText("Before ָ���滻���"), ShowIf("@GenMode == GenerationMode.Template || GenMode == GenerationMode.Insertion")]
        public List<ReplaceRuleData> Gen_Template_Replace_Before = new List<ReplaceRuleData>()
        {
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_ModuleName),
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_ClassName),
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_Time),
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_Author)
        };

        [PropertyOrder(26), ShowInInspector, BoxGroup("b2"), HorizontalGroup("b2/h2", Width = 0.6f), LabelText("After�����滻 �����ӽ�����Ч��"), ShowIf("@GenMode == GenerationMode.Template"), HideReferenceObjectPicker, ListDrawerSettings(DraggableItems = false)]
        public List<ReplaceRuleData2> Gen_Template_Replace_After_OnlySubView = new List<ReplaceRuleData2>();//��SubViewʱ�Ķ����滻





        /// <summary>
        /// ��������������б� (�������Ǽ̳�IGenerateCode�ӿڵ���) 
        /// </summary>
        /// <returns></returns>
        IEnumerable GetGeneratorList()
        {
            var q = typeof(IGenerateCode).Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsClass).Where(x => !x.IsGenericTypeDefinition).Where(x => typeof(IGenerateCode).IsAssignableFrom(x));
            List<Type> list = q.ToArray().ToList();

            //��� ValueDropdownList
            ValueDropdownList<string> dl = new Sirenix.OdinInspector.ValueDropdownList<string>();
            for (int i = 0; i < list.Count; i++)
                dl.Add(list[i].FullName, list[i].FullName);
            return dl;
        }


        /// <summary>
        /// �����ѡ�����������нӿ�(�������Ǽ̳�IGenerateCode�ӿڵ���)
        /// </summary>
        /// <returns></returns>
        IEnumerable GetGeneratorInterfaceList()
        {
            if (string.IsNullOrEmpty(Gen_Generator))
            {
                // GeneratorHelper.LogError("GetGeneratorInterfaceList Gen_Generator null");
                return new Sirenix.OdinInspector.ValueDropdownList<string>();
            }

            var q = typeof(IGenerateCode).Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsClass).Where(x => !x.IsGenericTypeDefinition).Where(x => typeof(IGenerateCode).IsAssignableFrom(x)).Where(x => x.FullName == Gen_Generator);


            List<Type> list = q.ToArray().ToList();
            if (list.Count <= 0)
            {
                GeneratorHelper.LogError("GetGeneratorInterfaceList q is 0");
                return new Sirenix.OdinInspector.ValueDropdownList<string>();
            }
            else if (list.Count > 1)
            {
                GeneratorHelper.LogError("GetGeneratorInterfaceList q > 1");
                return new Sirenix.OdinInspector.ValueDropdownList<string>() { "����", "����" };
            }
            else
            {
                //���������������Generation�ķ���
                Type t = list[0];
                MethodInfo[] arr = t.GetMethods();
                List<MethodInfo> minfoList = new List<MethodInfo>();
                foreach (var i in arr)
                {
                    if (i.Name.Contains("Generation"))
                        minfoList.Add(i);
                }
                // GeneratorHelper.Log("GetGeneratorInterfaceList ok:"+ minfoList.Count);
                //��� ValueDropdownList
                ValueDropdownList<string> dl = new Sirenix.OdinInspector.ValueDropdownList<string>();
                for (int i = 0; i < minfoList.Count; i++)
                    dl.Add(minfoList[i].Name, minfoList[i].Name);
                return dl;
            }
        }
    }



    /// <summary>
    /// Ŀ¼����ģʽ
    /// </summary>
    public enum FolderRuleMode
    {
        Flexible = 0,       //��ѡĿ¼
        Assign,             //ָ��Ŀ¼
    }

    /// <summary>
    /// ����ģʽ
    /// </summary>
    public enum GenerationMode
    {
        GeneratorCode = 0,  //ʹ�ô�������
        Template,           //ʹ��ģ������
        Insertion,          //�����Զ�������������ļ���
    }

    #endregion

    #region Naming

    [Serializable]
    public class NamingRule
    {
        [PropertyOrder(2), ShowInInspector, LabelText("����")]
        public NamingType type = NamingType.ģ������ǰ��׺;

        [PropertyOrder(3), ShowInInspector, LabelText("ָ��ȫ��"), ShowIf("@type == NamingType.ָ������")]
        public string assignName;

        [PropertyOrder(5), ShowInInspector, LabelText("ǰ׺"), ShowIf("@type == NamingType.ģ������ǰ��׺")]
        public string prefix;

        [PropertyOrder(5), ShowInInspector, LabelText("��׺"), ShowIf("@type == NamingType.ģ������ǰ��׺")]
        public string suffix;

        [PropertyOrder(8), ShowInInspector, LabelText("Ԥ��"), DisplayAsString, ShowIf("@type != NamingType.View����ר��"), ReadOnly]
        public string preview
        {
            get
            {
                switch (type)
                {
                    case NamingType.View����ר��: return "��Ч";
                    case NamingType.ָ������: return assignName;
                    case NamingType.ģ������ǰ��׺: return $"{prefix}ģ����{suffix}";
                    default: return "����";
                }
            }
        }
    }

    public enum NamingType
    {
        View����ר�� = 0,       //ֻ��View����Ż�ѡ��
        ָ������,              //�̶�������
        ģ������ǰ��׺,
    }

    #endregion

    #region Template


    [Serializable]
    public class ReplaceRuleData
    {
        public const string REPLACEKEY_ModuleName = "ģ����";
        public const string REPLACEKEY_ClassName = "����";
        public const string REPLACEKEY_Time = "ʱ��";
        public const string REPLACEKEY_Author = "����";

        [ShowInInspector, HorizontalGroup("h", Width = 0.2f), PropertyOrder(0), HideLabel, ReadOnly]
        public string key = "";

        [ShowInInspector, HorizontalGroup("h"), PropertyOrder(0), LabelText(" -> "), LabelWidth(20)]
        public string value = "";

        public ReplaceRuleData(string pkey)
        {
            key = pkey;
        }
    }

    [Serializable]
    public class ReplaceRuleData2
    {
        [ShowInInspector, HorizontalGroup("h"), PropertyOrder(0), HideLabel]
        public string key = "";

        [ShowInInspector, HorizontalGroup("h"), PropertyOrder(0), LabelText(">"), LabelWidth(10)]
        public string value = "";

        public ReplaceRuleData2(string pkey)
        {
            key = pkey;
        }
    }

    #endregion

    #region Insertion

    [Serializable]
    public class InsertionData
    {
        [ShowInInspector, PropertyOrder(0), LabelText("λ�ñ�ǣ�"), LabelWidth(55), TextArea(1, 2)]
        public string key = "";

        [ShowInInspector, PropertyOrder(2), LabelText("����ģ�壨��ʹ��ָ���滻��ǣ���ִ��ǰ���Ƚ����滻��"), LabelWidth(120), TextArea(2, 20)]
        public string value = "";

        [ShowInInspector, PropertyOrder(1), LabelText("�������򣨿�ʹ��ָ���滻��ǣ���ִ��ǰ���Ƚ����滻��"), LabelWidth(120), TextArea(1, 3)]
        public string checkExistsMatchPattern = "";

    }

    #endregion
}