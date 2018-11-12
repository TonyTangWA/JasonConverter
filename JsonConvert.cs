using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Globalization;

namespace JsonConvert.Sample {
   public  class JsonConvert : JavaScriptConverter {

      
        #region Abstract JavaScriptConverter Overrides Members
        public override IEnumerable<Type> SupportedTypes {
            get {
                return new ReadOnlyCollection<Type>((new List<Type>(new Type[] { 
                    typeof(AbEntry), 
                    //typeof(AbEntryObjectField), 
                    //typeof(GetConnectWizardModel),
                    //typeof(GetConnectWizardObjectField),
                    typeof(MostImportantFieldModel),
                    //typeof(MostImportantFieldObjectField),
                    //typeof(Options),
                    //typeof(CompareOperators),
                    //typeof(Searchquery),
                    //typeof(CompareFields),
                    typeof(ContactModel),
                    //typeof(ContactObjectField),
                    typeof(CompanyModel),
                    //typeof(CompanyObjectField),
                    typeof(AddressModel),
                    typeof(OpportunityModel),
                    //typeof(OpportunityObjectField)
                })));
            }
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer) {
            var result = new Dictionary<string, object>();

            if (obj == null)
                return result;

            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            //PropertyInfo[] properties = this.GetType().GetProperties(bindingFlags);

            PropertyInfo[] properties = obj.GetType().GetProperties(bindingFlags);

            foreach (PropertyInfo property in properties) {
                KeyValuePair<string, object> kvp = this.GetSerializedProperty(obj, property);
                if (!string.IsNullOrEmpty(kvp.Key))
                    result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer) {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (type == null)
                throw new ArgumentNullException("type");

            if (serializer == null)
                throw new ArgumentNullException("serializer");

            // This will fail if type has no default constructor.
            object result = Activator.CreateInstance(type);

            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            PropertyInfo[] properties = result.GetType().GetProperties(bindingFlags);

            foreach (PropertyInfo property in properties) {
                SetDerializedProperty(result, property, dictionary, serializer);
            }

            return result;
        }

        
        #endregion Abstract JavaScriptConverter Overrides Members

        #region protected methods
        protected virtual KeyValuePair<string, object> GetSerializedProperty(object obj, PropertyInfo property) {
            var result = new KeyValuePair<string, object>();

            if (property == null || !property.CanRead) {
                return result;
            }
            
            object value = property.GetValue(obj);

            JsonClassAttribute jsonClassAttribute = GetJsonClassAttribute(obj);
            bool ignoreNull = true;
            if (jsonClassAttribute != null) {
                ignoreNull = !jsonClassAttribute.SerializeNullValues;
            }

            if (value == null && ignoreNull) {
                return result;
            }
                       

            JsonNameAttribute jsonPropertyAttribute = GetJsonPropertyAttribute(property);
            if (jsonPropertyAttribute == null || jsonPropertyAttribute.Ignored) {
                return result;
            }

            bool ignoreDefault = true;
            if (jsonClassAttribute != null) {
                ignoreDefault = !jsonClassAttribute.SerializeDefaultValues;
            }

            if (IsNullOrDefault(value) && ignoreDefault) {
                return result;
            }

            if (property.PropertyType == typeof(DateTime)) {
                value = ((DateTime)value).ToString("G", DateTimeFormatInfo.InvariantInfo);
            }
           


            string name = jsonPropertyAttribute.PropertyName;
            return new KeyValuePair<string, object>(name, value);
        }

        protected bool IsNullOrDefault<T>(T value) {

            if (value == null)
                return true;

            var actualType = value.GetType();

            if (actualType.IsValueType)
                return value.Equals(Activator.CreateInstance(actualType));

            return false;
        }
        

       /// <summary>
        /// Get the Class Attribute e.g. [JsonClass(SerializeNullValues=true)]
       /// </summary>
       /// <param name="property"></param>
       /// <returns></returns>
 
       protected JsonClassAttribute GetJsonClassAttribute(object obj) {
           JsonClassAttribute jsonClassAttribute = null; 
           if (obj == null) {
               return jsonClassAttribute;
            }

            object[] attributes = obj.GetType().GetCustomAttributes(true);

           
            foreach (object attribute in attributes) {
                if (attribute is JsonClassAttribute) {
                    jsonClassAttribute = (JsonClassAttribute)attribute;
                }
            }

            return jsonClassAttribute;
        }

       /// <summary>
       /// Get the Property Attribute e.g. [JsonProperty(PropertyName = "Udf/$TYPEID(252)")]
       /// </summary>
       /// <param name="property"></param>
       /// <returns></returns>
       protected JsonNameAttribute GetJsonPropertyAttribute(PropertyInfo property) {
            if (property == null) {
                throw new ArgumentNullException("property");
            }

            object[] attributes = property.GetCustomAttributes(true);

            JsonNameAttribute jsonPropertyAttribute = null;
            bool ignore = false;

            foreach (object attribute in attributes) {
                if (attribute is ScriptIgnoreAttribute) {
                    ignore = true;
                }

                if (attribute is JsonNameAttribute) {
                    jsonPropertyAttribute = (JsonNameAttribute)attribute;
                }
            }

            JsonNameAttribute result = jsonPropertyAttribute ?? new JsonNameAttribute();
            result.Ignored |= ignore;

            if (string.IsNullOrWhiteSpace(result.PropertyName)) {
                result.PropertyName = property.Name;
            }


            return result;
        }

        public virtual void SetDerializedProperty(object obj, PropertyInfo property, IDictionary<string, object> dictionary, JavaScriptSerializer serializer) {
            if (obj == null || property == null || !property.CanWrite || dictionary == null || serializer == null)
                return;

            JsonNameAttribute jsonPropertyAttribute = GetJsonPropertyAttribute(property);
            if (jsonPropertyAttribute == null || jsonPropertyAttribute.Ignored)
                return;

            string name = jsonPropertyAttribute.PropertyName;
            if (!dictionary.ContainsKey(name))
                return;

            object value = dictionary[name];

            // Important! Use JavaScriptSerializer.ConvertToType so that V3JsonConvert of properties of this class are called recursively. 
            object convertedValue = serializer.ConvertToType(value, property.PropertyType);
            property.SetValue(obj, convertedValue);
        }
        #endregion protected methods

   }


    [AttributeUsage(AttributeTargets.Class)]
    public class JsonClassAttribute : Attribute {

        /// <summary>
        /// By default, properties with value null are not serialized. Set this to true to avoid that behavior.
        /// </summary>
        public bool SerializeNullValues { get; set; }

        /// <summary>
        /// By default, properties with default value are not serialized. Set this to true to avoid that behavior.
        /// Remember this will over write the SerializeNullValues settings.
        /// </summary>
        public bool SerializeDefaultValues { get; set; }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class JsonNameAttribute : Attribute {
        
        public bool Ignored        { get; set; }
        public string PropertyName { get; set; }
        

        public JsonNameAttribute() {
        }

        public JsonNameAttribute(string propertyName) {
            this.PropertyName = propertyName;
        }
    }


}
