using System;
using System.Collections.Generic;

namespace Dino.Common.Comparers
{
	public class DelegateEqualityComparer<T> : IEqualityComparer<T>
	{
		private readonly Func<T, T, bool> _equals;
		private readonly Func<T, int> _hashCode;

		public DelegateEqualityComparer(Func<T, T, bool> equals, Func<T, int> hashCode)
		{
			_equals = equals;
			_hashCode = hashCode;
		}

		public bool Equals(T x, T y)
		{
			return _equals(x, y);
		}

		public int GetHashCode(T obj)
		{
			if (_hashCode != null)
				return _hashCode(obj);
			return obj.GetHashCode();
		}
	}
}
