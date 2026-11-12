using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace System
{
	public static class ListExtensions
	{
		public static bool IsNull<T>(this List<T> list)
		{
			if (list == null)
			{
				return false;
			}
			return true;
		}

		public static bool IsNull(this object obj)
		{
			if (obj == null)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotNull(this object obj)
		{
			if (obj == null)
			{
				return false;
			}
			return true;
		}

		public static bool IsNotNullNHaveValue<T>(this List<T> list)
		{
			if (list != null && list.Count > 0)
			{
				return true;
			}
			return false;
		}

		public static bool HasObjects<T>(this List<T> list)
		{
			if (list != null && list.Count > 0)
			{
				return true;
			}
			return false;
		}

		public static bool HasNoObjects<T>(this List<T> list)
		{
			if (list != null && list.Count > 0)
			{
				return false;
			}
			return true;
		}

		public static List<T> ToList<T>(this DataTable table) where T : new()
		{
			IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
			List<T> result = new List<T>();
			foreach (DataRow row in table.Rows)
			{
				T item = CreateItemFromRow<T>(row, properties);
				result.Add(item);
			}
			return result;
		}

		private static bool IsNullableType(Type type)
		{
			if (type.IsGenericType)
			{
				return type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
			}
			return false;
		}

		private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties) where T : new()
		{
			T item = new T();
			foreach (PropertyInfo property in properties)
			{
				if (row.Table.Columns.Contains(property.Name))
				{
					if (property.PropertyType == typeof(DayOfWeek))
					{
						DayOfWeek day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), row[property.Name].ToString());
						property.SetValue(item, day, null);
						continue;
					}
					if (row[property.Name] == DBNull.Value)
					{
						property.SetValue(item, null, null);
						continue;
					}
					object propertyVal = row[property.Name];
					Type propertyType = item.GetType().GetProperty(property.Name).PropertyType;
					Type targetType = (IsNullableType(propertyType) ? Nullable.GetUnderlyingType(propertyType) : propertyType);
					propertyVal = Convert.ChangeType(propertyVal, targetType);
					property.SetValue(item, propertyVal, null);
				}
				else
				{
					property.SetValue(item, null, null);
				}
			}
			return item;
		}
	}
}
