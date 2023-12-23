"use client"
import React, {useState} from "react";
import {
    Modal,
    ModalContent,
    ModalHeader,
    ModalBody,
    ModalFooter,
    Button,
    useDisclosure,
    Input
} from "@nextui-org/react";
import {PlusFilledIcon, ShoppingCartBoldIcon} from "@nextui-org/shared-icons";
import {useRouter} from "next/navigation";

export function AddListItem(props: {api: string}) {
    const router = useRouter()
    const {isOpen, onOpen, onOpenChange} = useDisclosure();
    const [item, setItem] = useState('');
    const onSubmit = async () => {
        onOpenChange();
        await fetch(`${props.api}/ShoppingList/ListItem?item=${item}`, {
            method: 'POST',
            headers: {
                'Accept': 'application/json'
            }
        })
            .then(response => response.json())
            .then(data => console.log(data))
            .catch(error => console.error('Error:', error));
        setItem("")
        router.refresh();
    }

    return (
        <>
            <Button onPress={onOpen} color="primary">
                <PlusFilledIcon className="text-2xl text-default-400 pointer-events-none flex-shrink-0"/>
            </Button>
            <Modal
                isOpen={isOpen}
                onOpenChange={onOpenChange}
                placement="top-center"
            >
                <ModalContent>
                    {(onClose) => (
                        <>
                            <ModalHeader className="flex flex-col gap-1">Add Item</ModalHeader>
                            <ModalBody>
                                <Input
                                    autoFocus
                                    endContent={
                                        <ShoppingCartBoldIcon
                                            className="text-2xl text-default-400 pointer-events-none flex-shrink-0"/>
                                    }
                                    value={item}
                                    label="Item"
                                    placeholder="Add your Item"
                                    variant="bordered"
                                    onChange={e => setItem(e.target.value)}
                                />
                            </ModalBody>
                            <ModalFooter>
                                <Button color="danger" variant="flat" onPress={onClose}>
                                    Close
                                </Button>
                                <Button color="primary" onPress={onSubmit}>
                                    Add
                                </Button>
                            </ModalFooter>
                        </>
                    )}
                </ModalContent>
            </Modal>
        </>
    );
}