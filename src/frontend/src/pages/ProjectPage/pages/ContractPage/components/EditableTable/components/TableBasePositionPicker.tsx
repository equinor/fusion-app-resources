import * as React from 'react';
import BasePositionPicker from '../../../../../components/EditContractWizard/components/BasePositionPicker';
import { BasePosition } from '@equinor/fusion';

export type TableBasePositionProps<T> = {
    item: T;
    accessor: (item: T) => BasePosition | null;
    onChange: (key: any, accessKey: keyof T, value: any) => void;
    accessKey: keyof T;
    rowIdentifier: keyof T;
};

function TableBasePosition<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
}: TableBasePositionProps<T>) {
    const onBasePositionChange = React.useCallback(
        (basePosition: BasePosition) => {
            onChange(item[rowIdentifier], accessKey, basePosition);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    const basePositionId = accessor(item)?.id;
    return (
        <BasePositionPicker
            selectedBasePositionId={basePositionId}
            onSelect={onBasePositionChange}
        />
    );
}

export default TableBasePosition;
