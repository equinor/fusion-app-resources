import * as React from 'react';
import { TextInput } from '@equinor/fusion-components';

export type TableTextInputProps<T> = {
    item: T;
    accessor: (item: T) => string;
    onChange: (key: any, accessKey: keyof T, value: any) => void;
    accessKey: keyof T;
    rowIdentifier: keyof T;
    disabled: boolean;
    columnLabel: string;
};

function TableTextInput<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    disabled,
    columnLabel,
}: TableTextInputProps<T>) {
    const onInputChange = React.useCallback(
        (newValue: string) => {
            onChange(item[rowIdentifier], accessKey, newValue);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return (
        <TextInput
            value={accessor(item)}
            onChange={onInputChange}
            disabled={disabled}
            placeholder={accessor(item) || columnLabel}
            label={columnLabel}
        />
    );
}

export default TableTextInput;
