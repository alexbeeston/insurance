using System;
using System.Collections.Generic;
using System.Text;

namespace Insurance
{
	class Configs
	{
		public double IndividualDeduct { get; set; }
		public double IndividualMax { get; set; }
		public double FamilyDeduct { get; set; }
		public double FamilyMax { get; set; }

		/// <summary>
		/// The percentage we pay (probably 20%)
		/// </summary>
		public double CoinsuranceRate { get; set; }
	}
}
