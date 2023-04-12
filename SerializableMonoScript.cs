#region License and Information
/*****
* Provides a serializable wrapper for Unity's MonoScript class which is an
* Editor-only class. It allows you to drag-and-drop actual script assets
* onto a field. The generic version allows for filtering for certain types
* It does support MonoBehaviour derived as well as ScriptableObject MonoScript
* types. The generic version also provides two methods to either add the
* represented type as component to a given gameobject (AddToGameObject) or
* to create a scriptable object instance of that class (CreateSOInstance)
* 
* The generic argument does support filtering for interfaces as well.
* Note: Assigned types are NOT refactoring safe. The actual type is stored as
* and Assembly qualified type name. So be careful when refactoring.
* 
* Copyright (c) 2023 Bunny83
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to
* deal in the Software without restriction, including without limitation the
* rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
* sell copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
* IN THE SOFTWARE.
* 
*****/
#endregion License and Information
using UnityEngine;

namespace B83
{
    [System.Serializable]
    public class SerializableMonoScript
    {
        System.Type m_Type = null;
        [SerializeField]
        string m_TypeName = null;
        public System.Type Type
        {
            get
            {
                if (m_Type == null)
                {
                    if (string.IsNullOrEmpty(m_TypeName))
                        return null;
                    m_Type = System.Type.GetType(m_TypeName);
                }
                return m_Type;
            }
            set
            {
                m_Type = value;
                if (m_Type == null)
                    m_TypeName = "";
                else
                    m_TypeName = m_Type.AssemblyQualifiedName;
            }
        }
    }
    [System.Serializable]
    public class SerializableMonoScript<T> : SerializableMonoScript where T : class
    {
        public T CreateSOInstance()
        {
            var type = Type;
            if (type != null)
                return (T)(object)ScriptableObject.CreateInstance(type);
            return default(T);
        }
        public T AddToGameObject(GameObject aGO)
        {
            var type = Type;
            if (type != null)
                return (T)(object)aGO.AddComponent(type);
            return default(T);
        }
    }


#if UNITY_EDITOR
    namespace PropertyDrawers
    {
        using System.Collections.Generic;
        using UnityEditor;

        [CustomPropertyDrawer(typeof(SerializableMonoScript),true)]
        public class SerializableTypePropertyDrawer : PropertyDrawer
        {
            private System.Type m_FilterType = null;
            private static Dictionary<System.Type, MonoScript> m_MonoScriptCache = new Dictionary<System.Type, MonoScript>();
            private static MonoScript GetMonoScript(System.Type aType)
            {
                if (aType == null)
                    return null;
                if (m_MonoScriptCache.TryGetValue(aType, out MonoScript script) && script != null)
                {
                    return script;
                }
                var scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
                foreach(var s in scripts)
                {
                    var type = s.GetClass();
                    if (type != null)
                        m_MonoScriptCache[type] = s;
                    if (type == aType)
                        script = s;
                }
                return script;
            }
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (m_FilterType == null)
                {
                    if (fieldInfo.FieldType.IsGenericType)
                    {
                        var types = fieldInfo.FieldType.GetGenericArguments();
                        if (types != null && types.Length == 1)
                            m_FilterType = types[0];
                    }
                    else
                        m_FilterType = typeof(UnityEngine.Object);
                }
                var typeName = property.FindPropertyRelative("m_TypeName");
                System.Type type = System.Type.GetType(typeName.stringValue);
                MonoScript monoScript = GetMonoScript(type);
                EditorGUI.BeginChangeCheck();
                monoScript = (MonoScript)EditorGUI.ObjectField(position, label, monoScript, typeof(MonoScript), true);
                if (EditorGUI.EndChangeCheck())
                {
                    if (monoScript == null)
                        typeName.stringValue = "";
                    else
                    {
                        var newType = monoScript.GetClass();
                        if (newType != null && m_FilterType.IsAssignableFrom(newType))
                            typeName.stringValue = newType.AssemblyQualifiedName;
                        else
                            Debug.LogWarning("Dropped type does not derive or implement " + m_FilterType.Name);
                    }
                }
            }
        }
    }
#endif
}
