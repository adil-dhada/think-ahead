using UI.Models;

namespace UI.Data;

public static class ProductContext
{
    private static readonly List<Product> _products =
    [
        new(1, "Product 1", "Description for Product 1"),
        new(2, "Product 2", "Description for Product 2"),
        new(3, "Product 3", "Description for Product 3"),
    ];

    public static List<Product> Products => _products;

    public static void Add(string name, string description)
    {
        var id = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
        _products.Add(new(id, name, description));
    }
}
