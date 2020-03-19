import * as React from 'react';
import { Position, useCurrentContext } from '@equinor/fusion';

import { PositionPicker } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../../contractContex';
import { DefaultTableType } from './TableTypes';

function TablePositionPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: DefaultTableType<T, Position | null>) {
    const currentContext = useCurrentContext();
    const { contract } = useContractContext();
    const onPositionChange = React.useCallback(
        (basePosition: Position) => {
            onChange(item[rowIdentifier], accessKey, basePosition);
        },
        [onChange, item, accessKey, rowIdentifier]
    );

    if(!currentContext?.externalId) {
        return null;
    }

    return (
        <PositionPicker
            selectedPosition={accessor(item)}
            projectId={currentContext?.externalId}
            contractId={contract?.id || undefined}
            onSelect={onPositionChange}
        />
    );
}

export default TablePositionPicker;
