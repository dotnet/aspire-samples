import {AddListItem} from "@/app/AddListItem";
import {List} from "@/app/List";

export default async function Home() {
    const api = process.env['services__listly.service.api__1'];
    return (
        <main className="flex min-h-screen flex-col items-center space-y-5 p-24">
            <div className="text-6xl">Listly</div>
            <List/>
            <div className="flex flex-row space-x-2">
                {api !== undefined && <AddListItem api={api}/>}
            </div>
        </main>
    );
}
