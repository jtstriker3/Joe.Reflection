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
        /// <summary>
        /// Will try to get the Property Info based off the Specified Mapping. If the map is invalid then an Exception is thrown
        /// </summary>
        /// <param name="obj">Object to Try to get the PropertyInfo From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns>Property Info or thows Exception</returns>
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

        /// <summary>
        /// Will try to get the Property Info based off the Specified Mapping. If the map is invalid then an Exception is thrown
        /// </summary>
        /// <param name="type">Type to Try to get the PropertyInfo From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns>Property Info or thows Exception</returns>
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

        /// <summary>
        /// Will try to get the Property Info based off the Specified Mapping. If the map is invalid the returns null
        /// </summary>
        /// <param name="type">Type to Try to get the PropertyInfo From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns>Property Info Or null</returns>
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

        /// <summary>
        /// Gets the Value of the Specifed Property
        /// </summary>
        /// <param name="obj">Object to get the Property Value From</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <returns></returns>
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

        /// <summary>
        /// Set the Value of a Property Will Parse on '.' to get to nested properties
        /// </summary>
        /// <param name="obj">Object to set Property Of</param>
        /// <param name="propertyString">Mapping might be something like Person.Name</param>
        /// <param name="value">Value to Set the property to</param>
        public static void SetEvalProperty(Object obj, String propertyString, Object value)
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

        /// <summary>
        /// Check to see if type past in is assignable to generic type passed in
        /// </summary>
        /// <param name="givenType">Type to Check</param>
        /// <param name="genericType">Generic to Check Against</param>
        /// <returns></returns>
        public static bool IsAssignableToGenericType(Type givenType, Type genericType)
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