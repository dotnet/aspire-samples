using Listly.Database;

namespace Listly.Service.API.Abstractions.Managers;

public interface IShoppingListManager
{
    Task<List<ListItem>> GetListItems();

    Task AddListItem(ListItem item);

    Task BuyListItem(Guid id);
}