import * as React from 'react';
import { TextInput, EditIcon, ModalSideSheet, TextArea } from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';
import * as styles from '../styles.less';

type TextEditSideSheetProps = {
    isOpen: boolean;
    setIsOpen: (isOpen: boolean) => void;
    value: string;
    onChange: (value: string) => void;
};

const TextEditSideSheet: React.FC<TextEditSideSheetProps> = ({
    isOpen,
    setIsOpen,
    value,
    onChange,
}) => {
    return (
        <ModalSideSheet
            show={isOpen}
            onClose={() => setIsOpen(false)}
            id="text-edit-side-sheet"
            size="medium"
            header="Request description"
        >
            <div className={styles.textAreaContainer}>
                <TextArea
                    value={value}
                    onChange={onChange}
                    helperText={'Add a request description - Saved on close'}
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
    const [openSideSheet, setOpenSideSheet] = React.useState<boolean>(false);
    const onInputChange = React.useCallback(
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
                label={columnLabel}
            />
            <TextEditSideSheet
                isOpen={openSideSheet}
                setIsOpen={setOpenSideSheet}
                onChange={onInputChange}
                value={accessor(item)}
            />
        </>
    );
}

export default TableTextArea;
