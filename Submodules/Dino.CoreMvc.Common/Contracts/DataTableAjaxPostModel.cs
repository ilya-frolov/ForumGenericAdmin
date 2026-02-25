using System.Collections.Generic;

namespace Dino.Mvc.Common.Contracts
{
	public class DataTableAjaxPostModel
	{
		public int draw { get; set; }
		
		/// <summary>
		/// The start index. Used for "Skip".
		/// </summary>
		public int start { get; set; }

		/// <summary>
		/// How many items to get. Used for "Take".
		/// </summary>
		public int length { get; set; }

		public List<Column> columns { get; set; }

		public Search search { get; set; }

		public List<Order> order { get; set; }

		public class Column
		{
			public string data { get; set; }
			public string name { get; set; }
			public bool searchable { get; set; }
			public bool orderable { get; set; }
			public Search search { get; set; }
		}

		public class Search
		{
			public string value { get; set; }
			public string regex { get; set; }
		}

		public class Order
		{
			public int column { get; set; }
			public string dir { get; set; }
		}
	}
}
