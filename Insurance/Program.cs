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

			AdjustState(state, configs, 1000, (int)People.Mom);
			AdjustState(state, configs, 15000, (int)People.Baby);
		}

		static void AdjustState(State state, Configs configs, double billAmount, int indexOfPerson)
		{
			// State Validation
			if (state.CummulativeFamilyPayment > configs.FamilyMax) throw new Exception("bad state: current cummulative family amount is greater than family max");
			foreach (double cummulativeIndividualPayment in state.CummulativeIndividualPayments) if (cummulativeIndividualPayment > configs.IndividualMax) throw new Exception($"bad state: individual cummulative amount is {cummulativeIndividualPayment}. Shouldn't be greater than {configs.IndividualMax}.");

			// Continue
			if (state.CummulativeFamilyPayment >= configs.FamilyMax || state.CummulativeIndividualPayments[indexOfPerson] >= configs.IndividualMax)
			{
				return;
			}
			else
			{
				bool computeAsIndividual = (configs.IndividualDeduct - state.CummulativeIndividualPayments[indexOfPerson]) <= (configs.FamilyDeduct - state.CummulativeFamilyPayment);
				double currentCummulative = computeAsIndividual ? state.CummulativeIndividualPayments[indexOfPerson] : state.CummulativeFamilyPayment;
				double deductible = computeAsIndividual ? configs.IndividualDeduct : configs.FamilyDeduct;
				double max = computeAsIndividual ? configs.IndividualMax : configs.FamilyMax;

				double amountToPay = currentCummulative < deductible ?
					GetAmountPaidBeforeDeductibleIsMet(billAmount, currentCummulative, deductible, max, configs.CoinsuranceRate) :
					GetAmountPaidAfterDeductibleIsMet(billAmount, max - currentCummulative, configs.CoinsuranceRate);

				amountToPay = computeAsIndividual ?
					Math.Min(amountToPay, configs.FamilyMax - state.CummulativeFamilyPayment) :
					Math.Min(amountToPay, configs.IndividualMax - state.CummulativeIndividualPayments[indexOfPerson]);

				state.CummulativeIndividualPayments[indexOfPerson] += amountToPay;
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
