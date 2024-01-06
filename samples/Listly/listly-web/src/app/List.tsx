import {ListItem, ListItemProps} from "@/app/ListItem";

export async function List() {
    const api = process.env['services__listly.service.api__1'];
    const data = await fetch(`${api}/ShoppingList/ListItems`, {cache: "no-cache"});
    const result: ListItemProps[] = await data.json();
    const listContent = result.map((item) => (
        <ListItem key={item.id} {...item} />
    ));

    return (
        api !== undefined &&
        <div className="text-2xl">{listContent}</div>
    )
}