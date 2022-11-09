using System;

namespace Insurance
{
	class Program
	{
		static void Main(string[] args)
		{
			Configs configs = new Configs
			{
				IndividualDeduct = 2500,
				IndividualMax = 7000,
				FamilyDeduct = 5000,
				FamilyMax = 14000,
				CoinsuranceRate = .2
			};
			State state = new State();

			AdjustState(state, configs, 8000, People.Mom);
			AdjustState(state, configs, 3000, People.Baby);
		}

		static void AdjustState(State state, Configs configs, double billAmount, People person)
		{
			// State Validation
			if (state.CummulativeFamilyPayment > configs.FamilyMax) throw new Exception("bad state: current cummulative family amount is greater than family max");
			foreach (double individualAmount in state.CummulativeIndividualPayments) if (individualAmount > configs.IndividualMax) throw new Exception($"bad state: individual cummulative amount is {individualAmount}. Shouldn't be greater than {configs.IndividualMax}.");

			// Continue
			int i = (int)person;
			if (state.CummulativeFamilyPayment >= configs.FamilyMax || state.CummulativeIndividualPayments[i] >= configs.IndividualMax)
			{
				return;
			}
			else
			{
				// which deductible will get hit first?


				double paymentByIndividual = state.CummulativeIndividualPayments[i] < configs.IndividualDeduct ?
					GetAmountPaidBeforeDeductibleIsMet(billAmount, state.CummulativeIndividualPayments[i], configs.IndividualDeduct, configs.IndividualMax, configs.CoinsuranceRate) :
					GetAmountPaidAfterDeductibleIsMet(billAmount, configs.IndividualMax - state.CummulativeIndividualPayments[i], configs.CoinsuranceRate);
				if (state.CummulativeIndividualPayments[i] + paymentByIndividual > configs.IndividualMax) throw new Exception("calculated individual payment is greater than individual max out of pocket");

				double paymentByFamily = state.CummulativeFamilyPayment < configs.FamilyDeduct ?
					GetAmountPaidBeforeDeductibleIsMet(billAmount, state.CummulativeFamilyPayment, configs.FamilyDeduct, configs.FamilyMax, configs.CoinsuranceRate) :
					GetAmountPaidAfterDeductibleIsMet(billAmount, configs.FamilyMax - state.CummulativeFamilyPayment, configs.CoinsuranceRate);
				if (state.CummulativeFamilyPayment + paymentByFamily > configs.FamilyMax) throw new Exception("calculated family payment is greater than individual max out of pocket");

				state.CummulativeIndividualPayments[i] += Math.Min(paymentByIndividual, paymentByFamily);
			}
		}

		static double GetAmountPaidBeforeDeductibleIsMet(double billAmount, double currentCummulativeAmount, double deductible, double max, double coIsuranceRate)
		{
			if (currentCummulativeAmount + billAmount <= deductible)
			{
				// this bill will won't put you over the deductible, so you have to shell it out yourself
				return billAmount;
			}
			else
			{
				// this bill will put you over the deductible
				double amountTillDeductible = deductible - currentCummulativeAmount;
				double remainingBill = billAmount - amountTillDeductible;
				return amountTillDeductible + GetAmountPaidAfterDeductibleIsMet(remainingBill, max - deductible, coIsuranceRate);
			}
		}
		static double GetAmountPaidAfterDeductibleIsMet(double billAmountSubjectToCoinsurance, double amountTillOutofPocketMax, double coInsuranceRate)
		{
			return Math.Min(billAmountSubjectToCoinsurance * coInsuranceRate, amountTillOutofPocketMax);
		}
	}
}
