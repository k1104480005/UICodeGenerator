using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix;
using Sirenix.Utilities.Editor;

namespace UICodeGeneration
{
    /// <summary>
    /// UI组件数据
    /// </summary>
    [Serializable]
    public class UIComponentInfo
    {
        Color textColor => validated ? Color.white : Color.red;
        SdfIconType comIconType => GeneratorHelper.GetComponentIcon(comType);


        [PropertyOrder(-1), HorizontalGroup("hh2", Width = 20), OnInspectorGUI]
        void OnDrawComponentIconGUI()
        {
            if (SirenixEditorGUI.SDFIconButton("", 20, comIconType, style: SirenixGUIStyles.IconButton))
            {
                CodeGeneratorEditorWindow.LocationToSelection(this );
            }
        }

        /// <summary>
        /// 是否有效（此组件是否还存在？）(外部更新)
        /// </summary>
        [HideInInspector]
        public bool validated = true;

        /// <summary>
        /// Prefab名+Node名+组件类型+path
        /// </summary>
        [HideInInspector]
        public string uid;

        /// <summary>
        /// 组件类型
        /// </summary>
        [PropertyOrder(1), HorizontalGroup("hh2", Width = 0.3f), HideLabel, ReadOnly, GUIColor("$textColor")]
        public string comType;

        /// <summary>
        /// Node名_组件类型
        /// </summary>
        [PropertyOrder(2), HorizontalGroup("hh2"), HideLabel, ReadOnly, GUIColor("$textColor")]
        public string memberName;

        /// <summary>
        /// 组件相对路径
        /// </summary>
        [HideInInspector]
        public string path;

        /// <summary>
        /// 查看详细数据
        /// </summary>
        void OnContextMenu()
        {
            OdinEditorWindow.InspectObjectInDropDown(new UIComponentInfoReadOnlyWrapper(this));
        }



        public UIComponentInfo() { }

        public UIComponentInfo(GameObject go, Transform prefabRoot)
        {
            this.comType = "GameObject";
            this.memberName = go.name;
            this.path = GeneratorHelper.GetHierarchyWithRoot(go.transform, prefabRoot);
            uid = MD5Hashing.HashString(string.Format("{0}:{1}|{2}", prefabRoot.name, string.Format("{0}_{1}", this.comType, this.memberName), this.path));
        }

        public UIComponentInfo(Component component, Transform prefabRoot)
        {
            this.comType = component.GetType().Name;
            this.memberName = string.Format("{0}_{1}", component.name, comType);
            this.path = GeneratorHelper.GetHierarchyWithRoot(component.transform, prefabRoot);
            uid = MD5Hashing.HashString(string.Format("{0}:{1}|{2}", prefabRoot.name, this.comType, this.path));
        }

        public UIComponentInfo(Transform prefabRoot, string memberName, string path, string type)
        {
            this.comType = type;
            this.memberName = memberName;
            this.path = path;
            uid = MD5Hashing.HashString(string.Format("{0}:{1}|{2}", prefabRoot.name, this.comType, this.path));
        }

        public string GetName()
        {
            return string.Format("{0} {1}", comType, memberName);
        }
    }


    /// <summary>
    /// View代码数据
    /// </summary>
    public class ViewCodeInfo : BaseViewCodeInfo
    {
        public string className;

        public List<UIComponentInfo> source;

        public ViewCodeInfo(string className, List<UIComponentInfo> source)
        {
            if (string.IsNullOrEmpty(className))
                throw new System.ArgumentException("className");
            if (source == null)
                throw new System.ArgumentNullException("source cannot be null !");
            this.className = className;
            this.source = source;
        }
    }

    public class BaseViewCodeInfo
    {
        private global::System.Text.StringBuilder builder;
        private global::System.Collections.Generic.IDictionary<string, object> session;
        //private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        private string currentIndent = string.Empty;
        private global::System.Collections.Generic.Stack<int> indents;
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();

        public global::System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if (this.builder == null)
                    this.builder = new global::System.Text.StringBuilder();
                return this.builder;
            }
            set
            {
                this.builder = value;
            }
        }

        //protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors
        //{
        //    get
        //    {
        //        if (this.errors == null)
        //            this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
        //        return this.errors;
        //    }
        //}

        private global::System.Collections.Generic.Stack<int> Indents
        {
            get
            {
                if (this.indents == null)
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                return this.indents;
            }
        }

        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this._toStringHelper;
            }
        }

        public void Write(string textToAppend)
        {
            this.GenerationEnvironment.Append(textToAppend);
        }

        public void Write(string format, params object[] args)
        {
            this.GenerationEnvironment.AppendFormat(format, args);
        }


        public class ToStringInstanceHelper
        {
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            public global::System.IFormatProvider FormatProvider
            {
                get { return this.formatProvider; }
                set
                {
                    if (this.formatProvider == null)
                        throw new global::System.ArgumentNullException("formatProvider");
                    this.formatProvider = value;
                }
            }

            public string ToStringWithCulture(object objectToConvert)
            {
                if (objectToConvert == null)
                    throw new global::System.ArgumentNullException("objectToConvert");
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type))
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                global::System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new global::System.Type[] { iConvertibleType });
                if (methInfo != null)
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] { this.formatProvider })));
                return objectToConvert.ToString();
            }

        }

    }

}