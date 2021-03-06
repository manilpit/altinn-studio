using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altinn.Studio.Designer.RepositoryClient.JsonSubTypes
{
    // Copied from project https://github.com/manuc66/JsonSubTypes
    // https://raw.githubusercontent.com/manuc66/JsonSubTypes/07403192ea3f4959f6d42f5966ac56ceb0d6095b/JsonSubTypes/JsonSubtypes.cs

    /// <summary>
    /// A discriminated Json sub-type Converter implementation for .NET
    /// </summary>
    public class JsonSubtypes : JsonConverter
    {
        /// <summary>
        /// Known sub type attributes
        /// </summary>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
        public class KnownSubTypeAttribute : Attribute
        {
            /// <summary>
            /// Gets the sub type
            /// </summary>
            public Type SubType { get; private set; }

            /// <summary>
            /// Gets the associated value
            /// </summary>
            public object AssociatedValue { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="KnownSubTypeAttribute"/> class
            /// </summary>
            /// <param name="subType">the sub type</param>
            /// <param name="associatedValue">the associated value</param>
            public KnownSubTypeAttribute(Type subType, object associatedValue)
            {
                SubType = subType;
                AssociatedValue = associatedValue;
            }
        }

        /// <summary>
        /// known type with property attribute
        /// </summary>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
        public class KnownSubTypeWithPropertyAttribute : Attribute
        {
            /// <summary>
            /// Gets the sub type
            /// </summary>
            public Type SubType { get; private set; }

            /// <summary>
            /// Gets the property name
            /// </summary>
            public string PropertyName { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="KnownSubTypeWithPropertyAttribute"/> class
            /// </summary>
            /// <param name="subType">the sub type</param>
            /// <param name="propertyName">the property name</param>
            public KnownSubTypeWithPropertyAttribute(Type subType, string propertyName)
            {
                SubType = subType;
                PropertyName = propertyName;
            }
        }

        private readonly string _typeMappingPropertyName;

        private bool _isInsideRead;
        private JsonReader _reader;

        /// <inheritdoc/>
        public override bool CanRead
        {
            get
            {
                if (!_isInsideRead)
                {
                    return true;
                }

                return !string.IsNullOrEmpty(_reader.Path);
            }
        }

        /// <inheritdoc/>
        public sealed override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSubtypes"/> class
        /// </summary>
        public JsonSubtypes()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSubtypes"/> class
        /// </summary>
        /// <param name="typeMappingPropertyName">the type mapping property</param>
        public JsonSubtypes(string typeMappingPropertyName)
        {
            _typeMappingPropertyName = typeMappingPropertyName;
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return _typeMappingPropertyName != null;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Comment)
            {
                reader.Read();
            }

            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartArray:
                    return ReadArray(reader, objectType, serializer);
                case JsonToken.StartObject:
                    return ReadObject(reader, objectType, serializer);
                default:
                    throw new Exception("Array: Unrecognized token: " + reader.TokenType);
            }
        }

        private IList ReadArray(JsonReader reader, Type targetType, JsonSerializer serializer)
        {
            var elementType = GetElementType(targetType);

            var list = CreateCompatibleList(targetType, elementType);

            while (reader.TokenType != JsonToken.EndArray && reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Null:
                        list.Add(reader.Value);
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.StartObject:
                        list.Add(ReadObject(reader, elementType, serializer));
                        break;
                    case JsonToken.EndArray:
                        break;
                    default:
                        throw new Exception("Array: Unrecognized token: " + reader.TokenType);
                }
            }

            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(targetType.GetElementType(), list.Count);
                list.CopyTo(array, 0);
                list = array;
            }

            return list;
        }

        private static IList CreateCompatibleList(Type targetContainerType, Type elementType)
        {
            IList list;
            if (targetContainerType.IsArray || targetContainerType.GetTypeInfo().IsAbstract)
            {
                list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            }
            else
            {
                list = (IList)Activator.CreateInstance(targetContainerType);
            }

            return list;
        }

        private static Type GetElementType(Type arrayOrGenericContainer)
        {
            Type elementType;
            if (arrayOrGenericContainer.IsArray)
            {
                elementType = arrayOrGenericContainer.GetElementType();
            }
            else
            {
                elementType = arrayOrGenericContainer.GenericTypeArguments[0];
            }

            return elementType;
        }

        private object ReadObject(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var targetType = GetType(jObject, objectType) ?? objectType;

            return ReadJsonObject(CreateAnotherReader(jObject, reader), targetType, null, serializer);
        }

        private static JsonReader CreateAnotherReader(JObject jObject, JsonReader reader)
        {
            var jObjectReader = jObject.CreateReader();
            jObjectReader.Culture = reader.Culture;
            jObjectReader.CloseInput = reader.CloseInput;
            jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
            jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jObjectReader.FloatParseHandling = reader.FloatParseHandling;
            jObjectReader.DateFormatString = reader.DateFormatString;
            jObjectReader.DateParseHandling = reader.DateParseHandling;
            return jObjectReader;
        }

        /// <summary>
        /// Get type of json object
        /// </summary>
        /// <param name="jObject">the json oject</param>
        /// <param name="parentType">the parent type</param>
        /// <returns>The type</returns>
        public Type GetType(JObject jObject, Type parentType)
        {
            if (_typeMappingPropertyName == null)
            {
                return GetTypeByPropertyPresence(jObject, parentType);
            }

            return GetTypeFromDiscriminatorValue(jObject, parentType);
        }

        private static Type GetTypeByPropertyPresence(JObject jObject, Type parentType)
        {
            foreach (var type in parentType.GetTypeInfo().GetCustomAttributes<KnownSubTypeWithPropertyAttribute>())
            {
                JToken ignore;
                if (jObject.TryGetValue(type.PropertyName, out ignore))
                {
                    return type.SubType;
                }
            }

            return null;
        }

        private Type GetTypeFromDiscriminatorValue(JObject jObject, Type parentType)
        {
            JToken jToken;
            if (!jObject.TryGetValue(_typeMappingPropertyName, out jToken))
            {
                return null;
            }

            var discriminatorValue = jToken.ToObject<object>();
            if (discriminatorValue == null)
            {
                return null;
            }

            var typeMapping = GetSubTypeMapping(parentType);
            if (typeMapping.Any())
            {
                return GetTypeFromMapping(typeMapping, discriminatorValue);
            }

            return GetTypeByName(discriminatorValue as string, parentType);
        }

        private static Type GetTypeByName(string typeName, Type parentType)
        {
            if (typeName == null)
            {
                return null;
            }

            var insideAssembly = parentType.GetTypeInfo().Assembly;

            var typeByName = insideAssembly.GetType(typeName);
            if (typeByName == null)
            {
                var searchLocation = parentType.FullName.Substring(0, parentType.FullName.Length - parentType.Name.Length);
                typeByName = insideAssembly.GetType(searchLocation + typeName, false, true);
            }

            return typeByName;
        }

        private static Type GetTypeFromMapping(IReadOnlyDictionary<object, Type> typeMapping, object discriminatorValue)
        {
            var targetlookupValueType = typeMapping.First().Key.GetType();
            var lookupValue = ConvertJsonValueToType(discriminatorValue, targetlookupValueType);

            Type targetType;
            return typeMapping.TryGetValue(lookupValue, out targetType) ? targetType : null;
        }

        private static Dictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return type.GetTypeInfo().GetCustomAttributes<KnownSubTypeAttribute>().ToDictionary(x => x.AssociatedValue, x => x.SubType);
        }

        private static object ConvertJsonValueToType(object objectType, Type targetlookupValueType)
        {
            if (targetlookupValueType.GetTypeInfo().IsEnum)
            {
                return Enum.ToObject(targetlookupValueType, objectType);
            }

            return Convert.ChangeType(objectType, targetlookupValueType);
        }

        /// <summary>
        /// read json object
        /// </summary>
        /// <param name="reader">the json reader</param>
        /// <param name="objectType">the type</param>
        /// <param name="existingValue">existing value</param>
        /// <param name="serializer">the json serializer</param>
        /// <returns>returns deserialized object?</returns>
        protected object ReadJsonObject(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            _reader = reader;
            _isInsideRead = true;
            try
            {
                return serializer.Deserialize(reader, objectType);
            }
            finally
            {
                _isInsideRead = false;
            }
        }
    }
}
