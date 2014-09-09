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
        private static Dictionary<String, PropertyInfo> _infoCache = new Dictionary<String, PropertyInfo>();
        private static Dictionary<String, PropertyInfo> _tryinfoCache = new Dictionary<String, PropertyInfo>();
        /// <summary>
        /// Will try to get the Property Info based off the Specified Mapping. If the map is invalid then an Exception is thrown
        /// </summary>
        /// <param name="obj">Object to Try to get the PropertyInfo From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns>Property Info or thows Exception</returns>
        public static PropertyInfo GetEvalPropertyInfo(this Object obj, String propertyString)
        {
            PropertyInfo propInfo = null;
            if (obj != null)
            {
                var type = obj.GetType();

                propInfo = type.GetEvalPropertyInfo(propertyString);
            }
            return propInfo;
        }

        /// <summary>
        /// Will try to get the Property Info based off the Specified Mapping. If the map is invalid then an Exception is thrown
        /// </summary>
        /// <param name="type">Type to Try to get the PropertyInfo From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns>Property Info or thows Exception</returns>
        public static PropertyInfo GetEvalPropertyInfo(this Type type, String propertyString)
        {
            return GetEvalPropertyInfo(type, propertyString, true);
        }


        private static PropertyInfo GetEvalPropertyInfo(this Type type, String propertyString, bool throwError)
        {
            var key = "GetEvalPropertyInfo";

            Delegate getPropInfoDelegate = (Func<Type, String, PropertyInfo>)((Type t, String evalString) =>
            {
                PropertyInfo propInfo = null;
                String[] propertyArray = evalString.Split('.');
                if (t != null)
                {
                    foreach (String propertyName in propertyArray)
                    {
                        propInfo = t.GetProperty(propertyName);
                        if (propInfo != null)
                            t = propInfo.PropertyType;
                        else
                            if (throwError)
                                throw new Exception("Invalid Property String");
                            else
                                return null;
                    }

                    return propInfo;
                }

                return null;
            });



            return (PropertyInfo)Joe.Caching.Cache.Instance.GetOrAdd(key, TimeSpan.MaxValue, getPropInfoDelegate, type, propertyString);
        }

        /// <summary>
        /// Will try to get the Property Info based off the Specified Mapping. If the map is invalid the returns null
        /// </summary>
        /// <param name="type">Type to Try to get the PropertyInfo From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns>Property Info Or null</returns>
        public static PropertyInfo TryGetEvalPropertyInfo(this Type type, String propertyString)
        {
            var key = "TryGetEvalPropertyInfo";

            Delegate tryGetEvalPropertyInfoDelegate = (Func<Type, String, PropertyInfo>)((Type t, String evalString) =>
            {
                PropertyInfo info = null;
                try
                {
                    info = GetEvalPropertyInfo(t, evalString, false);
                }
                catch
                {
                    //Do Nothing
                }
                return info;
            });

            return (PropertyInfo)Joe.Caching.Cache.Instance.GetOrAdd(key, TimeSpan.MaxValue, tryGetEvalPropertyInfoDelegate, type, propertyString);

        }

        /// <summary>
        /// Gets the Value of the Specifed Property
        /// </summary>
        /// <param name="obj">Object to get the Property Value From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns></returns>
        public static Object GetEvalProperty(this Object obj, String propertyString)
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

        /// <summary>
        /// Set the Value of a Property Will Parse on '.' to get to nested properties
        /// </summary>
        /// <param name="obj">Object to set Property Of</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <param name="value">Value to Set the property to</param>
        public static void SetEvalProperty(this Object obj, String propertyString, Object value)
        {
            SetEvalProperty(obj, propertyString, value, null);
        }

        /// <summary>
        /// Set the Value of a Property Will Parse on '.' to get to nested properties
        /// </summary>
        /// <param name="obj">Object to set Property Of</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <param name="value">Value to Set the property to</param>
        /// <param name="ObjectCreated">Trigger when a mapping is to a nested object and that object is null</param>
        public static void SetEvalProperty(this Object obj, String propertyString, Object value, Action<Object, Object, PropertyInfo> ObjectCreated)
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
                //This could be propInfo.SetMethod.IsPublic in .net 4.5
                if (propInfo.CanWrite && propInfo.GetSetMethod(false) != null)
                {
                    if (!propInfo.PropertyType.IsValueType || !(Nullable.GetUnderlyingType(propInfo.PropertyType) != null))
                        value = Convert.ChangeType(value, propInfo.PropertyType);
                    propInfo.SetValue(obj, value, null);
                }
            }
            else
                throw new Exception("Invalid Property String");

        }

        /// <summary>
        /// Simple Reflective Map that will map One Object to Another where Proerpty Names Match
        /// </summary>
        /// <param name="fromObject">Object to Get Value From</param>
        /// <param name="toObject">Object to Set Values To</param>
        public static void RefelectiveMap(this Object fromObject, Object toObject)
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
                        IEnumerable toEnumerbale;
                        if (toInfo.PropertyType.IsClass)
                            toEnumerbale = (IEnumerable)Activator.CreateInstance(toInfo.PropertyType);
                        else
                            toEnumerbale = (IEnumerable)Activator.CreateInstance(typeof(List<>).MakeGenericType(toInfo.PropertyType.GetGenericArguments().First()));

                        toInfo.SetValue(toObject, toEnumerbale, null);

                        var fromEnumerable = (IEnumerable)fromInfo.GetValue(fromObject, null);
                        if (fromEnumerable != null)
                        {
                            foreach (Object value in fromEnumerable)
                            {
                                var toChildObject = Activator.CreateInstance(toInfo.PropertyType.GetGenericArguments()[0]);

                                if (typeof(ICollection<>).IsAssignableFrom(toInfo.GetType()))
                                {
                                    RefelectiveMap(value, toChildObject);
                                    toEnumerbale.GetType().GetMethod("Add").Invoke(toEnumerbale, new object[] { toChildObject });
                                }
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
                        var nestedObject = fromInfo.GetValue(fromObject, null);
                        if (fromObject != null)
                        {
                            RefelectiveMap(toChildObject, nestedObject);
                            toInfo.SetValue(toObject, toChildObject, null);
                        }
                    }
                }
            }
        }

        public static T Clone<T>(this T fromObject)
        {
            var clone = (T)Activator.CreateInstance<T>();

            RefelectiveMap(fromObject, clone);

            return clone;
        }


        /// <summary>
        /// Check to see if type past in is assignable to generic type passed in
        /// </summary>
        /// <param name="givenType">Type to Check</param>
        /// <param name="genericType">Generic to Check Against</param>
        /// <returns></returns>
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }
    }
}