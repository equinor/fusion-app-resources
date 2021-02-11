
import { TextInput, EditIcon, ModalSideSheet, TextArea } from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';
import * as styles from '../styles.less';
import { FC, useState, useCallback } from 'react';

type TextEditSideSheetProps = {
    isOpen: boolean;
    setIsOpen: (isOpen: boolean) => void;
    value: string;
    onChange: (value: string) => void;
    label: string;
};

const TextEditSideSheet: FC<TextEditSideSheetProps> = ({
    isOpen,
    setIsOpen,
    value,
    onChange,
    label,
}) => {
    return (
        <ModalSideSheet
            show={isOpen}
            onClose={() => setIsOpen(false)}
            id="text-edit-side-sheet"
            size="medium"
            header={label}
        >
            <div className={styles.textAreaContainer}>
                <TextArea
                    value={value}
                    onChange={onChange}
                    helperText={`${label} - Saves on close`}
                />
            </div>
        </ModalSideSheet>
    );
};

function TableTextArea<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: DefaultTableType<T, string>) {
    const [openSideSheet, setOpenSideSheet] = useState<boolean>(false);
    const onInputChange = useCallback(
        (newValue: string) => {
            onChange(item[rowIdentifier], accessKey, newValue);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return (
        <>
            <TextInput
                value={accessor(item)}
                onChange={onInputChange}
                icon={
                    <div className={styles.editButton} onClick={() => setOpenSideSheet(true)}>
                        <EditIcon />
                    </div>
                }
                placeholder={columnLabel}
            />
            <TextEditSideSheet
                isOpen={openSideSheet}
                setIsOpen={setOpenSideSheet}
                onChange={onInputChange}
                value={accessor(item)}
                label={columnLabel || 'Edit'}
            />
        </>
    );
}

export default TableTextArea;
