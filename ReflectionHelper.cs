using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Collections;

namespace Joe.Reflection
{
    public static class ReflectionHelper
    {
        public static PropertyInfo GetEvalPropertyInfo(Object obj, String propertyString)
        {

            String[] propertyArray = propertyString.Split('.');
            PropertyInfo propInfo = null;
            if (obj != null)
            {
                foreach (String propertyName in propertyArray)
                {
                    if (obj == null)
                        obj = Activator.CreateInstance(propInfo.PropertyType);

                    propInfo = obj.GetType().GetProperty(propertyName);
                    if (propInfo != null)
                        obj = propInfo.GetValue(obj, null);
                    else
                        throw new Exception("Invalid Property String");
                }
            }
            return propInfo;
        }

        public static PropertyInfo GetEvalPropertyInfo(Type type, String propertyString)
        {

            String[] propertyArray = propertyString.Split('.');
            PropertyInfo propInfo = null;
            if (type != null)
            {
                foreach (String propertyName in propertyArray)
                {
                    propInfo = type.GetProperty(propertyName);
                    if (propInfo != null)
                        type = propInfo.PropertyType;
                    else
                        throw new Exception("Invalid Property String");
                }
            }
            return propInfo;
        }

        public static PropertyInfo TryGetEvalPropertyInfo(Type type, String propertyString)
        {
            PropertyInfo info = null;
            try
            {
                info = GetEvalPropertyInfo(type, propertyString);
            }
            catch
            {

            }
            return info;
        }

        public static Object GetEvalProperty(Object obj, String propertyString)
        {
            String[] propertyArray = propertyString.Split('.');
            PropertyInfo propInfo = null;
            foreach (String propertyName in propertyArray)
            {
                if (obj != null)
                {
                    propInfo = obj.GetType().GetProperty(propertyName);
                    if (propInfo != null)
                        obj = propInfo.GetValue(obj, null);
                    else
                        throw new Exception("Invalid Property String");
                }
            }
            return obj;
        }

        public static void SetEvalProperty(Object obj, String propertyString, Object value)
        {
            SetEvalProperty(obj, propertyString, value, null);
        }

        public static void SetEvalProperty(Object obj, String propertyString, Object value, Action<Object, Object, PropertyInfo> ObjectCreated)
        {
            String[] propertyArray = propertyString.Split('.');
            PropertyInfo propInfo = null;
            int count = 0;
            Object parentObj = obj;
            foreach (String propertyName in propertyArray)
            {
                propInfo = obj.GetType().GetProperty(propertyName);
                if (propInfo != null)
                {
                    if (count < propertyArray.Length - 1)
                    {
                        obj = propInfo.GetValue(obj, null);
                    }
                    if (obj == null)
                    {
                        obj = Activator.CreateInstance(propInfo.PropertyType);
                        if (ObjectCreated != null)
                            ObjectCreated(obj, parentObj, propInfo);
                    }

                    parentObj = obj;
                }
                else
                    throw new Exception("Invalid Property String");
                count++;
            }

            if (propInfo != null)
            {
                if (propInfo.CanWrite && propInfo.SetMethod.IsPublic)
                {
                    if (!propInfo.PropertyType.IsValueType || !(Nullable.GetUnderlyingType(propInfo.PropertyType) != null))
                        value = Convert.ChangeType(value, propInfo.PropertyType);
                    propInfo.SetValue(obj, value, null);
                }
            }
            else
                throw new Exception("Invalid Property String");

        }

        public static void RefelectiveMap(Object fromObject, Object toObject)
        {
            foreach (PropertyInfo fromInfo in fromObject.GetType().GetProperties())
            {
                PropertyInfo toInfo = toObject.GetType().GetProperty(fromInfo.Name);
                if (toInfo != null)
                {
                    if (toInfo.PropertyType == fromInfo.PropertyType && toInfo.CanWrite && toInfo.GetSetMethod() != null)
                    {
                        toInfo.SetValue(toObject, fromInfo.GetValue(fromObject, null), null);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(fromInfo.PropertyType)
                        && !typeof(string).IsAssignableFrom(fromInfo.PropertyType)
                        && toInfo.GetSetMethod() != null)
                    {
                        var toEnumerbale = (IEnumerable)Activator.CreateInstance(toInfo.PropertyType);
                        toInfo.SetValue(toObject, toEnumerbale, null);

                        foreach (Object value in (IEnumerable)fromInfo.GetValue(fromObject, null))
                        {
                            var toChildObject = Activator.CreateInstance(toInfo.PropertyType.GetGenericArguments()[0]);

                            if (typeof(ICollection<>).IsAssignableFrom(toInfo.GetType()))
                            {
                                RefelectiveMap(value, toChildObject);
                                toEnumerbale.GetType().GetMethod("Add").Invoke(toEnumerbale, new object[] { toChildObject });
                            }
                        }


                    }
                    else if (typeof(object).IsAssignableFrom(toInfo.PropertyType)
                        && !typeof(string).IsAssignableFrom(toInfo.PropertyType)
                        && !typeof(int).IsAssignableFrom(toInfo.PropertyType)
                        && !typeof(char).IsAssignableFrom(toInfo.PropertyType)
                        && !typeof(IEnumerable).IsAssignableFrom(toInfo.PropertyType)
                        && toInfo.GetSetMethod() != null)
                    {
                        var toChildObject = Activator.CreateInstance(toInfo.PropertyType);
                        RefelectiveMap(toChildObject, fromInfo.GetValue(fromObject, null));
                        toInfo.SetValue(toObject, toChildObject, null);
                    }
                }
            }
        }
    }
}