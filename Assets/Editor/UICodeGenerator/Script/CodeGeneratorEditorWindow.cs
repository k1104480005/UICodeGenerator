using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

namespace UICodeGeneration
{
    /// <summary>
    /// UI代码生成界面(Odin)
    /// </summary>
    public class CodeGeneratorEditorWindow : OdinEditorWindow
    {
        static CodeGeneratorEditorWindow _Instance;
        public static CodeGeneratorEditorWindow Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = OpenWindow();
                return _Instance;
            }
        }

        [MenuItem("GameTools/UICodeGenerator #&u")]
        private static CodeGeneratorEditorWindow OpenWindow()
        {
            CodeGeneratorEditorWindow win = GetWindow<CodeGeneratorEditorWindow>();
            win.minSize = new Vector2(800, 700);
            //win.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 700);
            win.titleContent = new GUIContent("UICodeGenerator");
            return win;
        }

        protected override void OnDestroy()
        {
            AssetDatabase.Refresh();
        }

        protected override void OnEnable()
        {
            Selection.selectionChanged += RefreshOptionalComponents;
        }

        protected override void OnDisable()
        {
            Selection.selectionChanged -= RefreshOptionalComponents;
        }

        bool _IsSubView;//是否子界面
        bool _IsDirtyForExportInfo;//组件数据是否被修改过

        bool NoError_Flag1 => SETTING != null && PATHCOLLECT != null; //无错误标记1
        bool NoError_Flag2 => NoError_Flag1 && DragPrefabItem != null; //无错误标记2

        #region 基础配置

        [PropertyOrder(0), FoldoutGroup("配置信息"), LabelText("基础配置"), OnInspectorInit("FindSETTING"), LabelWidth(80), Required("发生错误，基础配置为空！")]
        public GenSettingSO SETTING;

        void FindSETTING()
        {
            string[] globalAssetPaths = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(GenSettingSO)}");

            //判断是否有多个配置
            if (globalAssetPaths.Length > 1)
                foreach (var assetPath in globalAssetPaths)
                    GeneratorHelper.LogError($"注意！含有多个配置 {typeof(GenSettingSO)},需手动指定配置！ Repeated Path: {UnityEditor.AssetDatabase.GUIDToAssetPath(assetPath)}");
            else if (globalAssetPaths.Length == 0)
                GeneratorHelper.LogError($"错误！已丢失配置 {typeof(GenSettingSO)}！请创建新配置");
            else
            {
                //找到唯一的配置资源并加载
                string guid = globalAssetPaths[0];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                SETTING = AssetDatabase.LoadAssetAtPath<GenSettingSO>(assetPath);
                if (SETTING != null)
                    GeneratorHelper.Log($"配置{typeof(GenSettingSO)}加载成功");
            }
        }

        #endregion

        #region 代码路径收集

        [PropertyOrder(2), FoldoutGroup("配置信息", Expanded = true), LabelText("路径收集"), OnInspectorInit("FIND_PATHCOLLECT"), LabelWidth(80), Required("发生错误，代码路径收集配置文件不存在！"), ShowIf("SETTING")]
        public CodePathCollectSO PATHCOLLECT;

        void FIND_PATHCOLLECT()
        {
            string[] globalAssetPaths = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(CodePathCollectSO)}");

            //判断是否有多个配置
            if (globalAssetPaths.Length > 1)
                foreach (var assetPath in globalAssetPaths)
                    GeneratorHelper.LogError($"注意！含有多个配置 {typeof(CodePathCollectSO)},需手动指定配置！ Repeated Path: {UnityEditor.AssetDatabase.GUIDToAssetPath(assetPath)}");
            else if (globalAssetPaths.Length == 0)
                GeneratorHelper.LogError($"错误！已丢失配置 {typeof(CodePathCollectSO)}！请创建新配置");
            else
            {
                //找到唯一的配置资源并加载
                string guid = globalAssetPaths[0];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PATHCOLLECT = AssetDatabase.LoadAssetAtPath<CodePathCollectSO>(assetPath);
                if (PATHCOLLECT != null)
                    GeneratorHelper.Log($"配置{typeof(CodePathCollectSO)}加载成功");
            }

            Repaint();
        }


        /// <summary>
        /// 更新路径收集数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="path"></param>
        private void Update_PATHCOLLECT(string key, string path)
        {
            if (PATHCOLLECT == null)
            {
                GeneratorHelper.LogError("更新失败，路径收集配置 为空！");
                this.ShowNotification(new GUIContent("更新失败，请查看Console获得更多信息"));
                return;
            }

            var temp = PATHCOLLECT.View2ScriptPath.Find(e => e.key == key);
            if (temp != null)
            {
                if (temp.value == path)
                    return;
                else
                    temp.value = path;
            }
            else
                PATHCOLLECT.View2ScriptPath.Add(new CodePathCollectData() { key = key, value = path });

            Repaint();
        }

        #endregion


        #region 说明书

        [PropertyOrder(3), PropertySpace(0, 4), FoldoutGroup("说明书"), OnInspectorGUI, ShowIf("NoError_Flag1")]
        void OnIntroduce()
        {
            SirenixEditorGUI.IconMessageBox("命名规则： ① 命名以“View”结尾的预设将被视为基础界面.  ② 命名不以“View”结尾的预设将被视为子界面.", SdfIconType.ChatDotsFill);
        }

        #endregion

        #region 工具区

        [PropertyOrder(10), FoldoutGroup("功能区"), ResponsiveButtonGroup("功能区/bg"), Button(SdfIconType.Tools, "修复代码路径数据", ButtonHeight = 24), ShowIf("NoError_Flag1")]
        public void FIX_PATHCOLLECT()
        {
            bool succ = EditorUtility.DisplayDialog("提示", "当UI预设已生成过界面代码但其组件数据丢失时可以在此修复，这将会重新收集代码路径数据，是否修复？", "修复", "取消");
            if (!succ)
            {
                GUIUtility.ExitGUI();
                return;
            }


            if (PATHCOLLECT == null)
            {
                GeneratorHelper.LogError("修复失败，CodePathCollectSO 为空！");
                this.ShowNotification(new GUIContent("修复失败，请查看Console获得更多信息"));
                return;
            }

            if (!Directory.Exists(SETTING.CodeGenSetting.OutputCodeRootFolder))
            {
                GeneratorHelper.LogError($"修复失败，基础配置中填写的[存放生成代码的根文件夹]不存在！:{SETTING.CodeGenSetting.OutputCodeRootFolder}");
                this.ShowNotification(new GUIContent("修复失败，请查看Console获得更多信息"));
                return;
            }

            PATHCOLLECT.View2ScriptPath.Clear();
            var rule = SETTING.CodeGenSetting.View_OutputRule;

            //检查生成器
            if (string.IsNullOrEmpty(rule.Gen_Generator))
            {
                GeneratorHelper.LogError($"发现输出规则未指定生成器 :{rule.RuleName}");
                this.ShowNotification(new GUIContent("修复失败，请查看Console获得更多信息"));
                return;
            }

            //检查生成器
            Type t = typeof(IGenerateCode).Assembly.GetType(rule.Gen_Generator);
            if (t == null)
            {
                GeneratorHelper.LogError($"未找到该生成器 :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("修复失败，请查看Console获得更多信息"));
                return;
            }

            //检查生成器
            var inst = Activator.CreateInstance(t);
            if (!(inst is IGenerateCode))
            {
                GeneratorHelper.LogError($"发生错误，inst is not IGenerateCode :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("修复失败，请查看Console获得更多信息"));
                return;
            }

            IGenerateCode i = (IGenerateCode)inst;

            //找到所有生成的代码文件
            string[] fileFullnameArr = Directory.GetFiles(SETTING.CodeGenSetting.OutputCodeRootFolder, $"*.cs", SearchOption.AllDirectories);

            foreach (string fullname in fileFullnameArr)
            {
                string textContent = File.ReadAllText(fullname);

                bool isViewCode = i.FilterViewByCodeContent(textContent, out string className);
                if (isViewCode)
                {
                    var temp = PATHCOLLECT.View2ScriptPath.Find(e => e.key == className);
                    if (temp != null)
                    {
                        if (temp.value == fullname)
                            continue;
                        else
                            temp.value = fullname;
                    }
                    else
                    {
                        PATHCOLLECT.View2ScriptPath.Add(new CodePathCollectData() { key = className, value = fullname });
                    }
                }
            }


            GeneratorHelper.Log($"修复工作已全部完成");
            this.ShowNotification(new GUIContent("修复工作已全部完成"));
            CleanUp();
        }

        [PropertyOrder(11), FoldoutGroup("功能区"), ResponsiveButtonGroup("功能区/bg"), Button(SdfIconType.EraserFill, "重置", ButtonHeight = 24), ShowIf("NoError_Flag1")]
        public void REFRESH_ALL()
        {
            bool succ = EditorUtility.DisplayDialog("提示", "重置当前所有，这将丢失所有未保存的修改，是否继续？", "确定", "取消");
            if (!succ)
            {
                GUIUtility.ExitGUI();
                return;
            }

            CleanUp();
            this.ShowNotification(new GUIContent("重置完成"));
        }

        #endregion

        #region 标头

        string _DragPrefabItemTitle => DragPrefabItem != null ? $"正在操作{DragPrefabItem.name}" : "未选择界面预设";
        string _DragPrefabItemTitle2 => _IsSubView ? "（子界面）" : "（基础界面）";

        [PropertyOrder(20), TitleGroup("tg", "$_DragPrefabItemTitle2", GroupName = "$_DragPrefabItemTitle", Alignment = TitleAlignments.Centered), BoxGroup("tg/hg/bg", false), HorizontalGroup("tg/hg", Width = 350), ShowIf("NoError_Flag1"), SceneObjectsOnly, HideLabel, Required("请拖入Hierarchy面板中的UI预设实例", InfoMessageType.Warning), OnValueChanged("OnDragPrefabItemChanged"), InlineButton("OnCheckUIPrefabNaming", "检查UI预设 ", Icon = SdfIconType.BugFill, IconAlignment = IconAlignment.LeftOfText, ShowIf = "DragPrefabItem"), SuffixLabel("Prefab           ", Overlay = true)]
        public GameObject DragPrefabItem;

        [PropertyOrder(22), PropertySpace(0, 2), TitleGroup("tg"), BoxGroup("tg/bg2", false), ShowIf("NoError_Flag2"), LabelText("文件位置："), LabelWidth(60), ReadOnly, DisplayAsString]
        public string ViewCodeFileFullName = "";//即将生成代码文件的FullName

        void OnDragPrefabItemChanged()
        {
            if (DragPrefabItem == null)
            {
                CleanUp();
                return;
            }

            //检查拖入的预设实例是否正确
            if (!PrefabInstanceCheck(DragPrefabItem))
            {
                DragPrefabItem = null;

                this.ShowNotification(new GUIContent("这不是一个预设实例！请查看Console获得更多信息"));
                GeneratorHelper.LogError("您拖入的并不是预设实例（Prefab Instance），请拖入Hierarchy面板中的UI预设实例。Hierarchy面板中没有UI预设实例？请从Project面板中找到需要操作的UI预设资源并拖到Hierarchy面板中（Scene中）");
                CleanUp();
                return;
            }

            //获得并纠正预设实例的根（因为容错允许拖入预设实例的子物体）
            DragPrefabItem = PrefabUtility.GetOutermostPrefabInstanceRoot(DragPrefabItem);

            //是否子界面（根据命名是否不以View结尾来确定）
            _IsSubView = IsSubView(DragPrefabItem.name);

            //加载与解析UI对应的组件数据
            _ExportInfoList = LoadUIDataByScript();
            if (_ExportInfoList == null)
            {
                CleanUp();
                return;
            }

            //初始化模块名
            _moduleName = GetModuleName();
            if (_moduleName == null)
            {
                CleanUp();
                return;
            }

            //刷新可选组件区
            RefreshOptionalComponents();

            //刷新其他输出规则显示
            RefreshOtherOutputRuleWrapper();
        }

        void OnCheckUIPrefabNaming()
        {
            if (DragPrefabItem == null)
                return;
            StringBuilder sb = new StringBuilder();
            Transform[] arr = DragPrefabItem.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in arr)
            {
                string name = child.gameObject.name;
                if (name.Contains(" ") || name.Contains("(") || name.Contains(")"))
                    sb.Append($" 命名错误{name} ->{GeneratorHelper.GetHierarchyWithRoot(child, DragPrefabItem.transform)}");
            }

            string err = sb.ToString();
            if (string.IsNullOrEmpty(err))
                this.ShowNotification(new GUIContent("检查通过"));
            else
            {
                this.ShowNotification(new GUIContent("检查发现错误，请查看Console获得更多信息"));
                GeneratorHelper.LogError($"检查UI预设时发现错误：{err}");
            }
        }

        #endregion

        #region 选择组件区

        List<ValidateNode> _selectionValidateCodeList = new List<ValidateNode>();


        [PropertyOrder(30), TitleGroup("tg"), HorizontalGroup("tg/hg3", Width = 0.3f, PaddingRight = 5), VerticalGroup("tg/hg3/vg"), ShowIf("NoError_Flag2"), LabelText("选择组件"), ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
        public List<ComponentWrapper> OptionalComponent = new List<ComponentWrapper>();

        [PropertyOrder(31), TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg"), OnInspectorGUI, ShowIf("@(_selectionValidateCodeList.Count == 0 || _selectionValidateCodeList.Contains(ValidateNode.ERROR_NODE)) && NoError_Flag2")]
        void OptionalCompontNoneTip()
        {
            if (_selectionValidateCodeList.Count == 0)
                SirenixEditorGUI.IconMessageBox("未选择节点", SdfIconType.EmojiNeutralFill);
            else if (_selectionValidateCodeList.Contains(ValidateNode.ERROR_NODE))
            {
                SirenixEditorGUI.IconMessageBox($"{(_selectionValidateCodeList.Count == 1 ? "选中的节点不属于UI预设" : "部分节点不属于UI预设")}", SdfIconType.EmojiDizzyFill, Color.yellow);
            }
        }

        /// <summary>
        /// 刷新可选组件
        /// </summary>
        void RefreshOptionalComponents()
        {
            List<Transform> selectedList = Selection.transforms.ToList();
            if (selectedList.Count == 0)
            {
                _selectionValidateCodeList.Clear();
                OptionalComponent.Clear();
                return;
            }

            if (selectedList.Count > 1)
            {
                _selectionValidateCodeList.Clear();
                OptionalComponent.Clear();
                return;
            }

            _selectionValidateCodeList.Clear();

            //统计选择节点的状态
            Dictionary<Transform, ValidateNode> dic = new Dictionary<Transform, ValidateNode>();
            foreach (var i in selectedList)
            {
                ValidateNode nodeState = ValidateSelection(i);
                _selectionValidateCodeList.Add(nodeState);
                if (nodeState == ValidateNode.ERROR_NODE)
                {
                    OptionalComponent.Clear();
                    return;
                }

                if (nodeState == ValidateNode.NONE_NODE)
                    continue;

                dic.Add(i, nodeState);
            }

            //统计每个节点身上的组件
            List<List<Component>> tlist = new List<List<Component>>();
            List<Component> totalCom = new List<Component>();
            foreach (var i in dic.Keys)
            {
                //此处并不会获取到GameObject，因为GameObjecet不是Component
                List<Component> l = i.GetComponents<Component>().ToList();
                tlist.Add(l);
                totalCom.AddRange(l);
            }

            //取列表交集(暂无法实现)
            // List<Component> sameList = IntersectAllComponent(tlist);

            //组装数据
            OptionalComponent.Clear();
            foreach (var com in totalCom)
            {
                //包装
                UIComponentInfo newInfo = new UIComponentInfo(com, DragPrefabItem.transform);
                OptionalComponent.Add(new ComponentWrapper(com, newInfo, OnSelectOPC));
            }

            //刷新状态
            RefreshOptionalComponentsSelectState();
        }


        /// <summary>
        /// 刷新组件Wrapper的选中状态
        /// </summary>
        void RefreshOptionalComponentsSelectState()
        {
            foreach (var i in OptionalComponent)
                i.isSelected = HasComponentInfo(i.uiData.uid);
        }

        /// <summary>
        /// 选择添加组件
        /// </summary>
        /// <param name="wrapper"></param>
        void OnSelectOPC(ComponentWrapper wrapper)
        {
            string uid = wrapper.uiData.uid;
            if (HasComponentInfo(uid))
                RemoveComponentInfo(uid);            //删除
            else
                AddComponentInfo(wrapper.uiData);    //加入

            RefreshOptionalComponentsSelectState();
        }

        #endregion

        #region 已选组件区

        [PropertyOrder(40), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), FoldoutGroup("tg/hg3/vg2/当前组件数据", Expanded = true), ShowIf("NoError_Flag2"), LabelText(" "), ListDrawerSettings(ShowFoldout = false, HideAddButton = true, DraggableItems = false, OnTitleBarGUI = "OnTitleBarGUI_ExportInfoList"), HideReferenceObjectPicker, OnCollectionChanged("On_ExportInfoListChangedAfter")]
        List<UIComponentInfo> _ExportInfoList = new List<UIComponentInfo>();


        /// <summary>
        /// 列表发生变化时 After
        /// </summary>
        void On_ExportInfoListChangedAfter(CollectionChangeInfo info, object value)
        {
            //有数据被删除
            if (info.ChangeType == CollectionChangeType.RemoveIndex)
            {
                RefreshOptionalComponentsSelectState();//刷新可选区的状态
            }

            _IsDirtyForExportInfo = true;
        }

        /// <summary>
        /// 列表工具
        /// </summary>
        void OnTitleBarGUI_ExportInfoList()
        {
            //强行让组件数据变脏
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.Robot))
            {
                _IsDirtyForExportInfo = true;
            }

            if (SirenixEditorGUI.ToolbarButton(SdfIconType.BugFill))
            {
                StringBuilder sb = new StringBuilder();
                //检查已选组件数据是否有错误
                foreach (var item in _ExportInfoList)
                {
                    //查找节点是否还存在于UI预设中
                    Transform node = DragPrefabItem.transform.Find(item.path);
                    bool foundNode = node != null;
                    bool foundComponent = true;
                    if (!foundNode)
                        sb.Append($"  组件数据错误，[节点]不存在！ {item.memberName} -> {item.path}");
                    else
                    {
                        //查找该节点的组件是否存在
                        Component com = node.GetComponent(item.comType);
                        if (com == null)
                        {
                            sb.Append($"  组件数据错误，[组件]不存在！ {item.memberName} | {item.comType}");
                            foundComponent = false;
                        }
                    }

                    //重新赋值
                    item.validated = foundComponent && foundNode;
                }
                string error = sb.ToString();
                if (string.IsNullOrEmpty(error))
                {
                    this.ShowNotification(new GUIContent("数据检查通过！"));
                }
                else
                {
                    this.ShowNotification(new GUIContent("发现数据错误！请查看Console获得更多信息"));
                    GeneratorHelper.LogError($"发现组件数据错误：{error}");
                }
            }
        }

        /// <summary>
        /// 定位至预设并选择
        /// </summary>
        /// <param name="target"></param>
        public static void LocationToSelection(UIComponentInfo target)
        {
            if (Instance != null && Instance.DragPrefabItem != null)
                UnityEditor.Selection.activeTransform = Instance.DragPrefabItem.transform.Find(target.path);
        }

        /// <summary>
        /// 加入组件数据
        /// </summary>
        /// <param name="info"></param>
        void AddComponentInfo(UIComponentInfo info)
        {
            UIComponentInfo temp = _ExportInfoList.Find(e => e.uid == info.uid);
            if (temp == null)
            {
                _ExportInfoList.Add(info);
                _IsDirtyForExportInfo = true;
            }
        }

        /// <summary>
        /// 删除组件数据
        /// </summary>
        /// <param name="uid"></param>
        void RemoveComponentInfo(string uid)
        {
            UIComponentInfo del = _ExportInfoList.Find(e => e.uid == uid);
            if (del != null)
            {
                _ExportInfoList.Remove(del);
                _IsDirtyForExportInfo = true;
            }
        }

        /// <summary>
        /// 查找组件数据是否存在
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        bool HasComponentInfo(string uid)
        {
            return _ExportInfoList.Find(e => e.uid == uid) != null;
        }

        #endregion

        #region 生成区-View

        [PropertyOrder(54), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH"), ShowIf("NoError_Flag2"), Button(SdfIconType.Gear, "", ButtonHeight = 28, Stretch = false, ButtonAlignment = 0.62f)]
        public void SeeViewOutputRule()
        {
            //查看与修改输出配置
            OdinEditorWindow.InspectObjectInDropDown(SETTING.CodeGenSetting.View_OutputRule);
        }

        [PropertyOrder(55), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH", Width = 80), ShowIf("NoError_Flag2"), Button(SdfIconType.FileEarmarkCodeFill, "Generation View Code", ButtonHeight = 28, Stretch = false, ButtonAlignment = 1f), GUIColor("#338ACB"), EnableIf("_IsDirtyForExportInfo")]
        public void GenerationView()
        {
            //无改动则无需生成
            if (!_IsDirtyForExportInfo)
                return;

            //尝试生成
            List<UIComponentInfo> comList = new List<UIComponentInfo>(_ExportInfoList);
            bool succ = ProduceCSharpCode_View(SETTING.CodeGenSetting.View_OutputRule, comList);
            if (succ)
            {
                _IsDirtyForExportInfo = false;
            }
        }


        [PropertyOrder(50), TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH", Width = 200), ShowIf("NoError_Flag2"), LabelText("模块名："), LabelWidth(45), ReadOnly, HideIf("_IsSubView")]
        public string _moduleName = "";


        [PropertyOrder(50), TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH", Width = 200), ShowIf("NoError_Flag2"), LabelText("无模块代码->快速覆盖原文件"), LabelWidth(164), ShowIf("@_IsSubView && NoError_Flag2")]
        public bool _NoModuleCodeFastOverrWrite = false;


        #endregion

        #region 生成区-Other

        [PropertyOrder(60), PropertySpace(4, 0), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), ListDrawerSettings(ShowFoldout = false, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "OnTitleBarGUI_OtherGen"), ShowIf("NoError_Flag2"), LabelText(" "), FoldoutGroup("生成其他代码", GroupID = "tg/hg3/vg2/FG3"), HideReferenceObjectPicker]
        public List<OutputRuleWrapper> otherOpRuleWrappers = new List<OutputRuleWrapper>();

        /// <summary>
        /// 刷新所有其他输出规则包装
        /// </summary>
        void RefreshOtherOutputRuleWrapper()
        {
            otherOpRuleWrappers.Clear();
            foreach (var i in SETTING.CodeGenSetting.OtherOutputRules)
            {
                if (i.ApplyToWho == ApplyTo.All)
                    otherOpRuleWrappers.Add(new OutputRuleWrapper(i));
                else if (i.ApplyToWho == ApplyTo.基础界面 && !_IsSubView)
                    otherOpRuleWrappers.Add(new OutputRuleWrapper(i));
                else if (i.ApplyToWho == ApplyTo.子界面 && _IsSubView)
                    otherOpRuleWrappers.Add(new OutputRuleWrapper(i));

            }
        }


        /// <summary>
        /// 生成Other代码
        /// </summary>
        /// <param name="wrapper"></param>
        public static void GenAOtherCode(OutputRuleWrapper wrapper)
        {
            if (CodeGeneratorEditorWindow.Instance != null)
                CodeGeneratorEditorWindow.Instance.GenAOtherCodeInternal(wrapper);
        }

        public void GenAOtherCodeInternal(OutputRuleWrapper wrapper)
        {
            if (!wrapper.Enable)
                return;

            //重新获取一下模块名，避免用户中途手动改变了预设名
            _moduleName = GetModuleName();
            //重新判断一下是否子界面，避免用户中途手动改变了预设名
            _IsSubView = IsSubView(DragPrefabItem.name);
            //非View代码 -> 子界面将使用预设名作为类名,而基础界面则是模块名作为类名
            string className = _IsSubView ? DragPrefabItem.name : $"{_moduleName}";

            List<UIComponentInfo> comList = new List<UIComponentInfo>(_ExportInfoList);
            ProduceCSharpCode(wrapper.info, comList, _moduleName, className, SETTING.CodeGenSetting.OutputCodeRootFolder);
        }

        /// <summary>
        /// 绘制列表头
        /// </summary>
        void OnTitleBarGUI_OtherGen()
        {
            if (SirenixEditorGUI.SDFIconButton("快捷生成", 16, SdfIconType.None, style: SirenixGUIStyles.ToolbarButton))
            {
                //生成所有选中的其他代码
            }

            if (SirenixEditorGUI.SDFIconButton("刷新", 16, SdfIconType.None, style: SirenixGUIStyles.ToolbarButton))
            {
                RefreshOtherOutputRuleWrapper();
            }
        }

        #endregion

        #region 通用方法

        /// <summary>
        /// 判断是否是预设实例
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool PrefabInstanceCheck(UnityEngine.Object target)
        {
            if (target == null)
                return false;
            PrefabInstanceStatus state = PrefabUtility.GetPrefabInstanceStatus(target);
            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(target);
            if (type == PrefabAssetType.Regular && state == PrefabInstanceStatus.Connected)
                return true;
            return false;
        }

        /// <summary>
        /// 判断是否是子界面
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        bool IsSubView(string prefabName)
        {
            return prefabName.IndexOf("View") == -1;
        }

        /// <summary>
        /// 加载UI预设对应的组件数据
        /// </summary>
        List<UIComponentInfo> LoadUIDataByScript()
        {
            if (!NoError_Flag2)
            {
                this.ShowNotification(new GUIContent("发生错误！请查看Console获得更多信息"));
                GeneratorHelper.LogError("LoadUIViewByScript 未通过检查的标记：NoError_Flag2");
                return null;
            }

            var temp = PATHCOLLECT.View2ScriptPath.Find(e => e.key == DragPrefabItem.name);
            if (temp == null)
            {
                GeneratorHelper.Log("没找到UI组件对应的组件数据，如果是新UI则忽略该条提醒");
                return new List<UIComponentInfo>();
            }

            ViewCodeFileFullName = temp.value;
            if (!File.Exists(ViewCodeFileFullName))
            {
                GeneratorHelper.LogError($"无法找到预设对应代码，原位置：{ViewCodeFileFullName} 是否已移动代码位置？建议使用【修复代码路径数据】功能恢复，或者可以选择重新创建！");
                if (EditorUtility.DisplayDialog("发现问题", $"无法找到预设对应代码,是否已移动代码位置？请查看Console获得更多信息！ 原位置：{ViewCodeFileFullName}", "我去找找", "重新创建"))
                    return null;
                else
                    return new List<UIComponentInfo>();
            }

            string viewCodeText = File.ReadAllText(ViewCodeFileFullName);
            if (string.IsNullOrEmpty(viewCodeText))
            {
                this.ShowNotification(new GUIContent("发生错误！请查看Console获得更多信息"));
                GeneratorHelper.LogError($"读取UI的View代码时内容为空，路径：{ViewCodeFileFullName}");
                return null;
            }

            //寻找可用规则
            OutputRule rule = SETTING.CodeGenSetting.View_OutputRule;
            if (rule == null)
            {
                this.ShowNotification(new GUIContent("发生错误！请查看Console获得更多信息"));
                GeneratorHelper.LogError($"在[基础配置]中没有找到类型为{(_IsSubView ? "OPCodeType.子界面" : "OPCodeType.基础界面")}的规则，请检查：{SETTING.name}");
                return null;
            }

            //检查生成器
            if (string.IsNullOrEmpty(rule.Gen_Generator))
            {
                GeneratorHelper.LogError($"发现输出规则未指定生成器 :{rule.RuleName}");
                this.ShowNotification(new GUIContent("发生错误，请查看Console获得更多信息"));
                return null;
            }

            //检查生成器
            Type t = typeof(IGenerateCode).Assembly.GetType(rule.Gen_Generator);
            if (t == null)
            {
                GeneratorHelper.LogError($"未找到该生成器 :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("发生错误，请查看Console获得更多信息"));
                return null;
            }

            //检查生成器
            var inst = Activator.CreateInstance(t);
            if (!(inst is IGenerateCode))
            {
                GeneratorHelper.LogError($"发生错误，inst is not IGenerateCode :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("发生错误，请查看Console获得更多信息"));
                return null;
            }

            //解析
            IGenerateCode i = (IGenerateCode)inst;
            Dictionary<string, UIComponentInfo> dic = i.ParseViewCode(DragPrefabItem, viewCodeText, out string error);

            //检验解析结果
            if (!string.IsNullOrEmpty(error))
            {
                GeneratorHelper.LogError($"解析发生错误：{error}");
                this.ShowNotification(new GUIContent("解析发生错误，请查看Console获得更多信息"));
            }
            if (dic == null)
            {
                GeneratorHelper.LogError($"解析失败，已终止 :{rule.Gen_Generator}");
                return null;
            }

            GeneratorHelper.Log("加载UI组件数据成功：" + ViewCodeFileFullName);
            return dic.Values.ToList();
        }

        /// <summary>
        /// 初始化模块名
        /// </summary>
        string GetModuleName()
        {
            if (_IsSubView)
                return "";

            OutputRule rule = SETTING.CodeGenSetting.View_OutputRule;
            if (rule == null)
            {
                this.ShowNotification(new GUIContent("发生错误！请查看Console获得更多信息"));
                GeneratorHelper.LogError($"在[基础配置]中没有找到类型为{(_IsSubView ? "OPCodeType.子界面" : "OPCodeType.基础界面")}的规则，请检查：{SETTING.name}");
                return null;
            }

            if (DragPrefabItem == null || string.IsNullOrEmpty(DragPrefabItem.name))
            {
                this.ShowNotification(new GUIContent("初始化模块名失败！请查看Console获得更多信息"));
                GeneratorHelper.LogError($"初始化模块名失败，DragPrefabItem：{DragPrefabItem}");
                return null;
            }

            string moduleName = DragPrefabItem.name;
            /*命名以View结尾的预设,删除尾部View后作为模块名*/
            int index = moduleName.LastIndexOf("View");
            if (index != -1)
                moduleName = moduleName.Remove(index, 4);


            //  GeneratorHelper.Log($"初始化模块名：{moduleName}");
            return moduleName;
        }


        /// <summary>
        /// 是否是当前Prefab的节点 
        /// (-1不是 0是当前的根节点 1是当前的子节点)
        /// </summary>
        /// <param name="Selection"></param>
        /// <returns> ValidateNode </returns>
        ValidateNode ValidateSelection(Transform Selection)
        {
            if (DragPrefabItem == null || Selection == null)
                return ValidateNode.NONE_NODE;

            if (Selection == DragPrefabItem.transform)
                return ValidateNode.ROOT_NODE;

            Transform tTrans = Selection;
            while (tTrans != null)
            {
                if (tTrans == DragPrefabItem.transform)
                    return ValidateNode.CHILD_NODE;
                tTrans = tTrans.parent;
            }

            return ValidateNode.ERROR_NODE;
        }

        enum ValidateNode
        {
            NONE_NODE = -1,
            ERROR_NODE,
            ROOT_NODE,
            CHILD_NODE
        }

        #endregion

        #region 生成方法

        /// <summary>
        /// 生成View代码
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="componentList"></param>
        /// <returns></returns>
        public bool ProduceCSharpCode_View(OutputRule rule, List<UIComponentInfo> componentList)
        {
            //重新获取一下模块名，避免用户中途手动改变了预设名
            _moduleName = GetModuleName();
            //重新判断一下是否子界面，避免用户中途手动改变了预设名
            _IsSubView = IsSubView(DragPrefabItem.name);
            //子界面将使用预设名作为类名,而基础界面则是模块名后加View作为类名
            string className = _IsSubView ? DragPrefabItem.name : $"{_moduleName}View";

            bool succ = ProduceCSharpCode(rule, componentList, _moduleName, className, SETTING.CodeGenSetting.OutputCodeRootFolder);

            //更新代码路径收集
            Update_PATHCOLLECT(DragPrefabItem.name, ViewCodeFileFullName);
            return succ;
        }

        /// <summary>
        /// 通用生成
        /// </summary>
        /// <param name="rule"> 生成规则配置 </param>
        /// <param name="componentList"> View组件数据 </param>
        /// <param name="moduleName"> 模块名（可能为空）（为空时表示子界面） </param>
        /// <param name="className"> 指定类名 </param>
        /// <param name="RootFolder"> 存放代码的根目录 </param>
        /// <returns></returns>
        public bool ProduceCSharpCode(OutputRule rule, List<UIComponentInfo> componentList, string moduleName, string className, string RootFolder)
        {
            if (rule == null || componentList == null || string.IsNullOrEmpty(className))
            {
                GeneratorHelper.LogError("ProduceCSharpCode error");
                return false;
            }

            if (!Directory.Exists(RootFolder))
            {
                GeneratorHelper.LogError("ProduceCSharpCode error: 存放代码的根目录不存在，请检查配置");
                return false;
            }

            if (rule.AssignRelativeFolderPath == null)
            {
                GeneratorHelper.LogError("ProduceCSharpCode error: 没找到文件夹规则配置，请检查配置");
                return false;
            }

            //插入模式独立处理
            if (rule.GenMode == GenerationMode.Insertion)
                return InsertionCSharpCode(rule, moduleName, className);

            //命名与路径
            //重新组合类名
            switch (rule.namingRule.type)
            {
                case NamingType.指定命名: className = string.Format($"{rule.namingRule.assignName}"); break;
                case NamingType.模块名加前后缀: className = string.Format($"{rule.namingRule.prefix}{className}{rule.namingRule.suffix}"); break;
                default:
                    className = string.Format($"{className}"); break;
            }
            string codeFileName = $"{className}.cs";


            string moduleFolderName = string.IsNullOrEmpty(moduleName) ? string.Empty : string.Format($"{moduleName}Module");
            string saveFolder = string.IsNullOrEmpty(moduleFolderName) ? RootFolder : Path.Combine(RootFolder, moduleFolderName).Replace('\\', '/');
            string previewFileFullName = "";//文件保存FullName
            string codeContentText;//最终代码文本

            //如果模块文件夹不存在则强行转为手动选择保存位置
            FolderRuleMode fMode = rule.FolderRuleTp;
            if (string.IsNullOrEmpty(moduleFolderName))
                fMode = FolderRuleMode.Flexible;
            if (fMode == FolderRuleMode.Assign)
            {
                //根据规则合并保存路径
                if (!string.IsNullOrEmpty(rule.AssignRelativeFolderPath))
                    saveFolder = Path.Combine(saveFolder, rule.AssignRelativeFolderPath).Replace('\\', '/');
                previewFileFullName = Path.Combine(saveFolder, codeFileName).Replace('\\', '/');
            }
            else
            {
                //强行手动标记
                bool forceFlexible = false;

                //无模块代码快速覆写时，重新寻找文件位置
                if (_NoModuleCodeFastOverrWrite)
                {
                    List<FileInfo> files = GeneratorHelper.FindCodeFile(RootFolder, codeFileName);
                    if (files.Count <= 0)
                    {
                        GeneratorHelper.Log($"无模块代码快速覆写时,无找到源文件,强行切换为手动保存 {codeFileName}");
                        forceFlexible = true;
                    }
                    else if (files.Count > 1)
                    {
                        GeneratorHelper.LogError($"无模块代码快速覆写时,发现多个同名文件,强行切换为手动保存 {codeFileName}");
                        forceFlexible = true;
                    }
                    else
                    {
                        GeneratorHelper.Log($"无模块代码快速覆写时找到文件路径 {files[0].FullName}");
                        previewFileFullName = files[0].FullName;
                    }
                }

                if (forceFlexible || !_NoModuleCodeFastOverrWrite)
                {
                    //手动选择保存路径
                    previewFileFullName = EditorUtility.SaveFilePanel("选择保存位置", RootFolder, className, "cs");
                    if (string.IsNullOrEmpty(previewFileFullName))
                    {
                        GeneratorHelper.Log($"ProduceCSharpCode 已主动取消生成");
                        this.ShowNotification(new GUIContent("终止操作"));
                        return false;
                    }
                }
            }

            //生成模式-生成器代码
            if (rule.GenMode == GenerationMode.GeneratorCode)
            {
                //检查生成器
                if (string.IsNullOrEmpty(rule.Gen_Generator))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode 发现输出规则未指定生成器 :{rule.RuleName}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                //检查生成器
                Type t = typeof(IGenerateCode).Assembly.GetType(rule.Gen_Generator);
                if (t == null)
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode 未找到该生成器 :{rule.Gen_Generator}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                //检查生成器
                var inst = Activator.CreateInstance(t);
                if (!(inst is IGenerateCode))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode 发生错误，生成器 is not IGenerateCode :{rule.Gen_Generator}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                IGenerateCode i = (IGenerateCode)inst;
                MethodInfo mi = t.GetMethod(rule.Gen_GeneratorMethod);
                object returnObject;
                if (mi.HasParamaters(new List<Type>() { typeof(ViewCodeInfo) }))
                    returnObject = mi.Invoke(inst, new object[] { new ViewCodeInfo(className, componentList) });
                else
                    returnObject = mi.Invoke(inst, null);

                codeContentText = (string)returnObject;
                if (string.IsNullOrEmpty(codeContentText))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode 生成器返回的代码文本为空 :{rule.Gen_Generator}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

            }
            //生成模式-模板文件
            else if (rule.GenMode == GenerationMode.Template)
            {
                if (string.IsNullOrEmpty(rule.Gen_Template) || !File.Exists(rule.Gen_Template))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode 模板文件不存在 :{rule.Gen_Template}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                string templateText = File.ReadAllText(rule.Gen_Template);
                if (string.IsNullOrEmpty(templateText))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode 模板文件无内容 :{rule.Gen_Template}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                //替换字符串
                foreach (var i in rule.Gen_Template_Replace_Before)
                {
                    if (string.IsNullOrEmpty(i.value))
                        continue;

                    switch (i.key)
                    {
                        case ReplaceRuleData.REPLACEKEY_ModuleName: templateText = templateText.Replace(i.value, string.IsNullOrEmpty(_moduleName) ? DragPrefabItem.name : _moduleName); break;//无模块名时使用预设名
                        case ReplaceRuleData.REPLACEKEY_ClassName: templateText = templateText.Replace(i.value, className); break;
                        case ReplaceRuleData.REPLACEKEY_Time: templateText = templateText.Replace(i.value, DateTime.Now.ToString()); break;
                        case ReplaceRuleData.REPLACEKEY_Author: templateText = templateText.Replace(i.value, "无作者"); break;
                    }
                }

                //子界面额外替换
                if (string.IsNullOrEmpty(moduleName))
                {
                    foreach (var i in rule.Gen_Template_Replace_After_OnlySubView)
                    {
                        if (string.IsNullOrEmpty(i.key) || string.IsNullOrEmpty(i.value))
                            continue;
                        templateText = templateText.Replace(i.key, i.value);
                    }
                }
                codeContentText = templateText;
            }
            //生成模式-插入代码
            else if (rule.GenMode == GenerationMode.Insertion)
            {
                return false;  //插入模式已单独处理！不可能跑到这里了
            }
            else
                return false;

            //判断是否能覆盖
            bool isExist = File.Exists(previewFileFullName);
            if (!rule.AllowOverwrite && isExist)
            {
                GeneratorHelper.LogError($"文件已存在，规则中已设置为不允许覆盖");
                this.ShowNotification(new GUIContent("文件已存在，规则中已设置为不允许覆盖"));
                return false;
            }

            if (fMode == FolderRuleMode.Flexible)
            {
                //手选模式而且文件已存在时需要提醒覆盖
                if (isExist)
                {
                    if (!EditorUtility.DisplayDialog("注意", $"文件{codeFileName}已存在，是否覆盖保存？（谨慎操作）", "覆盖", "取消"))
                    {
                        GeneratorHelper.Log($"生成终止，{codeFileName}已存在并不允许覆盖");
                        return false;
                    }
                }
            }

            //写入
            CreateFileDirectory(previewFileFullName);
            File.WriteAllText(previewFileFullName, codeContentText);
            GeneratorHelper.Log($"文件生成成功：{previewFileFullName}\n文本内容预览：\n{codeContentText}");
            this.ShowNotification(new GUIContent(isExist ? "文件覆盖成功" : "文件创建成功"));

            ViewCodeFileFullName = previewFileFullName;

            return true;
        }


        /// <summary>
        /// 插入模式则独立处理
        /// </summary>
        /// <returns></returns>
        bool InsertionCSharpCode(OutputRule rule, string moduleName, string className)
        {
            if (rule.GenMode != GenerationMode.Insertion)
            {
                GeneratorHelper.LogError($"插入模式- 非插入模式却调用了InsertionCSharpCode :{rule.InsertionTargetCodeFile}");
                this.ShowNotification(new GUIContent("错误调用！请查看Console获得更多信息"));
                return false;
            }



            if (string.IsNullOrEmpty(rule.InsertionTargetCodeFile))
            {
                GeneratorHelper.LogError($"插入模式- 目标文件不存在 :Null");
                this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                return false;
            }

            string targetFullName = Path.Combine(Application.dataPath, rule.InsertionTargetCodeFile).Replace('\\', '/');
            if (string.IsNullOrEmpty(rule.InsertionTargetCodeFile) || !File.Exists(targetFullName))
            {
                GeneratorHelper.LogError($"插入模式- 目标文件不存在 :{targetFullName}");
                this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                return false;
            }

            string tOriText = File.ReadAllText(targetFullName);
            if (string.IsNullOrEmpty(tOriText))
            {
                GeneratorHelper.LogError($"插入模式-  目标文件无内容 :{targetFullName}");
                this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                return false;
            }

            //开始插入代码
            int instFinishCount = 0;
            foreach (var i in rule.InsertionList)
            {
                if (string.IsNullOrEmpty(i.key))
                {
                    GeneratorHelper.LogError($"插入模式- 插入代码时发现位置标记为空！");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                //找到插入的位置
                int insIndex = tOriText.LastIndexOf(i.key);
                if (insIndex < 0)
                {
                    GeneratorHelper.LogError($"插入模式- 在目标文件的文本内容中无法找到位置标记{i.key}，目标文件{targetFullName}");
                    this.ShowNotification(new GUIContent("失败，请查看Console获得更多信息"));
                    return false;
                }

                if (string.IsNullOrEmpty(i.value))
                {
                    GeneratorHelper.LogError($"插入模式- 发现内容模板内容为空！，已跳过");
                    continue;
                }


                //根据输入的正则来判断是否插入的内容已存在(如果需要为空则表示不需要查重)
                string pattern = i.checkExistsMatchPattern;
                if (!string.IsNullOrEmpty(pattern))
                {
                    //查重前为正则表达式替换标记
                    foreach (var replace in rule.Gen_Template_Replace_Before)
                    {
                        if (string.IsNullOrEmpty(replace.value))
                            continue;
                        switch (replace.key)
                        {
                            case ReplaceRuleData.REPLACEKEY_ModuleName: pattern = pattern.Replace(replace.value, string.IsNullOrEmpty(_moduleName) ? DragPrefabItem.name : _moduleName); break;//无模块名时使用预设名
                            case ReplaceRuleData.REPLACEKEY_ClassName: pattern = pattern.Replace(replace.value, className); break;
                            case ReplaceRuleData.REPLACEKEY_Time: pattern = pattern.Replace(replace.value, DateTime.Now.ToString()); break;
                            case ReplaceRuleData.REPLACEKEY_Author: pattern = pattern.Replace(replace.value, "无作者"); break;
                        }
                    }

                    Match mat = Regex.Match(tOriText, pattern, RegexOptions.Multiline);
                    if (mat.Success)
                    {
                        GeneratorHelper.LogError($"插入模式- 发现重复代码，已跳过！正则：{pattern}");
                        continue;
                    }
                }

                //插入内容前-为即将插入的内容替换标记
                string contentCopy = i.value;
                foreach (var replace in rule.Gen_Template_Replace_Before)
                {
                    if (string.IsNullOrEmpty(replace.value))
                        continue;

                    switch (replace.key)
                    {
                        case ReplaceRuleData.REPLACEKEY_ModuleName: contentCopy = contentCopy.Replace(replace.value, string.IsNullOrEmpty(_moduleName) ? DragPrefabItem.name : _moduleName); break;//无模块名时使用预设名
                        case ReplaceRuleData.REPLACEKEY_ClassName: contentCopy = contentCopy.Replace(replace.value, className); break;
                        case ReplaceRuleData.REPLACEKEY_Time: contentCopy = contentCopy.Replace(replace.value, DateTime.Now.ToString()); break;
                        case ReplaceRuleData.REPLACEKEY_Author: contentCopy = contentCopy.Replace(replace.value, "无作者"); break;
                    }
                }

                // 插入内容
                tOriText = tOriText.Insert(insIndex, contentCopy);
                instFinishCount++;
            }

            if (instFinishCount <= 0)
            {
                GeneratorHelper.Log($"插入模式- 无任何替换被完成，已终止处理， 目标文件{targetFullName}");
                return false;
            }

            //内容- 所有插入操作都完成了
            string codeContentText = tOriText;
            File.WriteAllText(targetFullName, codeContentText);

            GeneratorHelper.Log($"插入模式-内容插入成功：{targetFullName}\n文本内容预览：\n{codeContentText}");
            this.ShowNotification(new GUIContent("内容插入成功"));
            return true;

        }

        /// <summary>
        /// 创建文件夹或文件
        /// </summary>
        /// <param name="pFilePath"></param>
        /// <param name="pCreateFile"></param>
        /// <returns>是否创建成功</returns>
        private bool CreateFileDirectory(string pFilePath, bool pCreateFile = false)
        {
            if (!File.Exists(pFilePath))
            {
                string tDir = Path.GetDirectoryName(pFilePath);
                if (!Directory.Exists(tDir))
                    Directory.CreateDirectory(tDir);
                if (pCreateFile)
                    File.Create(pFilePath).Close();
                return true;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// 清理当前所有内容
        /// </summary>
        private void CleanUp()
        {
            DragPrefabItem = null;
            _IsDirtyForExportInfo = false;
            ViewCodeFileFullName = string.Empty;
            _moduleName = string.Empty;
            _ExportInfoList?.Clear();
            OptionalComponent?.Clear();
            _NoModuleCodeFastOverrWrite = false;
        }

    }
}
