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
    /// UI�������ɽ���(Odin)
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

        bool _IsSubView;//�Ƿ��ӽ���
        bool _IsDirtyForExportInfo;//��������Ƿ��޸Ĺ�

        bool NoError_Flag1 => SETTING != null && PATHCOLLECT != null; //�޴�����1
        bool NoError_Flag2 => NoError_Flag1 && DragPrefabItem != null; //�޴�����2

        #region ��������

        [PropertyOrder(0), FoldoutGroup("������Ϣ"), LabelText("��������"), OnInspectorInit("FindSETTING"), LabelWidth(80), Required("�������󣬻�������Ϊ�գ�")]
        public GenSettingSO SETTING;

        void FindSETTING()
        {
            string[] globalAssetPaths = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(GenSettingSO)}");

            //�ж��Ƿ��ж������
            if (globalAssetPaths.Length > 1)
                foreach (var assetPath in globalAssetPaths)
                    GeneratorHelper.LogError($"ע�⣡���ж������ {typeof(GenSettingSO)},���ֶ�ָ�����ã� Repeated Path: {UnityEditor.AssetDatabase.GUIDToAssetPath(assetPath)}");
            else if (globalAssetPaths.Length == 0)
                GeneratorHelper.LogError($"�����Ѷ�ʧ���� {typeof(GenSettingSO)}���봴��������");
            else
            {
                //�ҵ�Ψһ��������Դ������
                string guid = globalAssetPaths[0];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                SETTING = AssetDatabase.LoadAssetAtPath<GenSettingSO>(assetPath);
                if (SETTING != null)
                    GeneratorHelper.Log($"����{typeof(GenSettingSO)}���سɹ�");
            }
        }

        #endregion

        #region ����·���ռ�

        [PropertyOrder(2), FoldoutGroup("������Ϣ", Expanded = true), LabelText("·���ռ�"), OnInspectorInit("FIND_PATHCOLLECT"), LabelWidth(80), Required("�������󣬴���·���ռ������ļ������ڣ�"), ShowIf("SETTING")]
        public CodePathCollectSO PATHCOLLECT;

        void FIND_PATHCOLLECT()
        {
            string[] globalAssetPaths = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(CodePathCollectSO)}");

            //�ж��Ƿ��ж������
            if (globalAssetPaths.Length > 1)
                foreach (var assetPath in globalAssetPaths)
                    GeneratorHelper.LogError($"ע�⣡���ж������ {typeof(CodePathCollectSO)},���ֶ�ָ�����ã� Repeated Path: {UnityEditor.AssetDatabase.GUIDToAssetPath(assetPath)}");
            else if (globalAssetPaths.Length == 0)
                GeneratorHelper.LogError($"�����Ѷ�ʧ���� {typeof(CodePathCollectSO)}���봴��������");
            else
            {
                //�ҵ�Ψһ��������Դ������
                string guid = globalAssetPaths[0];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PATHCOLLECT = AssetDatabase.LoadAssetAtPath<CodePathCollectSO>(assetPath);
                if (PATHCOLLECT != null)
                    GeneratorHelper.Log($"����{typeof(CodePathCollectSO)}���سɹ�");
            }

            Repaint();
        }


        /// <summary>
        /// ����·���ռ�����
        /// </summary>
        /// <param name="key"></param>
        /// <param name="path"></param>
        private void Update_PATHCOLLECT(string key, string path)
        {
            if (PATHCOLLECT == null)
            {
                GeneratorHelper.LogError("����ʧ�ܣ�·���ռ����� Ϊ�գ�");
                this.ShowNotification(new GUIContent("����ʧ�ܣ���鿴Console��ø�����Ϣ"));
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


        #region ˵����

        [PropertyOrder(3), PropertySpace(0, 4), FoldoutGroup("˵����"), OnInspectorGUI, ShowIf("NoError_Flag1")]
        void OnIntroduce()
        {
            SirenixEditorGUI.IconMessageBox("�������� �� �����ԡ�View����β��Ԥ�轫����Ϊ��������.  �� �������ԡ�View����β��Ԥ�轫����Ϊ�ӽ���.", SdfIconType.ChatDotsFill);
        }

        #endregion

        #region ������

        [PropertyOrder(10), FoldoutGroup("������"), ResponsiveButtonGroup("������/bg"), Button(SdfIconType.Tools, "�޸�����·������", ButtonHeight = 24), ShowIf("NoError_Flag1")]
        public void FIX_PATHCOLLECT()
        {
            bool succ = EditorUtility.DisplayDialog("��ʾ", "��UIԤ�������ɹ�������뵫��������ݶ�ʧʱ�����ڴ��޸����⽫�������ռ�����·�����ݣ��Ƿ��޸���", "�޸�", "ȡ��");
            if (!succ)
            {
                GUIUtility.ExitGUI();
                return;
            }


            if (PATHCOLLECT == null)
            {
                GeneratorHelper.LogError("�޸�ʧ�ܣ�CodePathCollectSO Ϊ�գ�");
                this.ShowNotification(new GUIContent("�޸�ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return;
            }

            if (!Directory.Exists(SETTING.CodeGenSetting.OutputCodeRootFolder))
            {
                GeneratorHelper.LogError($"�޸�ʧ�ܣ�������������д��[������ɴ���ĸ��ļ���]�����ڣ�:{SETTING.CodeGenSetting.OutputCodeRootFolder}");
                this.ShowNotification(new GUIContent("�޸�ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return;
            }

            PATHCOLLECT.View2ScriptPath.Clear();
            var rule = SETTING.CodeGenSetting.View_OutputRule;

            //���������
            if (string.IsNullOrEmpty(rule.Gen_Generator))
            {
                GeneratorHelper.LogError($"�����������δָ�������� :{rule.RuleName}");
                this.ShowNotification(new GUIContent("�޸�ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return;
            }

            //���������
            Type t = typeof(IGenerateCode).Assembly.GetType(rule.Gen_Generator);
            if (t == null)
            {
                GeneratorHelper.LogError($"δ�ҵ��������� :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("�޸�ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return;
            }

            //���������
            var inst = Activator.CreateInstance(t);
            if (!(inst is IGenerateCode))
            {
                GeneratorHelper.LogError($"��������inst is not IGenerateCode :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("�޸�ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return;
            }

            IGenerateCode i = (IGenerateCode)inst;

            //�ҵ��������ɵĴ����ļ�
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


            GeneratorHelper.Log($"�޸�������ȫ�����");
            this.ShowNotification(new GUIContent("�޸�������ȫ�����"));
            CleanUp();
        }

        [PropertyOrder(11), FoldoutGroup("������"), ResponsiveButtonGroup("������/bg"), Button(SdfIconType.EraserFill, "����", ButtonHeight = 24), ShowIf("NoError_Flag1")]
        public void REFRESH_ALL()
        {
            bool succ = EditorUtility.DisplayDialog("��ʾ", "���õ�ǰ���У��⽫��ʧ����δ������޸ģ��Ƿ������", "ȷ��", "ȡ��");
            if (!succ)
            {
                GUIUtility.ExitGUI();
                return;
            }

            CleanUp();
            this.ShowNotification(new GUIContent("�������"));
        }

        #endregion

        #region ��ͷ

        string _DragPrefabItemTitle => DragPrefabItem != null ? $"���ڲ���{DragPrefabItem.name}" : "δѡ�����Ԥ��";
        string _DragPrefabItemTitle2 => _IsSubView ? "���ӽ��棩" : "���������棩";

        [PropertyOrder(20), TitleGroup("tg", "$_DragPrefabItemTitle2", GroupName = "$_DragPrefabItemTitle", Alignment = TitleAlignments.Centered), BoxGroup("tg/hg/bg", false), HorizontalGroup("tg/hg", Width = 350), ShowIf("NoError_Flag1"), SceneObjectsOnly, HideLabel, Required("������Hierarchy����е�UIԤ��ʵ��", InfoMessageType.Warning), OnValueChanged("OnDragPrefabItemChanged"), InlineButton("OnCheckUIPrefabNaming", "���UIԤ�� ", Icon = SdfIconType.BugFill, IconAlignment = IconAlignment.LeftOfText, ShowIf = "DragPrefabItem"), SuffixLabel("Prefab           ", Overlay = true)]
        public GameObject DragPrefabItem;

        [PropertyOrder(22), PropertySpace(0, 2), TitleGroup("tg"), BoxGroup("tg/bg2", false), ShowIf("NoError_Flag2"), LabelText("�ļ�λ�ã�"), LabelWidth(60), ReadOnly, DisplayAsString]
        public string ViewCodeFileFullName = "";//�������ɴ����ļ���FullName

        void OnDragPrefabItemChanged()
        {
            if (DragPrefabItem == null)
            {
                CleanUp();
                return;
            }

            //��������Ԥ��ʵ���Ƿ���ȷ
            if (!PrefabInstanceCheck(DragPrefabItem))
            {
                DragPrefabItem = null;

                this.ShowNotification(new GUIContent("�ⲻ��һ��Ԥ��ʵ������鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError("������Ĳ�����Ԥ��ʵ����Prefab Instance����������Hierarchy����е�UIԤ��ʵ����Hierarchy�����û��UIԤ��ʵ�������Project������ҵ���Ҫ������UIԤ����Դ���ϵ�Hierarchy����У�Scene�У�");
                CleanUp();
                return;
            }

            //��ò�����Ԥ��ʵ���ĸ�����Ϊ�ݴ���������Ԥ��ʵ���������壩
            DragPrefabItem = PrefabUtility.GetOutermostPrefabInstanceRoot(DragPrefabItem);

            //�Ƿ��ӽ��棨���������Ƿ���View��β��ȷ����
            _IsSubView = IsSubView(DragPrefabItem.name);

            //���������UI��Ӧ���������
            _ExportInfoList = LoadUIDataByScript();
            if (_ExportInfoList == null)
            {
                CleanUp();
                return;
            }

            //��ʼ��ģ����
            _moduleName = GetModuleName();
            if (_moduleName == null)
            {
                CleanUp();
                return;
            }

            //ˢ�¿�ѡ�����
            RefreshOptionalComponents();

            //ˢ���������������ʾ
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
                    sb.Append($" ��������{name} ->{GeneratorHelper.GetHierarchyWithRoot(child, DragPrefabItem.transform)}");
            }

            string err = sb.ToString();
            if (string.IsNullOrEmpty(err))
                this.ShowNotification(new GUIContent("���ͨ��"));
            else
            {
                this.ShowNotification(new GUIContent("��鷢�ִ�����鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError($"���UIԤ��ʱ���ִ���{err}");
            }
        }

        #endregion

        #region ѡ�������

        List<ValidateNode> _selectionValidateCodeList = new List<ValidateNode>();


        [PropertyOrder(30), TitleGroup("tg"), HorizontalGroup("tg/hg3", Width = 0.3f, PaddingRight = 5), VerticalGroup("tg/hg3/vg"), ShowIf("NoError_Flag2"), LabelText("ѡ�����"), ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
        public List<ComponentWrapper> OptionalComponent = new List<ComponentWrapper>();

        [PropertyOrder(31), TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg"), OnInspectorGUI, ShowIf("@(_selectionValidateCodeList.Count == 0 || _selectionValidateCodeList.Contains(ValidateNode.ERROR_NODE)) && NoError_Flag2")]
        void OptionalCompontNoneTip()
        {
            if (_selectionValidateCodeList.Count == 0)
                SirenixEditorGUI.IconMessageBox("δѡ��ڵ�", SdfIconType.EmojiNeutralFill);
            else if (_selectionValidateCodeList.Contains(ValidateNode.ERROR_NODE))
            {
                SirenixEditorGUI.IconMessageBox($"{(_selectionValidateCodeList.Count == 1 ? "ѡ�еĽڵ㲻����UIԤ��" : "���ֽڵ㲻����UIԤ��")}", SdfIconType.EmojiDizzyFill, Color.yellow);
            }
        }

        /// <summary>
        /// ˢ�¿�ѡ���
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

            //ͳ��ѡ��ڵ��״̬
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

            //ͳ��ÿ���ڵ����ϵ����
            List<List<Component>> tlist = new List<List<Component>>();
            List<Component> totalCom = new List<Component>();
            foreach (var i in dic.Keys)
            {
                //�˴��������ȡ��GameObject����ΪGameObjecet����Component
                List<Component> l = i.GetComponents<Component>().ToList();
                tlist.Add(l);
                totalCom.AddRange(l);
            }

            //ȡ�б���(���޷�ʵ��)
            // List<Component> sameList = IntersectAllComponent(tlist);

            //��װ����
            OptionalComponent.Clear();
            foreach (var com in totalCom)
            {
                //��װ
                UIComponentInfo newInfo = new UIComponentInfo(com, DragPrefabItem.transform);
                OptionalComponent.Add(new ComponentWrapper(com, newInfo, OnSelectOPC));
            }

            //ˢ��״̬
            RefreshOptionalComponentsSelectState();
        }


        /// <summary>
        /// ˢ�����Wrapper��ѡ��״̬
        /// </summary>
        void RefreshOptionalComponentsSelectState()
        {
            foreach (var i in OptionalComponent)
                i.isSelected = HasComponentInfo(i.uiData.uid);
        }

        /// <summary>
        /// ѡ��������
        /// </summary>
        /// <param name="wrapper"></param>
        void OnSelectOPC(ComponentWrapper wrapper)
        {
            string uid = wrapper.uiData.uid;
            if (HasComponentInfo(uid))
                RemoveComponentInfo(uid);            //ɾ��
            else
                AddComponentInfo(wrapper.uiData);    //����

            RefreshOptionalComponentsSelectState();
        }

        #endregion

        #region ��ѡ�����

        [PropertyOrder(40), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), FoldoutGroup("tg/hg3/vg2/��ǰ�������", Expanded = true), ShowIf("NoError_Flag2"), LabelText(" "), ListDrawerSettings(ShowFoldout = false, HideAddButton = true, DraggableItems = false, OnTitleBarGUI = "OnTitleBarGUI_ExportInfoList"), HideReferenceObjectPicker, OnCollectionChanged("On_ExportInfoListChangedAfter")]
        List<UIComponentInfo> _ExportInfoList = new List<UIComponentInfo>();


        /// <summary>
        /// �б����仯ʱ After
        /// </summary>
        void On_ExportInfoListChangedAfter(CollectionChangeInfo info, object value)
        {
            //�����ݱ�ɾ��
            if (info.ChangeType == CollectionChangeType.RemoveIndex)
            {
                RefreshOptionalComponentsSelectState();//ˢ�¿�ѡ����״̬
            }

            _IsDirtyForExportInfo = true;
        }

        /// <summary>
        /// �б���
        /// </summary>
        void OnTitleBarGUI_ExportInfoList()
        {
            //ǿ����������ݱ���
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.Robot))
            {
                _IsDirtyForExportInfo = true;
            }

            if (SirenixEditorGUI.ToolbarButton(SdfIconType.BugFill))
            {
                StringBuilder sb = new StringBuilder();
                //�����ѡ��������Ƿ��д���
                foreach (var item in _ExportInfoList)
                {
                    //���ҽڵ��Ƿ񻹴�����UIԤ����
                    Transform node = DragPrefabItem.transform.Find(item.path);
                    bool foundNode = node != null;
                    bool foundComponent = true;
                    if (!foundNode)
                        sb.Append($"  ������ݴ���[�ڵ�]�����ڣ� {item.memberName} -> {item.path}");
                    else
                    {
                        //���Ҹýڵ������Ƿ����
                        Component com = node.GetComponent(item.comType);
                        if (com == null)
                        {
                            sb.Append($"  ������ݴ���[���]�����ڣ� {item.memberName} | {item.comType}");
                            foundComponent = false;
                        }
                    }

                    //���¸�ֵ
                    item.validated = foundComponent && foundNode;
                }
                string error = sb.ToString();
                if (string.IsNullOrEmpty(error))
                {
                    this.ShowNotification(new GUIContent("���ݼ��ͨ����"));
                }
                else
                {
                    this.ShowNotification(new GUIContent("�������ݴ�����鿴Console��ø�����Ϣ"));
                    GeneratorHelper.LogError($"����������ݴ���{error}");
                }
            }
        }

        /// <summary>
        /// ��λ��Ԥ�貢ѡ��
        /// </summary>
        /// <param name="target"></param>
        public static void LocationToSelection(UIComponentInfo target)
        {
            if (Instance != null && Instance.DragPrefabItem != null)
                UnityEditor.Selection.activeTransform = Instance.DragPrefabItem.transform.Find(target.path);
        }

        /// <summary>
        /// �����������
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
        /// ɾ���������
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
        /// ������������Ƿ����
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        bool HasComponentInfo(string uid)
        {
            return _ExportInfoList.Find(e => e.uid == uid) != null;
        }

        #endregion

        #region ������-View

        [PropertyOrder(54), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH"), ShowIf("NoError_Flag2"), Button(SdfIconType.Gear, "", ButtonHeight = 28, Stretch = false, ButtonAlignment = 0.62f)]
        public void SeeViewOutputRule()
        {
            //�鿴���޸��������
            OdinEditorWindow.InspectObjectInDropDown(SETTING.CodeGenSetting.View_OutputRule);
        }

        [PropertyOrder(55), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH", Width = 80), ShowIf("NoError_Flag2"), Button(SdfIconType.FileEarmarkCodeFill, "Generation View Code", ButtonHeight = 28, Stretch = false, ButtonAlignment = 1f), GUIColor("#338ACB"), EnableIf("_IsDirtyForExportInfo")]
        public void GenerationView()
        {
            //�޸Ķ�����������
            if (!_IsDirtyForExportInfo)
                return;

            //��������
            List<UIComponentInfo> comList = new List<UIComponentInfo>(_ExportInfoList);
            bool succ = ProduceCSharpCode_View(SETTING.CodeGenSetting.View_OutputRule, comList);
            if (succ)
            {
                _IsDirtyForExportInfo = false;
            }
        }


        [PropertyOrder(50), TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH", Width = 200), ShowIf("NoError_Flag2"), LabelText("ģ������"), LabelWidth(45), ReadOnly, HideIf("_IsSubView")]
        public string _moduleName = "";


        [PropertyOrder(50), TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), HorizontalGroup("tg/hg3/vg2/btnH", Width = 200), ShowIf("NoError_Flag2"), LabelText("��ģ�����->���ٸ���ԭ�ļ�"), LabelWidth(164), ShowIf("@_IsSubView && NoError_Flag2")]
        public bool _NoModuleCodeFastOverrWrite = false;


        #endregion

        #region ������-Other

        [PropertyOrder(60), PropertySpace(4, 0), ShowInInspector, TitleGroup("tg"), HorizontalGroup("tg/hg3"), VerticalGroup("tg/hg3/vg2"), ListDrawerSettings(ShowFoldout = false, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "OnTitleBarGUI_OtherGen"), ShowIf("NoError_Flag2"), LabelText(" "), FoldoutGroup("������������", GroupID = "tg/hg3/vg2/FG3"), HideReferenceObjectPicker]
        public List<OutputRuleWrapper> otherOpRuleWrappers = new List<OutputRuleWrapper>();

        /// <summary>
        /// ˢ������������������װ
        /// </summary>
        void RefreshOtherOutputRuleWrapper()
        {
            otherOpRuleWrappers.Clear();
            foreach (var i in SETTING.CodeGenSetting.OtherOutputRules)
            {
                if (i.ApplyToWho == ApplyTo.All)
                    otherOpRuleWrappers.Add(new OutputRuleWrapper(i));
                else if (i.ApplyToWho == ApplyTo.�������� && !_IsSubView)
                    otherOpRuleWrappers.Add(new OutputRuleWrapper(i));
                else if (i.ApplyToWho == ApplyTo.�ӽ��� && _IsSubView)
                    otherOpRuleWrappers.Add(new OutputRuleWrapper(i));

            }
        }


        /// <summary>
        /// ����Other����
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

            //���»�ȡһ��ģ�����������û���;�ֶ��ı���Ԥ����
            _moduleName = GetModuleName();
            //�����ж�һ���Ƿ��ӽ��棬�����û���;�ֶ��ı���Ԥ����
            _IsSubView = IsSubView(DragPrefabItem.name);
            //��View���� -> �ӽ��潫ʹ��Ԥ������Ϊ����,��������������ģ������Ϊ����
            string className = _IsSubView ? DragPrefabItem.name : $"{_moduleName}";

            List<UIComponentInfo> comList = new List<UIComponentInfo>(_ExportInfoList);
            ProduceCSharpCode(wrapper.info, comList, _moduleName, className, SETTING.CodeGenSetting.OutputCodeRootFolder);
        }

        /// <summary>
        /// �����б�ͷ
        /// </summary>
        void OnTitleBarGUI_OtherGen()
        {
            if (SirenixEditorGUI.SDFIconButton("�������", 16, SdfIconType.None, style: SirenixGUIStyles.ToolbarButton))
            {
                //��������ѡ�е���������
            }

            if (SirenixEditorGUI.SDFIconButton("ˢ��", 16, SdfIconType.None, style: SirenixGUIStyles.ToolbarButton))
            {
                RefreshOtherOutputRuleWrapper();
            }
        }

        #endregion

        #region ͨ�÷���

        /// <summary>
        /// �ж��Ƿ���Ԥ��ʵ��
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
        /// �ж��Ƿ����ӽ���
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        bool IsSubView(string prefabName)
        {
            return prefabName.IndexOf("View") == -1;
        }

        /// <summary>
        /// ����UIԤ���Ӧ���������
        /// </summary>
        List<UIComponentInfo> LoadUIDataByScript()
        {
            if (!NoError_Flag2)
            {
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError("LoadUIViewByScript δͨ�����ı�ǣ�NoError_Flag2");
                return null;
            }

            var temp = PATHCOLLECT.View2ScriptPath.Find(e => e.key == DragPrefabItem.name);
            if (temp == null)
            {
                GeneratorHelper.Log("û�ҵ�UI�����Ӧ��������ݣ��������UI����Ը�������");
                return new List<UIComponentInfo>();
            }

            ViewCodeFileFullName = temp.value;
            if (!File.Exists(ViewCodeFileFullName))
            {
                GeneratorHelper.LogError($"�޷��ҵ�Ԥ���Ӧ���룬ԭλ�ã�{ViewCodeFileFullName} �Ƿ����ƶ�����λ�ã�����ʹ�á��޸�����·�����ݡ����ָܻ������߿���ѡ�����´�����");
                if (EditorUtility.DisplayDialog("��������", $"�޷��ҵ�Ԥ���Ӧ����,�Ƿ����ƶ�����λ�ã���鿴Console��ø�����Ϣ�� ԭλ�ã�{ViewCodeFileFullName}", "��ȥ����", "���´���"))
                    return null;
                else
                    return new List<UIComponentInfo>();
            }

            string viewCodeText = File.ReadAllText(ViewCodeFileFullName);
            if (string.IsNullOrEmpty(viewCodeText))
            {
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError($"��ȡUI��View����ʱ����Ϊ�գ�·����{ViewCodeFileFullName}");
                return null;
            }

            //Ѱ�ҿ��ù���
            OutputRule rule = SETTING.CodeGenSetting.View_OutputRule;
            if (rule == null)
            {
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError($"��[��������]��û���ҵ�����Ϊ{(_IsSubView ? "OPCodeType.�ӽ���" : "OPCodeType.��������")}�Ĺ������飺{SETTING.name}");
                return null;
            }

            //���������
            if (string.IsNullOrEmpty(rule.Gen_Generator))
            {
                GeneratorHelper.LogError($"�����������δָ�������� :{rule.RuleName}");
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                return null;
            }

            //���������
            Type t = typeof(IGenerateCode).Assembly.GetType(rule.Gen_Generator);
            if (t == null)
            {
                GeneratorHelper.LogError($"δ�ҵ��������� :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                return null;
            }

            //���������
            var inst = Activator.CreateInstance(t);
            if (!(inst is IGenerateCode))
            {
                GeneratorHelper.LogError($"��������inst is not IGenerateCode :{rule.Gen_Generator}");
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                return null;
            }

            //����
            IGenerateCode i = (IGenerateCode)inst;
            Dictionary<string, UIComponentInfo> dic = i.ParseViewCode(DragPrefabItem, viewCodeText, out string error);

            //����������
            if (!string.IsNullOrEmpty(error))
            {
                GeneratorHelper.LogError($"������������{error}");
                this.ShowNotification(new GUIContent("��������������鿴Console��ø�����Ϣ"));
            }
            if (dic == null)
            {
                GeneratorHelper.LogError($"����ʧ�ܣ�����ֹ :{rule.Gen_Generator}");
                return null;
            }

            GeneratorHelper.Log("����UI������ݳɹ���" + ViewCodeFileFullName);
            return dic.Values.ToList();
        }

        /// <summary>
        /// ��ʼ��ģ����
        /// </summary>
        string GetModuleName()
        {
            if (_IsSubView)
                return "";

            OutputRule rule = SETTING.CodeGenSetting.View_OutputRule;
            if (rule == null)
            {
                this.ShowNotification(new GUIContent("����������鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError($"��[��������]��û���ҵ�����Ϊ{(_IsSubView ? "OPCodeType.�ӽ���" : "OPCodeType.��������")}�Ĺ������飺{SETTING.name}");
                return null;
            }

            if (DragPrefabItem == null || string.IsNullOrEmpty(DragPrefabItem.name))
            {
                this.ShowNotification(new GUIContent("��ʼ��ģ����ʧ�ܣ���鿴Console��ø�����Ϣ"));
                GeneratorHelper.LogError($"��ʼ��ģ����ʧ�ܣ�DragPrefabItem��{DragPrefabItem}");
                return null;
            }

            string moduleName = DragPrefabItem.name;
            /*������View��β��Ԥ��,ɾ��β��View����Ϊģ����*/
            int index = moduleName.LastIndexOf("View");
            if (index != -1)
                moduleName = moduleName.Remove(index, 4);


            //  GeneratorHelper.Log($"��ʼ��ģ������{moduleName}");
            return moduleName;
        }


        /// <summary>
        /// �Ƿ��ǵ�ǰPrefab�Ľڵ� 
        /// (-1���� 0�ǵ�ǰ�ĸ��ڵ� 1�ǵ�ǰ���ӽڵ�)
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

        #region ���ɷ���

        /// <summary>
        /// ����View����
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="componentList"></param>
        /// <returns></returns>
        public bool ProduceCSharpCode_View(OutputRule rule, List<UIComponentInfo> componentList)
        {
            //���»�ȡһ��ģ�����������û���;�ֶ��ı���Ԥ����
            _moduleName = GetModuleName();
            //�����ж�һ���Ƿ��ӽ��棬�����û���;�ֶ��ı���Ԥ����
            _IsSubView = IsSubView(DragPrefabItem.name);
            //�ӽ��潫ʹ��Ԥ������Ϊ����,��������������ģ�������View��Ϊ����
            string className = _IsSubView ? DragPrefabItem.name : $"{_moduleName}View";

            bool succ = ProduceCSharpCode(rule, componentList, _moduleName, className, SETTING.CodeGenSetting.OutputCodeRootFolder);

            //���´���·���ռ�
            Update_PATHCOLLECT(DragPrefabItem.name, ViewCodeFileFullName);
            return succ;
        }

        /// <summary>
        /// ͨ������
        /// </summary>
        /// <param name="rule"> ���ɹ������� </param>
        /// <param name="componentList"> View������� </param>
        /// <param name="moduleName"> ģ����������Ϊ�գ���Ϊ��ʱ��ʾ�ӽ��棩 </param>
        /// <param name="className"> ָ������ </param>
        /// <param name="RootFolder"> ��Ŵ���ĸ�Ŀ¼ </param>
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
                GeneratorHelper.LogError("ProduceCSharpCode error: ��Ŵ���ĸ�Ŀ¼�����ڣ���������");
                return false;
            }

            if (rule.AssignRelativeFolderPath == null)
            {
                GeneratorHelper.LogError("ProduceCSharpCode error: û�ҵ��ļ��й������ã���������");
                return false;
            }

            //����ģʽ��������
            if (rule.GenMode == GenerationMode.Insertion)
                return InsertionCSharpCode(rule, moduleName, className);

            //������·��
            //�����������
            switch (rule.namingRule.type)
            {
                case NamingType.ָ������: className = string.Format($"{rule.namingRule.assignName}"); break;
                case NamingType.ģ������ǰ��׺: className = string.Format($"{rule.namingRule.prefix}{className}{rule.namingRule.suffix}"); break;
                default:
                    className = string.Format($"{className}"); break;
            }
            string codeFileName = $"{className}.cs";


            string moduleFolderName = string.IsNullOrEmpty(moduleName) ? string.Empty : string.Format($"{moduleName}Module");
            string saveFolder = string.IsNullOrEmpty(moduleFolderName) ? RootFolder : Path.Combine(RootFolder, moduleFolderName).Replace('\\', '/');
            string previewFileFullName = "";//�ļ�����FullName
            string codeContentText;//���մ����ı�

            //���ģ���ļ��в�������ǿ��תΪ�ֶ�ѡ�񱣴�λ��
            FolderRuleMode fMode = rule.FolderRuleTp;
            if (string.IsNullOrEmpty(moduleFolderName))
                fMode = FolderRuleMode.Flexible;
            if (fMode == FolderRuleMode.Assign)
            {
                //���ݹ���ϲ�����·��
                if (!string.IsNullOrEmpty(rule.AssignRelativeFolderPath))
                    saveFolder = Path.Combine(saveFolder, rule.AssignRelativeFolderPath).Replace('\\', '/');
                previewFileFullName = Path.Combine(saveFolder, codeFileName).Replace('\\', '/');
            }
            else
            {
                //ǿ���ֶ����
                bool forceFlexible = false;

                //��ģ�������ٸ�дʱ������Ѱ���ļ�λ��
                if (_NoModuleCodeFastOverrWrite)
                {
                    List<FileInfo> files = GeneratorHelper.FindCodeFile(RootFolder, codeFileName);
                    if (files.Count <= 0)
                    {
                        GeneratorHelper.Log($"��ģ�������ٸ�дʱ,���ҵ�Դ�ļ�,ǿ���л�Ϊ�ֶ����� {codeFileName}");
                        forceFlexible = true;
                    }
                    else if (files.Count > 1)
                    {
                        GeneratorHelper.LogError($"��ģ�������ٸ�дʱ,���ֶ��ͬ���ļ�,ǿ���л�Ϊ�ֶ����� {codeFileName}");
                        forceFlexible = true;
                    }
                    else
                    {
                        GeneratorHelper.Log($"��ģ�������ٸ�дʱ�ҵ��ļ�·�� {files[0].FullName}");
                        previewFileFullName = files[0].FullName;
                    }
                }

                if (forceFlexible || !_NoModuleCodeFastOverrWrite)
                {
                    //�ֶ�ѡ�񱣴�·��
                    previewFileFullName = EditorUtility.SaveFilePanel("ѡ�񱣴�λ��", RootFolder, className, "cs");
                    if (string.IsNullOrEmpty(previewFileFullName))
                    {
                        GeneratorHelper.Log($"ProduceCSharpCode ������ȡ������");
                        this.ShowNotification(new GUIContent("��ֹ����"));
                        return false;
                    }
                }
            }

            //����ģʽ-����������
            if (rule.GenMode == GenerationMode.GeneratorCode)
            {
                //���������
                if (string.IsNullOrEmpty(rule.Gen_Generator))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode �����������δָ�������� :{rule.RuleName}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

                //���������
                Type t = typeof(IGenerateCode).Assembly.GetType(rule.Gen_Generator);
                if (t == null)
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode δ�ҵ��������� :{rule.Gen_Generator}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

                //���������
                var inst = Activator.CreateInstance(t);
                if (!(inst is IGenerateCode))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode �������������� is not IGenerateCode :{rule.Gen_Generator}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
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
                    GeneratorHelper.LogError($"ProduceCSharpCode ���������صĴ����ı�Ϊ�� :{rule.Gen_Generator}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

            }
            //����ģʽ-ģ���ļ�
            else if (rule.GenMode == GenerationMode.Template)
            {
                if (string.IsNullOrEmpty(rule.Gen_Template) || !File.Exists(rule.Gen_Template))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode ģ���ļ������� :{rule.Gen_Template}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

                string templateText = File.ReadAllText(rule.Gen_Template);
                if (string.IsNullOrEmpty(templateText))
                {
                    GeneratorHelper.LogError($"ProduceCSharpCode ģ���ļ������� :{rule.Gen_Template}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

                //�滻�ַ���
                foreach (var i in rule.Gen_Template_Replace_Before)
                {
                    if (string.IsNullOrEmpty(i.value))
                        continue;

                    switch (i.key)
                    {
                        case ReplaceRuleData.REPLACEKEY_ModuleName: templateText = templateText.Replace(i.value, string.IsNullOrEmpty(_moduleName) ? DragPrefabItem.name : _moduleName); break;//��ģ����ʱʹ��Ԥ����
                        case ReplaceRuleData.REPLACEKEY_ClassName: templateText = templateText.Replace(i.value, className); break;
                        case ReplaceRuleData.REPLACEKEY_Time: templateText = templateText.Replace(i.value, DateTime.Now.ToString()); break;
                        case ReplaceRuleData.REPLACEKEY_Author: templateText = templateText.Replace(i.value, "������"); break;
                    }
                }

                //�ӽ�������滻
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
            //����ģʽ-�������
            else if (rule.GenMode == GenerationMode.Insertion)
            {
                return false;  //����ģʽ�ѵ��������������ܵ�������
            }
            else
                return false;

            //�ж��Ƿ��ܸ���
            bool isExist = File.Exists(previewFileFullName);
            if (!rule.AllowOverwrite && isExist)
            {
                GeneratorHelper.LogError($"�ļ��Ѵ��ڣ�������������Ϊ��������");
                this.ShowNotification(new GUIContent("�ļ��Ѵ��ڣ�������������Ϊ��������"));
                return false;
            }

            if (fMode == FolderRuleMode.Flexible)
            {
                //��ѡģʽ�����ļ��Ѵ���ʱ��Ҫ���Ѹ���
                if (isExist)
                {
                    if (!EditorUtility.DisplayDialog("ע��", $"�ļ�{codeFileName}�Ѵ��ڣ��Ƿ񸲸Ǳ��棿������������", "����", "ȡ��"))
                    {
                        GeneratorHelper.Log($"������ֹ��{codeFileName}�Ѵ��ڲ���������");
                        return false;
                    }
                }
            }

            //д��
            CreateFileDirectory(previewFileFullName);
            File.WriteAllText(previewFileFullName, codeContentText);
            GeneratorHelper.Log($"�ļ����ɳɹ���{previewFileFullName}\n�ı�����Ԥ����\n{codeContentText}");
            this.ShowNotification(new GUIContent(isExist ? "�ļ����ǳɹ�" : "�ļ������ɹ�"));

            ViewCodeFileFullName = previewFileFullName;

            return true;
        }


        /// <summary>
        /// ����ģʽ���������
        /// </summary>
        /// <returns></returns>
        bool InsertionCSharpCode(OutputRule rule, string moduleName, string className)
        {
            if (rule.GenMode != GenerationMode.Insertion)
            {
                GeneratorHelper.LogError($"����ģʽ- �ǲ���ģʽȴ������InsertionCSharpCode :{rule.InsertionTargetCodeFile}");
                this.ShowNotification(new GUIContent("������ã���鿴Console��ø�����Ϣ"));
                return false;
            }



            if (string.IsNullOrEmpty(rule.InsertionTargetCodeFile))
            {
                GeneratorHelper.LogError($"����ģʽ- Ŀ���ļ������� :Null");
                this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return false;
            }

            string targetFullName = Path.Combine(Application.dataPath, rule.InsertionTargetCodeFile).Replace('\\', '/');
            if (string.IsNullOrEmpty(rule.InsertionTargetCodeFile) || !File.Exists(targetFullName))
            {
                GeneratorHelper.LogError($"����ģʽ- Ŀ���ļ������� :{targetFullName}");
                this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return false;
            }

            string tOriText = File.ReadAllText(targetFullName);
            if (string.IsNullOrEmpty(tOriText))
            {
                GeneratorHelper.LogError($"����ģʽ-  Ŀ���ļ������� :{targetFullName}");
                this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                return false;
            }

            //��ʼ�������
            int instFinishCount = 0;
            foreach (var i in rule.InsertionList)
            {
                if (string.IsNullOrEmpty(i.key))
                {
                    GeneratorHelper.LogError($"����ģʽ- �������ʱ����λ�ñ��Ϊ�գ�");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

                //�ҵ������λ��
                int insIndex = tOriText.LastIndexOf(i.key);
                if (insIndex < 0)
                {
                    GeneratorHelper.LogError($"����ģʽ- ��Ŀ���ļ����ı��������޷��ҵ�λ�ñ��{i.key}��Ŀ���ļ�{targetFullName}");
                    this.ShowNotification(new GUIContent("ʧ�ܣ���鿴Console��ø�����Ϣ"));
                    return false;
                }

                if (string.IsNullOrEmpty(i.value))
                {
                    GeneratorHelper.LogError($"����ģʽ- ��������ģ������Ϊ�գ���������");
                    continue;
                }


                //����������������ж��Ƿ����������Ѵ���(�����ҪΪ�����ʾ����Ҫ����)
                string pattern = i.checkExistsMatchPattern;
                if (!string.IsNullOrEmpty(pattern))
                {
                    //����ǰΪ������ʽ�滻���
                    foreach (var replace in rule.Gen_Template_Replace_Before)
                    {
                        if (string.IsNullOrEmpty(replace.value))
                            continue;
                        switch (replace.key)
                        {
                            case ReplaceRuleData.REPLACEKEY_ModuleName: pattern = pattern.Replace(replace.value, string.IsNullOrEmpty(_moduleName) ? DragPrefabItem.name : _moduleName); break;//��ģ����ʱʹ��Ԥ����
                            case ReplaceRuleData.REPLACEKEY_ClassName: pattern = pattern.Replace(replace.value, className); break;
                            case ReplaceRuleData.REPLACEKEY_Time: pattern = pattern.Replace(replace.value, DateTime.Now.ToString()); break;
                            case ReplaceRuleData.REPLACEKEY_Author: pattern = pattern.Replace(replace.value, "������"); break;
                        }
                    }

                    Match mat = Regex.Match(tOriText, pattern, RegexOptions.Multiline);
                    if (mat.Success)
                    {
                        GeneratorHelper.LogError($"����ģʽ- �����ظ����룬������������{pattern}");
                        continue;
                    }
                }

                //��������ǰ-Ϊ��������������滻���
                string contentCopy = i.value;
                foreach (var replace in rule.Gen_Template_Replace_Before)
                {
                    if (string.IsNullOrEmpty(replace.value))
                        continue;

                    switch (replace.key)
                    {
                        case ReplaceRuleData.REPLACEKEY_ModuleName: contentCopy = contentCopy.Replace(replace.value, string.IsNullOrEmpty(_moduleName) ? DragPrefabItem.name : _moduleName); break;//��ģ����ʱʹ��Ԥ����
                        case ReplaceRuleData.REPLACEKEY_ClassName: contentCopy = contentCopy.Replace(replace.value, className); break;
                        case ReplaceRuleData.REPLACEKEY_Time: contentCopy = contentCopy.Replace(replace.value, DateTime.Now.ToString()); break;
                        case ReplaceRuleData.REPLACEKEY_Author: contentCopy = contentCopy.Replace(replace.value, "������"); break;
                    }
                }

                // ��������
                tOriText = tOriText.Insert(insIndex, contentCopy);
                instFinishCount++;
            }

            if (instFinishCount <= 0)
            {
                GeneratorHelper.Log($"����ģʽ- ���κ��滻����ɣ�����ֹ���� Ŀ���ļ�{targetFullName}");
                return false;
            }

            //����- ���в�������������
            string codeContentText = tOriText;
            File.WriteAllText(targetFullName, codeContentText);

            GeneratorHelper.Log($"����ģʽ-���ݲ���ɹ���{targetFullName}\n�ı�����Ԥ����\n{codeContentText}");
            this.ShowNotification(new GUIContent("���ݲ���ɹ�"));
            return true;

        }

        /// <summary>
        /// �����ļ��л��ļ�
        /// </summary>
        /// <param name="pFilePath"></param>
        /// <param name="pCreateFile"></param>
        /// <returns>�Ƿ񴴽��ɹ�</returns>
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
        /// ����ǰ��������
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
