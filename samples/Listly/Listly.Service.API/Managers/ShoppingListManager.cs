using Listly.Database;
using Listly.Service.API.Abstractions.Managers;

namespace Listly.Service.API.Managers;

public class ShoppingListManager : IShoppingListManager
{
    private readonly ListlyDbContext _context;

    public ShoppingListManager(ListlyDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ListItem>> GetListItems()
    {
        return await this._context.GetListItemsCompiledAsync();
    }

    public Task AddListItem(ListItem item)
    {
        return this._context.AddListItem(item);
    }

    public Task BuyListItem(Guid id)
    {
        return this._context.SetBoughtTrue(id);
    }
}