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
    const currentOrgProject = currentContext as any;
    const { contract } = useContractContext();
    const onPositionChange = React.useCallback(
        (basePosition: Position) => {
            onChange(item[rowIdentifier], accessKey, basePosition);
        },
        [onChange, item, accessKey, rowIdentifier]
    );

    return (
        <PositionPicker
            label={columnLabel}
            selectedPosition={accessor(item)}
            projectId={currentOrgProject.externalId}
            contractId={contract?.id || undefined}
            onSelect={onPositionChange}
        />
    );
}

export default TablePositionPicker;
