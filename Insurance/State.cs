using System;
using System.Collections.Generic;
using System.Text;

namespace Insurance
{
	class State
	{
		public State()
		{
			CummulativeIndividualPayments = new List<double>() { 0, 0 };
		}

		public List<double> CummulativeIndividualPayments { get; set; }
		public double CummulativeFamilyPayment
		{
			get
			{
				double sum = 0;
				CummulativeIndividualPayments.ForEach(x => sum += x);
				return sum;
			}
		}

		public static int PersonToIndex(People person)
		{
			return (int)person;
		}
	}

	enum People
	{
		Mom = 0,
		Baby = 1
	}
}
