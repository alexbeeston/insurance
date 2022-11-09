using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Insurance
{
    class Program
    {
        static void Main(string[] args)
        {
            int momLow = 5000;
            int momHigh = 10000;
            int babyLow = 5000;
            int babyHigh = 10000;
            int stepSize = 500;
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

            List<List<double>> planACosts = new List<List<double>>();
            List<List<double>> planBCosts = new List<List<double>>();

            for (int babysBills = babyLow; babysBills <= babyHigh; babysBills += stepSize)
            {
                planACosts.Add(new List<double>());
                planBCosts.Add(new List<double>());
                for (int momsBill = momLow; momsBill <= momHigh; momsBill += stepSize)
                {
                    planACosts.Last().Add(ComputeCost(planA, momsBill, babysBills));
                    planBCosts.Last().Add(ComputeCost(planB, momsBill, babysBills));
                }
            }

            List<List<double>> diffs = new List<List<double>>();
            for (int i = 0; i < planACosts.Count; i++)
            {
                diffs.Add(new List<double>());
                for (int j = 0; j < planACosts[i].Count; j++)
                {
                    diffs.Last().Add(planBCosts[i][j] - planACosts[i][j]);
                }
            }

            string planACostsTable = GetCsvString(planACosts, momLow, momHigh, babyLow, babyHigh, stepSize);
            string planBCostsTable = GetCsvString(planBCosts, momLow, momHigh, babyLow, babyHigh, stepSize);
            string diffsTable = GetCsvString(diffs, momLow, momHigh, babyLow, babyHigh, stepSize);

            string output = planACostsTable + "\n\n" + planBCostsTable + "\n\n" + diffsTable;
            File.WriteAllText(@"..\..\..\output\data.csv", output);
        }

        static double ComputeCost(Configs configs, double momsBill, double babysBills)
        {
            State state = new State();
            AdjustState(state, configs, momsBill, (int)People.Mom);
            AdjustState(state, configs, babysBills, (int)People.Baby);
            return state.CummulativeFamilyPayment;
        }

        static string GetCsvString(List<List<double>> numbers, int momLow, int momHigh, int babyLow, int babyHigh, int stepSize)
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

            int i = 0;
            for (int babysCosts = babyLow; babysCosts <= babyHigh; babysCosts += stepSize)
            {
                stringBuilder.Append(babysCosts + ",");
                for (int j = 0; j < numbers[i].Count; j++)
                {
                    stringBuilder.Append(numbers[i][j]);
                    if (j + 1< numbers[i].Count)
                    {
                        stringBuilder.Append(",");
                    }
                }
                i++;
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
