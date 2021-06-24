//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        sergey.stoyan@hotmail.com
//        stoyan@cliversoft.com
//        http://www.cliversoft.com
//********************************************************************************************
#define COMPILE_GetObject_SetObject1 //!!!Stopwatch shows that compiling is not faster. Probably the reflection was improved.

using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

namespace Cliver
{
    /// <summary>
    /// Settings attributes which are defined by a Settings field.
    /// </summary>
    abstract public class SettingsFieldInfo
    {
        /// <summary>
        /// Settings' full name is the string that is used in code to refer to this field/property. 
        /// It defines exactly the Settings field/property in code but has nothing to do with the one's type. 
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// Path of the storage file. It consists of a directory which defined by the Settings based type and the file name which is the field's full name in the code.
        /// </summary>
        public readonly string File;

        /// <summary>
        /// Path of the init file. It consists of the directory where the entry assembly is located and the file name which is the field's full name in the code.
        /// </summary>
        public readonly string InitFile;

        /// <summary>
        /// Settings derived type.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Version info of the Settings type defined in the present code.
        /// </summary>
        public readonly SettingsTypeAttribute.TypeVersionAttribute TypeVersion;

        /// <summary>
        /// Encryption engine.
        /// </summary>
        public readonly StringEndec Endec = null;

        /// <summary>
        /// When TRUE, the Settings field is not initialized by default and needs an explicit initializing. 
        /// Such a field, when needed, must be initiated explicitly by Config.Reload(string settingsFieldFullName, bool throwExceptionIfCouldNotLoadFromStorageFile = false)
        /// </summary>
        public readonly bool Optional = false;

        /// <summary>
        /// When TRUE, the Settings field is serialized with indention.
        /// </summary>
        public readonly bool Indented = true;

        /// <summary>
        /// When FALSE, those serializable fields/properties of the Settings field whose values are NULL, are ignored while serializing.
        /// </summary>
        public readonly bool NullSerialized = false;

        internal Settings GetObject()
        {
            lock (this)
            {
                return (Settings)getObject();
            }
        }

        internal void SetObject(Settings settings)
        {
            lock (this)
            {
                setObject(settings);
            }
        }

#if !COMPILE_GetObject_SetObject
        abstract protected object getObject();
        abstract protected void setObject(Settings settings);

        protected SettingsFieldInfo(MemberInfo settingsTypeMemberInfo, Type settingsType)
        {
#else
        readonly Func<object> getObject;
        readonly Action<Settings> setObject;

        protected SettingsFieldInfo(MemberInfo settingsTypeMemberInfo, Type settingsType, Func<object> getObject, Action<Settings> setObject)
        {
            this.getObject = getObject;
            this.setObject = setObject;
#endif
            Type = settingsType;
            FullName = settingsTypeMemberInfo.DeclaringType.FullName + "." + settingsTypeMemberInfo.Name;
            /*//version with static __StorageDir
            string storageDir;
            for (; ; )
            {
                PropertyInfo fi = settingType.GetProperty(nameof(Settings.__StorageDir), BindingFlags.Static | BindingFlags.Public);
                if (fi != null)
                {
                    storageDir = (string)fi.GetValue(null);
                    break;
                }
                settingType = settingType.BaseType;
                if (settingType == null)
                    throw new Exception("Settings type " + Type.ToString() + " or some of its ancestors must define the public static getter " + nameof(Settings.__StorageDir));
            }
            File = storageDir + System.IO.Path.DirectorySeparatorChar + FullName + "." + Config.FILE_EXTENSION;
            */
            Settings s = (Settings)Activator.CreateInstance(Type); //!!!slightly slowler than calling a static by reflection. Doesn't run slower for a bigger class though.
            File = s.__StorageDir + System.IO.Path.DirectorySeparatorChar + FullName + "." + Config.FILE_EXTENSION;
            InitFile = Log.AppDir + System.IO.Path.DirectorySeparatorChar + FullName + "." + Config.FILE_EXTENSION;

            SettingsAttribute.EncryptedAttribute encryptedAttribute = settingsTypeMemberInfo.GetCustomAttributes<SettingsAttribute.EncryptedAttribute>(false).FirstOrDefault();
            if (encryptedAttribute == null)
                encryptedAttribute = settingsType.GetCustomAttributes<SettingsAttribute.EncryptedAttribute>(false).FirstOrDefault();
            if (encryptedAttribute != null)
                Endec = encryptedAttribute.Endec;

            SettingsFieldAttribute.OptionalAttribute optionalAttribute = settingsTypeMemberInfo.GetCustomAttributes<SettingsFieldAttribute.OptionalAttribute>(false).FirstOrDefault();
            if (optionalAttribute != null)
                Optional = optionalAttribute.Value;

            SettingsFieldAttribute.IndentedAttribute indentedAttribute = settingsTypeMemberInfo.GetCustomAttributes<SettingsFieldAttribute.IndentedAttribute>(false).FirstOrDefault();
            if (indentedAttribute != null)
                Indented = indentedAttribute.Value;

            SettingsFieldAttribute.NullSerializedAttribute nullSerializedAttribute = settingsTypeMemberInfo.GetCustomAttributes<SettingsFieldAttribute.NullSerializedAttribute>(false).FirstOrDefault();
            if (nullSerializedAttribute != null)
                NullSerialized = nullSerializedAttribute.Value;

            SettingsTypeAttribute.TypeVersionAttribute typeVersion = settingsType.GetCustomAttributes<SettingsTypeAttribute.TypeVersionAttribute>(false).FirstOrDefault();
            TypeVersion = typeVersion != null ? typeVersion : new SettingsTypeAttribute.TypeVersionAttribute(0, 0);
        }

        #region Type Version support

        /// <summary>
        /// Read the storage file as a JObject in order to migrate to the current format.
        /// </summary>
        /// <returns></returns>
        public Newtonsoft.Json.Linq.JObject ReadStorageFileAsJObject()
        {
            lock (this)
            {
                if (!System.IO.File.Exists(File))
                    return null;
                string s = System.IO.File.ReadAllText(File);
                if (Endec != null)
                    s = Endec.Decrypt(s);
                return Newtonsoft.Json.Linq.JObject.Parse(s);
            }
        }

        /// <summary>
        /// Write the JObject to the storage file in order to migrate to the current format.
        /// </summary>
        /// <returns></returns>
        public void WriteStorageFileAsJObject(Newtonsoft.Json.Linq.JObject o)
        {
            lock (this)
            {
                string s = o.ToString();
                if (Endec != null)
                    s = Endec.Decrypt(s);
                System.IO.File.WriteAllText(File, s);
            }
        }

        /// <summary>
        /// Read the storage file as a string in order to migrate to the current format.
        /// </summary>
        /// <returns></returns>
        public string ReadStorageFileAsString()
        {
            lock (this)
            {
                if (!System.IO.File.Exists(File))
                    return null;
                string s = System.IO.File.ReadAllText(File);
                if (Endec != null)
                    s = Endec.Decrypt(s);
                return s;
            }
        }

        /// <summary>
        /// Write the string to the storage file in order to migrate to the current format.
        /// </summary>
        /// <returns></returns>
        public void WriteStorageFileAsString(string s)
        {
            lock (this)
            {
                if (Endec != null)
                    s = Endec.Decrypt(s);
                System.IO.File.WriteAllText(File, s);
            }
        }

        #endregion
    }

    public class SettingsFieldFieldInfo : SettingsFieldInfo
    {
#if !COMPILE_GetObject_SetObject
        override protected object getObject()
        {
            return FieldInfo.GetValue(null);
        }

        override protected void setObject(Settings settings)
        {
            FieldInfo.SetValue(null, settings);
        }

        readonly FieldInfo FieldInfo;

        internal SettingsFieldFieldInfo(FieldInfo settingsTypeFieldInfo) : base(settingsTypeFieldInfo, settingsTypeFieldInfo.FieldType)
        {
            FieldInfo = settingsTypeFieldInfo;
        }
#else
        internal SettingsFieldInfo(FieldInfo settingsTypeFieldInfo) : base(
            settingsTypeFieldInfo,
            settingsTypeFieldInfo.FieldType,
            getGetValue(settingsTypeFieldInfo),
            getSetValue(settingsTypeFieldInfo)
            )
        {
        }
        protected static Func<object> getGetValue(FieldInfo fieldInfo)
        {
            MemberExpression me = Expression.Field(null, fieldInfo);
            return Expression.Lambda<Func<object>>(me).Compile();

        }
        protected static Action<Settings> getSetValue(FieldInfo fieldInfo)
        {
            ParameterExpression pe = Expression.Parameter(typeof(object));
            UnaryExpression ue = Expression.Convert(pe, fieldInfo.FieldType);
            MemberExpression me = Expression.Field(null, fieldInfo);
            BinaryExpression be = Expression.Assign(me, ue);
            return Expression.Lambda<Action<Settings>>(be, pe).Compile();
        }
#endif
    }

    public class SettingsFieldPropertyInfo : SettingsFieldInfo
    {
#if !COMPILE_GetObject_SetObject
        override protected object getObject()
        {
            return PropertyInfo.GetValue(null);
        }

        override protected void setObject(Settings settings)
        {
            PropertyInfo.SetValue(null, settings);
        }

        readonly PropertyInfo PropertyInfo;

        internal SettingsFieldPropertyInfo(PropertyInfo settingsTypePropertyInfo) : base(settingsTypePropertyInfo, settingsTypePropertyInfo.PropertyType)
        {
            PropertyInfo = settingsTypePropertyInfo;
        }
#else
        internal SettingsPropertyInfo(PropertyInfo settingsTypePropertyInfo) : base(
            settingsTypePropertyInfo,
            settingsTypePropertyInfo.PropertyType,
            getGetValue(settingsTypePropertyInfo.GetGetMethod(true)),
            getSetValue(settingsTypePropertyInfo.GetSetMethod(true))
            )
        {
        }
        protected static Func<object> getGetValue(MethodInfo methodInfo)
        {
            MethodCallExpression mce = Expression.Call(methodInfo);
            return Expression.Lambda<Func<object>>(mce).Compile();
        }
        protected static Action<Settings> getSetValue(MethodInfo methodInfo)
        {
            ParameterExpression pe = Expression.Parameter(typeof(object));
            UnaryExpression ue = Expression.Convert(pe, methodInfo.GetParameters().First().ParameterType);
            MethodCallExpression mce = Expression.Call(methodInfo, ue);
            return Expression.Lambda<Action<Settings>>(mce, pe).Compile();
        }
#endif
    }
}
