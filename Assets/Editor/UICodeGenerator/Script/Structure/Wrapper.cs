using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UICodeGeneration
{
    [Serializable]
    public class ComponentWrapper
    {
        private Component _info;
        public Component info { get { return _info; } }

        private UIComponentInfo _uiData;
        public UIComponentInfo uiData { get { return _uiData; } }


        //选择回调
        Action<ComponentWrapper> _onSelect;

        //选择状态（需外部更新）
        [HideInInspector] public bool isSelected;

        public ComponentWrapper(Component pinfo, UIComponentInfo puidata, Action<ComponentWrapper> pSelect)
        {
            _info = pinfo;
            _uiData = puidata;
            _onSelect = pSelect;
        }

        [HorizontalGroup("hh", Gap = 2), ShowInInspector, Button("$ComponentName", ButtonHeight = 22, IconAlignment = IconAlignment.LeftOfText), GUIColor("$color")]
        public void ComponentNameButton()
        {
            if (!isSelected)
                _onSelect?.Invoke(this);
            //OdinEditorWindow.InspectObjectInDropDown(new UIComponentInfoReadOnlyWrapper(uiData));
        }


        Color color => isSelected ? Color.grey : Color.white;

        SdfIconType comIcon => GeneratorHelper.GetComponentIcon(ComponentName);

        [HideInInspector]
        public string ComponentName { get { return _info.GetType().Name; } }


        [HorizontalGroup("hh", Width = 22, PaddingRight = 6), ShowInInspector, Button(SdfIconType.DashCircle, "", ButtonHeight = 22, Stretch = false, ButtonAlignment = 0, Style = ButtonStyle.Box), GUIColor("red"), ShowIf("isSelected")]
        public void StateButton()//减号
        {
            if (isSelected)
                _onSelect?.Invoke(this);
        }

        [HorizontalGroup("hh", Width = 22, PaddingRight = 6), ShowInInspector, Button(SdfIconType.PlusCircle, "", ButtonHeight = 22, Stretch = false, ButtonAlignment = 0, Style = ButtonStyle.Box), GUIColor("green"), HideIf("isSelected")]
        public void StateButton2()//加号
        {
            if (!isSelected)
                _onSelect?.Invoke(this);
        }
    }

    [Serializable]
    public class UIComponentInfoReadOnlyWrapper
    {
        private UIComponentInfo _uiData;
        public UIComponentInfo uiData { get { return _uiData; } }

        public UIComponentInfoReadOnlyWrapper(UIComponentInfo puidata)
        {
            _uiData = puidata;
        }

        [ShowInInspector, ReadOnly, LabelText("UID")]
        public string Uid { get { return uiData.uid; } }

        [ShowInInspector, ReadOnly, LabelText("组件类型")]
        public string ComType { get { return uiData.comType; } }

        [ShowInInspector, ReadOnly, LabelText("字段名")]
        public string MemberName { get { return uiData.memberName; } }

        [ShowInInspector, ReadOnly, LabelText("相对路径")]
        public string Path { get { return pathText; } }


        string pathText => (uiData.path == "" ? "（这是ROOT节点）" : uiData.path);

    }

    [Serializable]
    public class OutputRuleWrapper
    {
        private OutputRule _info;
        public OutputRule info { get { return _info; } }

        public OutputRuleWrapper(OutputRule pinfo)
        {
            _info = pinfo;
        }


        [ToggleGroup("Enable", "$ruleName")]
        public bool Enable;

        [HideInInspector]
        public string ruleName { get { return _info.RuleName; } }

        private string genBtnName => $"Gen {ruleName}";


        [PropertyOrder(1), HorizontalGroup("Enable/h"), ShowInInspector, ToggleGroup("Enable"), Button(SdfIconType.Gear, "", ButtonHeight = 24, ButtonAlignment = 0f, Stretch = false)]
        void SettingBtn()
        {
            OdinEditorWindow.InspectObjectInDropDown(_info);
        }

        [PropertyOrder(2), HorizontalGroup("Enable/h", Width = 0.25f), ShowInInspector, ToggleGroup("Enable"), Button(SdfIconType.FileEarmarkCode, "$genBtnName", ButtonHeight = 24, ButtonAlignment = 1f, Stretch = false), GUIColor("#7F5FCF")]
        void GenBtn()
        {
            CodeGeneratorEditorWindow.GenAOtherCode(this);
        }

    }
}
