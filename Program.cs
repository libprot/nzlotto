using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LottoGraph
{
    class Program
    {
        public class Digits: List<Digit>
        {
            public double GetNumWeight(int num)
            {
                int totalCount = this.Select((x) => x.GetCount()).Sum();
                int numCount = this.Select((x) => x.GetNumCount(num)).Sum();

                return (double)numCount / totalCount;
            }

            public void LoadData(List<int> data)
            {
                for (var i = Count; i < data.Count; i++)
                {
                    var digit = new Digit();
                    digit.Begin();
                    Add(digit);
                }
                for (var i = 0; i < data.Count; i++)
                {
                    this[i].Add(data[i]);
                }
            }

            public void FinishLoading()
            {
                for(var i = 0; i < Count; i++)
                {
                    this[i].End();
                }
            }
        }

        public class Distance
        {
            public Distance()
            {
            }

            private List<int> offsets = new List<int>();

            public int GetLastOffset()
            {
                return offsets.Last();
            }

            private List<int> Normalized()
            {
                var result = new List<int>();
                for(var i = 1; i < offsets.Count; i++)
                {
                    var delta =  offsets[i] - offsets[i - 1];
                    result.Add(delta);
                }
                return result;
            }

            public void Load(int num)
            {
                offsets.Add(num);
            }

            public int min {
                get
                {
                    var items = Normalized();
                    if (items.Count == 0)
                        return 400;
                    return items.Min();
                }
            }
            public int max
            {
                get
                {
                    var items = Normalized();
                    if (items.Count == 0)
                        return 400;
                    return items.Max();
                }
            }
            public int average
            {
                get
                {
                    var items = Normalized();
                    if (items.Count == 0)
                        return 400;
                    return (int)items.Average();
                }
            }
        }

        public class Digit
        {
            private static readonly RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();

            public Random rnd { private get; set; }
            private List<int> historicData;
            private KeyValuePair<int, int>[] stats;
            private int min;
            private int max;
            public Dictionary<int, Distance> distance = new Dictionary<int, Distance>();

            public int GetCount()
            {
                return historicData.Count;
            }

            public int GetNumCount(int num)
            {
                return historicData.Where((x) => x == num).Count();
            }

            private void CalculateDistance(int num, int offset)
            {
                if (!distance.ContainsKey(num))
                {
                    distance[num] = new Distance();
                }
                distance[num].Load(offset);
            }

            public int GetProbability(int num)
            {
                if (!distance.ContainsKey(num))
                    return 1;
                var last = historicData.Count - distance[num].GetLastOffset() + 1;

                if (distance[num].min > last)
                    return 1;
                if (distance[num].max < last)
                    return 99;

                if (distance[num].average > last)
                {
                    return 50 * last / distance[num].average;
                } else
                {
                    var newOffs =  50 * last / distance[num].max;
                    newOffs += 50;
                    if (newOffs > 99)
                        newOffs = 99;
                    return newOffs;
                }
            }

            public void Begin()
            {
                if (historicData == null)
                    historicData = new List<int>();
                historicData.Clear();
            }

            public void Add(int data)
            {
                historicData.Add(data);
            }

            public int RandomNumber(int val)
            {
                var d = val + 1;
                var randomNumber = new byte[4];

                provider.GetBytes(randomNumber);
                int v = (Math.Abs(BitConverter.ToInt32(randomNumber, 0)) % d) + min;
                return v;
            }

            public int Random()
            {
                if (rnd == null)
                    rnd = new Random();

                var total = (from item in stats
                             select item.Value).Sum();
                var index = RandomNumber(total);
                var currIndex = 0;

                foreach (var item in stats)
                {
                    if (index >= currIndex && index < currIndex + item.Value)
                        return item.Key;

                    currIndex += item.Value;
                }

                return 0;
            }

            public void End()
            {
                var stats = new Dictionary<int, int>();
                min = int.MaxValue;
                max = int.MinValue;

                for (var index = 0; index < historicData.Count; index++)
                {
                    var item = historicData[index];

                    if (stats.ContainsKey(item))
                        stats[item]++;
                    else
                        stats[item] = 1;

                    if (item < min)
                        min = item;
                    if (item > max)
                        max = item;

                }

                for(var index = historicData.Count - 1; index >= 0; index--)
                {
                    var item = historicData[index];

                    CalculateDistance(item, historicData.Count - index - 1);
                }

                var totalCount = stats.Count;
                double koef = (double)(max - min + 1) / totalCount;

                for (var i = min; i < max + 1; i++)
                {
                    if (!stats.ContainsKey(i))
                        stats[i] = 1;
                    else
                        stats[i] = (int)(stats[i] * koef);
                }

                var sorted = from stat in stats
                             orderby stat.Key
                             select stat;

                this.stats = sorted.ToArray();
            }
        }

        public static List<int> StringToPattern(string a)
        {
            var result = new List<int>();
            var items = a.Split(',');

            foreach (var item in items)
            {
                result.Add(int.Parse(item));
            }

            return result;
        }

        public static string PatternToString(List<int> a)
        {
            var result = new StringBuilder();

            for (var i = 0; i < a.Count; i++)
            {
                result.Append(a[i]);
                if (result.Length != 0 && i != a.Count - 1)
                    result.Append(",");
            }

            return result.ToString();
        }

        public static int isWinningNumbers(List<int> CurrentNumbers, List<int> winning, out bool isLastNumber)
        {
            int match = 0;
            var limitedCurrent = CurrentNumbers.GetRange(0, 6);

            for (var i = 0; i < limitedCurrent.Count; i++)
            {
                var maxRange = Math.Min(7, winning.Count - 1);
                if (winning.GetRange(0, maxRange).Contains(limitedCurrent[i]))
                {
                    match++;
                }
            }

            isLastNumber = winning.Count >= 7 ? limitedCurrent.Contains(winning[6]) : false;

            return match;
        }

        static void Main(string[] args)
        {
            var path = @"C:\temp\NZ Lotto.csv";

            using (var file = File.Open(path, FileMode.Open))
            {
                using (var stream = new StreamReader(file))
                {
                    string line;
                    int lineNumber = 0;

                    var grapth = new List<Dictionary<int, Dictionary<int, int>>>();

                    for(var i = 0; i < 5; i++)
                    {
                        grapth.Add(new Dictionary<int, Dictionary<int, int>>());
                    }

                    int minSum = int.MaxValue;
                    int maxSum = int.MinValue;
                    var digits = new Digits();
                    var overallProb = new Dictionary<int, int>();
                    var ticketsToSkip = 0;

                    var win_ticket = new List<int>(new int[] { 13, 18, 19, 32, 33, 39, 02, 06 });

                    do
                    {
                        var history = new List<int>();
                        line = stream.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var splitted = line.Split(",");

                            if (lineNumber != 0)
                            {
                                history.Add(int.Parse(splitted[2]));
                                history.Add(int.Parse(splitted[3]));
                                history.Add(int.Parse(splitted[4]));
                                history.Add(int.Parse(splitted[5]));
                                history.Add(int.Parse(splitted[6]));
                                history.Add(int.Parse(splitted[7]));
                            }

                            if (ticketsToSkip != 0 && lineNumber == ticketsToSkip)
                                win_ticket = history;

                            if (lineNumber++ <= ticketsToSkip)
                                continue;

                            foreach (var i in history)
                            {
                                if (!overallProb.ContainsKey(i))
                                    overallProb[i] = 0;
                                overallProb[i]++;
                            }

                            digits.LoadData(history);

                            var sum = history.Sum();
                            if (minSum > sum)
                                minSum = sum;
                            if (maxSum < sum)
                                maxSum = sum;

                            if (!grapth[0].ContainsKey(history[0]))
                                grapth[0][history[0]] = new Dictionary<int, int>();
                            if (!grapth[0][history[0]].ContainsKey(history[1]))
                                grapth[0][history[0]][history[1]] = 0;
                            grapth[0][history[0]][history[1]]++;

                            if (!grapth[1].ContainsKey(history[1]))
                                grapth[1][history[1]] = new Dictionary<int, int>();
                            if (!grapth[1][history[1]].ContainsKey(history[2]))
                                grapth[1][history[1]][history[2]] = 0;
                            grapth[1][history[1]][history[2]]++;

                            if (!grapth[2].ContainsKey(history[2]))
                                grapth[2][history[2]] = new Dictionary<int, int>();
                            if (!grapth[2][history[2]].ContainsKey(history[3]))
                                grapth[2][history[2]][history[3]] = 0;
                            grapth[2][history[2]][history[3]]++;

                            if (!grapth[3].ContainsKey(history[3]))
                                grapth[3][history[3]] = new Dictionary<int, int>();
                            if (!grapth[3][history[3]].ContainsKey(history[4]))
                                grapth[3][history[3]][history[4]] = 0;
                            grapth[3][history[3]][history[4]]++;

                            if (!grapth[4].ContainsKey(history[4]))
                                grapth[4][history[4]] = new Dictionary<int, int>();
                            if (!grapth[4][history[4]].ContainsKey(history[5]))
                                grapth[4][history[4]][history[5]] = 0;
                            grapth[4][history[4]][history[5]]++;
                        }

                    } while (line != null);

                    Console.WriteLine(PatternToString(win_ticket));

                    var maxNumbers = new int[] { 35, 36, 37, 38, 39, 40 };
                    var minNumbers = new int[] { 1, 2, 3, 4, 5, 6 };

                    for (var i = 0; i < grapth.Count; i++)
                    {
                        var minVal = grapth[i].Select((x) => x.Key).Min();
                        var maxVal = grapth[i].Select((x) => x.Key).Max();

                        if (maxVal < maxNumbers[i])
                        {
                            maxVal = maxNumbers[i];
                        }
                        if (minVal > minNumbers[i])
                        {
                            minVal = minNumbers[i];
                        }
                        for (var val = minVal; val <= maxVal; val++)
                        {
                            if (!grapth[i].ContainsKey(val))
                            {
                                grapth[i][val] = new Dictionary<int, int>();
                                grapth[i][val][val + 1] = 0;
                            }
                        }

                        foreach (var item in grapth[i])
                        {
                            var minKey = item.Value.Select((x) => x.Key).Min();
                            var maxKey = item.Value.Select((x) => x.Key).Max();

                            if (maxKey < maxNumbers[i + 1])
                                maxKey = maxNumbers[i + 1];
                            if (minKey > item.Key + 1)
                            {
                                minKey = item.Key + 1;
                            }

                            for(var subItem = minKey; subItem <= maxKey; subItem++)
                            {
                                if (!item.Value.ContainsKey(subItem))
                                {
                                    item.Value[subItem] = 0;
                                }
                            }
                        }
                    }

                    digits.FinishLoading();

                    var numz = new double[] {
                        1 /digits.GetNumWeight(1),
                        1 / digits.GetNumWeight(2),
                        1 / digits.GetNumWeight(11),
                        1 / digits.GetNumWeight(17),
                        1 / digits.GetNumWeight(30),
                        1 / digits.GetNumWeight(35)
                    };

                    Console.WriteLine(1 /digits.GetNumWeight(1) + " " + digits[0].GetProbability(1));
                    Console.WriteLine(1 / digits.GetNumWeight(2) + " " + digits[0].GetProbability(2));
                    Console.WriteLine(1 / digits.GetNumWeight(11) + " " + digits[0].GetProbability(11));
                    Console.WriteLine(1 / digits.GetNumWeight(17) + " " + digits[0].GetProbability(17));
                    Console.WriteLine(1 / digits.GetNumWeight(30) + " " + digits[0].GetProbability(30));
                    Console.WriteLine(1 / digits.GetNumWeight(35) + " " + digits[0].GetProbability(35));

                    Console.WriteLine(numz.Average());

                    for (var i = 0; i < grapth.Count; i++)
                    {
                        var firstNumberCandidates = new Dictionary<int, double>();
                        foreach (var item in grapth[i])
                        {
                            var candidate = item.Key;
                            var numWeight = digits.GetNumWeight(candidate);
                            var prob = digits[i].GetProbability(candidate) * numWeight;

                            firstNumberCandidates[candidate] = prob;
                        }
                        var numbersToTake = 25;
                        var numbersToSkip = (firstNumberCandidates.Count() - numbersToTake - 1) / 2;
                        var unprocessedNums = firstNumberCandidates.OrderBy((x) => x.Value).Reverse().Select((x) => x.Key);

                        var finalCandidates = unprocessedNums.Skip(numbersToSkip).Take(numbersToTake);
                        // take 5 numbers from the middle

                        Console.Write($"Possible candidates [i] ");
                        foreach (var item in finalCandidates)
                        {
                            Console.Write(item + " ");
                        }
                        Console.WriteLine();
                    }

                    var sortedOverall = overallProb.OrderBy((x) => x.Value).Reverse();
                    // remove top 10 %
                    int toRemove = 0;
                    int toSkipCount = sortedOverall.Count() * toRemove / 100;
                    var finalSortedOverall = sortedOverall.Skip(toSkipCount).Select((x) => x.Key);
                    var sortedGraph = new List<Dictionary<int, Dictionary<int, int>>>();

                    foreach (var item in grapth)
                    {
                        var index = grapth.IndexOf(item);

                        sortedGraph.Add(new Dictionary<int, Dictionary<int, int>>());

                        for (var i = 0; i < item.Count; i++)
                        {
                            var subItem = item.ElementAt(i);
                            var ordered = subItem.Value.OrderBy(x => x.Value).Reverse();
                            var newDict = new Dictionary<int, int>();

                            foreach (var orderedItem in ordered)
                            {
                                newDict[orderedItem.Key] = orderedItem.Value; 
                            }
                            sortedGraph[index][subItem.Key] = newDict;
                        }
                    }
                    grapth = sortedGraph;
                    sortedGraph = null;

                    int item0 = win_ticket[0];
                    int totalCombinations = 0;

                    var percentage = 100; // 1% - 100%

                    Func<Dictionary<int,int>, IEnumerable<int>> returnWithPercentage = (dictr) => {
                        var newList = new List<int>();
                        var totalCount = dictr.Select((x) => x.Value).Sum();
                        int currentCount = 0;
                        foreach (var item in dictr)
                        {
                            //if (currentCount < (totalCount * percentage / 100))
                            {
                                newList.Add(item.Key);
                            }
                            currentCount += item.Value;
                        }

                        return newList;
                    };

                    int won = 0;
                    int bonusticket = 0;

                    var probability = new Dictionary<string, int>();
                    Func<int[], int> calculateProbability = (int[] ticket) =>
                    {
                        int resultWeight = 0;

                        for(var i = 1; i < grapth.Count; i++)
                        {
                            if (i >= ticket.Count())
                                continue;
                            var prevNum = ticket[i - 1];
                            var currNum = ticket[i];
                            var currGraph = grapth[i - 1];
                            if (!currGraph.ContainsKey(prevNum))
                            {
                                continue;
                            }
                            var toLook = currGraph[prevNum];
                            resultWeight += toLook.Where((x) => x.Key == currNum).Select((x) => x.Value).FirstOrDefault();
                        }
                        return resultWeight;
                    };

                    foreach (var item1 in returnWithPercentage(grapth[0][item0]))
                    {
                        foreach (var item2 in returnWithPercentage(grapth[1][item1]))
                        {
                            foreach(var item3 in returnWithPercentage(grapth[2][item2]))
                            {
                                foreach (var item4 in returnWithPercentage(grapth[3][item3]))
                                {
                                    foreach (var item5 in returnWithPercentage(grapth[4][item4]))
                                    {
                                        int[] ticket = new int[] { item0, item1, item2, item3, item4, item5 };

                                      //  if (ticket[0] != win_ticket[0] || ticket[1] != win_ticket[1] || ticket[2] != win_ticket[2] || ticket[3] != win_ticket[3]
                                      //      /*|| ticket[4] != win_ticket[4] || ticket[5] != win_ticket[5]*/)
                                      //      continue;

                                        bool isGoodNumber = true;
                                        foreach (var t in ticket)
                                            if (finalSortedOverall.ToList().IndexOf(t) == -1)
                                                isGoodNumber = false;

                                        if (isGoodNumber == false)
                                            continue;

                                        var listTicket = new List<int>(ticket);
                                        probability[PatternToString(listTicket)] = calculateProbability(ticket);

                                    }
                                }
                             
                            }
                        }
                    }

                    var min = probability.Select((x) => x.Value).Min();
                    var max = probability.Select((x) => x.Value).Max();

                    var lowestRange = 0.55 * (max - min) + min;
                    var highestRange = 0.75 * (max - min) + min;

                    var reduced = probability.Where((x) => x.Value >= lowestRange && x.Value <= highestRange).Select((x) => x.Key);

                    var listOfProbs = new List<double>();

                    foreach(var item in reduced)
                    {
                        var ticket = StringToPattern(item);
                        int oddCount = 0;
                        int evenCount = 0;
                        var probs = new List<double>();

                        for (var i = 0; i < ticket.Count; i++)
                        {
                            var ticketNum = ticket[i];

                            var numWeight = digits.GetNumWeight(ticketNum);
                            var prob = digits[i].GetProbability(ticketNum) * numWeight;

                            probs.Add(prob);

                            if (ticketNum % 2 == 0)
                                oddCount++;
                            else
                                evenCount++;
                        }

                        double totalProb = 1.0 - (probs[0] / 100.0);
                        for(var i = 1; i < probs.Count; i++)
                        {
                            totalProb *= 1.0 - (probs[i] / 100.0);
                        }
                        totalProb = 1.0 - totalProb;

                        listOfProbs.Add(totalProb);

                        if (evenCount != oddCount && 2 * evenCount != oddCount && 2 * oddCount != evenCount)
                            continue;

                        bool islast = false;
                        int powerball = 2;

                        double lowestBound = 0.05607;
                        double highestBound = 0.05607 * 1.5;


                        if (!(totalProb >= lowestBound && totalProb <= highestBound)) // to good to be true
                            continue;

                        totalCombinations += 1;

                        if (ticket[0] != win_ticket[0] || ticket[1] != win_ticket[1] || ticket[2] != win_ticket[2] || ticket[3] != win_ticket[3] 
                            || ticket[4] != win_ticket[4] || ticket[5] != win_ticket[5])
                           continue;

                        var minProb = listOfProbs.Min();
                        var maxProb = listOfProbs.Max();
                        var averProb = listOfProbs.Average();

                        var result = isWinningNumbers(new List<int>(ticket), new List<int>(win_ticket), out islast);
                        //Console.Write(item);
                        //Console.WriteLine(" " + result + " " + totalProb * 100);

                        if (win_ticket.Count > 6 && powerball == win_ticket[7])
                        {
                            if (result == 6 && !islast)
                                won += 4000000;
                            if (result == 6 && islast)
                                won += 32000;
                            if (result == 5 && !islast)
                                won += 1240;
                            if (result == 5 && islast)
                                won += 101;
                            if (result == 4 && !islast)
                                won += 57;
                            if (result == 4 && islast)
                                won += 40;
                            if (result == 3 && !islast)
                            {
                                won += 15;
                                bonusticket++;
                            }
                        }

                        if (result == 3 && !islast)
                            ++bonusticket;
                        if (result == 4 & islast)
                            won += 23;
                        if (result == 4 && !islast)
                            won += 30;
                        if (result == 5 && islast)
                            won += 57;
                        if (result == 5 && !islast)
                            won += 595;
                        if (result == 6 && islast)
                            won += 22419;
                        if (result == 6 && !islast)
                            won += 166667;
                }


                Console.WriteLine($"first number is {item0} and total number of combinations for it {totalCombinations/8} bonus tickets {bonusticket} won {won}");


                }
                file.Close();
            }
            Console.ReadLine();
        }
    }
}
