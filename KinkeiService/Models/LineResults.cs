using System;
using System.Collections.Generic;
using System.Text;

namespace KinkeiService
{
	public class Line
	{
		public int terminal_number { get; set; }
		public string line_no { get; set; }
		public string line_name { get; set; }
		public List<Terminal> terminals { get; set; }
		public List<object> branch_point_names { get; set; }
	}

	public class LineResultFL
	{
		public List<Line> lines { get; set; }
	}

	public class Terminal
	{
		public int terminal_no { get; set; }
		public string substatio_name { get; set; }
		public string device_no { get; set; }
	}
}
