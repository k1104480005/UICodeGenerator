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
    /// 相关配置信息
    /// </summary>
    [Serializable]
    public class CodeGenSetting
    {
        /// <summary>
        /// 存放生成代码的根目录
        /// </summary>
        [PropertyOrder(1), PropertySpace(2, 0), LabelText("存放生成代码的根文件夹"), FolderPath()]
        public string OutputCodeRootFolder = "";

        [PropertyOrder(2), PropertySpace(4, 0), BoxGroup("b", ShowLabel = false), LabelText("View输出规则"), HideReferenceObjectPicker]
        public OutputRule View_OutputRule = new OutputRule();


        [PropertyOrder(4), PropertySpace(8, 0), LabelText("其他输出规则"), ListDrawerSettings(ShowFoldout = true)]
        public List<OutputRule> OtherOutputRules = new List<OutputRule>();

    }

    [System.Flags]
    public enum ApplyTo
    {
        All = 基础界面 | 子界面,
        基础界面 = 1 << 1,
        子界面 = 1 << 2,
    }

    #region Output



    [Serializable]
    public class OutputRule
    {
        public OutputRule()
        {
            RuleName = $"New 输出规则";
        }


        [PropertyOrder(-2), PropertySpace(4, 4), HorizontalGroup("beg", Width = 200), LabelWidth(55), LabelText("规则注释"), GUIColor("#56B5AA")]
        public string RuleName;

        [PropertyOrder(-1), PropertySpace(2, 4), HorizontalGroup("beg", Width = 120), LabelWidth(80), LabelText("允许覆盖保存?"), InfoBox("已激活覆盖保存！", InfoMessageType.Warning, VisibleIf = "AllowOverwrite"), HideIf("InsertionMode")]
        public bool AllowOverwrite = false;

        [PropertyOrder(0), PropertySpace(2, 4), HorizontalGroup("beg2"), LabelWidth(40), LabelText("生效于"), EnumToggleButtons]
        public ApplyTo ApplyToWho = ApplyTo.All;

        [PropertyOrder(1), BoxGroup("b", LabelText = "目录规则", CenterLabel = true), HorizontalGroup("b/h", Width = 0.3f), LabelWidth(30), LabelText("类型"), HideIf("InsertionMode")]
        public FolderRuleMode FolderRuleTp;

        [PropertyOrder(2), BoxGroup("b"), HorizontalGroup("b/h"), ShowInInspector, LabelWidth(170), LabelText("相对路径（相对于模块文件夹）"), ShowIf("@FolderRuleTp == FolderRuleMode.Assign"), HideIf("InsertionMode")]
        public string AssignRelativeFolderPath = "";



        [PropertyOrder(10), BoxGroup("b3", CenterLabel = true, LabelText = "命名规则"), HideReferenceObjectPicker, LabelWidth(80), HideLabel, HideIf("InsertionMode")]
        public NamingRule namingRule;


        [PropertyOrder(20), BoxGroup("b2", CenterLabel = true, LabelText = "生成模式"), LabelWidth(80), LabelText("模式")]
        public GenerationMode GenMode;

        [PropertyOrder(21), BoxGroup("b2"), ValueDropdown("@this.GetGeneratorList()", DropdownTitle = "请选择生成器脚本"), LabelWidth(80), LabelText("指定生成器"), ShowIf("@GenMode == GenerationMode.GeneratorCode")]
        public string Gen_Generator = "";

        [PropertyOrder(22), BoxGroup("b2"), ValueDropdown("@this.GetGeneratorInterfaceList()", DropdownTitle = "请选择接口"), LabelWidth(80), LabelText("指定接口"), ShowIf("@GenMode == GenerationMode.GeneratorCode")]
        public string Gen_GeneratorMethod = "";

        [PropertyOrder(21), BoxGroup("b2"), LabelWidth(80), LabelText("指定模板文件"), FilePath(ParentFolder = "$fileParent", Extensions = "cs"), ShowIf("@GenMode == GenerationMode.Template")]
        public string Gen_Template = "";

        bool InsertionMode => GenMode == GenerationMode.Insertion;//插入模式的访问器
        string fileParent => Application.dataPath;

        [PropertyOrder(22), BoxGroup("b2"), LabelWidth(80), LabelText("目标文件"), FilePath(ParentFolder = "$fileParent", Extensions = "cs"), ShowIf("InsertionMode")]
        public string InsertionTargetCodeFile = "";

        [PropertyOrder(23), ShowInInspector, BoxGroup("b2"), LabelText("插入信息"), ShowIf("InsertionMode"), HideReferenceObjectPicker, ListDrawerSettings(DraggableItems = false, ShowIndexLabels = true)]
        public List<InsertionData> InsertionList = new List<InsertionData>();

        [PropertyOrder(25), BoxGroup("b2"), HorizontalGroup("b2/h2", Width = 0.4f), ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false), HideReferenceObjectPicker, LabelText("Before 指定替换标记"), ShowIf("@GenMode == GenerationMode.Template || GenMode == GenerationMode.Insertion")]
        public List<ReplaceRuleData> Gen_Template_Replace_Before = new List<ReplaceRuleData>()
        {
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_ModuleName),
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_ClassName),
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_Time),
            new ReplaceRuleData(ReplaceRuleData.REPLACEKEY_Author)
        };

        [PropertyOrder(26), ShowInInspector, BoxGroup("b2"), HorizontalGroup("b2/h2", Width = 0.6f), LabelText("After额外替换 （仅子界面生效）"), ShowIf("@GenMode == GenerationMode.Template"), HideReferenceObjectPicker, ListDrawerSettings(DraggableItems = false)]
        public List<ReplaceRuleData2> Gen_Template_Replace_After_OnlySubView = new List<ReplaceRuleData2>();//仅SubView时的额外替换





        /// <summary>
        /// 获得所有生成器列表 (生成器是继承IGenerateCode接口的类) 
        /// </summary>
        /// <returns></returns>
        IEnumerable GetGeneratorList()
        {
            var q = typeof(IGenerateCode).Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsClass).Where(x => !x.IsGenericTypeDefinition).Where(x => typeof(IGenerateCode).IsAssignableFrom(x));
            List<Type> list = q.ToArray().ToList();

            //组成 ValueDropdownList
            ValueDropdownList<string> dl = new Sirenix.OdinInspector.ValueDropdownList<string>();
            for (int i = 0; i < list.Count; i++)
                dl.Add(list[i].FullName, list[i].FullName);
            return dl;
        }


        /// <summary>
        /// 获得已选生成器的所有接口(生成器是继承IGenerateCode接口的类)
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
                return new Sirenix.OdinInspector.ValueDropdownList<string>() { "错误", "错误" };
            }
            else
            {
                //获得所有命名包含Generation的方法
                Type t = list[0];
                MethodInfo[] arr = t.GetMethods();
                List<MethodInfo> minfoList = new List<MethodInfo>();
                foreach (var i in arr)
                {
                    if (i.Name.Contains("Generation"))
                        minfoList.Add(i);
                }
                // GeneratorHelper.Log("GetGeneratorInterfaceList ok:"+ minfoList.Count);
                //组成 ValueDropdownList
                ValueDropdownList<string> dl = new Sirenix.OdinInspector.ValueDropdownList<string>();
                for (int i = 0; i < minfoList.Count; i++)
                    dl.Add(minfoList[i].Name, minfoList[i].Name);
                return dl;
            }
        }
    }



    /// <summary>
    /// 目录规则模式
    /// </summary>
    public enum FolderRuleMode
    {
        Flexible = 0,       //手选目录
        Assign,             //指定目录
    }

    /// <summary>
    /// 生成模式
    /// </summary>
    public enum GenerationMode
    {
        GeneratorCode = 0,  //使用代码生成
        Template,           //使用模板生成
        Insertion,          //插入自定义代码至已有文件中
    }

    #endregion

    #region Naming

    [Serializable]
    public class NamingRule
    {
        [PropertyOrder(2), ShowInInspector, LabelText("类型")]
        public NamingType type = NamingType.模块名加前后缀;

        [PropertyOrder(3), ShowInInspector, LabelText("指定全名"), ShowIf("@type == NamingType.指定命名")]
        public string assignName;

        [PropertyOrder(5), ShowInInspector, LabelText("前缀"), ShowIf("@type == NamingType.模块名加前后缀")]
        public string prefix;

        [PropertyOrder(5), ShowInInspector, LabelText("后缀"), ShowIf("@type == NamingType.模块名加前后缀")]
        public string suffix;

        [PropertyOrder(8), ShowInInspector, LabelText("预览"), DisplayAsString, ShowIf("@type != NamingType.View代码专用"), ReadOnly]
        public string preview
        {
            get
            {
                switch (type)
                {
                    case NamingType.View代码专用: return "无效";
                    case NamingType.指定命名: return assignName;
                    case NamingType.模块名加前后缀: return $"{prefix}模块名{suffix}";
                    default: return "错误";
                }
            }
        }
    }

    public enum NamingType
    {
        View代码专用 = 0,       //只有View规则才会选择
        指定命名,              //固定的名字
        模块名加前后缀,
    }

    #endregion

    #region Template


    [Serializable]
    public class ReplaceRuleData
    {
        public const string REPLACEKEY_ModuleName = "模块名";
        public const string REPLACEKEY_ClassName = "类名";
        public const string REPLACEKEY_Time = "时间";
        public const string REPLACEKEY_Author = "作者";

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
        [ShowInInspector, PropertyOrder(0), LabelText("位置标记："), LabelWidth(55), TextArea(1, 2)]
        public string key = "";

        [ShowInInspector, PropertyOrder(2), LabelText("内容模板（可使用指定替换标记）（执行前会先进行替换）"), LabelWidth(120), TextArea(2, 20)]
        public string value = "";

        [ShowInInspector, PropertyOrder(1), LabelText("查重正则（可使用指定替换标记）（执行前会先进行替换）"), LabelWidth(120), TextArea(1, 3)]
        public string checkExistsMatchPattern = "";

    }

    #endregion
}