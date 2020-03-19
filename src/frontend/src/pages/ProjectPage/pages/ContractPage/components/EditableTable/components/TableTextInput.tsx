import * as React from 'react';
import { TextInput } from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';

function TableTextInput<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: DefaultTableType<T, string>) {
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
           
            placeholder={columnLabel}
        />
    );
}

export default TableTextInput;
