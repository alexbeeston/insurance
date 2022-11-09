using System;
using System.Text;
using System.IO;

namespace Insurance
{
    class Program
    {
        static void Main(string[] args)
        {
            int momLow = 6000;
            int momHigh = 30000;
            int babyLow = 2500;
            int babyHigh = 30000;
            int stepSize = 1000;
            Configs planA = new Configs
            {
                IndividualDeduct = 2500,
                IndividualMax = 7000,
                FamilyDeduct = 5000,
                FamilyMax = 14000,
                CoinsuranceRate = .2
            };
            Configs planB = new Configs
            {
                IndividualDeduct = 4500,
                IndividualMax = 4500,
                FamilyDeduct = 9000,
                FamilyMax = 9000,
                CoinsuranceRate = 0
            };

            string planACsv = GetCsvString(planA, momLow, momHigh, babyLow, babyHigh, stepSize);
            string planBCsv = GetCsvString(planB, momLow, momHigh, babyLow, babyHigh, stepSize);
            string finalString = planACsv + "\n\n\n" + planBCsv;
            File.WriteAllText(@"..\..\..\output\data.csv", finalString);
        }

        static string GetCsvString(Configs configs, int momLow, int momHigh, int babyLow, int babyHigh, int stepSize)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(",");
            for (int momsHeader = momLow; momsHeader <= momHigh; momsHeader += stepSize)
            {
                stringBuilder.Append(momsHeader);
                if (momsHeader + stepSize <= momHigh)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append("\n");

            for (int babysCosts = babyLow; babysCosts <= babyHigh; babysCosts += stepSize)
            {
                stringBuilder.Append(babysCosts + ",");
                for (int momsCosts = momLow; momsCosts <= momHigh; momsCosts += stepSize)
                {
                    State state = new State();
                    AdjustState(state, configs, momsCosts, (int)People.Mom);
                    AdjustState(state, configs, babysCosts, (int)People.Baby);
                    stringBuilder.Append(state.CummulativeFamilyPayment);
                    if (momsCosts + stepSize <= momHigh)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append("\n");
            }
            return stringBuilder.ToString();
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
