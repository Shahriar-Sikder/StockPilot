namespace System
{
	public static class ObjectExtensions
	{
		public static bool IsFalse(this bool obj)
		{
			if (obj.IsNotNull() && obj)
			{
				return false;
			}
			return true;
		}

		public static bool IsNotNullOrEmpty(this string obj)
		{
			if (string.IsNullOrEmpty(obj))
			{
				return false;
			}
			return true;
		}

		public static bool IsNotNullOrEmpty(this short obj)
		{
			if (obj.IsNotNull() && obj.ToString() != string.Empty)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotNullOrEmpty(this decimal obj)
		{
			if (obj.IsNotNull() && obj.ToString() != string.Empty)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotNullOrEmpty(this double obj)
		{
			if (obj.IsNotNull() && obj.ToString() != string.Empty)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotNullOrEmpty(this int obj)
		{
			if (obj.IsNotNull() && obj.ToString() != string.Empty)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotZero(this short obj)
		{
			if (obj.IsNotNullOrEmpty() && obj != 0)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotZero(this decimal obj)
		{
			if (obj.IsNotNullOrEmpty() && obj != 0m)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotZero(this double obj)
		{
			if (obj.IsNotNullOrEmpty() && obj != 0.0)
			{
				return true;
			}
			return false;
		}

		public static bool IsNotZero(this int obj)
		{
			if (obj.IsNotNullOrEmpty() && obj != 0)
			{
				return true;
			}
			return false;
		}

		public static short ToShort(this int obj)
		{
			if (obj.IsNotNullOrEmpty())
			{
				return Convert.ToInt16(obj);
			}
			return 0;
		}

		public static string ToUpperOrEmpty(this string obj)
		{
			if (string.IsNullOrEmpty(obj))
			{
				return string.Empty;
			}
			return obj.ToUpper();
		}
	}
}
