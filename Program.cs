using System;
using System.Collections.Generic;
using System.Linq;

// モデルクラス
public class PurchaseItem
{
    public int Id { get; set; }
    public string Category { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
}

public class BundleDiscount
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Func<List<PurchaseItem>, bool> Condition { get; set; }
    public Func<List<PurchaseItem>, decimal> CalculatePrice { get; set; }
}

public class BundleDiscountCalculator
{
    private List<PurchaseItem> _items;
    private List<BundleDiscount> _discounts;

    public BundleDiscountCalculator(List<PurchaseItem> items, List<BundleDiscount> discounts)
    {
        _items = items;
        _discounts = discounts;
    }

    public (decimal MinPrice, List<List<int>> OptimalCombination) FindMinimumPrice()
    {
        decimal minPrice = decimal.MaxValue;
        List<List<int>> optimalCombination = null;

        var allIndices = Enumerable.Range(0, _items.Count).ToList();
        var allCombinations = GenerateAllCombinations(allIndices, _discounts);

        foreach (var combination in allCombinations)
        {
            var price = CalculateTotalPrice(combination);
            if (price < minPrice)
            {
                minPrice = price;
                optimalCombination = combination;
            }
        }

        return (minPrice, optimalCombination);
    }

    private List<List<List<int>>> GenerateAllCombinations(List<int> indices, List<BundleDiscount> discounts)
    {
        var result = new List<List<List<int>>>();

        void Backtrack(List<List<int>> currentCombination, List<int> remainingIndices)
        {
            result.Add(new List<List<int>>(currentCombination));

            foreach (var discount in discounts)
            {
                var applicableItems = remainingIndices
                    .Where(i => discount.Condition(new List<PurchaseItem> { _items[i] }))
                    .ToList();

                if (applicableItems.Count == 0) continue;

                var itemCombinations = GenerateItemCombinations(applicableItems, discount);

                foreach (var itemSet in itemCombinations)
                {
                    if (discount.Condition(itemSet.Select(i => _items[i]).ToList()))
                    {
                        var newCombination = new List<List<int>>(currentCombination) { itemSet };
                        var newRemaining = remainingIndices.Except(itemSet).ToList();
                        Backtrack(newCombination, newRemaining);
                    }
                }
            }
        }

        Backtrack(new List<List<int>>(), indices);
        return result;
    }

    private List<List<int>> GenerateItemCombinations(List<int> indices, BundleDiscount discount)
    {
        var combinations = new List<List<int>>();

        for (int size = 2; size <= indices.Count; size++)
        {
            var combos = GetCombinations(indices, size);
            foreach (var combo in combos)
            {
                var items = combo.Select(i => _items[i]).ToList();
                if (discount.Condition(items))
                {
                    combinations.Add(combo);
                }
            }
        }

        return combinations;
    }

    private List<List<int>> GetCombinations(List<int> items, int size)
    {
        if (size == 0) return new List<List<int>> { new List<int>() };
        if (items.Count == 0) return new List<List<int>>();

        var result = new List<List<int>>();
        var head = items[0];
        var tail = items.Skip(1).ToList();

        foreach (var combination in GetCombinations(tail, size - 1))
        {
            combination.Insert(0, head);
            result.Add(combination);
        }

        foreach (var combination in GetCombinations(tail, size))
        {
            result.Add(combination);
        }

        return result;
    }

    private decimal CalculateTotalPrice(List<List<int>> combination)
    {
        var usedItems = new HashSet<int>();
        decimal totalPrice = 0;

        for (int discountIdx = 0; discountIdx < combination.Count; discountIdx++)
        {
            var itemIndices = combination[discountIdx];
            if (itemIndices.Count == 0) continue;

            var items = itemIndices.Select(i => _items[i]).ToList();
            var discount = _discounts[discountIdx];

            totalPrice += discount.CalculatePrice(items);
            foreach (var idx in itemIndices)
            {
                usedItems.Add(idx);
            }
        }

        for (int i = 0; i < _items.Count; i++)
        {
            if (!usedItems.Contains(i))
            {
                totalPrice += _items[i].Price;
            }
        }

        return totalPrice;
    }
}

public class Program
{
    public static void Main()
    {
        var items = new List<PurchaseItem>
        {
            new PurchaseItem { Id = 1, Category = "ゲーム機本体", ProductName = "SWITCH", Price = 40000 },
            new PurchaseItem { Id = 2, Category = "ゲーム機本体", ProductName = "SWITCH2", Price = 42000 },
            new PurchaseItem { Id = 3, Category = "ゲーム機ソフト", ProductName = "スーパーマリオ", Price = 5000 },
            new PurchaseItem { Id = 4, Category = "ゲーム機ソフト", ProductName = "マリオカート", Price = 6000 },
            new PurchaseItem { Id = 5, Category = "ゲームソフト", ProductName = "ドンキーコング", Price = 3000 },
            new PurchaseItem { Id = 6, Category = "オンライン用カード", ProductName = "nintendo online カード", Price = 1000 },
            new PurchaseItem { Id = 7, Category = "オンライン用カード", ProductName = "nintendo online カード", Price = 1000 }
        };

        var discounts = new List<BundleDiscount>
        {
            new BundleDiscount
            {
                Id = 1,
                Name = "ゲーム機本体 + マリオシリーズ",
                Condition = (itemSet) =>
                {
                    var hasConsole = itemSet.Any(i => i.Category == "ゲーム機本体");
                    var hasMario = itemSet.Any(i => (i.ProductName == "スーパーマリオ" || i.ProductName == "マリオカート"));
                    return hasConsole && hasMario && itemSet.Count == 2;
                },
                CalculatePrice = (itemSet) => 37000m
            },
            new BundleDiscount
            {
                Id = 2,
                Name = "ゲーム機ソフト + オンライン用カード",
                Condition = (itemSet) =>
                {
                    var hasSoftware = itemSet.Any(i => i.Category == "ゲーム機ソフト");
                    var hasCard = itemSet.Any(i => i.Category == "オンライン用カード");
                    return hasSoftware && hasCard;
                },
                CalculatePrice = (itemSet) =>
                {
                    var softwarePrice = itemSet
                        .Where(i => i.Category == "ゲーム機ソフト")
                        .Sum(i => i.Price - 550);
                    var cardPrice = itemSet
                        .Where(i => i.Category == "オンライン用カード")
                        .Sum(i => i.Price);
                    return softwarePrice + cardPrice;
                }
            }
        };

        var calculator = new BundleDiscountCalculator(items, discounts);
        var (minPrice, combination) = calculator.FindMinimumPrice();

        Console.WriteLine($"最安価格：{minPrice:C}");
        Console.WriteLine($"割引組み合わせ：{combination.Count}個");
    }
}
