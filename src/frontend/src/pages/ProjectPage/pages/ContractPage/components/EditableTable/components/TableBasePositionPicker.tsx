import * as React from 'react';
import BasePositionPicker from '../../../../../components/EditContractWizard/components/BasePositionPicker';
import { BasePosition } from '@equinor/fusion';
import { DefaultTableType } from './TableTypes';

function TableBasePosition<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
}: DefaultTableType<T, BasePosition | null>) {
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
