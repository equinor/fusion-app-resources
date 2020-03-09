import * as React from 'react';
import { Position } from '@equinor/fusion';

import ContractPositionPicker from '../../../../../components/EditContractWizard/components/ContractPositionPicker';

export type TablePositionPickerProps<T> = {
    item: T;
    accessor: (item: T) => Position | null;
    onChange: (key: any, accessKey: keyof T, value: any) => void;
    accessKey: keyof T;
    rowIdentifier: keyof T;
    columnLabel: string;
};

function TablePositionPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: TablePositionPickerProps<T>) {
    const onPositionChange = React.useCallback(
        (basePosition: Position) => {
            onChange(item[rowIdentifier], accessKey, basePosition);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return (
        <ContractPositionPicker
            label={columnLabel}
            onSelect={onPositionChange}
            selectedPosition={accessor(item)}
        />
    );
}

export default TablePositionPicker;
