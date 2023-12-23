import {CheckListItem} from "@/app/CheckListItem";

export interface ListItemProps {
  id: string;
  content: string;
  added: Date;
  bought: boolean;
}

export function ListItem(props: ListItemProps) {
  const api = process.env['services__listly.service.api__1'];

  return (
      api !== undefined &&
      <div className="container mx-auto mt-10">
        <div className="max-w-sm rounded overflow-hidden shadow-lg bg-white flex items-center">
          <CheckListItem id={props.id} api={api}/>
          <div className="px-6 py-4">
            <div className="font-bold text-xl mb-2">{props.content}</div>
            <p className="text-gray-700 text-base">
              Added: {props.added.toLocaleString("en-US").split("T")[0]}
            </p>
          </div>
        </div>
      </div>
  );
}
